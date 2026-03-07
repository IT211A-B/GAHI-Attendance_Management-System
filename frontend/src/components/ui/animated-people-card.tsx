/**
 * AnimatedPeopleCard
 * ──────────────────
 * Two mini cards (Students / Staff) each with a circular SVG ring progress
 * chart and animated counter. Driven entirely by Anime.js v4.
 *
 * Animation sequence:
 *  1. Cards stagger in from translateY:30, scale:0.9 — outElastic ease
 *  2. Icons spin in from -90° at scale:0 — outBack ease
 *  3. Ring circle strokeDashoffset animates from circumference → target offset
 *     (ring fills proportionally to each person type's share of the total)
 *  4. Counter counts up from 0 to the actual value — outExpo ease
 *
 * Ring math:
 *  circumference = 2π × 36
 *  percent       = val / (students + staff)
 *  targetOffset  = circumference - (percent/100) × circumference
 *
 * @prop students - Unique student count
 * @prop staff    - Unique staff count
 *
 * Library: Anime.js v4  (named imports: animate, stagger)
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { animate, stagger } from "animejs";
import { GraduationCap, Users } from "lucide-react";

interface AnimatedPeopleCardProps {
  students: number;
  staff: number;
}

export default function AnimatedPeopleCard({
  students,
  staff,
}: AnimatedPeopleCardProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const animationsRef = useRef<ReturnType<typeof animate>[]>([]);

  useEffect(() => {
    if (!containerRef.current) return;

    // Kill previous animations
    animationsRef.current.forEach((a) => a.pause());
    animationsRef.current = [];

    const cards = containerRef.current.querySelectorAll(".people-item");
    const counters = containerRef.current.querySelectorAll(".people-count");
    const icons = containerRef.current.querySelectorAll(".people-icon");
    const rings = containerRef.current.querySelectorAll<SVGCircleElement>(".ring-progress");

    // Cards stagger entrance
    animationsRef.current.push(
      animate(cards, {
        opacity: [0, 1],
        translateY: [30, 0],
        scale: [0.9, 1],
        ease: "outElastic(1, .8)",
        duration: 1000,
        delay: stagger(150, { start: 300 }),
      })
    );

    // Icon bounce
    animationsRef.current.push(
      animate(icons, {
        scale: [0, 1],
        rotate: [-90, 0],
        ease: "outBack",
        duration: 800,
        delay: stagger(200, { start: 500 }),
      })
    );

    // Counter animation
    const values = [students, staff];
    counters.forEach((counter, i) => {
      const obj = { val: 0 };
      animationsRef.current.push(
        animate(obj, {
          val: values[i],
          ease: "outExpo",
          duration: 2000,
          delay: 600 + i * 150,
          onUpdate: () => {
            counter.textContent = String(Math.round(obj.val));
          },
        })
      );
    });

    // Ring progress animation
    rings.forEach((ring, i) => {
      const total = students + staff;
      const val = i === 0 ? students : staff;
      const percent = total > 0 ? (val / total) * 100 : 0;
      const circumference = 2 * Math.PI * 36;
      const target = circumference - (percent / 100) * circumference;

      animationsRef.current.push(
        animate(ring, {
          strokeDashoffset: [circumference, target],
          ease: "inOutCubic",
          duration: 1500,
          delay: 700 + i * 200,
        })
      );
    });

    return () => {
      animationsRef.current.forEach((a) => a.pause());
      animationsRef.current = [];
    };
  }, [students, staff]);

  const total = students + staff;
  const studentPercent = total > 0 ? (students / total) * 100 : 0;
  const staffPercent = total > 0 ? (staff / total) * 100 : 0;
  const circumference = 2 * Math.PI * 36;

  const items = [
    {
      label: "Students",
      value: students,
      percent: studentPercent,
      icon: GraduationCap,
      color: "#3b82f6",
      bgColor: "bg-blue-50",
      textColor: "text-blue-600",
    },
    {
      label: "Staff",
      value: staff,
      percent: staffPercent,
      icon: Users,
      color: "#a855f7",
      bgColor: "bg-purple-50",
      textColor: "text-purple-600",
    },
  ];

  return (
    <div ref={containerRef} className="grid grid-cols-2 gap-4">
      {items.map((item, i) => (
        <div
          key={i}
          className="people-item relative flex flex-col items-center p-4 rounded-xl bg-gradient-to-b from-gray-50/80 to-white border border-gray-100 hover:shadow-md transition-shadow"
          style={{ opacity: 0 }}
        >
          {/* Mini ring chart */}
          <div className="relative w-20 h-20 mb-3">
            <svg className="w-20 h-20 -rotate-90" viewBox="0 0 80 80">
              <circle
                cx="40"
                cy="40"
                r="36"
                fill="none"
                stroke="#f3f4f6"
                strokeWidth="5"
              />
              <circle
                className="ring-progress"
                cx="40"
                cy="40"
                r="36"
                fill="none"
                stroke={item.color}
                strokeWidth="5"
                strokeLinecap="round"
                strokeDasharray={circumference}
                strokeDashoffset={circumference}
                style={{ filter: `drop-shadow(0 0 3px ${item.color}50)` }}
              />
            </svg>
            <div className="people-icon absolute inset-0 flex items-center justify-center" style={{ transform: "scale(0)" }}>
              <div className={`p-2 rounded-lg ${item.bgColor}`}>
                <item.icon className={`h-5 w-5 ${item.textColor}`} strokeWidth={2.2} />
              </div>
            </div>
          </div>
          <span className="people-count text-2xl font-bold text-gray-900 tabular-nums">
            0
          </span>
          <span className="text-xs text-gray-500 font-medium mt-0.5">
            {item.label}
          </span>
        </div>
      ))}
    </div>
  );
}
