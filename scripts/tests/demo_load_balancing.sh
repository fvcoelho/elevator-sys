#!/bin/bash

echo "=========================================="
echo "  LOAD BALANCING DEMONSTRATION"
echo "=========================================="
echo ""
echo "This demo shows how requests are distributed across 3 elevators"
echo ""

# Clean up any existing requests
rm -rf requests/*.txt processed/*.txt 2>/dev/null

# Create requests directory
mkdir -p requests processed

echo "Creating 6 requests at different floors..."
echo ""

# Get base timestamp
BASE_TS=$(date +%Y%m%d_%H%M%S)

# Create 6 requests with slight time delays
for i in {1..6}; do
    TIMESTAMP="${BASE_TS}_$(printf "%03d" $i)"
    
    case $i in
        1)
            PICKUP=2
            DEST=5
            echo "Request $i: Floor $PICKUP → $DEST (near Elevator A at floor 1)"
            ;;
        2)
            PICKUP=9
            DEST=15
            echo "Request $i: Floor $PICKUP → $DEST (near Elevator B at floor 10)"
            ;;
        3)
            PICKUP=19
            DEST=3
            echo "Request $i: Floor $PICKUP → $DEST (near Elevator C at floor 20)"
            ;;
        4)
            PICKUP=5
            DEST=10
            echo "Request $i: Floor $PICKUP → $DEST (middle area)"
            ;;
        5)
            PICKUP=12
            DEST=18
            echo "Request $i: Floor $PICKUP → $DEST (upper area)"
            ;;
        6)
            PICKUP=7
            DEST=14
            echo "Request $i: Floor $PICKUP → $DEST (middle area)"
            ;;
    esac
    
    touch "requests/${TIMESTAMP}_from_${PICKUP}_to_${DEST}.txt"
done

echo ""
echo "=========================================="
echo "EXPECTED LOAD DISTRIBUTION:"
echo "=========================================="
echo ""
echo "Initial Elevator Positions:"
echo "  Elevator A: Floor 1  (bottom)"
echo "  Elevator B: Floor 10 (middle)"
echo "  Elevator C: Floor 20 (top)"
echo ""
echo "Expected Assignments (distance-based):"
echo "  Request 1 (Floor 2)  → Elevator A (distance 1) ✓"
echo "  Request 2 (Floor 9)  → Elevator B (distance 1) ✓"
echo "  Request 3 (Floor 19) → Elevator C (distance 1) ✓"
echo "  Request 4 (Floor 5)  → Next available elevator"
echo "  Request 5 (Floor 12) → Next available elevator"
echo "  Request 6 (Floor 7)  → Next available elevator"
echo ""
echo "Result: Requests distributed across ALL elevators!"
echo "=========================================="
echo ""
echo "Files created in requests/ directory"
echo "Start the elevator system to see load balancing:"
echo "  dotnet run --project src/ElevatorSystem"
echo ""
echo "Watch the logs/ directory for per-elevator activity:"
echo "  tail -f logs/elevator_*.log"
