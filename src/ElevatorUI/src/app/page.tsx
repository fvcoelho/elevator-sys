"use client";

import { useEffect } from "react";
import { useAppSelector } from "@/hooks/use-app-selector";
import { useAppDispatch } from "@/hooks/use-app-dispatch";
import { useElevatorApi } from "@/hooks/use-elevator-api";
import type { ElevatorDto } from "@/types/elevator";
import { selectStatus, selectMessageCount, selectIsEmergencyStopped, selectAlgorithm, selectVipFloors, statusCleared } from "@/store/slices/elevatorSlice";
import { selectIsConnected } from "@/store/slices/connectionSlice";
import {
  selectReturnQueue,
  selectTotalPeople,
  selectWaitingLobby,
  passengerAdded,
  passengersCleared,
  returnQueueConsumed,
  requestLogEntryAdded,
} from "@/store/slices/passengersSlice";
import { timelineCleared } from "@/store/slices/timelineSlice";
import { StatusBar } from "@/components/status-bar";
import { ElevatorShaft } from "@/components/elevator-shaft";
import { ElevatorPanel } from "@/components/elevator-panel";
import { SystemConfig } from "@/components/system-config";
import { BuildingView } from "@/components/building-view";
import { DevTimeline } from "@/components/dev-timeline";
import { RequestLog } from "@/components/request-log";

const MAX_FLOOR = 20;

export default function Home() {
  const dispatch = useAppDispatch();
  const status = useAppSelector(selectStatus);
  const isConnected = useAppSelector(selectIsConnected);
  const messageCount = useAppSelector(selectMessageCount);
  const isEmergencyStopped = useAppSelector(selectIsEmergencyStopped);
  const currentAlgorithm = useAppSelector(selectAlgorithm);
  const vipFloors = useAppSelector(selectVipFloors);
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
    resetServer,
  } = useElevatorApi();

  const handleResetServer = async () => {
    await resetServer();
    dispatch(passengersCleared());
    dispatch(statusCleared());
    dispatch(timelineCleared());
  };

  // Process return trips — passengers whose delay expired after arrival
  useEffect(() => {
    if (returnQueue.length === 0) return;
    const trips = [...returnQueue];
    dispatch(returnQueueConsumed());
    for (const trip of trips) {
      const dto: Parameters<typeof requestRide>[0] = { pickupFloor: trip.fromFloor, destinationFloor: 1 };
      if (trip.priorityMode === "High") dto.priority = "High";
      else if (trip.priorityMode === "VIP") dto.accessLevel = "VIP";
      else if (trip.priorityMode === "Freight") dto.preferredElevatorType = "Freight";
      requestRide(dto)
        .then((res) => {
          dispatch(requestLogEntryAdded({
            requestId: res.requestId,
            name: trip.name,
            pickupFloor: trip.fromFloor,
            destinationFloor: 1,
            priorityMode: trip.priorityMode,
          }));
        })
        .catch(() => {
          // silently ignore return trip failure
        });
    }
  }, [returnQueue, dispatch, requestRide]);

  return (
    <div className="min-h-screen bg-background px-6 pb-20 pt-4 space-y-6">
      <h1 className="text-2xl font-bold tracking-tight">
        Elevator System
        <span className="text-xl font-normal text-muted-foreground">
          {" "}- Powered by{" "}
          <a href={(process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5081") + "/swagger"} target="_blank" rel="noopener noreferrer" className="underline hover:text-foreground transition-colors">
            REST API
          </a>
          {" "}· WebSocket · Redux {" "} (record &amp; play)
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
        onResetServer={handleResetServer}
      />

      <div className="flex gap-4">
        {/* Elevator shafts + Building view */}
        <div className="flex-1 min-w-0 space-y-2">
          <h2 className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Elevator Shafts</h2>
          <div className="flex gap-3 overflow-x-auto items-start">
          {status?.elevators.map((elevator: ElevatorDto) => (
            <ElevatorShaft
              key={elevator.index}
              elevator={elevator}
              maxFloor={MAX_FLOOR}
              vipFloors={vipFloors}
              onToggleMaintenance={toggleMaintenance}
            />
          ))}

          {status && (
            <BuildingView
              maxFloor={MAX_FLOOR}
              totalPeople={totalPeople}
              waitingLobby={waitingLobby}
              vipFloors={vipFloors}
            />
          )}

          {!status && (
            <div className="flex items-center justify-center flex-1 text-muted-foreground text-sm py-20">
              Connecting to elevator system...
            </div>
          )}
          </div>
        </div>

        {/* Controls sidebar */}
        <aside className="w-80 flex-shrink-0 space-y-4">
          <h2 className="text-xs font-medium uppercase tracking-wider text-muted-foreground">Controls</h2>
          <SystemConfig
            status={status}
            onUpdateConfig={updateConfig}
            onAddElevator={addElevator}
          />

          <ElevatorPanel
            onRequestRide={requestRide}
            onPassengerAdded={(name, pickupFloor, destinationFloor, returnDelaySec, requestId, priorityMode) =>
              dispatch(passengerAdded({ name, pickupFloor, destinationFloor, returnDelaySec, requestId, priorityMode }))
            }
            onClearPassengers={() => dispatch(passengersCleared())}
          />

          <RequestLog />

        </aside>
      </div>

      <DevTimeline />
    </div>
  );
}
