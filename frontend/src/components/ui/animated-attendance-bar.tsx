/**
 * AnimatedAttendanceBar
 * ──────────────────────
 * A labelled progress bar with GSAP-animated fill and a live count-up.
 *
 * Animation:
 *  1. Bar fill: width 0% → percentage%  (power3.out, 1200ms)
 *  2. Counter text: updates each frame — "{count} ({pct}%)"
 *  3. `delay` prop staggers bars:  delay × 0.2s added to timeline
 *  4. Pulsing glow dot at the leading edge of the bar (CSS animate-pulse)
 *
 * @prop label     - Display label e.g. "On Time"
 * @prop count     - Raw count value
 * @prop total     - Denominator for percentage calculation
 * @prop color     - Tailwind bg class e.g. "bg-green-500"
 * @prop accentHex - Hex color for the glow dot e.g. "#22c55e"
 * @prop delay     - Stagger index
 *
 * Library: GSAP
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { gsap } from "gsap";

interface AnimatedAttendanceBarProps {
  label: string;
  count: number;
  total: number;
  color: string;
  accentHex: string;
  delay?: number;
}

export default function AnimatedAttendanceBar({
  label,
  count,
  total,
  color,
  accentHex,
  delay = 0,
}: AnimatedAttendanceBarProps) {
  const barRef = useRef<HTMLDivElement>(null);
  const fillRef = useRef<HTMLDivElement>(null);
  const countRef = useRef<HTMLSpanElement>(null);
  const percentage = total > 0 ? (count / total) * 100 : 0;

  useEffect(() => {
    const fill = fillRef.current;
    const countEl = countRef.current;
    if (!fill) return;

    const tl = gsap.timeline({ delay: 0.3 + delay * 0.2 });

    // Animate the fill width
    tl.fromTo(
      fill,
      { width: "0%" },
      {
        width: `${percentage}%`,
        duration: 1.2,
        ease: "power3.out",
      }
    );

    // Count up percentage
    if (countEl) {
      tl.to(
        { val: 0 },
        {
          val: percentage,
          duration: 1,
          ease: "power2.out",
          onUpdate: function () {
            const v = this.targets()[0].val;
            countEl.textContent = `${count} (${v.toFixed(1)}%)`;
          },
        },
        "-=1.0"
      );
    }

    return () => {
      tl.kill();
    };
  }, [count, total, percentage, delay]);

  return (
    <div ref={barRef} className="group">
      <div className="flex items-center justify-between mb-1.5">
        <span className="text-sm font-medium text-gray-600 group-hover:text-gray-900 transition-colors">
          {label}
        </span>
        <span ref={countRef} className="text-sm font-semibold text-gray-900 tabular-nums">
          0 (0.0%)
        </span>
      </div>
      <div className="w-full bg-gray-100 rounded-full h-2.5 overflow-hidden">
        <div
          ref={fillRef}
          className={`h-2.5 rounded-full ${color} relative`}
          style={{ width: 0 }}
        >
          {/* Glow pulse */}
          <div
            className="absolute right-0 top-1/2 -translate-y-1/2 w-3 h-3 rounded-full animate-pulse"
            style={{
              background: accentHex,
              boxShadow: `0 0 8px ${accentHex}, 0 0 16px ${accentHex}40`,
            }}
          />
        </div>
      </div>
    </div>
  );
}
