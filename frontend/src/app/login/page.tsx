/**
 * @file login/page.tsx
 * @description Animated login page for DonBosco AMS.
 *
 * Split-screen layout:
 *   LEFT  – dark branded panel with animated SVG constellation network,
 *           floating geometric shapes, and typing tagline (Anime.js)
 *   RIGHT – glassmorphic login card with staggered field entrances,
 *           error shake, and button ripple (Anime.js)
 *
 * Libraries: anime.js v4 (named imports only)
 */
"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Shield, Eye, EyeOff, Lock, User, ChevronRight } from "lucide-react";
import { authService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import { notify, extractErrorMessage } from "@/lib/toast";
import { APP_NAME, APP_ORG, APP_FULL_NAME } from "@/lib/constants";
import { animate, stagger } from "animejs";

/* ─────────────────────── helpers ─────────────────────── */

/** Generate random constellation nodes for the left panel SVG */
function generateNodes(count: number, w: number, h: number) {
  return Array.from({ length: count }, () => ({
    x: Math.random() * w,
    y: Math.random() * h,
    r: Math.random() * 2.5 + 1,
  }));
}

/** Build edges between nearby nodes */
function generateEdges(
  nodes: { x: number; y: number }[],
  maxDist: number
) {
  const edges: { x1: number; y1: number; x2: number; y2: number }[] = [];
  for (let i = 0; i < nodes.length; i++) {
    for (let j = i + 1; j < nodes.length; j++) {
      const dx = nodes[i].x - nodes[j].x;
      const dy = nodes[i].y - nodes[j].y;
      if (Math.sqrt(dx * dx + dy * dy) < maxDist) {
        edges.push({
          x1: nodes[i].x,
          y1: nodes[i].y,
          x2: nodes[j].x,
          y2: nodes[j].y,
        });
      }
    }
  }
  return edges;
}

/* ─────────────────────── component ─────────────────────── */

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const router = useRouter();
  const setUser = useAuthStore((s) => s.setUser);

  /* refs */
  const formRef = useRef<HTMLFormElement>(null);
  const cardRef = useRef<HTMLDivElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const taglineRef = useRef<HTMLParagraphElement>(null);
  const leftPanelRef = useRef<HTMLDivElement>(null);
  const animsRef = useRef<ReturnType<typeof animate>[]>([]);
  const hasAnimated = useRef(false);
  const hoverAnimRef = useRef<ReturnType<typeof animate> | null>(null);
  const mountedRef = useRef(true);

  /* ── constellation data (deferred to avoid SSR hydration mismatch) ── */
  const [constellationData, setConstellationData] = useState<{
    nodes: { x: number; y: number; r: number }[];
    edges: { x1: number; y1: number; x2: number; y2: number }[];
  } | null>(null);

  useEffect(() => {
    const nodes = generateNodes(40, 600, 800);
    const edges = generateEdges(nodes, 140);
    setConstellationData({ nodes, edges });
    return () => { mountedRef.current = false; };
  }, []);

  /* ── entrance animations ── */
  useEffect(() => {
    if (hasAnimated.current || !constellationData) return;
    hasAnimated.current = true;

    // Small delay to let DOM mount
    const t = setTimeout(() => {
      /* — left panel logo slam — */
      if (leftPanelRef.current) {
        const logo = leftPanelRef.current.querySelector("[data-logo]");
        if (logo) {
          animsRef.current.push(
            animate(logo, {
              scale: [0, 1],
              rotate: [-180, 0],
              opacity: [0, 1],
              ease: "outBack(2)",
              duration: 900,
            })
          );
        }

        /* — tagline typing — */
        if (taglineRef.current) {
          const text = APP_FULL_NAME;
          taglineRef.current.textContent = "";
          const chars = text.split("").map((ch) => {
            const span = document.createElement("span");
            span.textContent = ch;
            span.style.opacity = "0";
            taglineRef.current!.appendChild(span);
            return span;
          });
          animsRef.current.push(
            animate(chars, {
              opacity: [0, 1],
              translateY: [8, 0],
              delay: stagger(35, { start: 600 }),
              ease: "outExpo",
              duration: 400,
            })
          );
        }

        /* — constellation nodes pulse in — */
        const dots = leftPanelRef.current.querySelectorAll("[data-dot]");
        if (dots.length && constellationData) {
          animsRef.current.push(
            animate(dots, {
              opacity: { from: 0, to: 0.8, ease: "outExpo" },
              r: { from: 0, to: (_: unknown, i: number) => constellationData.nodes[i]?.r ?? 2, ease: "outElastic(2, .5)" },
              delay: stagger(30, { start: 400 }),
              duration: 1000,
            })
          );
        }

        /* — constellation edges draw-in — */
        const lines = leftPanelRef.current.querySelectorAll("[data-edge]");
        if (lines.length) {
          animsRef.current.push(
            animate(lines, {
              opacity: [0, 0.15],
              delay: stagger(20, { start: 600 }),
              duration: 800,
              ease: "outSine",
            })
          );
        }

        /* — floating shapes — */
        const shapes = leftPanelRef.current.querySelectorAll("[data-shape]");
        if (shapes.length) {
          animsRef.current.push(
            animate(shapes, {
              translateY: [40, 0],
              opacity: [0, 0.15],
              rotate: [45, 0],
              delay: stagger(150, { start: 800 }),
              duration: 1200,
              ease: "outQuint",
            })
          );
          // continuous float
          animsRef.current.push(
            animate(shapes, {
              translateY: [-12, 12],
              rotate: [-5, 5],
              delay: stagger(300),
              duration: 4000,
              ease: "inOutSine",
              loop: true,
              alternate: true,
            })
          );
        }
      }

      /* — card entrance — */
      if (cardRef.current) {
        animsRef.current.push(
          animate(cardRef.current, {
            opacity: [0, 1],
            translateY: [50, 0],
            scale: [0.95, 1],
            ease: "outExpo",
            duration: 1000,
            delay: 200,
          })
        );
      }

      /* — form fields stagger in — */
      if (formRef.current) {
        const fields = formRef.current.querySelectorAll("[data-field]");
        if (fields.length) {
          animsRef.current.push(
            animate(fields, {
              opacity: [0, 1],
              translateX: [-30, 0],
              delay: stagger(120, { start: 600 }),
              duration: 800,
              ease: "outExpo",
            })
          );
        }
      }
    }, 100);

    return () => {
      clearTimeout(t);
      animsRef.current.forEach((a) => a.pause());
      animsRef.current = [];
    };
  }, [constellationData]);

  /* ── error shake ── */
  useEffect(() => {
    if (!error || !errorRef.current) return;
    const shakeAnim = animate(errorRef.current, {
      translateX: [-8, 8, -6, 6, -3, 3, 0],
      duration: 500,
      ease: "outElastic(2, .4)",
    });
    animsRef.current.push(shakeAnim);
    return () => { shakeAnim.pause(); };
  }, [error]);

  /* ── form submit ── */
  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setError("");
      setIsLoading(true);

      try {
        const res = await authService.login({ username, password });
        if (res.success && res.data) {
          setUser(res.data);
          notify.success("Welcome back!");

          /* success — quick scale-down before navigate */
          if (cardRef.current) {
            animate(cardRef.current, {
              scale: [1, 0.96],
              opacity: [1, 0],
              duration: 350,
              ease: "inQuad",
            });
          }
          setTimeout(() => {
            if (mountedRef.current) router.push("/dashboard");
          }, 380);
        } else {
          setError(res.message || "Login failed");
        }
      } catch (err: unknown) {
        setError(extractErrorMessage(err));
      } finally {
        setIsLoading(false);
      }
    },
    [username, password, setUser, router]
  );

  /* ── button hover scale ── */
  const handleBtnEnter = useCallback(
    (e: React.MouseEvent<HTMLButtonElement>) => {
      if (hoverAnimRef.current) hoverAnimRef.current.pause();
      hoverAnimRef.current = animate(e.currentTarget, {
        scale: [1, 1.03],
        duration: 250,
        ease: "outBack(3)",
      });
    },
    []
  );
  const handleBtnLeave = useCallback(
    (e: React.MouseEvent<HTMLButtonElement>) => {
      if (hoverAnimRef.current) hoverAnimRef.current.pause();
      hoverAnimRef.current = animate(e.currentTarget, {
        scale: [1.03, 1],
        duration: 300,
        ease: "outSine",
      });
    },
    []
  );

  /* ─────────── render ─────────── */
  return (
    <div className="min-h-screen flex flex-col lg:flex-row bg-gray-950">
      {/* ──────── LEFT PANEL ──────── */}
      <div
        ref={leftPanelRef}
        className="hidden lg:flex lg:w-[48%] relative overflow-hidden items-center justify-center bg-gradient-to-br from-indigo-950 via-blue-950 to-slate-950"
      >
        {/* SVG constellation */}
        <svg
          ref={svgRef}
          className="absolute inset-0 w-full h-full"
          viewBox="0 0 600 800"
          preserveAspectRatio="xMidYMid slice"
        >
          {constellationData?.edges.map((e, i) => (
            <line
              key={`e${i}`}
              data-edge
              x1={e.x1}
              y1={e.y1}
              x2={e.x2}
              y2={e.y2}
              stroke="#60a5fa"
              strokeWidth="0.5"
              opacity="0"
            />
          ))}
          {constellationData?.nodes.map((n, i) => (
            <circle
              key={`d${i}`}
              data-dot
              cx={n.x}
              cy={n.y}
              r={n.r}
              fill="#93c5fd"
              opacity="0"
            />
          ))}
        </svg>

        {/* Floating geometric shapes */}
        <div className="absolute inset-0 pointer-events-none">
          <div
            data-shape
            className="absolute top-[15%] left-[10%] w-20 h-20 border border-blue-500/20 rounded-2xl opacity-0"
          />
          <div
            data-shape
            className="absolute top-[60%] left-[20%] w-14 h-14 border border-indigo-400/20 rounded-full opacity-0"
          />
          <div
            data-shape
            className="absolute top-[35%] right-[15%] w-24 h-24 border border-cyan-400/15 rotate-45 opacity-0"
          />
          <div
            data-shape
            className="absolute bottom-[20%] right-[25%] w-16 h-16 border border-blue-300/15 rounded-xl opacity-0"
          />
          <div
            data-shape
            className="absolute top-[75%] left-[55%] w-10 h-10 border border-violet-400/20 rounded-full opacity-0"
          />
        </div>

        {/* Center branding */}
        <div className="relative z-10 text-center px-12 max-w-md">
          {/* Logo */}
          <div
            data-logo
            className="inline-flex items-center justify-center w-24 h-24 rounded-3xl bg-gradient-to-br from-blue-500 to-indigo-600 mb-8 shadow-2xl shadow-blue-500/25 opacity-0"
          >
            <Shield className="w-12 h-12 text-white" />
          </div>

          {/* App name */}
          <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-200 via-white to-indigo-200 bg-clip-text text-transparent mb-4">
            {APP_NAME}
          </h1>

          {/* Typing tagline */}
          <p
            ref={taglineRef}
            className="text-blue-300/70 text-lg font-light tracking-wide min-h-[1.75rem]"
          />

          {/* Decorative divider */}
          <div className="mt-8 flex items-center justify-center gap-3">
            <span className="h-px w-12 bg-gradient-to-r from-transparent to-blue-500/40" />
            <span className="w-2 h-2 rounded-full bg-blue-400/40" />
            <span className="h-px w-12 bg-gradient-to-l from-transparent to-blue-500/40" />
          </div>

          <p className="mt-6 text-sm text-blue-400/50 font-medium tracking-widest uppercase">
            Attendance Management
          </p>
        </div>
      </div>

      {/* ──────── RIGHT PANEL ──────── */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-12 bg-gradient-to-br from-gray-50 via-white to-blue-50/30">
        <div className="w-full max-w-[420px]">
          {/* Mobile logo (shown only on small screens) */}
          <div className="lg:hidden text-center mb-8">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 mb-3 shadow-lg shadow-blue-500/20">
              <Shield className="w-7 h-7 text-white" />
            </div>
            <h1 className="text-xl font-bold text-gray-900">{APP_NAME}</h1>
          </div>

          {/* Login card */}
          <div
            ref={cardRef}
            className="bg-white/80 backdrop-blur-xl rounded-3xl shadow-xl shadow-gray-200/50 border border-gray-100/80 p-8 sm:p-10 opacity-0"
          >
            {/* Header */}
            <div className="mb-8">
              <h2 className="text-2xl font-bold text-gray-900">Welcome back</h2>
              <p className="text-sm text-gray-500 mt-1">
                Sign in to continue to your dashboard
              </p>
            </div>

            {/* Error banner */}
            {error && (
              <div
                ref={errorRef}
                className="mb-6 p-3.5 bg-red-50 border border-red-200/80 rounded-xl flex items-start gap-2.5"
              >
                <div className="mt-0.5 shrink-0 w-5 h-5 rounded-full bg-red-100 flex items-center justify-center">
                  <span className="text-red-500 text-xs font-bold">!</span>
                </div>
                <p className="text-sm text-red-600 font-medium">{error}</p>
              </div>
            )}

            {/* Form */}
            <form ref={formRef} onSubmit={handleSubmit} className="space-y-5">
              {/* Username */}
              <div data-field className="opacity-0">
                <label
                  htmlFor="username"
                  className="block text-sm font-semibold text-gray-700 mb-1.5"
                >
                  Username
                </label>
                <div className="relative group">
                  <div className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400 group-focus-within:text-blue-500 transition-colors">
                    <User className="w-4 h-4" />
                  </div>
                  <input
                    id="username"
                    type="text"
                    placeholder="Enter your username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required
                    autoComplete="username"
                    className="w-full pl-10 pr-4 py-3 rounded-xl border border-gray-200 bg-gray-50/50 text-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
                  />
                </div>
              </div>

              {/* Password */}
              <div data-field className="opacity-0">
                <label
                  htmlFor="password"
                  className="block text-sm font-semibold text-gray-700 mb-1.5"
                >
                  Password
                </label>
                <div className="relative group">
                  <div className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400 group-focus-within:text-blue-500 transition-colors">
                    <Lock className="w-4 h-4" />
                  </div>
                  <input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    placeholder="Enter your password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    autoComplete="current-password"
                    className="w-full pl-10 pr-12 py-3 rounded-xl border border-gray-200 bg-gray-50/50 text-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                    aria-label={
                      showPassword ? "Hide password" : "Show password"
                    }
                  >
                    {showPassword ? (
                      <EyeOff className="w-4 h-4" />
                    ) : (
                      <Eye className="w-4 h-4" />
                    )}
                  </button>
                </div>
              </div>

              {/* Submit */}
              <div data-field className="opacity-0 pt-2">
                <button
                  type="submit"
                  disabled={isLoading}
                  onMouseEnter={handleBtnEnter}
                  onMouseLeave={handleBtnLeave}
                  className="relative w-full flex items-center justify-center gap-2 py-3.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-semibold text-sm shadow-lg shadow-blue-500/25 hover:shadow-blue-500/40 focus:outline-none focus:ring-2 focus:ring-blue-500/40 focus:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed transition-shadow"
                >
                  {isLoading ? (
                    <svg
                      className="animate-spin h-5 w-5"
                      viewBox="0 0 24 24"
                      fill="none"
                    >
                      <circle
                        className="opacity-25"
                        cx="12"
                        cy="12"
                        r="10"
                        stroke="currentColor"
                        strokeWidth="4"
                      />
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                      />
                    </svg>
                  ) : (
                    <>
                      Sign in
                      <ChevronRight className="w-4 h-4" />
                    </>
                  )}
                </button>
              </div>
            </form>

            {/* Security note */}
            <div className="mt-8 flex items-center justify-center gap-2 text-xs text-gray-400">
              <Lock className="w-3 h-3" />
              <span>Secured with encrypted authentication</span>
            </div>
          </div>

          {/* Footer */}
          <p
            className="text-center text-xs text-gray-400 mt-6"
            suppressHydrationWarning
          >
            &copy; {new Date().getFullYear()} {APP_ORG}. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  );
}
