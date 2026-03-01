"use client";

import { useCallback, useRef, useState } from "react";
import type { ElevatorDto } from "@/types/elevator";

export type PassengerStatus = "waiting" | "riding" | "arrived";

export interface Passenger {
  id: number;
  name: string;
  pickupFloor: number;
  destinationFloor: number;
  status: PassengerStatus;
  currentFloor: number;
}

export function usePassengers() {
  const [passengers, setPassengers] = useState<Passenger[]>([]);
  const nextId = useRef(1);

  const addPassenger = useCallback(
    (name: string, pickupFloor: number, destinationFloor: number) => {
      const passenger: Passenger = {
        id: nextId.current++,
        name,
        pickupFloor,
        destinationFloor,
        status: "waiting",
        currentFloor: pickupFloor,
      };
      setPassengers((prev) => [...prev, passenger]);
    },
    []
  );

  // Sync passenger state based on elevator positions from WebSocket
  const syncWithElevators = useCallback((elevators: ElevatorDto[]) => {
    setPassengers((prev) => {
      let changed = false;
      const next = prev.map((p) => {
        if (p.status === "waiting") {
          // Check if any elevator is at the pickup floor with doors open
          const atPickup = elevators.some(
            (e) =>
              e.currentFloor === p.pickupFloor &&
              (e.state === "DOOR_OPEN" ||
                e.state === "DOOR_CLOSING" ||
                e.state === "MOVING_UP" ||
                e.state === "MOVING_DOWN")
          );
          if (atPickup) {
            changed = true;
            return { ...p, status: "riding" as const };
          }
        }
        if (p.status === "riding") {
          // Check if any elevator is at the destination floor with doors open
          const atDest = elevators.some(
            (e) =>
              e.currentFloor === p.destinationFloor &&
              (e.state === "DOOR_OPEN" || e.state === "DOOR_OPENING")
          );
          if (atDest) {
            changed = true;
            return {
              ...p,
              status: "arrived" as const,
              currentFloor: p.destinationFloor,
            };
          }
        }
        return p;
      });

      if (!changed) return prev;

      // Remove arrived passengers after a delay by marking them
      return next;
    });
  }, []);

  // Get passengers on a floor (waiting or arrived)
  const getByFloor = useCallback(
    (floor: number): Passenger[] => {
      return passengers.filter(
        (p) =>
          (p.status === "waiting" && p.currentFloor === floor) ||
          (p.status === "arrived" && p.currentFloor === floor)
      );
    },
    [passengers]
  );

  // Get passengers currently riding in elevators
  const getRiding = useCallback((): Passenger[] => {
    return passengers.filter((p) => p.status === "riding");
  }, [passengers]);

  const totalPeople = passengers.length;
  const waitingLobby = passengers.filter(
    (p) => p.status === "waiting" && p.currentFloor === 1
  ).length;

  return {
    passengers,
    addPassenger,
    syncWithElevators,
    getByFloor,
    getRiding,
    totalPeople,
    waitingLobby,
  };
}
