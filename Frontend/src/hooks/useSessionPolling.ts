"use client";
import { useState, useEffect, useRef, useCallback } from "react";
import { sessionApi } from "@/services/api";
import type { SessionStatus } from "@/types";

const POLL_INTERVAL_MS = 3000;
const MAX_POLL_ATTEMPTS = 60; // 3 min timeout

interface PollState {
  status:    "idle" | "polling" | "complete" | "failed" | "timeout";
  data:      SessionStatus | null;
  attempts:  number;
  error:     string | null;
}

export function useSessionPolling(sessionId: string | null, autoStart = true) {
  const [state, setState] = useState<PollState>({
    status: "idle", data: null, attempts: 0, error: null,
  });

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const attemptsRef = useRef(0);

  const stop = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  const poll = useCallback(async () => {
    if (!sessionId) return;

    attemptsRef.current++;

    if (attemptsRef.current > MAX_POLL_ATTEMPTS) {
      stop();
      setState(s => ({ ...s, status: "timeout", error: "انتهت مهلة الانتظار. يرجى تحديث الصفحة." }));
      return;
    }

    try {
      const res = await sessionApi.getStatus(sessionId);
      if (!res.success || !res.data) return;

      const statusData = res.data;
      setState(s => ({ ...s, data: statusData, attempts: attemptsRef.current }));

      if (statusData.status === "complete") {
        stop();
        setState(s => ({ ...s, status: "complete" }));
      } else if (statusData.status === "failed") {
        stop();
        setState(s => ({ ...s, status: "failed", error: statusData.errorMessage ?? "فشل التوليد" }));
      } else {
        setState(s => ({ ...s, status: "polling" }));
      }
    } catch {
      // Network blip — keep polling, don't crash
    }
  }, [sessionId, stop]);

  const start = useCallback(() => {
    if (!sessionId || intervalRef.current) return;
    attemptsRef.current = 0;
    setState({ status: "polling", data: null, attempts: 0, error: null });
    poll(); // immediate first check
    intervalRef.current = setInterval(poll, POLL_INTERVAL_MS);
  }, [sessionId, poll]);

  useEffect(() => {
    if (autoStart && sessionId) start();
    return stop;
  }, [autoStart, sessionId, start, stop]);

  return { ...state, start, stop };
}
