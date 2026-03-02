"use client";

import { useEffect } from "react";
import { useAppSelector } from "@/hooks/use-app-selector";
import { useAppDispatch } from "@/hooks/use-app-dispatch";
import { useElevatorApi } from "@/hooks/use-elevator-api";
import type { ElevatorDto } from "@/types/elevator";
import { selectStatus, selectMessageCount, selectIsEmergencyStopped, selectAlgorithm } from "@/store/slices/elevatorSlice";
import { selectIsConnected } from "@/store/slices/connectionSlice";
import {
  selectReturnQueue,
  selectTotalPeople,
  selectWaitingLobby,
  passengerAdded,
  passengersCleared,
  returnQueueConsumed,
} from "@/store/slices/passengersSlice";
import { StatusBar } from "@/components/status-bar";
import { ElevatorShaft } from "@/components/elevator-shaft";
import { ElevatorPanel } from "@/components/elevator-panel";
import { TrafficGenerator } from "@/components/traffic-generator";
import { SystemConfig } from "@/components/system-config";
import { BuildingView } from "@/components/building-view";
import { DevTimeline } from "@/components/dev-timeline";

const MAX_FLOOR = 20;

export default function Home() {
  const dispatch = useAppDispatch();
  const status = useAppSelector(selectStatus);
  const isConnected = useAppSelector(selectIsConnected);
  const messageCount = useAppSelector(selectMessageCount);
  const isEmergencyStopped = useAppSelector(selectIsEmergencyStopped);
  const currentAlgorithm = useAppSelector(selectAlgorithm);
  const returnQueue = useAppSelector(selectReturnQueue);
  const totalPeople = useAppSelector(selectTotalPeople);
  const waitingLobby = useAppSelector(selectWaitingLobby);

  const {
    requestRide,
    emergencyStop,
    emergencyResume,
    setAlgorithm,
    toggleMaintenance,
    getMetrics,
    updateConfig,
    addElevator,
  } = useElevatorApi();

  // Process return trips — passengers whose delay expired after arrival
  useEffect(() => {
    if (returnQueue.length === 0) return;
    const trips = [...returnQueue];
    dispatch(returnQueueConsumed());
    for (const trip of trips) {
      requestRide({ pickupFloor: trip.fromFloor, destinationFloor: 1 }).catch(
        () => {
          // silently ignore return trip failure
        }
      );
    }
  }, [returnQueue, dispatch, requestRide]);

  return (
    <div className="min-h-screen bg-background p-4 pb-20 space-y-4">
      <h1 className="text-2xl font-bold">
        Elevator System
        <span className="text-base font-normal text-muted-foreground">
          {" "}- powered by WebSocket and{" "}
          <a href="http://localhost:5081/swagger" target="_blank" rel="noopener noreferrer" className="underline hover:text-foreground transition-colors">
            REST API
          </a>
        </span>
      </h1>

      <StatusBar
        status={status}
        isConnected={isConnected}
        messageCount={messageCount}
        isEmergencyStopped={isEmergencyStopped}
        onEmergencyStop={emergencyStop}
        onEmergencyResume={emergencyResume}
        currentAlgorithm={currentAlgorithm}
        onSetAlgorithm={setAlgorithm}
        onGetMetrics={getMetrics}
      />

      <div className="flex gap-4">
        {/* Elevator shafts + Building view */}
        <div className="flex gap-3 overflow-x-auto flex-1 items-start">
          {status?.elevators.map((elevator: ElevatorDto) => (
            <ElevatorShaft
              key={elevator.index}
              elevator={elevator}
              maxFloor={MAX_FLOOR}
              onToggleMaintenance={toggleMaintenance}
            />
          ))}

          {status && (
            <BuildingView
              maxFloor={MAX_FLOOR}
              totalPeople={totalPeople}
              waitingLobby={waitingLobby}
            />
          )}

          {!status && (
            <div className="flex items-center justify-center flex-1 text-muted-foreground text-sm py-20">
              Connecting to elevator system...
            </div>
          )}
        </div>

        {/* Controls sidebar */}
        <div className="w-80 flex-shrink-0 space-y-4">
          <SystemConfig
            status={status}
            onUpdateConfig={updateConfig}
            onAddElevator={addElevator}
          />

          <ElevatorPanel
            onRequestRide={requestRide}
            onPassengerAdded={(name, pickupFloor, destinationFloor, returnDelaySec) =>
              dispatch(passengerAdded({ name, pickupFloor, destinationFloor, returnDelaySec }))
            }
            onClearPassengers={() => dispatch(passengersCleared())}
          />

          <TrafficGenerator
            onRequestRide={requestRide}
            onPassengerAdded={(name, pickupFloor, destinationFloor) =>
              dispatch(passengerAdded({ name, pickupFloor, destinationFloor }))
            }
          />
        </div>
      </div>

      <DevTimeline />
    </div>
  );
}
