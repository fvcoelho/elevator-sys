"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Label } from "@/components/ui/label";
import type { DispatchAlgorithm, MetricsDto } from "@/types/elevator";

interface SystemControlsProps {
  currentAlgorithm: string;
  isEmergencyStopped: boolean;
  onEmergencyStop: () => Promise<unknown>;
  onEmergencyResume: () => Promise<unknown>;
  onSetAlgorithm: (alg: DispatchAlgorithm) => Promise<unknown>;
  onGetMetrics: () => Promise<MetricsDto>;
}

export function SystemControls({
  currentAlgorithm,
  isEmergencyStopped,
  onEmergencyStop,
  onEmergencyResume,
  onSetAlgorithm,
  onGetMetrics,
}: SystemControlsProps) {
  const [metrics, setMetrics] = useState<MetricsDto | null>(null);
  const [loadingMetrics, setLoadingMetrics] = useState(false);

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
      <CardHeader>
        <CardTitle className="text-base">System Controls</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label>Emergency</Label>
          <div className="flex gap-2">
            <Button
              variant="destructive"
              size="sm"
              className="flex-1"
              disabled={isEmergencyStopped}
              onClick={() => onEmergencyStop()}
            >
              Stop
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="flex-1"
              disabled={!isEmergencyStopped}
              onClick={() => onEmergencyResume()}
            >
              Resume
            </Button>
          </div>
        </div>

        <Separator />

        <div className="space-y-2">
          <Label>Dispatch Algorithm</Label>
          <Select
            value={currentAlgorithm}
            onValueChange={(v) => onSetAlgorithm(v as DispatchAlgorithm)}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Simple">Simple</SelectItem>
              <SelectItem value="SCAN">SCAN</SelectItem>
              <SelectItem value="LOOK">LOOK</SelectItem>
              <SelectItem value="Custom">Custom</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <Separator />

        <div className="space-y-2">
          <Label>Metrics</Label>
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
      </CardContent>
    </Card>
  );
}
