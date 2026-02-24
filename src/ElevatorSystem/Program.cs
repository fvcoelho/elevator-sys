using ElevatorSystem;

// Configuration Constants
const int ELEVATOR_COUNT = 3;        // Configurable: 3-5 elevators
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;            // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;       // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;    // 1.5 seconds per floor
const string REQUESTS_DIR = "requests";   // Directory for pending requests
const string PROCESSED_DIR = "processed"; // Directory for processed requests

// Create multi-elevator system
var system = new ElevatorSystem.ElevatorSystem(
    elevatorCount: ELEVATOR_COUNT,
    minFloor: MIN_FLOOR,
    maxFloor: MAX_FLOOR,
    doorOpenMs: DOOR_OPEN_MS,
    floorTravelMs: FLOOR_TRAVEL_MS);

// Create directories if they don't exist
if (!Directory.Exists(REQUESTS_DIR))
{
    Directory.CreateDirectory(REQUESTS_DIR);
}

if (!Directory.Exists(PROCESSED_DIR))
{
    Directory.CreateDirectory(PROCESSED_DIR);
}

// Track processed filenames to avoid reprocessing
var processedFiles = new HashSet<string>();

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
            // Get all .txt files in requests directory
            var files = Directory.GetFiles(REQUESTS_DIR, "*.txt");

            foreach (var filepath in files)
            {
                var filename = Path.GetFileName(filepath);

                // Skip if already processed
                if (processedFiles.Contains(filename))
                    continue;

                try
                {
                    // Parse filename:
                    // Format: "20260223_214530_123_from_5_to_15.txt" (7 parts with milliseconds)
                    // Or: "20260223_214530_123_from_5_to_15_H.txt" (8 parts with priority)
                    var nameWithoutExt = filename.Replace(".txt", "");
                    var parts = nameWithoutExt.Split('_');

                    // Determine format and extract pickup/destination/priority
                    int pickup = 0, destination = 0;
                    RequestPriority priority = RequestPriority.Normal;
                    bool validFormat = false;

                    if (parts.Length == 7 && parts[3] == "from" && parts[5] == "to")
                    {
                        // Format with milliseconds (no priority)
                        validFormat = int.TryParse(parts[4], out pickup) &&
                                     int.TryParse(parts[6], out destination);
                    }
                    else if (parts.Length == 8 && parts[3] == "from" && parts[5] == "to")
                    {
                        // Format with milliseconds and priority
                        validFormat = int.TryParse(parts[4], out pickup) &&
                                     int.TryParse(parts[6], out destination);

                        if (validFormat)
                        {
                            // Parse priority from last part
                            priority = parts[7].ToUpper() switch
                            {
                                "H" or "HIGH" => RequestPriority.High,
                                "N" or "NORMAL" => RequestPriority.Normal,
                                _ => RequestPriority.Normal
                            };
                        }
                    }

                    if (validFormat)
                    {
                        // Create request
                        var request = new Request(pickup, destination, priority, MIN_FLOOR, MAX_FLOOR);
                        system.AddRequest(request);

                        // Mark as processed
                        processedFiles.Add(filename);

                        // Move file to processed/ directory
                        var processedPath = Path.Combine(PROCESSED_DIR, filename);
                        File.Move(filepath, processedPath);

                        Console.WriteLine($"[FILE] Processed and archived: {filename} (Request #{request.RequestId})");
                    }
                    else
                    {
                        Console.WriteLine($"[FILE] Invalid filename format: {filename}");
                        processedFiles.Add(filename); // Skip this file in future iterations
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"[FILE] Invalid request in {filename}: {ex.Message}");
                    processedFiles.Add(filename); // Skip this file in future iterations
                }
                catch (IOException ex)
                {
                    // File might be locked, retry next iteration
                    Console.WriteLine($"[FILE] Could not access {filename}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FILE] Error monitoring directory: {ex.Message}");
        }

        await Task.Delay(500, cts.Token); // Check every 500ms
    }
}, cts.Token);

// Main console interface loop
Console.WriteLine($"=== ELEVATOR SYSTEM ({ELEVATOR_COUNT} elevators, floors {MIN_FLOOR}-{MAX_FLOOR}) ===\n");
Console.WriteLine("Press [R] to REQUEST a ride | Press [S] to view STATUS | Press [Q] to QUIT");
Console.WriteLine($"\nMonitoring directory: {Path.GetFullPath(REQUESTS_DIR)}");
Console.WriteLine($"Archiving to: {Path.GetFullPath(PROCESSED_DIR)}\n");

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

            // Get priority
            Console.Write("Priority [N]ormal / [H]igh (default: Normal): ");
            var priorityInput = Console.ReadLine()?.Trim().ToUpper();

            RequestPriority priority = priorityInput switch
            {
                "H" => RequestPriority.High,
                _ => RequestPriority.Normal
            };

            // Create and add request
            try
            {
                var request = new Request(pickupFloor, destinationFloor, priority, MIN_FLOOR, MAX_FLOOR);
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
