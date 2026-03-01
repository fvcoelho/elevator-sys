"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { Passenger } from "@/hooks/use-passengers";

interface BuildingViewProps {
  maxFloor: number;
  totalPeople: number;
  waitingLobby: number;
  getByFloor: (floor: number) => Passenger[];
}

export function BuildingView({
  maxFloor,
  totalPeople,
  waitingLobby,
  getByFloor,
}: BuildingViewProps) {
  const floors = Array.from({ length: maxFloor }, (_, i) => maxFloor - i);

  return (
    <Card className="w-40 flex-shrink-0">
      <CardHeader className="px-3 py-2 space-y-0.5">
        <CardTitle className="text-sm font-bold text-center">
          Building
        </CardTitle>
        <p className="text-xs text-center text-muted-foreground">
          total people {totalPeople}
        </p>
        <p className="text-xs text-center text-muted-foreground">
          waiting lobby {waitingLobby}
        </p>
      </CardHeader>
      <CardContent className="px-3 pb-2">
        <div className="flex flex-col gap-0.5">
          {floors.map((floor) => {
            const people = getByFloor(floor);
            return (
              <div
                key={floor}
                className="flex items-center h-6 rounded text-xs bg-muted px-1.5 overflow-hidden"
              >
                {people.length > 0 ? (
                  <span className="font-semibold truncate">
                    {people.map((p) => p.name).join(", ")}
                  </span>
                ) : (
                  <span className="text-muted-foreground/0">&nbsp;</span>
                )}
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
