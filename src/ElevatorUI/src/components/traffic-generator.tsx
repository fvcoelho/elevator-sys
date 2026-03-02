"use client";

import { useCallback, useRef, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import type { CreateRequestDto, RequestResponseDto } from "@/types/elevator";
import { randomName } from "@/lib/passenger-names";

const MIN_FLOOR = 1;
const MAX_FLOOR = 20;

type StandardMode = "light" | "moderate" | "rush";
type TrafficMode = StandardMode | "realistic";

interface TrafficConfig {
  label: string;
  description: string;
  durationSec: number;
  delayRange: [number, number];
  pattern: (rand: () => number) => { pickup: number; destination: number };
}

function randomFloor(): number {
  return Math.floor(Math.random() * (MAX_FLOOR - MIN_FLOOR + 1)) + MIN_FLOOR;
}

function randomRange(min: number, max: number): number {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

const CONFIGS: Record<StandardMode, TrafficConfig> = {
  light: {
    label: "Light",
    description: "1-2 req/sec, ~20 requests, 15s",
    durationSec: 15,
    delayRange: [500, 1000],
    pattern: () => {
      const pickup = randomFloor();
      let destination = randomFloor();
      while (destination === pickup) destination = randomFloor();
      return { pickup, destination };
    },
  },
  moderate: {
    label: "Moderate",
    description: "3-5 req/sec, ~50 requests, 15s",
    durationSec: 15,
    delayRange: [200, 350],
    pattern: () => {
      const roll = Math.random() * 100;
      if (roll < 40) {
        return { pickup: 1, destination: randomRange(5, MAX_FLOOR) };
      } else if (roll < 80) {
        return { pickup: randomRange(5, MAX_FLOOR), destination: 1 };
      } else {
        const pickup = randomFloor();
        let destination = randomFloor();
        while (destination === pickup) destination = randomFloor();
        return { pickup, destination };
      }
    },
  },
  rush: {
    label: "Rush Hour",
    description: "8-15 req/sec, ~150 requests, 20s",
    durationSec: 20,
    delayRange: [65, 125],
    pattern: () => {
      const roll = Math.random() * 100;
      if (roll < 60) {
        return { pickup: 1, destination: randomRange(2, MAX_FLOOR) };
      } else if (roll < 90) {
        return { pickup: randomRange(2, MAX_FLOOR), destination: 1 };
      } else {
        const pickup = randomFloor();
        let destination = randomFloor();
        while (destination === pickup) destination = randomFloor();
        return { pickup, destination };
      }
    },
  },
};

const REALISTIC_CONFIG = {
  label: "Realistic",
  description: "Lobby → floor, delay, floor → lobby",
  peopleDurationSec: 30,
  spawnDelayRange: [1000, 3000] as [number, number],
  stayDelayRange: [5000, 15000] as [number, number],
};

interface LogEntry {
  index: number;
  pickup: number;
  destination: number;
  ok: boolean;
}

interface TrafficGeneratorProps {
  onRequestRide: (dto: CreateRequestDto) => Promise<RequestResponseDto>;
  onPassengerAdded?: (name: string, pickup: number, destination: number, requestId?: number) => void;
}

export function TrafficGenerator({ onRequestRide, onPassengerAdded }: TrafficGeneratorProps) {
  const [running, setRunning] = useState(false);
  const [activeMode, setActiveMode] = useState<TrafficMode | null>(null);
  const [requestCount, setRequestCount] = useState(0);
  const [failCount, setFailCount] = useState(0);
  const [elapsed, setElapsed] = useState(0);
  const [log, setLog] = useState<LogEntry[]>([]);
  const cancelRef = useRef(false);

  const generateStandard = useCallback(
    async (mode: Exclude<TrafficMode, "realistic">) => {
      const config = CONFIGS[mode];
      cancelRef.current = false;
      setRunning(true);
      setActiveMode(mode);
      setRequestCount(0);
      setFailCount(0);
      setElapsed(0);
      setLog([]);

      const start = Date.now();
      const durationMs = config.durationSec * 1000;
      let count = 0;
      let fails = 0;

      const timer = setInterval(() => {
        setElapsed(Math.floor((Date.now() - start) / 1000));
      }, 500);

      while (Date.now() - start < durationMs && !cancelRef.current) {
        const { pickup, destination } = config.pattern(Math.random);
        const name = randomName();
        count++;

        try {
          const res = await onRequestRide({
            pickupFloor: pickup,
            destinationFloor: destination,
          });
          onPassengerAdded?.(name, pickup, destination, res.requestId);
          setLog((prev) => [
            { index: count, pickup, destination, ok: true },
            ...prev.slice(0, 49),
          ]);
        } catch {
          fails++;
          setLog((prev) => [
            { index: count, pickup, destination, ok: false },
            ...prev.slice(0, 49),
          ]);
        }

        setRequestCount(count);
        setFailCount(fails);

        const [min, max] = config.delayRange;
        await new Promise((r) => setTimeout(r, randomRange(min, max)));
      }

      clearInterval(timer);
      setElapsed(Math.floor((Date.now() - start) / 1000));
      setRunning(false);
      setActiveMode(null);
    },
    [onRequestRide, onPassengerAdded]
  );

  const generateRealistic = useCallback(async () => {
    cancelRef.current = false;
    setRunning(true);
    setActiveMode("realistic");
    setRequestCount(0);
    setFailCount(0);
    setElapsed(0);
    setLog([]);

    const start = Date.now();
    const spawnDuration =
      REALISTIC_CONFIG.peopleDurationSec * 1000;
    let count = 0;
    let fails = 0;
    const pendingReturns: Promise<void>[] = [];

    const timer = setInterval(() => {
      setElapsed(Math.floor((Date.now() - start) / 1000));
    }, 500);

    // Spawn people: lobby -> random floor, then schedule return trip
    while (Date.now() - start < spawnDuration && !cancelRef.current) {
      const floor = randomRange(2, MAX_FLOOR);
      const name = randomName();
      count++;
      const idx = count;

      // Send lobby -> floor
      try {
        const res = await onRequestRide({ pickupFloor: 1, destinationFloor: floor });
        onPassengerAdded?.(name, 1, floor, res.requestId);
        setLog((prev) => [
          { index: idx, pickup: 1, destination: floor, ok: true },
          ...prev.slice(0, 49),
        ]);
      } catch {
        fails++;
        setLog((prev) => [
          { index: idx, pickup: 1, destination: floor, ok: false },
          ...prev.slice(0, 49),
        ]);
      }

      setRequestCount(count);
      setFailCount(fails);

      // Schedule return trip: floor -> lobby after a random stay delay
      const capturedFloor = floor;
      const capturedName = name;
      const returnPromise = (async () => {
        const [sMin, sMax] = REALISTIC_CONFIG.stayDelayRange;
        await new Promise((r) => setTimeout(r, randomRange(sMin, sMax)));
        if (cancelRef.current) return;

        count++;
        const retIdx = count;
        try {
          const retRes = await onRequestRide({
            pickupFloor: capturedFloor,
            destinationFloor: 1,
          });
          onPassengerAdded?.(capturedName, capturedFloor, 1, retRes.requestId);
          setLog((prev) => [
            { index: retIdx, pickup: capturedFloor, destination: 1, ok: true },
            ...prev.slice(0, 49),
          ]);
        } catch {
          fails++;
          setLog((prev) => [
            {
              index: retIdx,
              pickup: capturedFloor,
              destination: 1,
              ok: false,
            },
            ...prev.slice(0, 49),
          ]);
        }
        setRequestCount(count);
        setFailCount(fails);
      })();
      pendingReturns.push(returnPromise);

      // Delay before spawning next person
      const [dMin, dMax] = REALISTIC_CONFIG.spawnDelayRange;
      await new Promise((r) => setTimeout(r, randomRange(dMin, dMax)));
    }

    // Wait for all return trips to complete
    await Promise.all(pendingReturns);

    clearInterval(timer);
    setElapsed(Math.floor((Date.now() - start) / 1000));
    setRunning(false);
    setActiveMode(null);
  }, [onRequestRide, onPassengerAdded]);

  const generate = useCallback(
    (mode: TrafficMode) => {
      if (mode === "realistic") {
        generateRealistic();
      } else {
        generateStandard(mode);
      }
    },
    [generateStandard, generateRealistic]
  );

  const handleCancel = () => {
    cancelRef.current = true;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Traffic Generator</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="grid grid-cols-2 gap-2">
          {(Object.entries(CONFIGS) as [StandardMode, TrafficConfig][]).map(
            ([mode, config]) => (
              <Button
                key={mode}
                variant={activeMode === mode ? "default" : "outline"}
                size="sm"
                disabled={running}
                onClick={() => generate(mode)}
              >
                {config.label}
              </Button>
            )
          )}
          <Button
            variant={activeMode === "realistic" ? "default" : "outline"}
            size="sm"
            className="col-span-2"
            disabled={running}
            onClick={() => generate("realistic")}
          >
            {REALISTIC_CONFIG.label}
          </Button>
        </div>

        {running && (
          <Button
            variant="destructive"
            size="sm"
            className="w-full"
            onClick={handleCancel}
          >
            Stop
          </Button>
        )}

        {(running || requestCount > 0) && (
          <>
            <Separator />
            <div className="flex items-center gap-2 flex-wrap">
              {running && (
                <Badge variant="default" className="animate-pulse">
                  Running
                </Badge>
              )}
              {!running && requestCount > 0 && (
                <Badge variant="secondary">Done</Badge>
              )}
              <span className="text-xs text-muted-foreground">
                {requestCount} sent
                {failCount > 0 && (
                  <span className="text-red-500"> ({failCount} failed)</span>
                )}
                {" | "}
                {elapsed}s elapsed
              </span>
            </div>
          </>
        )}

        {log.length > 0 && (
          <div className="max-h-40 overflow-y-auto rounded border bg-muted/50 p-2 text-xs font-mono space-y-0.5">
            {log.map((entry) => (
              <div
                key={entry.index}
                className={entry.ok ? "" : "text-red-500"}
              >
                [{entry.index}] {entry.pickup} → {entry.destination}
                {!entry.ok && " ✗"}
              </div>
            ))}
          </div>
        )}

        <p className="text-xs text-muted-foreground">
          Light: random floors. Moderate: 40% lobby↑ 40% ↓lobby 20% inter.
          Rush: 60% lobby↑ 30% ↓lobby 10% inter. Realistic: lobby→floor,
          wait 5-15s, then floor→lobby.
        </p>
      </CardContent>
    </Card>
  );
}
