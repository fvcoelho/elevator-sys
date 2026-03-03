"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useAppSelector } from "@/hooks/use-app-selector";
import { selectStatus, selectMessageCount } from "@/store/slices/elevatorSlice";

export function WsPayloadViewer() {
  const [open, setOpen] = useState(false);
  const status = useAppSelector(selectStatus);
  const messageCount = useAppSelector(selectMessageCount);

  return (
    <Card>
      <CardHeader className="px-3 py-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold">WS Payload</CardTitle>
          <div className="flex items-center gap-2">
            {status && (
              <Badge variant="secondary" className="text-xs tabular-nums">
                #{messageCount}
              </Badge>
            )}
            <Button
              variant="ghost"
              size="sm"
              className="h-6 px-2 text-xs"
              onClick={() => setOpen((v) => !v)}
            >
              {open ? "Hide" : "Show"}
            </Button>
          </div>
        </div>
      </CardHeader>

      {open && (
        <CardContent className="px-3 pb-3">
          {status ? (
            <pre className="max-h-96 overflow-auto rounded bg-muted p-2 text-[10px] leading-tight font-mono whitespace-pre-wrap break-all">
              {JSON.stringify(status, null, 2)}
            </pre>
          ) : (
            <p className="text-xs text-muted-foreground">No payload yet.</p>
          )}
        </CardContent>
      )}
    </Card>
  );
}
