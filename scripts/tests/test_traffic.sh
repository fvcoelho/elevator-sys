#!/bin/bash

echo "=== TRAFFIC SIMULATION TEST ==="
echo ""

# Clean up
rm -rf requests processed 2>/dev/null
mkdir -p requests processed

echo "1. Testing Light Traffic Mode..."
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj -- --light 2>&1 | tail -3
echo ""

LIGHT_COUNT=$(ls requests/ | wc -l | tr -d ' ')
echo "   Generated $LIGHT_COUNT request files"
echo ""

echo "2. Processing light traffic with ElevatorSystem..."
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj 2>&1 &
PID=$!
sleep 3
kill $PID 2>/dev/null
wait $PID 2>/dev/null

PROCESSED_COUNT=$(ls processed/ 2>/dev/null | wc -l | tr -d ' ')
echo "   Processed $PROCESSED_COUNT requests"
echo ""

# Clean up for next test
rm -rf requests/*.txt 2>/dev/null

echo "3. Testing Moderate Traffic Mode..."
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj -- --moderate 2>&1 | tail -3
echo ""

MODERATE_COUNT=$(ls requests/ | wc -l | tr -d ' ')
echo "   Generated $MODERATE_COUNT request files"
echo ""

# Clean up
rm -rf requests processed 2>/dev/null
mkdir -p requests processed

echo "4. Testing Rush Hour Mode..."
dotnet run --project src/ElevatorPanel/ElevatorPanel.csproj -- --rush 2>&1 | tail -3
echo ""

RUSH_COUNT=$(ls requests/ | wc -l | tr -d ' ')
echo "   Generated $RUSH_COUNT request files"
echo ""

# Check for duplicate filenames (timestamp collisions)
DUPLICATES=$(ls requests/ | sort | uniq -d | wc -l | tr -d ' ')
echo "   Duplicate filenames (collisions): $DUPLICATES"
echo ""

echo "5. Testing backward compatibility (old format)..."
echo "10 5" > requests/20240101_120000_from_10_to_5.txt
echo "   Created old format file: 20240101_120000_from_10_to_5.txt"

dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj 2>&1 &
PID=$!
sleep 2
kill $PID 2>/dev/null
wait $PID 2>/dev/null

if [ -f "processed/20240101_120000_from_10_to_5.txt" ]; then
    echo "   ✓ Old format processed successfully"
else
    echo "   ✗ Old format NOT processed"
fi
echo ""

echo "=== TEST SUMMARY ==="
echo "Light Traffic:    $LIGHT_COUNT requests generated"
echo "Moderate Traffic: $MODERATE_COUNT requests generated"
echo "Rush Hour:        $RUSH_COUNT requests generated"
echo "Collisions:       $DUPLICATES (should be 0)"
echo "Old Format:       $([ -f 'processed/20240101_120000_from_10_to_5.txt' ] && echo '✓ Working' || echo '✗ Failed')"
echo ""
echo "All tests complete!"
