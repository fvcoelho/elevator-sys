"use client";

import { useMemo } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { ElevatorDto } from "@/types/elevator";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectRequestLog } from "@/store/slices/passengersSlice";

// --- Props ---

interface ElevatorShaftProps {
  elevator: ElevatorDto;
  maxFloor: number;
  vipFloors: number[];
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
  vipFloors,
  onToggleMaintenance,
}: ElevatorShaftProps) {
  const floors = Array.from({ length: maxFloor }, (_, i) => maxFloor - i);
  const requestLog = useAppSelector(selectRequestLog);
  const nameByRequestId = useMemo(() => {
    const map = new Map<number, string>();
    for (const entry of requestLog) {
      if (entry.requestId !== undefined) map.set(entry.requestId, entry.name);
    }
    return map;
  }, [requestLog]);
  const servedSet = elevator.servedFloors
    ? new Set(elevator.servedFloors)
    : null;
  const targetSet = new Set(elevator.targetFloors);
  const vipSet = new Set(vipFloors);

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
            const isVip = vipSet.has(floor);

            return (
              <div
                key={floor}
                className={`flex items-center justify-center h-6 rounded text-xs font-mono transition-colors
                  ${isCurrent ? `${stateColor(elevator.state)} text-white font-bold ring-2 ${isVip ? "ring-red-400" : "ring-transparent"}` : ""}
                  ${!isCurrent && isTarget ? "ring-2 ring-blue-400 bg-blue-50 dark:bg-blue-950" : ""}
                  ${!isCurrent && !isTarget && isServed && !isVip ? "bg-muted" : ""}
                  ${!isCurrent && !isTarget && isVip ? "border-2 border-dashed border-red-500 bg-red-50 dark:bg-red-950/30 text-red-600 dark:text-red-400 font-semibold" : ""}
                  ${!isServed && !isCurrent ? "bg-muted/30 text-muted-foreground/40 border border-dashed border-muted-foreground/20" : ""}
                `}
              >
                <span>{floor}</span>
                {isCurrent && elevator.requestIds.length > 0 && (
                  <span className="ml-1 truncate text-[10px] font-semibold">
                    {elevator.requestIds
                      .map((id) => nameByRequestId.get(id) ?? `#${id}`)
                      .join(" ")}
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
