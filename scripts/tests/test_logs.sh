#!/bin/bash

echo "Starting elevator system for 10 seconds to process requests..."
timeout 10 dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj 2>&1 | grep -E "(FILE|ELEVATOR|TRACKING)" &
sleep 10

echo ""
echo "=== ELEVATOR A LOG ==="
cat logs/elevator_A.log
echo ""
echo "=== ELEVATOR B LOG ==="
cat logs/elevator_B.log
echo ""
echo "=== ELEVATOR C LOG ==="
cat logs/elevator_C.log
