using ElevatorSystem;

// Configuration Constants
const int ELEVATOR_COUNT = 3;        // Configurable: 3-5 elevators
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;            // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;       // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;    // 1.5 seconds per floor
const string REQUEST_FILE = "elevator_requests.txt"; // Request file path

// Create multi-elevator system
var system = new ElevatorSystem.ElevatorSystem(
    elevatorCount: ELEVATOR_COUNT,
    minFloor: MIN_FLOOR,
    maxFloor: MAX_FLOOR,
    doorOpenMs: DOOR_OPEN_MS,
    floorTravelMs: FLOOR_TRAVEL_MS);

// Initialize request file if it doesn't exist
if (!File.Exists(REQUEST_FILE))
{
    File.WriteAllText(REQUEST_FILE, "# Elevator Requests (format: pickup destination)\n");
}

// Track processed lines to avoid reprocessing
var processedLines = new HashSet<string>();

// Track request ID to line content mapping for completion updates
var requestIdToLineContent = new Dictionary<int, string>();

// Lock for file updates
var fileLock = new object();

// Create cancellation token source
using var cts = new CancellationTokenSource();

// Start processing loop in background
var processingTask = Task.Run(async () =>
{
    try
    {
        await system.ProcessRequestsAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when canceling
    }
}, cts.Token);

// Start file monitoring task in background
var fileMonitorTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            if (File.Exists(REQUEST_FILE))
            {
                List<string> allLines;
                lock (fileLock)
                {
                    allLines = File.ReadAllLines(REQUEST_FILE).ToList();
                }

                for (int i = 0; i < allLines.Count; i++)
                {
                    var line = allLines[i];

                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    // Check if already processed
                    if (processedLines.Contains(line))
                        continue;

                    // Extract request data (without any existing status)
                    var requestPart = line.Split('#')[0].Trim();
                    var parts = requestPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 2 &&
                        int.TryParse(parts[0], out int pickup) &&
                        int.TryParse(parts[1], out int destination))
                    {
                        // Check if line already has a status
                        bool hasStatus = line.Contains("# waiting") || line.Contains("# doing") || line.Contains("# done") || line.Contains("# error");

                        if (!hasStatus)
                        {
                            // Mark as waiting
                            allLines[i] = $"{requestPart} # waiting";
                            processedLines.Add(line);

                            lock (fileLock)
                            {
                                File.WriteAllLines(REQUEST_FILE, allLines);
                            }

                            Console.WriteLine($"[FILE] Status: waiting - {pickup} → {destination}");
                            await Task.Delay(100); // Small delay before processing
                        }
                        else if (line.Contains("# waiting"))
                        {
                            // Process waiting request
                            try
                            {
                                var request = new Request(pickup, destination, MIN_FLOOR, MAX_FLOOR);
                                system.AddRequest(request);
                                Console.WriteLine($"[FILE] Request added to system: {pickup} → {destination}");

                                // Track this request ID for completion updates
                                requestIdToLineContent[request.RequestId] = requestPart;

                                // Mark as doing (being processed)
                                allLines[i] = $"{requestPart} # doing";
                                processedLines.Add(line);

                                lock (fileLock)
                                {
                                    File.WriteAllLines(REQUEST_FILE, allLines);
                                }

                                Console.WriteLine($"[FILE] Status: doing - {pickup} → {destination} (Request #{request.RequestId})");
                            }
                            catch (ArgumentException ex)
                            {
                                Console.WriteLine($"[FILE] Invalid request: {line} - {ex.Message}");

                                // Mark as error
                                allLines[i] = $"{requestPart} # error: {ex.Message}";
                                processedLines.Add(line);

                                lock (fileLock)
                                {
                                    File.WriteAllLines(REQUEST_FILE, allLines);
                                }
                            }
                        }
                    }
                }

                // Check for completed requests and update file
                var completedIds = system.GetCompletedRequestIds();
                if (completedIds.Any())
                {
                    foreach (var completedId in completedIds)
                    {
                        if (requestIdToLineContent.TryGetValue(completedId, out var lineContent))
                        {
                            // Find and update the line in the file
                            lock (fileLock)
                            {
                                var currentLines = File.ReadAllLines(REQUEST_FILE).ToList();
                                for (int i = 0; i < currentLines.Count; i++)
                                {
                                    if (currentLines[i].StartsWith(lineContent) && currentLines[i].Contains("# doing"))
                                    {
                                        currentLines[i] = $"{lineContent} # done";
                                        File.WriteAllLines(REQUEST_FILE, currentLines);
                                        Console.WriteLine($"[FILE] Status: done - {lineContent} (Request #{completedId})");
                                        break;
                                    }
                                }
                            }

                            // Remove from tracking
                            requestIdToLineContent.Remove(completedId);
                        }
                    }

                    // Clear completed requests from system
                    system.ClearCompletedRequestIds();
                }
            }
        }
        catch (IOException)
        {
            // File is being written to, skip this iteration
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FILE] Error reading file: {ex.Message}");
        }

        await Task.Delay(500, cts.Token); // Check every 500ms
    }
}, cts.Token);

// Main console interface loop
Console.WriteLine($"=== ELEVATOR SYSTEM ({ELEVATOR_COUNT} elevators, floors {MIN_FLOOR}-{MAX_FLOOR}) ===\n");
Console.WriteLine("Press [R] to REQUEST a ride");
Console.WriteLine("Press [S] to view STATUS");
Console.WriteLine("Press [Q] to QUIT");
Console.WriteLine($"\nMonitoring file: {Path.GetFullPath(REQUEST_FILE)}\n");

// Display initial status
Console.WriteLine(system.GetSystemStatus());

while (true)
{
    var key = Console.ReadKey(intercept: true);
    var keyChar = char.ToUpperInvariant(key.KeyChar);

    switch (keyChar)
    {
        case 'R':
            // Request a ride
            Console.WriteLine("\n=== NEW RIDE REQUEST ===");

            // Get pickup floor
            Console.Write($"Pickup floor ({MIN_FLOOR}-{MAX_FLOOR}): ");
            var pickupInput = Console.ReadLine();
            if (!int.TryParse(pickupInput, out int pickupFloor))
            {
                Console.WriteLine("Invalid input. Please enter a number.\n");
                break;
            }

            // Get destination floor
            Console.Write($"Destination floor ({MIN_FLOOR}-{MAX_FLOOR}): ");
            var destInput = Console.ReadLine();
            if (!int.TryParse(destInput, out int destinationFloor))
            {
                Console.WriteLine("Invalid input. Please enter a number.\n");
                break;
            }

            // Create and add request
            try
            {
                var request = new Request(pickupFloor, destinationFloor, MIN_FLOOR, MAX_FLOOR);
                system.AddRequest(request);
                Console.WriteLine();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n");
            }
            break;

        case 'S':
            // Display status
            Console.WriteLine();
            Console.WriteLine(system.GetSystemStatus());
            break;

        case 'Q':
            // Quit
            Console.WriteLine("\n\nShutting down elevator system...");
            cts.Cancel();
            try
            {
                await Task.WhenAll(processingTask, fileMonitorTask);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            Console.WriteLine("Elevator system stopped. Goodbye!");
            return;

        default:
            Console.WriteLine($"\nUnknown key '{key.KeyChar}'. Press [R] Request | [S] Status | [Q] Quit\n");
            break;
    }

    // Small delay to allow status updates to be visible
    await Task.Delay(100);
}
