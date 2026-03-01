"use client";

import { useEffect } from "react";
import { useElevatorWebSocket } from "@/hooks/use-elevator-websocket";
import { useElevatorApi } from "@/hooks/use-elevator-api";
import { usePassengers } from "@/hooks/use-passengers";
import { StatusBar } from "@/components/status-bar";
import { ElevatorShaft } from "@/components/elevator-shaft";
import { ElevatorPanel } from "@/components/elevator-panel";
import { SystemControls } from "@/components/system-controls";
import { TrafficGenerator } from "@/components/traffic-generator";
import { SystemConfig } from "@/components/system-config";
import { BuildingView } from "@/components/building-view";

const MAX_FLOOR = 20;

export default function Home() {
  const { status, isConnected } = useElevatorWebSocket();
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
  const {
    addPassenger,
    syncWithElevators,
    getByFloor,
    getRiding,
    totalPeople,
    waitingLobby,
  } = usePassengers();

  // Sync passenger statuses with elevator positions
  useEffect(() => {
    if (status?.elevators) {
      syncWithElevators(status.elevators);
    }
  }, [status?.elevators, syncWithElevators]);

  return (
    <div className="min-h-screen bg-background p-4 space-y-4">
      <h1 className="text-2xl font-bold">Elevator System</h1>

      <StatusBar status={status} isConnected={isConnected} />

      <div className="flex gap-4">
        {/* Elevator shafts + Building view */}
        <div className="flex gap-3 overflow-x-auto flex-1">
          {status?.elevators.map((elevator) => (
            <ElevatorShaft
              key={elevator.index}
              elevator={elevator}
              maxFloor={MAX_FLOOR}
              onToggleMaintenance={toggleMaintenance}
              ridingPassengers={getRiding()}
            />
          ))}

          {status && (
            <BuildingView
              maxFloor={MAX_FLOOR}
              totalPeople={totalPeople}
              waitingLobby={waitingLobby}
              getByFloor={getByFloor}
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

          <ElevatorPanel onRequestRide={requestRide} onPassengerAdded={addPassenger} />

          <SystemControls
            currentAlgorithm={status?.algorithm ?? "Custom"}
            isEmergencyStopped={status?.isEmergencyStopped ?? false}
            onEmergencyStop={emergencyStop}
            onEmergencyResume={emergencyResume}
            onSetAlgorithm={setAlgorithm}
            onGetMetrics={getMetrics}
          />

          <TrafficGenerator onRequestRide={requestRide} onPassengerAdded={addPassenger} />
        </div>
      </div>
    </div>
  );
}
