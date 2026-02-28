"use client";

import { useState } from "react";
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

interface ElevatorPanelProps {
  onRequestRide: (dto: CreateRequestDto) => Promise<RequestResponseDto>;
}

export function ElevatorPanel({ onRequestRide }: ElevatorPanelProps) {
  const [pickup, setPickup] = useState("");
  const [destination, setDestination] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [feedback, setFeedback] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    const pickupFloor = parseInt(pickup, 10);
    const destFloor = parseInt(destination, 10);

    if (isNaN(pickupFloor) || pickupFloor < 1 || pickupFloor > 20) {
      setFeedback({ type: "error", message: "Pickup floor must be 1-20" });
      return;
    }
    if (isNaN(destFloor) || destFloor < 1 || destFloor > 20) {
      setFeedback({
        type: "error",
        message: "Destination floor must be 1-20",
      });
      return;
    }
    if (pickupFloor === destFloor) {
      setFeedback({
        type: "error",
        message: "Pickup and destination must differ",
      });
      return;
    }

    setSubmitting(true);
    setFeedback(null);

    try {
      const res = await onRequestRide({
        pickupFloor,
        destinationFloor: destFloor,
        priority,
      });
      setFeedback({
        type: "success",
        message: `Ride #${res.requestId}: Floor ${res.pickupFloor} → ${res.destinationFloor}`,
      });
      setPickup("");
      setDestination("");
    } catch (err) {
      setFeedback({
        type: "error",
        message: err instanceof Error ? err.message : "Request failed",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Request Ride</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="space-y-1">
          <Label htmlFor="pickup">Pickup Floor</Label>
          <Input
            id="pickup"
            type="number"
            min={1}
            max={20}
            placeholder="1-20"
            value={pickup}
            onChange={(e) => setPickup(e.target.value)}
          />
        </div>

        <div className="space-y-1">
          <Label htmlFor="destination">Destination Floor</Label>
          <Input
            id="destination"
            type="number"
            min={1}
            max={20}
            placeholder="1-20"
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
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
            </SelectContent>
          </Select>
        </div>

        <Button
          className="w-full"
          onClick={handleSubmit}
          disabled={submitting}
        >
          {submitting ? "Sending..." : "Request Ride"}
        </Button>

        {feedback && (
          <p
            className={`text-sm ${feedback.type === "success" ? "text-green-600" : "text-red-600"}`}
          >
            {feedback.message}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
