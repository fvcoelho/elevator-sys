"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectRequestLog } from "@/store/slices/passengersSlice";

function floorLabel(floor: number): string {
  return floor === 1 ? "Lobby" : `F${floor}`;
}

const PRIORITY_STYLES: Record<string, string> = {
  High:    "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  VIP:     "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
  Freight: "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400",
};

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
              <div key={i} className="text-xs font-mono leading-snug flex items-center gap-1">
                <span className="text-muted-foreground w-8 shrink-0">
                  {entry.requestId !== undefined ? `#${entry.requestId}` : "#?"}
                </span>
                <span className="font-semibold">{entry.name}</span>
                <span className="text-muted-foreground">→</span>
                <span>{floorLabel(entry.pickupFloor)}</span>
                <span className="text-muted-foreground">→</span>
                <span>{floorLabel(entry.destinationFloor)}</span>
                {entry.priorityMode && entry.priorityMode !== "Normal" && (
                  <span className={`ml-auto shrink-0 rounded px-1 py-0 text-[10px] font-sans font-medium ${PRIORITY_STYLES[entry.priorityMode] ?? ""}`}>
                    {entry.priorityMode}
                  </span>
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
