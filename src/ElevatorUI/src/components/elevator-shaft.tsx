"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { ElevatorDto } from "@/types/elevator";

interface ElevatorShaftProps {
  elevator: ElevatorDto;
  maxFloor: number;
  onToggleMaintenance: (index: number) => void;
}

function stateColor(state: string): string {
  switch (state) {
    case "IDLE":
      return "bg-green-500";
    case "MOVING_UP":
    case "MOVING_DOWN":
      return "bg-blue-500";
    case "DOOR_OPEN":
    case "DOOR_OPENING":
    case "DOOR_CLOSING":
      return "bg-yellow-500";
    case "MAINTENANCE":
      return "bg-orange-500";
    case "EMERGENCY_STOP":
      return "bg-red-500";
    default:
      return "bg-gray-500";
  }
}

function stateLabel(state: string): string {
  switch (state) {
    case "IDLE":
      return "Idle";
    case "MOVING_UP":
      return "Up";
    case "MOVING_DOWN":
      return "Down";
    case "DOOR_OPEN":
      return "Open";
    case "DOOR_OPENING":
      return "Opening";
    case "DOOR_CLOSING":
      return "Closing";
    case "MAINTENANCE":
      return "Maint.";
    case "EMERGENCY_STOP":
      return "E-Stop";
    default:
      return state;
  }
}

export function ElevatorShaft({
  elevator,
  maxFloor,
  onToggleMaintenance,
}: ElevatorShaftProps) {
  const floors = Array.from({ length: maxFloor }, (_, i) => maxFloor - i);
  const servedSet = elevator.servedFloors
    ? new Set(elevator.servedFloors)
    : null;
  const targetSet = new Set(elevator.targetFloors);

  return (
    <Card className="w-36 flex-shrink-0">
      <CardHeader className="px-3 py-2 space-y-1">
        <CardTitle className="text-sm font-semibold text-center">
          {elevator.label}
        </CardTitle>
        <div className="flex justify-center">
          <Badge variant="outline" className="text-xs">
            {elevator.type}
          </Badge>
        </div>
        <div className="flex justify-center">
          <Badge
            variant="secondary"
            className={`text-xs text-white ${stateColor(elevator.state)}`}
          >
            {stateLabel(elevator.state)}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="px-3 pb-2 space-y-1">
        <div className="flex flex-col gap-0.5">
          {floors.map((floor) => {
            const isCurrent = floor === elevator.currentFloor;
            const isTarget = targetSet.has(floor);
            const isServed = servedSet === null || servedSet.has(floor);

            return (
              <div
                key={floor}
                className={`flex items-center justify-center h-6 rounded text-xs font-mono transition-colors
                  ${isCurrent ? `${stateColor(elevator.state)} text-white font-bold` : ""}
                  ${!isCurrent && isTarget ? "ring-2 ring-blue-400 bg-blue-50 dark:bg-blue-950" : ""}
                  ${!isCurrent && !isTarget && isServed ? "bg-muted" : ""}
                  ${!isServed && !isCurrent ? "bg-muted/30 text-muted-foreground/40 border border-dashed border-muted-foreground/20" : ""}
                `}
              >
                {floor}
              </div>
            );
          })}
        </div>

        <Button
          variant={elevator.inMaintenance ? "destructive" : "outline"}
          size="sm"
          className="w-full mt-2 text-xs"
          onClick={() => onToggleMaintenance(elevator.index)}
        >
          {elevator.inMaintenance ? "Exit Maint." : "Maintenance"}
        </Button>
      </CardContent>
    </Card>
  );
}
