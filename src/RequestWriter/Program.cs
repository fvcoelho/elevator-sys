namespace RequestWriter;

class Program
{
    private const int MIN_FLOOR = 1;
    private const int MAX_FLOOR = 20;
    private const string REQUESTS_DIR = "requests";

    static void Main(string[] args)
    {
        // Create requests directory if it doesn't exist
        if (!Directory.Exists(REQUESTS_DIR))
        {
            Directory.CreateDirectory(REQUESTS_DIR);
        }

        // Handle command line arguments
        if (args.Length == 2)
        {
            if (int.TryParse(args[0], out int pickup) && int.TryParse(args[1], out int destination))
            {
                if (ValidateFloor(pickup) && ValidateFloor(destination) && pickup != destination)
                {
                    WriteRequest(pickup, destination);
                    Console.WriteLine($"✓ Request added: {pickup} → {destination}");
                    return;
                }
            }
            Console.WriteLine("Invalid arguments. Usage: dotnet run -- <pickup> <destination>");
            Console.WriteLine($"Floors must be between {MIN_FLOOR} and {MAX_FLOOR}, and pickup must differ from destination.");
            return;
        }

        // Interactive mode
        ShowWelcome();
        RunInteractiveMode();
    }

    static void ShowWelcome()
    {
        Console.Clear();
        Console.WriteLine("=== ELEVATOR REQUEST WRITER ===");
        Console.WriteLine();
        Console.WriteLine("Enter elevator requests to send to the system.");
        Console.WriteLine($"Requests are written to: {Path.GetFullPath(REQUESTS_DIR)}");
        Console.WriteLine();
    }

    static void RunInteractiveMode()
    {
        while (true)
        {
            Console.WriteLine("[A] Add Request");
            Console.WriteLine("[Q] Quit");
            Console.WriteLine();
            Console.Write("> ");

            var key = Console.ReadKey(intercept: true);
            Console.WriteLine(key.KeyChar);
            Console.WriteLine();

            switch (char.ToUpper(key.KeyChar))
            {
                case 'A':
                    AddRequest();
                    break;
                case 'Q':
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    Console.WriteLine();
                    break;
            }
        }
    }

    static void AddRequest()
    {
        // Get pickup floor
        int? pickup = PromptForFloor("Pickup floor");
        if (pickup == null)
        {
            Console.WriteLine();
            return;
        }

        // Get destination floor
        int? destination = PromptForFloor("Destination floor");
        if (destination == null)
        {
            Console.WriteLine();
            return;
        }

        // Validate pickup != destination
        if (pickup.Value == destination.Value)
        {
            Console.WriteLine($"❌ Error: Pickup and destination cannot be the same floor ({pickup.Value}).");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
            return;
        }

        // Write to file
        var filename = WriteRequest(pickup.Value, destination.Value);
        if (filename != null)
        {
            Console.WriteLine();
            Console.WriteLine($"✓ Request added: {pickup.Value} → {destination.Value}");
            Console.WriteLine($"  Created file: {filename}");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("❌ Failed to write request to file. Please try again.");
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }
    }

    static int? PromptForFloor(string prompt)
    {
        Console.Write($"{prompt} ({MIN_FLOOR}-{MAX_FLOOR}): ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("❌ Error: No input provided.");
            return null;
        }

        if (!int.TryParse(input, out int floor))
        {
            Console.WriteLine($"❌ Error: '{input}' is not a valid number.");
            return null;
        }

        if (!ValidateFloor(floor))
        {
            Console.WriteLine($"❌ Error: Floor {floor} is out of range ({MIN_FLOOR}-{MAX_FLOOR}).");
            return null;
        }

        return floor;
    }

    static bool ValidateFloor(int floor)
    {
        return floor >= MIN_FLOOR && floor <= MAX_FLOOR;
    }

    static string? WriteRequest(int pickup, int destination)
    {
        try
        {
            // Generate timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Create filename
            var filename = $"{timestamp}_from_{pickup}_to_{destination}.txt";
            var filepath = Path.Combine(REQUESTS_DIR, filename);

            // Write request to individual file
            File.WriteAllText(filepath, $"{pickup} {destination}");

            return filename;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ File error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            return null;
        }
    }
}
