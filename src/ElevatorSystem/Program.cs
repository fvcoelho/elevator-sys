using ElevatorSystem;

// Configuration Constants
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;            // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;       // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;    // 1.5 seconds per floor
const string REQUESTS_DIR = "requests";   // Directory for pending requests
const string PROCESSED_DIR = "processed"; // Directory for processed requests

// ── System Profile ──
// Change this single value to switch configurations:
//   Standard  → [Local, Local, Local]             3 elevators, all floors
//   Mixed     → [Local, Local, Express]           2 local + 1 express (lobby + floors 15-20)
//   Full      → [Local, Local, Express, Freight]  2 local + 1 express + 1 freight (capacity 20)
const string PROFILE = "Mixed";

ElevatorConfig[] configs = PROFILE switch
{
    "Mixed" => new[]
    {
        new ElevatorConfig { Label = "A", InitialFloor = 1,  Type = ElevatorType.Local,   Capacity = 10 },
        new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local,   Capacity = 10 },
        new ElevatorConfig { Label = "C", InitialFloor = 20, Type = ElevatorType.Express,
            ServedFloors = new HashSet<int> { 1 }.Union(Enumerable.Range(15, 6)).ToHashSet(), Capacity = 12 },
    },
    "Full" => new[]
    {
        new ElevatorConfig { Label = "A",  InitialFloor = 1,  Type = ElevatorType.Local,   Capacity = 10 },
        new ElevatorConfig { Label = "B",  InitialFloor = 10, Type = ElevatorType.Local,   Capacity = 10 },
        new ElevatorConfig { Label = "C",  InitialFloor = 20, Type = ElevatorType.Express,
            ServedFloors = new HashSet<int> { 1 }.Union(Enumerable.Range(15, 6)).ToHashSet(), Capacity = 12 },
        new ElevatorConfig { Label = "F1", InitialFloor = 1,  Type = ElevatorType.Freight, Capacity = 20 },
    },
    _ => new[] // "Standard"
    {
        new ElevatorConfig { Label = "A", InitialFloor = 1,  Type = ElevatorType.Local, Capacity = 10 },
        new ElevatorConfig { Label = "B", InitialFloor = 10, Type = ElevatorType.Local, Capacity = 10 },
        new ElevatorConfig { Label = "C", InitialFloor = 20, Type = ElevatorType.Local, Capacity = 10 },
    },
};

// Create multi-elevator system
var system = new ElevatorSystem.ElevatorSystem(
    minFloor: MIN_FLOOR,
    maxFloor: MAX_FLOOR,
    doorOpenMs: DOOR_OPEN_MS,
    floorTravelMs: FLOOR_TRAVEL_MS,
    doorTransitionMs: 1000,
    elevatorConfigs: configs);

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
                        var request = new Request(pickup, destination, priority, minFloor: MIN_FLOOR, maxFloor: MAX_FLOOR);
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
Console.WriteLine($"=== ELEVATOR SYSTEM ({PROFILE}: {configs.Length} elevators, floors {MIN_FLOOR}-{MAX_FLOOR}) ===\n");
Console.WriteLine("Press [R] REQUEST | [S] STATUS | [A] ANALYTICS | [D] DISPATCH | [M] MAINTENANCE | [Q] QUIT");
Console.WriteLine($"\nMonitoring directory: {Path.GetFullPath(REQUESTS_DIR)}");
Console.WriteLine($"Archiving to: {Path.GetFullPath(PROCESSED_DIR)}");
Console.WriteLine($"Current Algorithm: {system.Algorithm}\n");

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
                var request = new Request(pickupFloor, destinationFloor, priority, minFloor: MIN_FLOOR, maxFloor: MAX_FLOOR);
                system.AddRequest(request);
                Console.WriteLine();
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access Denied: {ex.Message}\n");
            }
            break;

        case 'S':
            // Display status
            Console.WriteLine();
            Console.WriteLine(system.GetSystemStatus());
            break;

        case 'A':
            // Display analytics
            Console.WriteLine();
            DisplayAnalytics(system);
            break;

        case 'D':
            // Change dispatch algorithm
            Console.WriteLine("\n=== SELECT DISPATCH ALGORITHM ===");
            Console.WriteLine("  [1] Simple (closest + idle preference)");
            Console.WriteLine("  [2] SCAN (direction-aware, +100 bonus)");
            Console.WriteLine("  [3] LOOK (balanced, reverses at last request)");
            Console.WriteLine($"\nCurrent: {system.Algorithm}");
            Console.Write("\nSelect algorithm [1-3]: ");

            var algoKey = Console.ReadKey(intercept: true);
            Console.WriteLine(); // New line after key press

            switch (algoKey.KeyChar)
            {
                case '1':
                    system.Algorithm = ElevatorSystem.DispatchAlgorithm.Simple;
                    Console.WriteLine("✓ Switched to Simple algorithm");
                    break;
                case '2':
                    system.Algorithm = ElevatorSystem.DispatchAlgorithm.SCAN;
                    Console.WriteLine("✓ Switched to SCAN algorithm");
                    break;
                case '3':
                    system.Algorithm = ElevatorSystem.DispatchAlgorithm.LOOK;
                    Console.WriteLine("✓ Switched to LOOK algorithm");
                    break;
                default:
                    Console.WriteLine("✗ Invalid selection - keeping current algorithm");
                    break;
            }
            Console.WriteLine($"Active Algorithm: {system.Algorithm}\n");
            break;

        case 'M':
            // Maintenance mode
            Console.WriteLine("\n=== MAINTENANCE MODE ===");
            for (int i = 0; i < system.ElevatorCount; i++)
            {
                var elev = system.GetElevator(i);
                var label = (char)('A' + i);
                var modeLabel = elev.InMaintenance ? "IN MAINTENANCE" : "Active";
                Console.WriteLine($"  [{label}] Elevator {label}: Floor {elev.CurrentFloor} | {modeLabel}");
            }
            Console.Write("\nSelect elevator (A-" + (char)('A' + system.ElevatorCount - 1) + "): ");

            var mKey = Console.ReadKey(intercept: true);
            Console.WriteLine();
            var mIndex = char.ToUpper(mKey.KeyChar) - 'A';

            if (mIndex < 0 || mIndex >= system.ElevatorCount)
            {
                Console.WriteLine("Invalid elevator selection.\n");
                break;
            }

            var selectedElevator = system.GetElevator(mIndex);
            var selectedLabel = (char)('A' + mIndex);

            if (selectedElevator.InMaintenance)
            {
                selectedElevator.ExitMaintenance();
                Console.WriteLine($"Elevator {selectedLabel} exited maintenance mode.\n");
            }
            else
            {
                selectedElevator.EnterMaintenance();
                Console.WriteLine($"Elevator {selectedLabel} entered maintenance mode.\n");
            }
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
            Console.WriteLine($"\nUnknown key '{key.KeyChar}'. Press [R] Request | [S] Status | [A] Analytics | [D] Dispatch | [M] Maintenance | [Q] Quit\n");
            break;
    }

    // Small delay to allow status updates to be visible
    await Task.Delay(100);
}

