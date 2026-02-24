#!/bin/bash

# Test script to demonstrate priority-based request handling

echo "Creating test request files with different priorities..."

# Create requests directory if it doesn't exist
mkdir -p requests

# Create timestamp
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Low priority request (floor 5 to 15)
echo "Creating LOW priority request: 5 → 15"
touch "requests/${TIMESTAMP}_000_from_5_to_15_L.txt"

# Wait a bit to ensure different timestamps
sleep 1
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Normal priority request (floor 10 to 20)
echo "Creating NORMAL priority request: 10 → 20"
touch "requests/${TIMESTAMP}_000_from_10_to_20_N.txt"

# Wait a bit
sleep 1
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# High priority request (floor 3 to 8)
echo "Creating HIGH priority request: 3 → 8"
touch "requests/${TIMESTAMP}_000_from_3_to_8_H.txt"

# Wait a bit
sleep 1
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Emergency priority request (floor 18 to 2)
echo "Creating EMERGENCY priority request: 18 → 2"
touch "requests/${TIMESTAMP}_000_from_18_to_2_E.txt"

echo ""
echo "Test files created in requests/ directory"
echo "Run the elevator system to see priority-based processing:"
echo "  dotnet run --project src/ElevatorSystem"
echo ""
echo "Expected processing order (highest priority first):"
echo "  1. Emergency: 18 → 2"
echo "  2. High: 3 → 8"
echo "  3. Normal: 10 → 20"
echo "  4. Low: 5 → 15"
