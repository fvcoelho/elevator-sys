"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import type { SystemStatusDto } from "@/types/elevator";

interface StatusBarProps {
  status: SystemStatusDto | null;
  isConnected: boolean;
}

export function StatusBar({ status, isConnected }: StatusBarProps) {
  return (
    <Card>
      <CardContent className="flex items-center gap-4 py-3">
        <Badge variant={isConnected ? "default" : "destructive"}>
          {isConnected ? "Connected" : "Disconnected"}
        </Badge>

        <Separator orientation="vertical" className="h-5" />

        {status ? (
          <>
            <span className="text-sm text-muted-foreground">
              Elevators:{" "}
              <span className="font-medium text-foreground">
                {status.elevatorCount}
              </span>
            </span>

            <Separator orientation="vertical" className="h-5" />

            <span className="text-sm text-muted-foreground">
              Pending:{" "}
              <span className="font-medium text-foreground">
                {status.pendingRequests}
              </span>
            </span>

            <Separator orientation="vertical" className="h-5" />

            <span className="text-sm text-muted-foreground">
              Algorithm:{" "}
              <span className="font-medium text-foreground">
                {status.algorithm}
              </span>
            </span>

            {status.isEmergencyStopped && (
              <>
                <Separator orientation="vertical" className="h-5" />
                <Badge variant="destructive">EMERGENCY STOP</Badge>
              </>
            )}
          </>
        ) : (
          <span className="text-sm text-muted-foreground">
            Waiting for data...
          </span>
        )}
      </CardContent>
    </Card>
  );
}
