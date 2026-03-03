"use client";

import { useEffect, useRef, useState } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { MemoryStick } from "lucide-react";
import type { SystemStatusDto, DispatchAlgorithm, MetricsDto } from "@/types/elevator";

function formatBytes(bytes: number): string {
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface StatusBarProps {
  status: SystemStatusDto | null;
  isConnected: boolean;
  messageCount?: number;
  isEmergencyStopped: boolean;
  onEmergencyStop: () => Promise<unknown>;
  onEmergencyResume: () => Promise<unknown>;
  currentAlgorithm: string;
  onSetAlgorithm: (alg: DispatchAlgorithm) => Promise<unknown>;
  onGetMetrics: () => Promise<MetricsDto>;
  onResetServer: () => Promise<unknown>;
}

export function StatusBar({
  status,
  isConnected,
  messageCount = 0,
  isEmergencyStopped,
  onEmergencyStop,
  onEmergencyResume,
  currentAlgorithm,
  onSetAlgorithm,
  onGetMetrics,
  onResetServer,
}: StatusBarProps) {
  const [blink, setBlink] = useState(false);
  const prevCount = useRef(messageCount);
  const [metrics, setMetrics] = useState<MetricsDto | null>(null);
  const [loadingMetrics, setLoadingMetrics] = useState(false);

  useEffect(() => {
    if (messageCount > prevCount.current) {
      prevCount.current = messageCount;
      setBlink(true);
      const timer = setTimeout(() => setBlink(false), 200);
      return () => clearTimeout(timer);
    }
  }, [messageCount]);

  const handleLoadMetrics = async () => {
    setLoadingMetrics(true);
    try {
      const m = await onGetMetrics();
      setMetrics(m);
    } catch {
      // silently ignore
    } finally {
      setLoadingMetrics(false);
    }
  };

  return (
    <Card>
      <CardContent className="flex flex-wrap items-center gap-x-4 gap-y-2 py-3">
        <div className="inline-flex items-center gap-4">
          <Badge
            variant={isConnected ? "default" : "destructive"}
            className={`transition-opacity duration-150 ${blink ? "opacity-40" : "opacity-100"}`}
          >
            {isConnected ? "Connected" : "Disconnected"}
          </Badge>

          {status && (
            <span className="text-sm text-muted-foreground">
              Elevators:{" "}
              <span className="font-medium text-foreground">
                {status.elevatorCount}
              </span>
            </span>
          )}
        </div>

        <Separator orientation="vertical" className="h-5 hidden sm:block" />

        {status ? (
          <>
            <div className="inline-flex items-center gap-4">
              <Select
                value={currentAlgorithm}
                onValueChange={(v) => onSetAlgorithm(v as DispatchAlgorithm)}
              >
                <SelectTrigger className="h-7 w-[120px] text-sm">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Simple">Simple</SelectItem>
                  <SelectItem value="SCAN">SCAN</SelectItem>
                  <SelectItem value="LOOK">LOOK</SelectItem>
                  <SelectItem value="Custom">Custom</SelectItem>
                </SelectContent>
              </Select>

              <span className="text-sm text-muted-foreground inline-flex items-center gap-1">
                <MemoryStick className="h-3.5 w-3.5" />
                Memory:{" "}
                <span className="font-medium text-foreground">
                  {formatBytes(status.memoryUsedBytes)}
                </span>
              </span>
            </div>

            <Separator orientation="vertical" className="h-5 hidden sm:block" />

            <Popover>
              <PopoverTrigger asChild>
                <Button variant="outline" size="sm" className="h-7 text-sm">
                  Metrics
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-64">
                <div className="space-y-2">
                  <Button
                    variant="outline"
                    size="sm"
                    className="w-full"
                    onClick={handleLoadMetrics}
                    disabled={loadingMetrics}
                  >
                    {loadingMetrics ? "Loading..." : "Load Metrics"}
                  </Button>

                  {metrics && (
                    <div className="rounded border p-3 text-xs space-y-1 bg-muted/50">
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Total Requests</span>
                        <span className="font-medium">{metrics.totalRequests}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Completed</span>
                        <span className="font-medium">{metrics.completedRequests}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Avg Wait</span>
                        <span className="font-medium">
                          {metrics.averageWaitTimeMs.toFixed(0)}ms
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Avg Ride</span>
                        <span className="font-medium">
                          {metrics.averageRideTimeMs.toFixed(0)}ms
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Utilization</span>
                        <span className="font-medium">
                          {(metrics.systemUtilization * 100).toFixed(1)}%
                        </span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Peak Concurrent</span>
                        <span className="font-medium">
                          {metrics.peakConcurrentRequests}
                        </span>
                      </div>

                      {Object.keys(metrics.elevatorStats).length > 0 && (
                        <>
                          <Separator className="my-1" />
                          {Object.entries(metrics.elevatorStats).map(
                            ([key, stat]) => (
                              <div key={key} className="flex justify-between">
                                <span className="text-muted-foreground">
                                  {stat.label}
                                </span>
                                <span className="font-medium">
                                  {stat.tripsCompleted} trips,{" "}
                                  {(stat.utilization * 100).toFixed(0)}%
                                </span>
                              </div>
                            )
                          )}
                        </>
                      )}
                    </div>
                  )}
                </div>
              </PopoverContent>
            </Popover>

            <Separator orientation="vertical" className="h-5 hidden sm:block" />

            <div className="flex gap-1.5 ml-auto">
              <Button
                variant="secondary"
                size="sm"
                className="h-7 text-sm"
                onClick={() => onResetServer()}
              >
                Reset
              </Button>
              <Button
                variant="destructive"
                size="sm"
                className="h-7 text-sm"
                disabled={isEmergencyStopped}
                onClick={() => onEmergencyStop()}
              >
                Stop
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="h-7 text-sm"
                disabled={!isEmergencyStopped}
                onClick={() => onEmergencyResume()}
              >
                Resume
              </Button>
            </div>

            {isEmergencyStopped && (
              <Badge variant="destructive">EMERGENCY STOP</Badge>
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