static void DisplayAnalytics(ElevatorSystem.ElevatorSystem system)
{
    var metrics = system.GetPerformanceMetrics();

    Console.WriteLine("=== SYSTEM ANALYTICS ===\n");

    // Request statistics
    Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
    Console.WriteLine($"Completed: {metrics.CompletedRequests}");
    Console.WriteLine($"In Progress: {metrics.TotalRequests - metrics.CompletedRequests}");
    Console.WriteLine($"Peak Concurrent: {metrics.PeakConcurrentRequests}");
    Console.WriteLine();

    // Timing statistics
    if (metrics.CompletedRequests > 0)
    {
        Console.WriteLine($"Average Wait Time: {metrics.AverageWaitTime.TotalSeconds:F1}s");
        Console.WriteLine($"Average Ride Time: {metrics.AverageRideTime.TotalSeconds:F1}s");
    }
    if (metrics.TotalRequests > 0)
    {
        Console.WriteLine($"Average Dispatch: {metrics.AverageDispatchTime.TotalMilliseconds:F2}ms");
    }
    Console.WriteLine();

    // Elevator performance
    if (metrics.ElevatorStats.Any())
    {
        Console.WriteLine("Elevator Performance:");
        foreach (var kvp in metrics.ElevatorStats.OrderBy(k => k.Key))
        {
            var elev = kvp.Value;
            Console.WriteLine($"  {kvp.Key}: {elev.TripsCompleted} trips | {elev.Utilization:F1}% util | {elev.FloorsTraversed} floors");
        }
        Console.WriteLine();
        Console.WriteLine($"System Utilization: {metrics.SystemUtilization:F1}%");
        Console.WriteLine();
    }

    // Priority breakdown
    if (metrics.RequestsByPriority.Any())
    {
        Console.WriteLine("Requests by Priority:");
        foreach (var kvp in metrics.RequestsByPriority.OrderByDescending(k => k.Key))
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine();
    }

    // VIP statistics
    if (metrics.VIPRequests > 0 || metrics.StandardRequests > 0)
    {
        Console.WriteLine($"VIP Requests: {metrics.VIPRequests}");
        Console.WriteLine($"Standard Requests: {metrics.StandardRequests}");
        Console.WriteLine();
    }

    // Floor heatmap (top 5)
    if (metrics.FloorHeatmap.Any())
    {
        Console.WriteLine("Top Floor Usage:");
        foreach (var kvp in metrics.FloorHeatmap.OrderByDescending(k => k.Value).Take(5))
        {
            Console.WriteLine($"  Floor {kvp.Key}: {kvp.Value} requests");
        }
        Console.WriteLine();
    }
}
