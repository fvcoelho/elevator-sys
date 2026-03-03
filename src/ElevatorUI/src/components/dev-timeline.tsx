"use client";

import { useEffect, useRef, useState } from "react";
import { useAppSelector } from "@/hooks/use-app-selector";
import { useAppDispatch } from "@/hooks/use-app-dispatch";
import { store } from "@/store";
import {
  selectIsRecording,
  selectIsReplaying,
  selectIsPlaying,
  selectRecordedActions,
  selectCursor,
  selectCurrentRecordedAction,
  recordingStarted,
  recordingStopped,
  replayEntered,
  replayExited,
  playStarted,
  playStopped,
  cursorSet,
  timelineCleared,
  replaySnapshot,
} from "@/store/slices/timelineSlice";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";

const PLAY_INTERVAL_MS = 200;

function fmtTime(ts: number) {
  return new Date(ts).toLocaleTimeString("en-US", {
    hour12: false,
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    fractionalSecondDigits: 2,
  });
}

function shortType(type: string) {
  // "passengers/passengerAdded" → "passengerAdded"
  return type.split("/").pop() ?? type;
}

export function DevTimeline() {
  const dispatch = useAppDispatch();
  const isRecording = useAppSelector(selectIsRecording);
  const isReplaying = useAppSelector(selectIsReplaying);
  const isPlaying = useAppSelector(selectIsPlaying);
  const actions = useAppSelector(selectRecordedActions);
  const cursor = useAppSelector(selectCursor);
  const current = useAppSelector(selectCurrentRecordedAction);

  const [expanded, setExpanded] = useState(false);
  const playTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const listRef = useRef<HTMLDivElement>(null);

  const hasActions = actions.length > 0;
  const safeCursor = cursor < 0 ? 0 : cursor;

  // Auto-scroll the action list to the current cursor entry
  useEffect(() => {
    if (!expanded || !listRef.current) return;
    const el = listRef.current.querySelector(`[data-idx="${safeCursor}"]`);
    el?.scrollIntoView({ block: "nearest" });
  }, [safeCursor, expanded]);

  // Play: advance cursor at regular intervals
  useEffect(() => {
    if (!isPlaying) {
      if (playTimerRef.current) {
        clearInterval(playTimerRef.current);
        playTimerRef.current = null;
      }
      return;
    }

    playTimerRef.current = setInterval(() => {
      const { timeline } = store.getState();
      const next = timeline.cursor + 1;
      if (next >= timeline.actions.length) {
        dispatch(playStopped());
        return;
      }
      dispatch(cursorSet(next));
      dispatch(replaySnapshot(timeline.actions[next].snapshot));
    }, PLAY_INTERVAL_MS);

    return () => {
      if (playTimerRef.current) {
        clearInterval(playTimerRef.current);
        playTimerRef.current = null;
      }
    };
  }, [isPlaying, dispatch]);

  // --- Handlers ---

  function enterReplayAt(idx: number) {
    if (!isReplaying) dispatch(replayEntered(idx));
    else dispatch(cursorSet(idx));
    dispatch(playStopped());
    dispatch(replaySnapshot(actions[idx].snapshot));
  }

  function handleRecord() {
    if (isReplaying) dispatch(replayExited());
    dispatch(recordingStarted());
  }

  function handleStopRecording() {
    dispatch(recordingStopped());
  }

  function handleExitReplay() {
    dispatch(replayExited());
  }

  function handleStepBack() {
    if (!hasActions) return;
    enterReplayAt(Math.max(0, safeCursor - 1));
  }

  function handleStepForward() {
    if (!hasActions) return;
    enterReplayAt(Math.min(actions.length - 1, safeCursor + 1));
  }

  function handlePlayPause() {
    if (!hasActions) return;
    if (!isReplaying) {
      dispatch(replayEntered(safeCursor));
      dispatch(replaySnapshot(actions[safeCursor].snapshot));
    }
    if (isPlaying) dispatch(playStopped());
    else dispatch(playStarted());
  }

  function handleScrub(e: React.ChangeEvent<HTMLInputElement>) {
    const idx = parseInt(e.target.value, 10);
    if (!actions[idx]) return;
    enterReplayAt(idx);
  }

  function handleClear() {
    dispatch(timelineCleared());
    setExpanded(false);
  }

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 border-t bg-background/95 backdrop-blur shadow-[0_-4px_16px_rgba(0,0,0,0.08)]">
      {/* Replay mode banner */}
      {isReplaying && (
        <div className="flex items-center gap-2 px-4 py-1 bg-amber-500/10 border-b border-amber-500/20 text-xs">
          <span className="h-1.5 w-1.5 rounded-full bg-amber-500 animate-pulse shrink-0" />
          <span className="font-semibold text-amber-600 dark:text-amber-400 shrink-0">
            REPLAY
          </span>
          {current && (
            <span className="text-muted-foreground truncate">
              &mdash; {current.type} &nbsp;·&nbsp; {fmtTime(current.timestamp)}
            </span>
          )}
          <Button
            variant="ghost"
            size="sm"
            className="ml-auto h-5 text-xs px-2 text-amber-600 dark:text-amber-400 hover:text-foreground shrink-0"
            onClick={handleExitReplay}
          >
            Exit ✕
          </Button>
        </div>
      )}

      {/* Controls bar */}
      <div className="flex items-center gap-1.5 px-3 py-1.5 min-w-0">
        {/* Record / Stop recording */}
        {!isRecording ? (
          <Button
            variant="outline"
            size="sm"
            className="h-7 px-2 gap-1 text-xs shrink-0"
            onClick={handleRecord}
          >
            <span className="h-2 w-2 rounded-full bg-red-500 shrink-0" />
            Rec
          </Button>
        ) : (
          <Button
            variant="destructive"
            size="sm"
            className="h-7 px-2 gap-1 text-xs shrink-0 animate-pulse"
            onClick={handleStopRecording}
          >
            <span className="h-2 w-2 rounded-full bg-white shrink-0" />
            Stop
          </Button>
        )}

        {isRecording && (
          <Badge variant="outline" className="text-xs h-5 font-mono shrink-0">
            {actions.length}
          </Badge>
        )}

        {hasActions && (
          <>
            <Separator orientation="vertical" className="h-5 shrink-0" />

            {/* Playback controls */}
            <Button
              variant="ghost"
              size="sm"
              className="h-7 w-7 p-0 text-base shrink-0"
              disabled={safeCursor <= 0}
              onClick={handleStepBack}
              title="Step back"
            >
              ‹
            </Button>

            <Button
              variant={isReplaying && isPlaying ? "default" : "ghost"}
              size="sm"
              className="h-7 w-7 p-0 shrink-0"
              onClick={handlePlayPause}
              title={isPlaying ? "Pause" : "Play"}
            >
              {isPlaying ? "⏸" : "▶"}
            </Button>

            <Button
              variant="ghost"
              size="sm"
              className="h-7 w-7 p-0 text-base shrink-0"
              disabled={safeCursor >= actions.length - 1}
              onClick={handleStepForward}
              title="Step forward"
            >
              ›
            </Button>

            <Separator orientation="vertical" className="h-5 shrink-0" />

            {/* Scrubber */}
            <input
              type="range"
              min={0}
              max={Math.max(0, actions.length - 1)}
              value={safeCursor}
              onChange={handleScrub}
              className="flex-1 min-w-0 h-1 accent-primary cursor-pointer"
            />

            {/* Counter + current action label */}
            <span className="text-xs text-muted-foreground tabular-nums shrink-0">
              {safeCursor + 1}/{actions.length}
            </span>

            {current && (
              <Badge
                variant={isReplaying ? "default" : "outline"}
                className="text-xs h-5 hidden md:flex max-w-[160px] truncate shrink-0"
              >
                {shortType(current.type)}
              </Badge>
            )}
          </>
        )}

        <div className="flex-1 min-w-0" />

        {hasActions && (
          <>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 px-2 text-xs shrink-0"
              onClick={handleClear}
            >
              Clear
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-7 px-2 text-xs shrink-0"
              onClick={() => setExpanded((v) => !v)}
            >
              {expanded ? "▲" : "▼"}
            </Button>
          </>
        )}

        <span className="text-[10px] text-muted-foreground/60 font-mono shrink-0 hidden sm:block pl-1">
          Redux timeline
        </span>
      </div>

      {/* Expandable action list */}
      {expanded && hasActions && (
        <div
          ref={listRef}
          className="border-t max-h-44 overflow-y-auto"
        >
          {actions.map((act, idx) => (
            <button
              key={act.id}
              data-idx={idx}
              className={`w-full text-left px-3 py-0.5 text-xs flex items-center gap-2 transition-colors hover:bg-muted/60
                ${idx === safeCursor ? "bg-primary/10 font-semibold" : ""}
              `}
              onClick={() => enterReplayAt(idx)}
            >
              <span className="text-muted-foreground tabular-nums w-8 shrink-0 text-right text-[10px]">
                {idx + 1}
              </span>
              <span className="font-mono flex-1 truncate">{act.type}</span>
              <span className="text-muted-foreground tabular-nums shrink-0 text-[10px]">
                {fmtTime(act.timestamp)}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
