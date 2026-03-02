"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { ElevatorDto } from "@/types/elevator";

// --- Props ---

interface ElevatorShaftProps {
  elevator: ElevatorDto;
  maxFloor: number;
  onToggleMaintenance: (index: number) => void;
}

// --- Helpers ---

const STATE_COLORS: Record<string, string> = {
  IDLE: "bg-green-500",
  MOVING_UP: "bg-blue-500",
  MOVING_DOWN: "bg-blue-500",
  DOOR_OPEN: "bg-yellow-500",
  DOOR_OPENING: "bg-yellow-500",
  DOOR_CLOSING: "bg-yellow-500",
  MAINTENANCE: "bg-orange-500",
  EMERGENCY_STOP: "bg-red-500",
};

const STATE_LABELS: Record<string, string> = {
  IDLE: "Idle",
  MOVING_UP: "Up",
  MOVING_DOWN: "Down",
  DOOR_OPEN: "Open",
  DOOR_OPENING: "Opening",
  DOOR_CLOSING: "Closing",
  MAINTENANCE: "Maint.",
  EMERGENCY_STOP: "E-Stop",
};

function stateColor(state: string): string {
  return STATE_COLORS[state] ?? "bg-gray-500";
}

function stateLabel(state: string): string {
  return STATE_LABELS[state] ?? state;
}

// --- Component ---

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
      <CardHeader className="px-3 py-2 space-y-1 h-[5.5rem]">
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
                <span>{floor}</span>
                {isCurrent && elevator.requestIds.length > 0 && (
                  <span className="ml-1 truncate text-[10px] font-semibold">
                    {elevator.requestIds.map((id) => `#${id}`).join(" ")}
                  </span>
                )}
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

        <a
          href={`http://localhost:5081/api/logs/${elevator.label}`}
          download={`elevator_${elevator.label}.log`}
          className="block w-full mt-1"
        >
          <Button variant="ghost" size="sm" className="w-full text-xs text-muted-foreground">
            Download log
          </Button>
        </a>
      </CardContent>
    </Card>
  );
}
