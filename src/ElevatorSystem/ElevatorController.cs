using System.Collections.Concurrent;

namespace ElevatorSystem;

public class ElevatorController
{
    private readonly Elevator _elevator;
    private readonly ConcurrentQueue<int> _requestQueue = new();

    public ElevatorController(Elevator elevator)
    {
        _elevator = elevator ?? throw new ArgumentNullException(nameof(elevator));
    }

    public void RequestElevator(int floor)
    {
        if (floor < _elevator.MinFloor || floor > _elevator.MaxFloor)
        {
            throw new ArgumentException($"Floor must be between {_elevator.MinFloor} and {_elevator.MaxFloor}");
        }

        _requestQueue.Enqueue(floor);
        var pending = _requestQueue.ToArray();
        Console.WriteLine($"Request received for floor {floor} → Pending: [{string.Join(", ", pending)}]");
    }

    public async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Dequeue request from controller queue and add to elevator targets
            if (_requestQueue.TryDequeue(out int requestedFloor))
            {
                _elevator.AddRequest(requestedFloor);
            }

            // Process elevator targets if idle and has targets
            if (_elevator.State == ElevatorState.IDLE && _elevator.HasTargets())
            {
                if (_elevator.TryGetNextTarget(out int targetFloor))
                {
                    // Move to target floor
                    while (_elevator.CurrentFloor != targetFloor)
                    {
                        if (_elevator.CurrentFloor < targetFloor)
                        {
                            await _elevator.MoveUp();
                        }
                        else
                        {
                            await _elevator.MoveDown();
                        }
                    }

                    // Open and close doors
                    await _elevator.OpenDoor();
                    await _elevator.CloseDoor();
                }
            }

            // Small delay to prevent busy-waiting
            await Task.Delay(100, cancellationToken);
        }
    }

    public string GetStatus()
    {
        var targets = _elevator.GetTargets();
        var pendingRequests = _requestQueue.Count;

        return $"Floor: {_elevator.CurrentFloor} | State: {_elevator.State} | Queue: [{string.Join(", ", targets)}] | Pending: {pendingRequests}";
    }
}
