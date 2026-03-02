"use client";

import { useCallback, useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { CreateRequestDto, RequestResponseDto } from "@/types/elevator";
import { randomName } from "@/lib/passenger-names";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectTotalPeople } from "@/store/slices/passengersSlice";

// --- Props ---

interface ElevatorPanelProps {
  onRequestRide: (dto: CreateRequestDto) => Promise<RequestResponseDto>;
  onPassengerAdded?: (
    name: string,
    pickup: number,
    destination: number,
    returnDelaySec?: number,
    requestId?: number,
    priorityMode?: string
  ) => void;
  onClearPassengers?: () => void;
}

// --- Helpers ---

function randInt(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randomFloor(): string {
  return String(randInt(2, 20));
}

function randomDelaySec(): string {
  return String(randInt(5, 15));
}

// --- Log entry ---

interface LogEntry {
  id: number;
  text: string;
  ok: boolean;
}

// --- Component ---

export function ElevatorPanel({
  onRequestRide,
  onPassengerAdded,
  onClearPassengers,
}: ElevatorPanelProps) {
  const [passengerName, setPassengerName] = useState("");
  const [destination, setDestination] = useState("");
  const [delay, setDelay] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [log, setLog] = useState<LogEntry[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const totalPeople = useAppSelector(selectTotalPeople);

  // Avoid SSR hydration mismatch — randomize after mount
  useEffect(() => {
    setPassengerName(randomName());
    setDestination(randomFloor());
    setDelay(randomDelaySec());
  }, []);

  const rollAll = useCallback(() => {
    setPassengerName(randomName());
    setDestination(randomFloor());
    setDelay(randomDelaySec());
  }, []);

  const addLog = useCallback((text: string, ok: boolean) => {
    setLog((prev) => [{ id: Date.now(), text, ok }, ...prev.slice(0, 49)]);
  }, []);

  const handleSubmit = async () => {
    const destFloor = parseInt(destination, 10);
    const delaySec = parseInt(delay, 10) || 10;

    if (isNaN(destFloor) || destFloor < 2 || destFloor > 20) {
      addLog("Floor must be 2-20", false);
      return;
    }

    setSubmitting(true);
    const name = passengerName;

    const dto: Parameters<typeof onRequestRide>[0] = {
      pickupFloor: 1,
      destinationFloor: destFloor,
    };
    if (priority === "High") dto.priority = "High";
    else if (priority === "VIP") dto.accessLevel = "VIP";
    else if (priority === "Freight") dto.preferredElevatorType = "Freight";

    try {
      const res = await onRequestRide(dto);
      onPassengerAdded?.(name, 1, destFloor, delaySec, res.requestId, priority);
      addLog(
        `${name} → #${res.requestId}: Lobby → F${res.destinationFloor} (${delaySec}s) → Lobby`,
        true
      );

      // Re-roll for next request
      setPassengerName(randomName());
      setDestination(randomFloor());
      setDelay(randomDelaySec());
    } catch (err) {
      addLog(
        err instanceof Error ? err.message : "Request failed",
        false
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-base">Request Round-Trip Ride</CardTitle>
      </CardHeader>

      <CardContent className="space-y-3">
        {/* Passenger + Randomize */}
        <div className="flex gap-2 items-end">
          <div className="space-y-1 flex-1">
            <Label htmlFor="passenger">Passenger</Label>
            <Input
              id="passenger"
              value={passengerName}
              onChange={(e) => setPassengerName(e.target.value)}
            />
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={rollAll}
            type="button"
            className="shrink-0"
          >
            Randomize
          </Button>
        </div>

        {/* Floor, Delay, Priority */}
        <div className="grid grid-cols-3 gap-2">
          <div className="space-y-1">
            <Label htmlFor="destination">Floor</Label>
            <Input
              id="destination"
              type="number"
              min={2}
              max={20}
              value={destination}
              onChange={(e) => setDestination(e.target.value)}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="delay">Delay (s)</Label>
            <Input
              id="delay"
              type="number"
              min={1}
              value={delay}
              onChange={(e) => setDelay(e.target.value)}
            />
          </div>
          <div className="space-y-1">
            <Label>Priority</Label>
            <Select value={priority} onValueChange={setPriority}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Normal">Normal</SelectItem>
                <SelectItem value="High">High</SelectItem>
                <SelectItem value="VIP">VIP</SelectItem>
                <SelectItem value="Freight">Freight</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">
          Lobby → floor, stay {delay || "?"}s, then return to lobby.
        </p>

        <div className="flex gap-2">
          <Button
            className="flex-1"
            onClick={handleSubmit}
            disabled={submitting}
          >
            {submitting ? "Sending..." : "Request"}
          </Button>
          <Button
            variant="outline"
            size="default"
            className="shrink-0"
            disabled={log.length === 0 && totalPeople === 0}
            onClick={() => {
              setLog([]);
              onClearPassengers?.();
            }}
          >
            Clear
          </Button>
        </div>

        {/* Log */}
        {log.length > 0 && (
          <div className="max-h-40 overflow-y-auto rounded border bg-muted/50 p-2 text-xs font-mono space-y-0.5">
            {log.map((entry) => (
              <div
                key={entry.id}
                className={entry.ok ? "" : "text-red-500"}
              >
                {entry.text}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
