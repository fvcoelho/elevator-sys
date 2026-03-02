// --- Passenger Types ---

export type PassengerStatus = "waiting" | "riding" | "arrived" | "returning";

export interface Passenger {
  id: number;
  name: string;
  pickupFloor: number;
  destinationFloor: number;
  status: PassengerStatus;
  currentFloor: number;
  returnDelaySec?: number;
  arrivedAt?: number;
  elevatorIndex?: number;
}

export interface ReturnTripRequest {
  name: string;
  fromFloor: number;
}

// --- Elevator Types ---

export type ElevatorState =
  | "IDLE"
  | "MOVING_UP"
  | "MOVING_DOWN"
  | "DOOR_OPEN"
  | "DOOR_OPENING"
  | "DOOR_CLOSING"
  | "MAINTENANCE"
  | "EMERGENCY_STOP";

export type DispatchAlgorithm = "Simple" | "SCAN" | "LOOK" | "Custom";

export interface ElevatorDto {
  index: number;
  label: string;
  currentFloor: number;
  state: ElevatorState;
  type: string;
  inMaintenance: boolean;
  inEmergencyStop: boolean;
  capacity: number;
  servedFloors: number[] | null;
  targetFloors: number[];
}

export interface SystemStatusDto {
  elevatorCount: number;
  pendingRequests: number;
  isEmergencyStopped: boolean;
  algorithm: string;
  peopleWaiting: number;
  peopleInTransit: number;
  memoryUsedBytes: number;
  elevators: ElevatorDto[];
}

export interface CreateRequestDto {
  pickupFloor: number;
  destinationFloor: number;
  priority?: string;
  accessLevel?: string;
  preferredElevatorType?: string;
}

export interface RequestResponseDto {
  requestId: number;
  pickupFloor: number;
  destinationFloor: number;
  direction: string;
  priority: string;
  accessLevel: string;
  preferredElevatorType: string | null;
}

export interface ElevatorMetricsDto {
  label: string;
  tripsCompleted: number;
  floorsTraversed: number;
  totalMovingTimeMs: number;
  totalIdleTimeMs: number;
  totalDoorTimeMs: number;
  utilization: number;
  averageFloorsPerTrip: number;
}

export type ElevatorType = "Local" | "Express" | "Freight";

export interface ElevatorConfigDto {
  label: string;
  initialFloor: number;
  type: string;
  capacity: number;
  servedFloors: number[] | null;
}

export interface UpdateConfigDto {
  minFloor: number;
  maxFloor: number;
  doorOpenMs: number;
  floorTravelMs: number;
  doorTransitionMs: number;
  algorithm: string;
  vipFloors: number[];
  elevators: ElevatorConfigDto[];
}

export interface AddElevatorDto {
  label: string;
  initialFloor: number;
  type: string;
  capacity: number;
  servedFloors: number[] | null;
}

export interface MetricsDto {
  totalRequests: number;
  completedRequests: number;
  averageWaitTimeMs: number;
  averageRideTimeMs: number;
  averageDispatchTimeMs: number;
  systemUtilization: number;
  peakConcurrentRequests: number;
  floorHeatmap: Record<string, number>;
  requestsByPriority: Record<string, number>;
  vipRequests: number;
  standardRequests: number;
  elevatorStats: Record<string, ElevatorMetricsDto>;
}
