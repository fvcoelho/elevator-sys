# Running the Elevator System

This document explains how to run the multi-elevator system with log monitoring.

## Running the System

Open separate terminal windows for each component:

```bash
# Terminal 1: ElevatorSystem (main application)
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj

# Terminal 2: ElevatorPainel (elevator painel request)
dotnet run --project src/ElevatorPainel/ElevatorPainel.csproj

# Terminal 3: Elevator A log monitor
tail -f logs/elevator_A.log

# Terminal 4: Elevator B log monitor
tail -f logs/elevator_B.log

# Terminal 5: Elevator C log monitor
tail -f logs/elevator_C.log
```

**Tip:** Arrange the terminal windows to view all components simultaneously - logs on top, applications on bottom.

## Log Files

Elevator logs are written to:
- `logs/elevator_A.log`
- `logs/elevator_B.log`
- `logs/elevator_C.log`

These files are created automatically on first run.

## System Controls

Once running, use the ElevatorSystem console interface:
- **[R]** - Request a new ride (pickup + destination)
- **[S]** - View system status
- **[A]** - View analytics (performance metrics)
- **[D]** - Change dispatch algorithm (Simple/SCAN/LOOK)
- **[Q]** - Quit system

## Troubleshooting

### Log files not updating
Make sure the logs directory exists and is writable:
```bash
mkdir -p logs
chmod 755 logs
```

### Ports already in use
If you see "Address already in use" errors, find and stop any existing dotnet processes:
```bash
# Find processes
pgrep -f "dotnet.*Elevator"

# Kill specific process by PID
kill <PID>
```

## Architecture

- **ElevatorSystem**: Main elevator control system with dispatcher
- **ElevatorPainel**: Elevator painel request
- **Logs**: Real-time elevator state and operation logs

## Performance

The system can handle 100+ concurrent requests efficiently:
- Dispatch time: < 1ms per request
- 3 elevators serving floors 1-20
- Multiple dispatch algorithms (Simple, SCAN, LOOK)
- Real-time performance analytics
