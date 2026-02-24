# Scripts Directory

This directory contains test and utility scripts for the elevator system.

## Test Scripts (`tests/`)

### Load Testing & Simulation

- **`demo_load_balancing.sh`**
  - Demonstrates load balancing across multiple elevators
  - Creates multiple concurrent ride requests
  - Useful for testing dispatch algorithm efficiency

- **`test_traffic.sh`**
  - Simulates realistic building traffic patterns
  - Tests system under sustained load
  - Monitors system performance metrics

### Priority & Request Testing

- **`test_priority.sh`**
  - Tests priority request handling (Normal vs High)
  - Validates VIP access features
  - Verifies priority queue behavior

- **`test_priority_simple.sh`**
  - Simplified priority testing
  - Tests basic two-level priority system
  - Quick validation of priority dispatch

### Logging & Monitoring

- **`test_logs.sh`**
  - Tests elevator logging functionality
  - Validates log file creation and format
  - Monitors real-time log output

## Usage

All test scripts should be run from the project root directory:

```bash
# Run load balancing demo
./scripts/tests/demo_load_balancing.sh

# Run traffic simulation
./scripts/tests/test_traffic.sh

# Test priority system
./scripts/tests/test_priority_simple.sh
```

## Prerequisites

Before running test scripts:

1. Build the project:
   ```bash
   dotnet build
   ```

2. Ensure the elevator system is running:
   ```bash
   dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj
   ```

3. Monitor logs in separate terminals if needed:
   ```bash
   tail -f logs/elevator_A.log
   tail -f logs/elevator_B.log
   tail -f logs/elevator_C.log
   ```

## Adding New Test Scripts

When adding new test scripts:

1. Place them in the `tests/` directory
2. Make them executable: `chmod +x scripts/tests/your_script.sh`
3. Add documentation to this README
4. Follow naming convention: `test_*.sh` or `demo_*.sh`
