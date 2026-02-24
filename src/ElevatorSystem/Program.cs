using ElevatorSystem;

// Configuration Constants
const int ELEVATOR_COUNT = 3;        // Configurable: 3-5 elevators
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 20;            // Extended from 10 to 20
const int DOOR_OPEN_MS = 3000;       // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;    // 1.5 seconds per floor

// Create multi-elevator system
var system = new ElevatorSystem.ElevatorSystem(
    elevatorCount: ELEVATOR_COUNT,
    minFloor: MIN_FLOOR,
    maxFloor: MAX_FLOOR,
    doorOpenMs: DOOR_OPEN_MS,
    floorTravelMs: FLOOR_TRAVEL_MS);

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

// Main console interface loop
Console.WriteLine($"=== ELEVATOR SYSTEM ({ELEVATOR_COUNT} elevators, floors {MIN_FLOOR}-{MAX_FLOOR}) ===\n");
Console.WriteLine("Press [R] to REQUEST a ride");
Console.WriteLine("Press [S] to view STATUS");
Console.WriteLine("Press [Q] to QUIT\n");

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
                await processingTask;
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
