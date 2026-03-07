/**
 * AnimatedDonutChart
 * ──────────────────
 * SVG donut chart with Anime.js-driven segment draw-in animations.
 *
 * Segments:
 *  - On Time  → green  (#22c55e)
 *  - Late     → amber  (#f59e0b)
 *  - Absent   → red    (#ef4444)
 *
 * Animation sequence:
 *  1. Each segment starts invisible (strokeDasharray "0 C") and its target
 *     length is stored in data-seglen. Anime.js interpolates drawLen 0→target
 *     via onUpdate, updating strokeDasharray each frame while keeping the
 *     positioning strokeDashoffset constant.
 *  2. Segments draw in sequentially — delay: 400 + i×300 ms
 *  3. Legend items fade in + slide up, staggered from 1200ms
 *  4. Center counter counts up 0→total over 2000ms
 *
 * Segment offset math:
 *  - All 3 segments share the single SVG ring, positioned by strokeDashoffset
 *  - rotate(-90 90 90) shifts drawing start to 12 o'clock
 *  - 4° gaps separate each segment within the 360° available
 *
 * Library: Anime.js v4  (named imports: animate, stagger)
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { animate, stagger } from "animejs";

interface AnimatedDonutChartProps {
  onTime: number;
  late: number;
  absent: number;
}

export default function AnimatedDonutChart({
  onTime,
  late,
  absent,
}: AnimatedDonutChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const animationsRef = useRef<ReturnType<typeof animate>[]>([]);
  const total = onTime + late + absent;

  useEffect(() => {
    if (!containerRef.current || total === 0) return;

    // Kill previous animations
    animationsRef.current.forEach((a) => a.pause());
    animationsRef.current = [];

    const paths = containerRef.current.querySelectorAll<SVGCircleElement>(".donut-segment");
    const labels = containerRef.current.querySelectorAll(".donut-label");
    const centerText = containerRef.current.querySelector(".center-value");

    const circumference = 2 * Math.PI * 70;

    // Animate donut segments by drawing dasharray from 0 to target length
    paths.forEach((path, i) => {
      const targetLen = Number(path.dataset.seglen);
      const dashObj = { drawLen: 0 };

      // Start fully hidden
      path.setAttribute("stroke-dasharray", `0 ${circumference}`);
      path.style.opacity = "1";

      const anim = animate(dashObj, {
        drawLen: targetLen,
        ease: "inOutCubic",
        duration: 1500,
        delay: 400 + i * 300,
        onUpdate: () => {
          path.setAttribute(
            "stroke-dasharray",
            `${dashObj.drawLen} ${circumference - dashObj.drawLen}`
          );
        },
      });
      animationsRef.current.push(anim);
    });

    // Fade in labels
    const labelAnim = animate(labels, {
      opacity: [0, 1],
      translateY: [10, 0],
      ease: "outExpo",
      duration: 800,
      delay: stagger(200, { start: 1200 }),
    });
    animationsRef.current.push(labelAnim);

    // Center text counter
    if (centerText) {
      const counter = { val: 0 };
      const counterAnim = animate(counter, {
        val: total,
        ease: "outExpo",
        duration: 2000,
        delay: 400,
        onUpdate: () => {
          centerText.textContent = String(Math.round(counter.val));
        },
      });
      animationsRef.current.push(counterAnim);
    }

    return () => {
      animationsRef.current.forEach((a) => a.pause());
      animationsRef.current = [];
    };
  }, [onTime, late, absent, total]);

  if (total === 0) {
    return (
      <div className="flex items-center justify-center h-56 text-sm text-gray-400">
        No data to display
      </div>
    );
  }

  const radius = 70;
  const circumference = 2 * Math.PI * radius;
  const gap = 4;
  const totalGap = gap * 3;
  const availableDegrees = 360 - totalGap;

  const onTimeDeg = (onTime / total) * availableDegrees;
  const lateDeg = (late / total) * availableDegrees;
  const absentDeg = (absent / total) * availableDegrees;

  const onTimeLen = (onTimeDeg / 360) * circumference;
  const lateLen = (lateDeg / 360) * circumference;
  const absentLen = (absentDeg / 360) * circumference;

  const gapLen = (gap / 360) * circumference;

  const segments = [
    {
      color: "#22c55e",
      length: onTimeLen,
      offset: 0,
      label: "On Time",
      value: onTime,
      percent: ((onTime / total) * 100).toFixed(1),
    },
    {
      color: "#f59e0b",
      length: lateLen,
      offset: -(onTimeLen + gapLen),
      label: "Late",
      value: late,
      percent: ((late / total) * 100).toFixed(1),
    },
    {
      color: "#ef4444",
      length: absentLen,
      offset: -(onTimeLen + lateLen + gapLen * 2),
      label: "Absent",
      value: absent,
      percent: ((absent / total) * 100).toFixed(1),
    },
  ];

  return (
    <div ref={containerRef} className="flex items-center gap-8">
      {/* SVG Donut */}
      <div className="relative flex-shrink-0">
        <svg width="180" height="180" viewBox="0 0 180 180">
          {/* Background ring */}
          <circle
            cx="90"
            cy="90"
            r={radius}
            fill="none"
            stroke="#f3f4f6"
            strokeWidth="16"
          />
          {/* Segments */}
          {segments.map((seg, i) => (
            <circle
              key={i}
              className="donut-segment"
              cx="90"
              cy="90"
              r={radius}
              fill="none"
              stroke={seg.color}
              strokeWidth="16"
              strokeLinecap="round"
              strokeDasharray={`0 ${circumference}`}
              strokeDashoffset={seg.offset}
              data-seglen={seg.length}
              transform="rotate(-90 90 90)"
              style={{
                filter: `drop-shadow(0 0 4px ${seg.color}40)`,
                transition: "stroke-width 0.2s",
                opacity: 0,
              }}
            />
          ))}
        </svg>
        {/* Center text */}
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="center-value text-2xl font-bold text-gray-900">0</span>
          <span className="text-xs text-gray-400 font-medium">Total</span>
        </div>
      </div>

      {/* Legend */}
      <div className="space-y-3">
        {segments.map((seg, i) => (
          <div key={i} className="donut-label flex items-center gap-3" style={{ opacity: 0 }}>
            <div
              className="w-3 h-3 rounded-full"
              style={{
                backgroundColor: seg.color,
                boxShadow: `0 0 0 2px white, 0 0 0 3.5px ${seg.color}40`,
              }}
            />
            <div>
              <p className="text-sm font-semibold text-gray-700">
                {seg.label}
              </p>
              <p className="text-xs text-gray-400">
                {seg.value} ({seg.percent}%)
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
