using ElevatorSystem;

// Configuration Constants
const int MIN_FLOOR = 1;
const int MAX_FLOOR = 10;
const int INITIAL_FLOOR = 1;
const int DOOR_OPEN_MS = 2000;      // 2 seconds
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

while (true)
{
    Console.WriteLine("\n" + new string('=', 40));
    Console.WriteLine(controller.GetStatus());
    Console.WriteLine(new string('=', 40));
    Console.WriteLine("\nCommands: [R]equest | [S]tatus | [Q]uit");
    Console.Write("> ");

    var input = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (string.IsNullOrEmpty(input))
    {
        continue;
    }

    switch (input)
    {
        case "Q":
        case "QUIT":
        case "EXIT":
            Console.WriteLine("\nShutting down elevator system...");
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

        case "S":
        case "STATUS":
            // Status will be displayed in next loop iteration
            break;

        case "R":
        case "REQUEST":
            Console.Write("Enter floor number (1-10): ");
            var floorInput = Console.ReadLine()?.Trim();

            if (int.TryParse(floorInput, out int floor))
            {
                try
                {
                    controller.RequestElevator(floor);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a number between 1 and 10.");
            }
            break;

        default:
            Console.WriteLine("Unknown command. Please use R (Request), S (Status), or Q (Quit).");
            break;
    }

    // Small delay to allow status updates to be visible
    await Task.Delay(100);
}
