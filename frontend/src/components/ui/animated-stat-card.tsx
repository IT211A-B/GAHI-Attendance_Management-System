/**
 * AnimatedStatCard
 * ─────────────────
 * A stat card with GSAP-powered entrance and interactive hover animations.
 *
 * Animation sequence (runs once on mount):
 *  1. Card slides up from y:60, fades in, scales from 0.9, unflips from rotateX:15  (back.out ease)
 *  2. Icon div spins in from -180° at scale:0  (back.out(2.5) ease)
 *  3. Shimmer div sweeps left→right across the card
 *  4. `delay` prop staggers each card by  delay × 0.15 s
 *
 * Counter animation (separate effect — runs when `value` changes):
 *  - Animates from the current display value to the new value (power2.out, 1500ms)
 *  - Does NOT restart the entrance animation on data refresh (guarded by hasEnteredRef)
 *
 * Hover:
 *  - mouseenter: lifts card (y:-6), scale:1.03, colored glow boxShadow, icon rotates 15°
 *  - mouseleave: restores all properties
 *
 * Library: GSAP
 * @see src/app/dashboard/page.tsx — usage
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef, useState } from "react";
import { gsap } from "gsap";
import { cn } from "@/lib/utils";
import { LucideIcon } from "lucide-react";

interface AnimatedStatCardProps {
  title: string;
  value: number;
  icon: LucideIcon;
  description?: string;
  color?: "blue" | "green" | "yellow" | "red" | "purple";
  delay?: number;
}

const colorMap = {
  blue: {
    bg: "bg-gradient-to-br from-blue-50 to-blue-100/50",
    icon: "text-blue-600",
    iconBg: "bg-blue-100",
    border: "border-blue-200/60",
    glow: "rgba(59,130,246,0.15)",
    accent: "#3b82f6",
  },
  green: {
    bg: "bg-gradient-to-br from-green-50 to-emerald-100/50",
    icon: "text-green-600",
    iconBg: "bg-green-100",
    border: "border-green-200/60",
    glow: "rgba(34,197,94,0.15)",
    accent: "#22c55e",
  },
  yellow: {
    bg: "bg-gradient-to-br from-amber-50 to-yellow-100/50",
    icon: "text-amber-600",
    iconBg: "bg-amber-100",
    border: "border-amber-200/60",
    glow: "rgba(245,158,11,0.15)",
    accent: "#f59e0b",
  },
  red: {
    bg: "bg-gradient-to-br from-red-50 to-rose-100/50",
    icon: "text-red-600",
    iconBg: "bg-red-100",
    border: "border-red-200/60",
    glow: "rgba(239,68,68,0.15)",
    accent: "#ef4444",
  },
  purple: {
    bg: "bg-gradient-to-br from-purple-50 to-violet-100/50",
    icon: "text-purple-600",
    iconBg: "bg-purple-100",
    border: "border-purple-200/60",
    glow: "rgba(168,85,247,0.15)",
    accent: "#a855f7",
  },
};

export default function AnimatedStatCard({
  title,
  value,
  icon: Icon,
  description,
  color = "blue",
  delay = 0,
}: AnimatedStatCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const valueRef = useRef<HTMLSpanElement>(null);
  const iconRef = useRef<HTMLDivElement>(null);
  const shimmerRef = useRef<HTMLDivElement>(null);
  const [displayValue, setDisplayValue] = useState(0);
  const hasEnteredRef = useRef(false);
  const colors = colorMap[color];

  // Entrance animation — runs only once on mount
  useEffect(() => {
    const card = cardRef.current;
    const iconEl = iconRef.current;
    const shimmer = shimmerRef.current;
    if (!card || hasEnteredRef.current) return;

    hasEnteredRef.current = true;
    const tl = gsap.timeline({ delay: delay * 0.15 });

    // Card entrance - slide up + fade in with elastic ease
    tl.fromTo(
      card,
      { y: 60, opacity: 0, scale: 0.9, rotateX: 15 },
      {
        y: 0,
        opacity: 1,
        scale: 1,
        rotateX: 0,
        duration: 0.8,
        ease: "back.out(1.7)",
      }
    );

    // Icon bounce
    if (iconEl) {
      tl.fromTo(
        iconEl,
        { scale: 0, rotation: -180 },
        { scale: 1, rotation: 0, duration: 0.6, ease: "back.out(2.5)" },
        "-=0.4"
      );
    }

    // Shimmer sweep
    if (shimmer) {
      tl.fromTo(
        shimmer,
        { x: "-100%" },
        { x: "200%", duration: 0.8, ease: "power2.inOut" },
        "-=0.3"
      );
    }

    // Hover interactions
    const handleEnter = () => {
      gsap.to(card, {
        y: -6,
        scale: 1.03,
        boxShadow: `0 20px 40px ${colors.glow}, 0 8px 16px rgba(0,0,0,0.06)`,
        duration: 0.3,
        ease: "power2.out",
      });
      if (iconEl) {
        gsap.to(iconEl, {
          rotation: 15,
          scale: 1.15,
          duration: 0.3,
          ease: "power2.out",
        });
      }
    };

    const handleLeave = () => {
      gsap.to(card, {
        y: 0,
        scale: 1,
        boxShadow: "0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)",
        duration: 0.3,
        ease: "power2.out",
      });
      if (iconEl) {
        gsap.to(iconEl, {
          rotation: 0,
          scale: 1,
          duration: 0.3,
          ease: "power2.out",
        });
      }
    };

    card.addEventListener("mouseenter", handleEnter);
    card.addEventListener("mouseleave", handleLeave);

    return () => {
      card.removeEventListener("mouseenter", handleEnter);
      card.removeEventListener("mouseleave", handleLeave);
      tl.kill();
    };
  }, [delay, colors.glow]);

  // Counter animation — runs when value changes
  useEffect(() => {
    const counterTween = gsap.to(
      { val: displayValue },
      {
        val: value,
        duration: hasEnteredRef.current ? 1.5 : 0.01,
        ease: "power2.out",
        onUpdate: function () {
          setDisplayValue(Math.round(this.targets()[0].val));
        },
      }
    );

    return () => {
      counterTween.kill();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);

  return (
    <div
      ref={cardRef}
      className={cn(
        "relative overflow-hidden rounded-2xl border p-6 cursor-default",
        "bg-white/80 backdrop-blur-sm",
        colors.border
      )}
      style={{
        opacity: 0,
        perspective: "1000px",
        boxShadow: "0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)",
      }}
    >
      {/* Shimmer overlay */}
      <div
        ref={shimmerRef}
        className="absolute inset-0 pointer-events-none"
        style={{
          background:
            "linear-gradient(90deg, transparent 0%, rgba(255,255,255,0.4) 50%, transparent 100%)",
          transform: "translateX(-100%)",
        }}
      />

      {/* Decorative corner accent */}
      <div
        className="absolute -top-8 -right-8 w-24 h-24 rounded-full opacity-[0.07]"
        style={{ background: colors.accent }}
      />
      <div
        className="absolute -bottom-4 -left-4 w-16 h-16 rounded-full opacity-[0.05]"
        style={{ background: colors.accent }}
      />

      <div className="relative z-10 flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-500 tracking-wide uppercase">
            {title}
          </p>
          <p className="mt-2">
            <span
              ref={valueRef}
              className="text-3xl font-extrabold text-gray-900 tabular-nums"
            >
              {displayValue.toLocaleString()}
            </span>
          </p>
          {description && (
            <p className="text-xs text-gray-400 mt-1.5">{description}</p>
          )}
        </div>
        <div
          ref={iconRef}
          className={cn(
            "p-3.5 rounded-xl shadow-sm",
            colors.iconBg
          )}
        >
          <Icon className={cn("h-6 w-6", colors.icon)} strokeWidth={2.2} />
        </div>
      </div>
    </div>
  );
}
