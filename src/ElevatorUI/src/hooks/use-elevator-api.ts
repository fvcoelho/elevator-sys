"use client";

import { useCallback } from "react";
import type {
  CreateRequestDto,
  RequestResponseDto,
  MetricsDto,
  DispatchAlgorithm,
  UpdateConfigDto,
  AddElevatorDto,
} from "@/types/elevator";

const API_BASE =
  (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5081") + "/api";

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new Error(body?.error ?? `Request failed: ${res.status}`);
  }
  return res.json();
}

export function useElevatorApi() {
  const requestRide = useCallback(async (dto: CreateRequestDto) => {
    return apiFetch<RequestResponseDto>("/requests", {
      method: "POST",
      body: JSON.stringify(dto),
    });
  }, []);

  const emergencyStop = useCallback(async () => {
    return apiFetch<{ message: string }>("/emergency/stop", {
      method: "POST",
    });
  }, []);

  const emergencyResume = useCallback(async () => {
    return apiFetch<{ message: string }>("/emergency/resume", {
      method: "POST",
    });
  }, []);

  const setAlgorithm = useCallback(async (algorithm: DispatchAlgorithm) => {
    return apiFetch<{ algorithm: string }>("/dispatch/algorithm", {
      method: "PUT",
      body: JSON.stringify({ algorithm }),
    });
  }, []);

  const toggleMaintenance = useCallback(async (index: number) => {
    return apiFetch<{ message: string; inMaintenance: boolean }>(
      `/elevators/${index}/maintenance`,
      { method: "POST" }
    );
  }, []);

  const getMetrics = useCallback(async () => {
    return apiFetch<MetricsDto>("/metrics");
  }, []);

  const updateConfig = useCallback(async (dto: UpdateConfigDto) => {
    return apiFetch<{ message: string; elevatorCount: number }>("/config", {
      method: "PUT",
      body: JSON.stringify(dto),
    });
  }, []);

  const addElevator = useCallback(async (dto: AddElevatorDto) => {
    return apiFetch<unknown>("/elevators", {
      method: "POST",
      body: JSON.stringify(dto),
    });
  }, []);

  return {
    requestRide,
    emergencyStop,
    emergencyResume,
    setAlgorithm,
    toggleMaintenance,
    getMetrics,
    updateConfig,
    addElevator,
  };
}
