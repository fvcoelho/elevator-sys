using ElevatorSystem;

// Configuration Constants
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 10;
const int INITIAL_FLOOR = 1;
const int DOOR_OPEN_MS = 3000;      // 3 seconds
const int FLOOR_TRAVEL_MS = 1500;   // 1.5 seconds per floor

// Create elevator and controller instances
var elevator = new Elevator(MIN_FLOOR, MAX_FLOOR, INITIAL_FLOOR, DOOR_OPEN_MS, FLOOR_TRAVEL_MS);
var controller = new ElevatorController(elevator);

// Create cancellation token source
using var cts = new CancellationTokenSource();

// Start processing loop in background
var processingTask = Task.Run(async () =>
{
    try
    {
        await controller.ProcessRequestsAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Expected when canceling
    }
}, cts.Token);

// Main console interface loop
Console.WriteLine("=== ELEVATOR SYSTEM STARTED ===\n");
Console.WriteLine("Press: [1-9] Floor 1-9 | [0] Floor 10 | [Q] Quit");

while (true)
{
    var key = Console.ReadKey(intercept: true);
    var keyChar = char.ToUpperInvariant(key.KeyChar);

    // Handle floor number keys 1-9 and 0 (for floor 10)
    if (keyChar >= '1' && keyChar <= '9')
    {
        int floor = keyChar - '0'; // Convert char to int (1-9)
        try
        {
            Console.WriteLine($"\nRequesting floor {floor}...");
            controller.RequestElevator(floor);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
    else if (keyChar == '0')
    {
        // 0 key represents floor 10
        try
        {
            Console.WriteLine("\nRequesting floor 10...");
            controller.RequestElevator(10);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
    else
    {
        switch (keyChar)
        {
            case 'Q':
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
                Console.WriteLine($"\nUnknown key '{key.KeyChar}'. Use keys 1-9, 0 (floor 10) or Q (quit).");
                break;
        }
    }

    // Small delay to allow status updates to be visible
    await Task.Delay(100);
}
