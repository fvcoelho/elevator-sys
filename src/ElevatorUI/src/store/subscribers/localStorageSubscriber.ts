import type { Store } from "@reduxjs/toolkit";
import type { RootState } from "../index";

function debounce<T extends () => void>(fn: T, ms: number): T {
  let timer: ReturnType<typeof setTimeout> | null = null;
  return (() => {
    if (timer !== null) clearTimeout(timer);
    timer = setTimeout(fn, ms);
  }) as T;
}

export function setupLocalStorageSubscriber(store: Store<RootState>) {
  const save = debounce(() => {
    const s = store.getState();
    try {
      localStorage.setItem(
        "elevator-ui:status",
        JSON.stringify({ status: s.elevator.status })
      );
      localStorage.setItem(
        "elevator-ui:passengers",
        JSON.stringify({
          passengers: s.passengers.passengers,
          nextId: s.passengers.nextId,
        })
      );
    } catch {
      // ignore quota errors
    }
  }, 300);

  store.subscribe(save);
}
