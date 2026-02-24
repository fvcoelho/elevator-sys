#!/bin/bash

# Test script to demonstrate simplified priority-based request handling

echo "=========================================="
echo "  SIMPLIFIED PRIORITY DEMONSTRATION"
echo "=========================================="
echo ""
echo "This demo shows Normal vs High priority processing"
echo ""

# Clean up any existing requests
rm -rf requests/*.txt processed/*.txt 2>/dev/null

# Create requests directory
mkdir -p requests

echo "Creating test request files..."
echo ""

# Get base timestamp
BASE_TS=$(date +%Y%m%d_%H%M%S)

# Create Normal priority requests
echo "1. Normal priority: Floor 5 → 15"
touch "requests/${BASE_TS}_001_from_5_to_15_N.txt"

sleep 1
BASE_TS=$(date +%Y%m%d_%H%M%S)

echo "2. Normal priority: Floor 10 → 20"
touch "requests/${BASE_TS}_002_from_10_to_20.txt"

sleep 1
BASE_TS=$(date +%Y%m%d_%H%M%S)

# Create High priority requests
echo "3. High priority: Floor 3 → 8"
touch "requests/${BASE_TS}_003_from_3_to_8_H.txt"

sleep 1
BASE_TS=$(date +%Y%m%d_%H%M%S)

echo "4. High priority: Floor 18 → 2"
touch "requests/${BASE_TS}_004_from_18_to_2_H.txt"

echo ""
echo "=========================================="
echo "EXPECTED PROCESSING ORDER:"
echo "=========================================="
echo ""
echo "Requests were added in this order:"
echo "  1. Normal: 5 → 15  (timestamp 001)"
echo "  2. Normal: 10 → 20 (timestamp 002)"
echo "  3. High: 3 → 8     (timestamp 003)"
echo "  4. High: 18 → 2    (timestamp 004)"
echo ""
echo "But they will be PROCESSED in this order:"
echo "  1. High: 3 → 8     (High priority, earliest)"
echo "  2. High: 18 → 2    (High priority, next)"
echo "  3. Normal: 5 → 15  (Normal priority, earliest)"
echo "  4. Normal: 10 → 20 (Normal priority, next)"
echo ""
echo "=========================================="
echo "KEY POINTS:"
echo "=========================================="
echo "  • High priority requests jump the queue"
echo "  • Within same priority, FIFO order applies"
echo "  • High priority selects CLOSEST elevator"
echo "  • Normal priority PREFERS idle elevators"
echo "=========================================="
echo ""
echo "Files created in requests/ directory"
echo "Start the elevator system:"
echo "  dotnet run --project src/ElevatorSystem"
echo ""
echo "Watch per-elevator logs:"
echo "  tail -f logs/elevator_*.log"
