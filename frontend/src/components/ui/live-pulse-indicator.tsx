/**
 * LivePulseIndicator
 * ──────────────────
 * Compact "LIVE" status badge with three looping Anime.js animations.
 * Used in card headers next to real-time data sections.
 *
 * Animations (all loop: true while isActive=true):
 *  - Dot:  scale 1→1.3→1, opacity 1→0.7→1  (inOutSine, 1500ms)
 *  - Ring: scale 1→2.5, opacity 0.5→0       (outExpo, 2000ms) — expanding ripple
 *  - Text: opacity 0.6→1→0.6               (inOutSine, 2000ms) — breathing glow
 *
 * All animations are paused and cleaned up on unmount via animationsRef.
 *
 * @prop isActive - When false, no animations run (default: true)
 *
 * Library: Anime.js v4  (named import: animate)
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { animate } from "animejs";
import { Activity } from "lucide-react";

interface LivePulseIndicatorProps {
  isActive?: boolean;
}

export default function LivePulseIndicator({ isActive = true }: LivePulseIndicatorProps) {
  const dotRef = useRef<HTMLDivElement>(null);
  const ringRef = useRef<HTMLDivElement>(null);
  const textRef = useRef<HTMLSpanElement>(null);
  const animationsRef = useRef<ReturnType<typeof animate>[]>([]);

  useEffect(() => {
    if (!isActive) return;

    // Kill any previous animations
    animationsRef.current.forEach((a) => a.pause());
    animationsRef.current = [];

    // Dot pulse
    if (dotRef.current) {
      animationsRef.current.push(
        animate(dotRef.current, {
          scale: [1, 1.3, 1],
          opacity: [1, 0.7, 1],
          ease: "inOutSine",
          duration: 1500,
          loop: true,
        })
      );
    }

    // Ring expansion
    if (ringRef.current) {
      animationsRef.current.push(
        animate(ringRef.current, {
          scale: [1, 2.5],
          opacity: [0.5, 0],
          ease: "outExpo",
          duration: 2000,
          loop: true,
        })
      );
    }

    // Text glow
    if (textRef.current) {
      animationsRef.current.push(
        animate(textRef.current, {
          opacity: [0.6, 1, 0.6],
          ease: "inOutSine",
          duration: 2000,
          loop: true,
        })
      );
    }

    return () => {
      animationsRef.current.forEach((a) => a.pause());
      animationsRef.current = [];
    };
  }, [isActive]);

  return (
    <div className="flex items-center gap-2">
      <div className="relative">
        <div
          ref={dotRef}
          className="w-2.5 h-2.5 rounded-full bg-emerald-500"
          style={{ boxShadow: "0 0 6px rgba(34,197,94,0.5)" }}
        />
        <div
          ref={ringRef}
          className="absolute inset-0 w-2.5 h-2.5 rounded-full bg-emerald-400"
        />
      </div>
      <Activity className="h-3.5 w-3.5 text-emerald-600" />
      <span ref={textRef} className="text-xs font-semibold text-emerald-700 tracking-wide uppercase">
        Live
      </span>
    </div>
  );
}
