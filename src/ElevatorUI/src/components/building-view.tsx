"use client";

import { useEffect, useMemo, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { Passenger } from "@/types/elevator";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectPassengers } from "@/store/slices/passengersSlice";

// --- Props ---

interface BuildingViewProps {
  maxFloor: number;
  totalPeople: number;
  waitingLobby: number;
  vipFloors?: number[];
}

// --- Helpers ---

function passengerColor(p: Passenger): string {
  if (p.status === "waiting" && p.currentFloor === 1) return "text-red-500";
  if (p.status === "arrived" && p.currentFloor === 1) return "text-blue-500";
  return "";
}

function formatCountdown(p: Passenger, now: number): string | null {
  if (p.status !== "arrived" || !p.returnDelaySec || !p.arrivedAt) return null;
  const remaining = Math.max(
    0,
    Math.ceil(p.returnDelaySec - (now - p.arrivedAt) / 1000)
  );
  return `${remaining}s`;
}

// --- Component ---

export function BuildingView({ maxFloor, totalPeople, waitingLobby, vipFloors = [] }: BuildingViewProps) {
  const floors = Array.from({ length: maxFloor }, (_, i) => maxFloor - i);
  const [now, setNow] = useState(Date.now());
  const passengers = useAppSelector(selectPassengers);
  const vipSet = new Set(vipFloors);

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(interval);
  }, []);

  const byFloor = useMemo(() => {
    const map = new Map<number, Passenger[]>();
    for (const p of passengers) {
      if (
        p.status === "waiting" ||
        p.status === "arrived" ||
        p.status === "returning"
      ) {
        const list = map.get(p.currentFloor) ?? [];
        list.push(p);
        map.set(p.currentFloor, list);
      }
    }
    return map;
  }, [passengers]);

  return (
    <Card className="w-96 flex-shrink-0">
      <CardHeader className="px-3 py-2 space-y-0.5 min-h-[5.5rem]">
        <CardTitle className="text-sm font-bold text-center">
          Building
        </CardTitle>
        <div className="flex justify-center gap-3 text-xs text-muted-foreground">
          <span>people {totalPeople}</span>
          <span>lobby {waitingLobby}</span>
        </div>
        {vipFloors.length > 0 && (
          <div className="flex justify-center items-center gap-1.5 text-[10px] text-red-500">
            <span className="inline-block w-3 h-3 rounded border-2 border-dashed border-red-500 bg-red-50 dark:bg-red-950/30 flex-shrink-0" />
            VIP floor
          </div>
        )}
      </CardHeader>

      <CardContent className="px-3 pb-2">
        <div className="flex flex-col gap-0.5">
          {floors.map((floor) => {
            const people = byFloor.get(floor) ?? [];

            const isLobby = floor === 1;
            const isVip = vipSet.has(floor);

            return (
              <div
                key={floor}
                className={`flex items-center rounded text-xs px-1.5 ${isLobby ? "min-h-6 flex-wrap py-0.5" : "h-6 overflow-hidden"} ${isVip ? "border-2 border-dashed border-red-500 bg-red-50 dark:bg-red-950/30" : "bg-muted"}`}
              >
                {people.length > 0 ? (
                  <span className={isLobby ? "flex flex-wrap gap-x-1" : "truncate"}>
                    {people.map((p, i) => (
                      <span key={p.id}>
                        {!isLobby && i > 0 && ", "}
                        <span className={`font-semibold ${passengerColor(p)}`}>
                          {p.name}
                        </span>
                        {p.requestId !== undefined && (
                          <span className="text-muted-foreground/60 text-[10px] ml-0.5">#{p.requestId}</span>
                        )}
                        {formatCountdown(p, now) && (
                          <span className="text-muted-foreground ml-0.5">
                            {formatCountdown(p, now)}
                          </span>
                        )}
                        {p.status === "returning" && (
                          <span className="text-blue-500 ml-0.5">&darr;</span>
                        )}
                      </span>
                    ))}
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
