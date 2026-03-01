"use client";

import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
import type {
  SystemStatusDto,
  UpdateConfigDto,
  AddElevatorDto,
  ElevatorConfigDto,
  DispatchAlgorithm,
} from "@/types/elevator";

interface SystemConfigProps {
  status: SystemStatusDto | null;
  onUpdateConfig: (dto: UpdateConfigDto) => Promise<unknown>;
  onAddElevator: (dto: AddElevatorDto) => Promise<unknown>;
}

const DEFAULT_CONFIG: UpdateConfigDto = {
  minFloor: 1,
  maxFloor: 20,
  doorOpenMs: 3000,
  floorTravelMs: 1500,
  doorTransitionMs: 1000,
  algorithm: "Custom",
  vipFloors: [],
  elevators: [
    { label: "Elevator A", initialFloor: 1, type: "Local", capacity: 10, servedFloors: null },
    { label: "Elevator B", initialFloor: 10, type: "Local", capacity: 10, servedFloors: null },
    { label: "Elevator C", initialFloor: 20, type: "Local", capacity: 10, servedFloors: null },
  ],
};

export function SystemConfig({
  status,
  onUpdateConfig,
}: SystemConfigProps) {
  const [expanded, setExpanded] = useState(false);
  const [config, setConfig] = useState<UpdateConfigDto>(DEFAULT_CONFIG);
  const [initialized, setInitialized] = useState(false);
  const [applying, setApplying] = useState(false);
  const [feedback, setFeedback] = useState<{ ok: boolean; msg: string } | null>(null);

  // Add elevator dialog state
  const [dialogOpen, setDialogOpen] = useState(false);
  const [newLabel, setNewLabel] = useState("");
  const [newFloor, setNewFloor] = useState(1);
  const [newType, setNewType] = useState("Local");
  const [newCapacity, setNewCapacity] = useState(10);

  // Sync from status on first expand
  useEffect(() => {
    if (expanded && !initialized && status) {
      setConfig((prev) => ({
        ...prev,
        algorithm: status.algorithm,
        elevators: status.elevators.map((e) => ({
          label: e.label,
          initialFloor: e.currentFloor,
          type: e.type,
          capacity: e.capacity,
          servedFloors: e.servedFloors,
        })),
      }));
      setInitialized(true);
    }
  }, [expanded, initialized, status]);

  const handleApply = async () => {
    setApplying(true);
    setFeedback(null);
    try {
      await onUpdateConfig(config);
      setFeedback({ ok: true, msg: "Configuration applied" });
      setInitialized(false); // Re-sync on next expand
    } catch (err) {
      setFeedback({
        ok: false,
        msg: err instanceof Error ? err.message : "Failed to apply config",
      });
    } finally {
      setApplying(false);
    }
  };

  const handleAddElevator = () => {
    const elevator: ElevatorConfigDto = {
      label: newLabel || `Elevator ${String.fromCharCode(65 + config.elevators.length)}`,
      initialFloor: newFloor,
      type: newType,
      capacity: newCapacity,
      servedFloors: null,
    };
    setConfig((prev) => ({
      ...prev,
      elevators: [...prev.elevators, elevator],
    }));
    // Reset dialog
    setNewLabel("");
    setNewFloor(1);
    setNewType("Local");
    setNewCapacity(10);
    setDialogOpen(false);
  };

  const handleRemoveElevator = (index: number) => {
    setConfig((prev) => ({
      ...prev,
      elevators: prev.elevators.filter((_, i) => i !== index),
    }));
  };

  const vipFloorsStr = config.vipFloors.join(", ");

  return (
    <Card>
      <CardHeader
        className="cursor-pointer"
        onClick={() => setExpanded(!expanded)}
      >
        <CardTitle className="text-base flex items-center justify-between">
          System Configuration
          <span className="text-xs text-muted-foreground font-normal">
            {expanded ? "collapse" : "expand"}
          </span>
        </CardTitle>
      </CardHeader>

      {expanded && (
        <CardContent className="space-y-4">
          {/* Floor Range */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">Floor Range</Label>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Min Floor</Label>
                <Input
                  type="number"
                  value={config.minFloor}
                  onChange={(e) =>
                    setConfig((prev) => ({ ...prev, minFloor: parseInt(e.target.value) || 1 }))
                  }
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Max Floor</Label>
                <Input
                  type="number"
                  value={config.maxFloor}
                  onChange={(e) =>
                    setConfig((prev) => ({ ...prev, maxFloor: parseInt(e.target.value) || 20 }))
                  }
                />
              </div>
            </div>
          </div>

          <Separator />

          {/* Timing */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">Timing</Label>
            <div className="space-y-2">
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Door Open (ms)</Label>
                <Input
                  type="number"
                  value={config.doorOpenMs}
                  onChange={(e) =>
                    setConfig((prev) => ({ ...prev, doorOpenMs: parseInt(e.target.value) || 3000 }))
                  }
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Floor Travel (ms)</Label>
                <Input
                  type="number"
                  value={config.floorTravelMs}
                  onChange={(e) =>
                    setConfig((prev) => ({
                      ...prev,
                      floorTravelMs: parseInt(e.target.value) || 1500,
                    }))
                  }
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs text-muted-foreground">Door Transition (ms)</Label>
                <Input
                  type="number"
                  value={config.doorTransitionMs}
                  onChange={(e) =>
                    setConfig((prev) => ({
                      ...prev,
                      doorTransitionMs: parseInt(e.target.value) || 1000,
                    }))
                  }
                />
              </div>
            </div>
          </div>

          <Separator />

          {/* Algorithm */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">Algorithm</Label>
            <Select
              value={config.algorithm}
              onValueChange={(v) =>
                setConfig((prev) => ({ ...prev, algorithm: v as DispatchAlgorithm }))
              }
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

          {/* VIP Floors */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">VIP Floors</Label>
            <Input
              placeholder="e.g. 13, 14"
              value={vipFloorsStr}
              onChange={(e) => {
                const floors = e.target.value
                  .split(",")
                  .map((s) => parseInt(s.trim()))
                  .filter((n) => !isNaN(n));
                setConfig((prev) => ({ ...prev, vipFloors: floors }));
              }}
            />
            <p className="text-xs text-muted-foreground">Comma-separated floor numbers</p>
          </div>

          <Separator />

          {/* Elevators */}
          <div className="space-y-2">
            <Label className="text-sm font-medium">
              Elevators ({config.elevators.length})
            </Label>

            <div className="space-y-2">
              {config.elevators.map((elev, i) => (
                <div
                  key={i}
                  className="flex items-center gap-2 rounded border p-2 text-xs bg-muted/50"
                >
                  <div className="flex-1 min-w-0">
                    <div className="font-medium truncate">{elev.label}</div>
                    <div className="text-muted-foreground">
                      Floor {elev.initialFloor} &middot; Cap {elev.capacity}
                    </div>
                  </div>
                  <Badge variant="outline" className="text-[10px] shrink-0">
                    {elev.type}
                  </Badge>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 w-6 p-0 shrink-0 text-muted-foreground hover:text-destructive"
                    disabled={config.elevators.length <= 1}
                    onClick={() => handleRemoveElevator(i)}
                  >
                    x
                  </Button>
                </div>
              ))}
            </div>

            <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
              <DialogTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full"
                  disabled={config.elevators.length >= 10}
                >
                  Add Elevator
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Add Elevator</DialogTitle>
                </DialogHeader>
                <div className="space-y-3 py-2">
                  <div className="space-y-1">
                    <Label>Label</Label>
                    <Input
                      placeholder={`Elevator ${String.fromCharCode(65 + config.elevators.length)}`}
                      value={newLabel}
                      onChange={(e) => setNewLabel(e.target.value)}
                    />
                  </div>
                  <div className="space-y-1">
                    <Label>Initial Floor</Label>
                    <Input
                      type="number"
                      value={newFloor}
                      onChange={(e) => setNewFloor(parseInt(e.target.value) || 1)}
                    />
                  </div>
                  <div className="space-y-1">
                    <Label>Type</Label>
                    <Select value={newType} onValueChange={setNewType}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Local">Local</SelectItem>
                        <SelectItem value="Express">Express</SelectItem>
                        <SelectItem value="Freight">Freight</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-1">
                    <Label>Capacity</Label>
                    <Input
                      type="number"
                      value={newCapacity}
                      onChange={(e) => setNewCapacity(parseInt(e.target.value) || 10)}
                    />
                  </div>
                </div>
                <DialogFooter>
                  <DialogClose asChild>
                    <Button variant="outline" size="sm">
                      Cancel
                    </Button>
                  </DialogClose>
                  <Button size="sm" onClick={handleAddElevator}>
                    Add
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </div>

          <Separator />

          {/* Apply / Reset */}
          <div className="flex gap-2">
            <Button
              variant="outline"
              className="flex-1"
              disabled={applying}
              onClick={() => {
                setConfig(DEFAULT_CONFIG);
                setFeedback(null);
              }}
            >
              Reset to Defaults
            </Button>
            <Button
              className="flex-1"
              disabled={applying || config.elevators.length === 0}
              onClick={handleApply}
            >
              {applying ? "Applying..." : "Apply"}
            </Button>
          </div>

          {feedback && (
            <p
              className={`text-xs text-center ${
                feedback.ok ? "text-green-600" : "text-red-500"
              }`}
            >
              {feedback.msg}
            </p>
          )}
        </CardContent>
      )}
    </Card>
  );
}
