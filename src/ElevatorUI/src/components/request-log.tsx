"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectRequestLog } from "@/store/slices/passengersSlice";

function floorLabel(floor: number): string {
  return floor === 1 ? "Lobby" : `F${floor}`;
}

export function RequestLog() {
  const log = useAppSelector(selectRequestLog);
  const reversed = [...log].reverse();

  return (
    <Card>
      <CardHeader className="px-3 py-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold">Request Log</CardTitle>
          {log.length > 0 && (
            <Badge variant="secondary" className="text-xs tabular-nums">
              {log.length}
            </Badge>
          )}
        </div>
      </CardHeader>

      <CardContent className="px-3 pb-3">
        {reversed.length === 0 ? (
          <p className="text-xs text-muted-foreground">No requests yet.</p>
        ) : (
          <div className="max-h-52 overflow-y-auto rounded bg-muted/50 border p-2 space-y-0.5">
            {reversed.map((entry, i) => (
              <div key={i} className="text-xs font-mono leading-snug">
                <span className="text-muted-foreground w-8 inline-block">
                  {entry.requestId !== undefined ? `#${entry.requestId}` : "#?"}
                </span>
                {": "}
                <span className="font-semibold">{entry.name}</span>
                {" → "}
                <span>{floorLabel(entry.pickupFloor)}</span>
                {" → "}
                <span>{floorLabel(entry.destinationFloor)}</span>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
