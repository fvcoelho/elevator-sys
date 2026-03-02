"use client";

import { useEffect } from "react";
import { Provider } from "react-redux";
import { store } from "@/store";
import { wsConnect, wsDisconnect } from "@/store/slices/connectionSlice";
import { hydrateElevator } from "@/store/slices/elevatorSlice";
import { hydratePassengers } from "@/store/slices/passengersSlice";
import type { SystemStatusDto, Passenger } from "@/types/elevator";

function readStorage<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return fallback;
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

export function StoreProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    // Hydrate from localStorage after mount — this runs only on the client,
    // after the SSR hydration pass completes, avoiding the mismatch.
    const { status } = readStorage<{ status: SystemStatusDto | null }>(
      "elevator-ui:status",
      { status: null }
    );
    store.dispatch(hydrateElevator({ status }));

    const { passengers, nextId } = readStorage<{
      passengers: Passenger[];
      nextId: number;
    }>("elevator-ui:passengers", { passengers: [], nextId: 1 });
    store.dispatch(hydratePassengers({ passengers, nextId }));

    store.dispatch(wsConnect());
    return () => {
      store.dispatch(wsDisconnect());
    };
  }, []);

  return <Provider store={store}>{children}</Provider>;
}
