namespace ElevatorPainel;

class Program
{
    private const int MIN_FLOOR = 1;
    private const int MAX_FLOOR = 20;
    private const string REQUESTS_DIR = "requests";

    static async Task Main(string[] args)
    {
        // Create requests directory if it doesn't exist
        if (!Directory.Exists(REQUESTS_DIR))
        {
            Directory.CreateDirectory(REQUESTS_DIR);
        }

        // Handle traffic mode flags
        if (args.Length == 1)
        {
            switch (args[0].ToLower())
            {
                case "--light" or "-l":
                    await GenerateLightTraffic();
                    return;
                case "--moderate" or "-m":
                    await GenerateModerateTraffic();
                    return;
                case "--rush" or "-r":
                    await GenerateRushHourTraffic();
                    return;
            }
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
        await RunInteractiveMode();
    }

    static void ShowWelcome()
    {
        Console.Clear();
        Console.WriteLine("=== ELEVATOR PAINEL REQUEST ===");
        Console.WriteLine();
        Console.WriteLine("Enter elevator requests to send to the system.");
        Console.WriteLine($"Requests are written to: {Path.GetFullPath(REQUESTS_DIR)}");
        Console.WriteLine();
    }

    static async Task RunInteractiveMode()
    {
        while (true)
        {
            Console.WriteLine("[R] Request");
            Console.WriteLine("[G] Generate Traffic (Light/Moderate/Rush Hour)");
            Console.WriteLine("[Q] Quit");
            Console.WriteLine();
            Console.Write("> ");

            var key = Console.ReadKey(intercept: true);
            Console.WriteLine(key.KeyChar);
            Console.WriteLine();

            switch (char.ToUpper(key.KeyChar))
            {
                case 'R':
                    AddRequest();
                    break;
                case 'G':
                    await GenerateTraffic();
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

        // Get priority
        Console.Write("High priority? [y/N]: ");
        var priorityInput = Console.ReadLine()?.Trim().ToUpper();
        bool highPriority = priorityInput == "Y" || priorityInput == "YES";

        // Write to file
        var filename = WriteRequest(pickup.Value, destination.Value, highPriority);
        if (filename != null)
        {
            Console.WriteLine();
            var priorityLabel = highPriority ? " [HIGH]" : "";
            Console.WriteLine($"✓ Request added: {pickup.Value} → {destination.Value}{priorityLabel}");
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

    static string? WriteRequest(int pickup, int destination, bool highPriority = false)
    {
        try
        {
            // Generate timestamp with milliseconds to prevent collisions
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

            // Create filename (append _H for high priority)
            var prioritySuffix = highPriority ? "_H" : "";
            var filename = $"{timestamp}_from_{pickup}_to_{destination}{prioritySuffix}.txt";
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

    static async Task GenerateTraffic()
    {
        Console.WriteLine();
        Console.WriteLine("=== TRAFFIC GENERATION ===");
        Console.WriteLine();
        Console.WriteLine("[L] Light Traffic    (1-2 req/sec,  ~20 requests)");
        Console.WriteLine("[M] Moderate Traffic (3-5 req/sec,  ~50 requests)");
        Console.WriteLine("[R] Rush Hour        (8-15 req/sec, ~150 requests)");
        Console.WriteLine("[C] Cancel");
        Console.WriteLine();
        Console.Write("> ");

        var key = Console.ReadKey(intercept: true);
        Console.WriteLine(key.KeyChar);
        Console.WriteLine();

        switch (char.ToUpper(key.KeyChar))
        {
            case 'L':
                await GenerateLightTraffic();
                break;
            case 'M':
                await GenerateModerateTraffic();
                break;
            case 'R':
                await GenerateRushHourTraffic();
                break;
            case 'C':
                Console.WriteLine("Cancelled.");
                break;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }

    static async Task GenerateLightTraffic()
    {
        Console.WriteLine("Generating LIGHT traffic...");
        Console.WriteLine("Duration: 15 seconds | Rate: 1-2 req/sec | Target: ~20 requests");
        Console.WriteLine();

        var random = new Random();
        var requestCount = 0;
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(15);

        while (DateTime.Now - startTime < duration)
        {
            // Generate random request
            var pickup = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
            var destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);

            // Ensure different floors
            while (destination == pickup)
                destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);

            // Write request
            var filename = WriteRequest(pickup, destination);
            if (filename != null)
            {
                requestCount++;
                Console.WriteLine($"  [{requestCount}] {pickup} → {destination}");
            }

            // Inter-request delay: 500-1000ms (1-2 req/sec)
            await Task.Delay(random.Next(500, 1001));
        }

        Console.WriteLine();
        Console.WriteLine($"✓ Light traffic complete: {requestCount} requests generated");
    }

    static async Task GenerateModerateTraffic()
    {
        Console.WriteLine("Generating MODERATE traffic...");
        Console.WriteLine("Duration: 15 seconds | Rate: 3-5 req/sec | Target: ~50 requests");
        Console.WriteLine();

        var random = new Random();
        var requestCount = 0;
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(15);

        while (DateTime.Now - startTime < duration)
        {
            int pickup, destination;
            var pattern = random.Next(100);

            if (pattern < 40) // 40% ground to upper
            {
                pickup = 1;
                destination = random.Next(5, MAX_FLOOR + 1);
            }
            else if (pattern < 80) // 40% upper to ground
            {
                pickup = random.Next(5, MAX_FLOOR + 1);
                destination = 1;
            }
            else // 20% inter-floor
            {
                pickup = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
                destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
                while (destination == pickup)
                    destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
            }

            // Write request
            var filename = WriteRequest(pickup, destination);
            if (filename != null)
            {
                requestCount++;
                Console.WriteLine($"  [{requestCount}] {pickup} → {destination}");
            }

            // Inter-request delay: 200-350ms (3-5 req/sec)
            await Task.Delay(random.Next(200, 351));
        }

        Console.WriteLine();
        Console.WriteLine($"✓ Moderate traffic complete: {requestCount} requests generated");
    }

    static async Task GenerateRushHourTraffic()
    {
        Console.WriteLine("Generating RUSH HOUR traffic...");
        Console.WriteLine("Duration: 20 seconds | Rate: 8-15 req/sec | Target: ~150 requests");
        Console.WriteLine();

        var random = new Random();
        var requestCount = 0;
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(20);

        while (DateTime.Now - startTime < duration)
        {
            int pickup, destination;
            var pattern = random.Next(100);

            if (pattern < 60) // 60% ground to upper (morning arrival)
            {
                pickup = 1;
                destination = random.Next(2, MAX_FLOOR + 1);
            }
            else if (pattern < 90) // 30% upper to ground (evening departure)
            {
                pickup = random.Next(2, MAX_FLOOR + 1);
                destination = 1;
            }
            else // 10% inter-floor
            {
                pickup = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
                destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
                while (destination == pickup)
                    destination = random.Next(MIN_FLOOR, MAX_FLOOR + 1);
            }

            // Write request
            var filename = WriteRequest(pickup, destination);
            if (filename != null)
            {
                requestCount++;
                Console.WriteLine($"  [{requestCount}] {pickup} → {destination}");
            }

            // Inter-request delay: 65-125ms (8-15 req/sec)
            await Task.Delay(random.Next(65, 126));
        }

        Console.WriteLine();
        Console.WriteLine($"✓ Rush hour traffic complete: {requestCount} requests generated");
    }
}
