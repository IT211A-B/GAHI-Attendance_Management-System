/**
 * FloatingParticles
 * ─────────────────
 * 18 soft coloured dots that drift continuously in the background of the dashboard.
 *
 * Implementation:
 *  - Particles created via document.createElement (avoids React re-renders)
 *  - Positioned absolutely with random left/top percentages
 *  - GSAP tween per particle: repeat:-1, yoyo:true moves each to a random
 *    position over 10–20s with sine.inOut easing
 *  - Colors: muted blue, green, purple, amber, pink at 10–15% opacity
 *
 * ⚠️ Positioning note:
 *  - Container is `position: absolute; inset: 0` (NOT fixed)
 *  - This keeps particles within the dashboard scroll area and away from the sidebar
 *
 * Cleanup:
 *  - All GSAP tweens stored in tweensRef and killed on unmount
 *  - All DOM elements removed via removeChild with container.contains() guard
 *
 * Library: GSAP
 * @see ANIMATIONS.md — full documentation
 */
"use client";

import { useEffect, useRef, useCallback } from "react";
import { gsap } from "gsap";

interface Particle {
  x: number;
  y: number;
  size: number;
  color: string;
  speed: number;
  angle: number;
  el?: HTMLDivElement;
}

const COLORS = [
  "rgba(59,130,246,0.15)",
  "rgba(34,197,94,0.12)",
  "rgba(168,85,247,0.12)",
  "rgba(245,158,11,0.1)",
  "rgba(236,72,153,0.1)",
];

export default function FloatingParticles() {
  const containerRef = useRef<HTMLDivElement>(null);
  const particlesRef = useRef<Particle[]>([]);
  const tweensRef = useRef<gsap.core.Tween[]>([]);

  const createParticle = useCallback((container: HTMLDivElement): Particle => {
    const el = document.createElement("div");
    const size = Math.random() * 6 + 3;
    const color = COLORS[Math.floor(Math.random() * COLORS.length)];
    el.style.cssText = `
      position: absolute;
      width: ${size}px;
      height: ${size}px;
      border-radius: 50%;
      background: ${color};
      pointer-events: none;
      will-change: transform;
    `;
    container.appendChild(el);

    return {
      x: Math.random() * 100,
      y: Math.random() * 100,
      size,
      color,
      speed: Math.random() * 0.3 + 0.1,
      angle: Math.random() * Math.PI * 2,
      el: el as HTMLDivElement,
    };
  }, []);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const count = 18;
    const particles: Particle[] = [];

    for (let i = 0; i < count; i++) {
      particles.push(createParticle(container));
    }
    particlesRef.current = particles;

    // Animate each particle with GSAP
    particles.forEach((p) => {
      if (!p.el) return;

      // Initial position
      gsap.set(p.el, {
        left: `${p.x}%`,
        top: `${p.y}%`,
        opacity: 0,
      });

      // Fade in
      const fadeIn = gsap.to(p.el, {
        opacity: 1,
        duration: 1,
        delay: Math.random() * 2,
      });
      tweensRef.current.push(fadeIn);

      // Continuous floating — use repeat:-1 instead of recursive onComplete
      const float = gsap.to(p.el, {
        left: `${Math.random() * 90 + 5}%`,
        top: `${Math.random() * 90 + 5}%`,
        scale: 0.6 + Math.random() * 0.8,
        opacity: 0.3 + Math.random() * 0.7,
        duration: 10 + Math.random() * 10,
        ease: "sine.inOut",
        repeat: -1,
        yoyo: true,
      });
      tweensRef.current.push(float);
    });

    return () => {
      // Kill all GSAP tweens to prevent memory leaks
      tweensRef.current.forEach((tween) => tween.kill());
      tweensRef.current = [];

      // Remove DOM elements
      particles.forEach((p) => {
        if (p.el && container.contains(p.el)) {
          container.removeChild(p.el);
        }
      });
      particlesRef.current = [];
    };
  }, [createParticle]);

  return (
    <div
      ref={containerRef}
      className="absolute inset-0 pointer-events-none overflow-hidden z-0"
      aria-hidden="true"
    />
  );
}
