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

// Track last file position for incremental reading
var lastFilePosition = 0L;

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
                var fileInfo = new FileInfo(REQUEST_FILE);
                if (fileInfo.Length > lastFilePosition)
                {
                    using var reader = new StreamReader(REQUEST_FILE);
                    reader.BaseStream.Seek(lastFilePosition, SeekOrigin.Begin);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Skip comments and empty lines
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                            continue;

                        // Parse "pickup destination" format
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0], out int pickup) &&
                            int.TryParse(parts[1], out int destination))
                        {
                            try
                            {
                                var request = new Request(pickup, destination, MIN_FLOOR, MAX_FLOOR);
                                system.AddRequest(request);
                                Console.WriteLine($"[FILE] Request added from file: {pickup} → {destination}");
                            }
                            catch (ArgumentException ex)
                            {
                                Console.WriteLine($"[FILE] Invalid request in file: {line} - {ex.Message}");
                            }
                        }
                    }

                    lastFilePosition = fileInfo.Length;
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
