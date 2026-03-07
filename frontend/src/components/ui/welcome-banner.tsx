/**
 * WelcomeBanner
 * ─────────────
 * Full-width gradient hero banner replacing the plain "Dashboard" heading.
 * Combines GSAP entrance animations with a Lottie illustration.
 *
 * Visual design:
 *  - Gradient: #667eea → #764ba2 → #f093fb (indigo → purple → pink)
 *  - Glass overlay for depth
 *  - Three CSS-animated bokeh blobs: animate-float-slow/medium/fast
 *  - Lottie animation (analytics theme) on md+ screens
 *
 * Greeting logic:
 *  - hour < 12  → "Good Morning"
 *  - hour < 18  → "Good Afternoon"
 *  - otherwise  → "Good Evening"
 *
 * GSAP animation sequence:
 *  1. Banner slides in from y:-30, scaleY:0.8  (power3.out, 700ms)
 *  2. Text children (<p>, <h1>, <p>) cascade from x:-40 with 120ms stagger
 *  3. Lottie container scales in from 0.7  (back.out(1.5), 600ms)
 *
 * Lottie:
 *  - Hosted JSON on lottie.host — fully decorative, gracefully degrades if unavailable
 *  - Rendered via @lottiefiles/react-lottie-player <Player autoplay loop />
 *
 * @prop dateText - Formatted date shown in the subtitle (from formatDate utility)
 *
 * Libraries: GSAP + Lottie (@lottiefiles/react-lottie-player)
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef } from "react";
import { Player } from "@lottiefiles/react-lottie-player";
import { gsap } from "gsap";

interface WelcomeBannerProps {
  dateText: string;
}

// A simple, self-contained Lottie animation URL (analytics/dashboard theme)
const LOTTIE_URL =
  "https://lottie.host/4db68bbd-31f6-4cd8-84eb-189de081159a/IGmMCqhzpt.json";

export default function WelcomeBanner({ dateText }: WelcomeBannerProps) {
  const bannerRef = useRef<HTMLDivElement>(null);
  const textRef = useRef<HTMLDivElement>(null);
  const lottieContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const banner = bannerRef.current;
    const text = textRef.current;
    const lottie = lottieContainerRef.current;
    if (!banner) return;

    const tl = gsap.timeline();

    // Banner entrance
    tl.fromTo(
      banner,
      { opacity: 0, y: -30, scaleY: 0.8 },
      {
        opacity: 1,
        y: 0,
        scaleY: 1,
        duration: 0.7,
        ease: "power3.out",
      }
    );

    // Text cascade
    if (text) {
      const children = text.children;
      tl.fromTo(
        children,
        { opacity: 0, x: -40 },
        {
          opacity: 1,
          x: 0,
          duration: 0.5,
          stagger: 0.12,
          ease: "power2.out",
        },
        "-=0.3"
      );
    }

    // Lottie fade in
    if (lottie) {
      tl.fromTo(
        lottie,
        { opacity: 0, scale: 0.7 },
        { opacity: 1, scale: 1, duration: 0.6, ease: "back.out(1.5)" },
        "-=0.5"
      );
    }

    return () => {
      tl.kill();
    };
  }, []);

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return "Good Morning";
    if (hour < 18) return "Good Afternoon";
    return "Good Evening";
  };

  return (
    <div
      ref={bannerRef}
      className="relative overflow-hidden rounded-2xl border border-indigo-200/40 p-6 md:p-8"
      style={{
        opacity: 0,
        background:
          "linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%)",
      }}
    >
      {/* Glass overlay */}
      <div
        className="absolute inset-0"
        style={{
          background:
            "linear-gradient(135deg, rgba(255,255,255,0.1) 0%, rgba(255,255,255,0.05) 50%, rgba(255,255,255,0) 100%)",
        }}
      />

      {/* Animated floating shapes */}
      <div className="absolute top-4 left-[20%] w-20 h-20 rounded-full bg-white/10 blur-xl animate-float-slow" />
      <div className="absolute bottom-2 right-[30%] w-16 h-16 rounded-full bg-white/10 blur-lg animate-float-medium" />
      <div className="absolute top-1/2 right-[15%] w-12 h-12 rounded-full bg-white/10 blur-md animate-float-fast" />

      <div className="relative z-10 flex items-center justify-between">
        <div ref={textRef}>
          <p className="text-sm font-medium text-white/70 tracking-wide uppercase mb-1">
            Dashboard
          </p>
          <h1 className="text-2xl md:text-3xl font-bold text-white tracking-tight">
            {getGreeting()} 👋
          </h1>
          <p className="text-sm text-white/80 mt-2 font-medium">
            Attendance overview for{" "}
            <span className="text-white font-semibold underline decoration-white/30 underline-offset-2">
              {dateText}
            </span>
          </p>
        </div>
        <div
          ref={lottieContainerRef}
          className="hidden md:block w-32 h-32 -my-4 -mr-2"
          style={{ opacity: 0 }}
        >
          <Player
            autoplay
            loop
            src={LOTTIE_URL}
            style={{ width: "100%", height: "100%" }}
          />
        </div>
      </div>
    </div>
  );
}
