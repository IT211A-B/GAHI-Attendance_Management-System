# Dashboard Animation System

Documentation for the animated dashboard feature built on top of the Attendance Management System frontend.
Covers every decision, component, animation library choice, bug fix, and usage guide.

---

## Table of Contents

1. [Overview](#overview)
2. [Libraries Added](#libraries-added)
3. [Architecture](#architecture)
4. [Components](#components)
   - [AnimatedStatCard](#animatedstatcard)
   - [AnimatedDonutChart](#animateddonutchart)
   - [AnimatedAttendanceBar](#animatedattendancebar)
   - [AnimatedDepartmentTable](#animateddepartmenttable)
   - [AnimatedPeopleCard](#animatedpeoplecard)
   - [WelcomeBanner](#welcomebanner)
   - [FloatingParticles](#floatingparticles)
   - [LivePulseIndicator](#livepulseindicator)
5. [CSS Additions](#css-additions)
6. [Dashboard Page Composition](#dashboard-page-composition)
7. [Bugs Found & Fixed](#bugs-found--fixed)
8. [Design Decisions](#design-decisions)
9. [File Map](#file-map)

---

## Overview

The dashboard at `/dashboard` was redesigned with rich, performant animations using three complementary libraries:

| Library | Role |
|---------|------|
| **GSAP** | Layout-level animations — card entrances, hover states, skeleton loaders, table staggering, shimmer sweeps |
| **Anime.js v4** | Data-driven SVG/DOM animations — donut chart draw-in, ring progress, counters, pulse loops |
| **Lottie** | Decorative illustration animation in the welcome banner (JSON-based) |

All original API integration, data types, and service calls remained unchanged.

---

## Libraries Added

```json
"gsap": "^3.x",
"@lottiefiles/react-lottie-player": "^3.x",
"animejs": "^4.3.6",
"@types/animejs": "^3.x"
```

Install command used:

```bash
npm install gsap @lottiefiles/react-lottie-player animejs @types/animejs
```

> **Note:** `animejs` v4 dropped the default export. All usage must use named imports:
> ```ts
> import { animate, stagger } from "animejs";
> ```

---

## Architecture

```
src/
├── app/
│   ├── globals.css                        ← Added float keyframes + glass-card utility
│   └── dashboard/
│       └── page.tsx                       ← Rewritten: composes all animated components
└── components/
    └── ui/
        ├── animated-stat-card.tsx         ← GSAP
        ├── animated-donut-chart.tsx       ← Anime.js
        ├── animated-attendance-bar.tsx    ← GSAP
        ├── animated-department-table.tsx  ← GSAP
        ├── animated-people-card.tsx       ← Anime.js
        ├── welcome-banner.tsx             ← Lottie + GSAP
        ├── floating-particles.tsx         ← GSAP
        └── live-pulse-indicator.tsx       ← Anime.js
```

Each component is fully self-contained with its own animation lifecycle — setup on mount, cleanup on unmount.

---

## Components

---

### AnimatedStatCard

**File:** `src/components/ui/animated-stat-card.tsx`  
**Library:** GSAP  
**Replaces:** `StatCard` from `@/components/ui`

#### Animation Sequence (on mount)

1. Card slides up from `y:60`, fades in from `opacity:0`, scales from `0.9`, unflips from `rotateX:15` — elastic `back.out(1.7)` ease, 800ms
2. Icon div spins in from `-180deg` rotation at scale `0` — `back.out(2.5)` ease, 600ms
3. Shimmer div sweeps across the card left→right, 800ms
4. Cards are staggered via the `delay` prop (each card adds 0.15s to its timeline delay)

#### Counter Animation (on value change — separate from entrance)

- Animates from the current display value to the new `value` prop
- Uses `power2.out` easing over 1500ms
- Separated from entrance so data refresh **does not** restart the card entrance

#### Hover Effects

- `mouseenter`: lifts card `y:-6`, scales to `1.03`, adds colored glow `boxShadow`, rotates icon `15deg` at `scale:1.15`
- `mouseleave`: restores all properties

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `title` | `string` | required | Label above the number |
| `value` | `number` | required | The stat number to count up to |
| `icon` | `LucideIcon` | required | Lucide icon component |
| `description` | `string?` | — | Small text below the value |
| `color` | `"blue"\|"green"\|"yellow"\|"red"\|"purple"` | `"blue"` | Color theme |
| `delay` | `number` | `0` | Stagger index (multiplied by 0.15s) |

---

### AnimatedDonutChart

**File:** `src/components/ui/animated-donut-chart.tsx`  
**Library:** Anime.js v4

#### How the Donut is Built

- Pure SVG using `<circle>` elements with `strokeDasharray` / `strokeDashoffset`
- 3 segments: On Time (green `#22c55e`), Late (amber `#f59e0b`), Absent (red `#ef4444`)
- 4° gaps between each segment, all fitting within the 360° circle
- Each segment stores its target draw length in a `data-seglen` attribute

#### Animation Sequence

1. Each segment starts at `strokeDasharray="0 circumference"` (invisible)
2. Anime.js interpolates `drawLen: 0 → segmentLength`, updating `strokeDasharray` on every frame via `onUpdate`
3. Segments draw in sequentially — each delayed by 300ms after the previous (`delay: 400 + i * 300`)
4. Legend items fade in with `translateY: 10→0` stagger starting at 1200ms
5. Center counter counts up from 0 to `total` over 2000ms

#### Positioning Logic

The segment offsets are computed so each segment picks up where the previous left off:
- Segment 0 (On Time): `offset = 0`
- Segment 1 (Late): `offset = -(onTimeLen + gapLen)`
- Segment 2 (Absent): `offset = -(onTimeLen + lateLen + gapLen * 2)`

The `rotate(-90 90 90)` SVG transform ensures drawing starts from the 12 o'clock position.

#### Props

| Prop | Type | Description |
|------|------|-------------|
| `onTime` | `number` | Count of on-time attendees |
| `late` | `number` | Count of late attendees |
| `absent` | `number` | Count of absent |

Renders "No data to display" if all three are `0`.

---

### AnimatedAttendanceBar

**File:** `src/components/ui/animated-attendance-bar.tsx`  
**Library:** GSAP

#### Animation

1. Fill bar width animates from `0%` to `percentage%` via `power3.out` over 1200ms
2. Counter text updates simultaneously — formats as `"{count} ({pct}%)"` 
3. `delay` prop staggers each bar (adds `delay * 0.2s`)
4. A subtle pulsing glow dot sits at the leading edge of the filled bar

#### Props

| Prop | Type | Description |
|------|------|-------------|
| `label` | `string` | "On Time", "Late", or "Absent" |
| `count` | `number` | Raw count |
| `total` | `number` | Total scans (used for percentage calculation) |
| `color` | `string` | Tailwind bg class e.g. `"bg-green-500"` |
| `accentHex` | `string` | Hex color for the glow dot e.g. `"#22c55e"` |
| `delay` | `number` | Stagger index |

---

### AnimatedDepartmentTable

**File:** `src/components/ui/animated-department-table.tsx`  
**Library:** GSAP

#### Animation Sequence

1. Header row fades in from `y:-10` over 400ms
2. All data rows stagger in from `x:-30, opacity:0, scale:0.98` — 80ms between each row
3. Rate bar fills animate from `0%` to their target `attendanceRate * 100%`, staggered in sync with rows

#### Visual Enhancements

- Status pills for Present/Late/Absent with colored backgrounds
- Rate bars colored by threshold: green ≥ 90%, amber ≥ 70%, red below 70%
- Drop-shadow glow on rate bars matching their color
- Hover: whole row highlights, department name turns blue

#### Props

| Prop | Type | Description |
|------|------|-------------|
| `departments` | `DepartmentAttendanceSummary[]` | Array from API response |

---

### AnimatedPeopleCard

**File:** `src/components/ui/animated-people-card.tsx`  
**Library:** Anime.js v4

Displays Students vs Staff as two side-by-side mini cards, each with a circular ring progress chart.

#### Animation Sequence

1. Both cards stagger in from `translateY:30, scale:0.9` with elastic ease
2. Icons spin in from `-90deg` at `scale:0` 
3. Ring SVG progress animates from `strokeDashoffset=circumference` (empty) to the target offset
4. Counter inside each card counts up from 0

#### Ring Chart Math

```
circumference = 2π × 36
percent = val / (students + staff)
targetOffset = circumference - (percent / 100) × circumference
```

#### Props

| Prop | Type | Description |
|------|------|-------------|
| `students` | `number` | Unique student count |
| `staff` | `number` | Unique staff count |

---

### WelcomeBanner

**File:** `src/components/ui/welcome-banner.tsx`  
**Libraries:** GSAP + Lottie (`@lottiefiles/react-lottie-player`)

Replaces the plain `<h1>Dashboard</h1>` header with a full-width gradient banner.

#### Visual Design

- Gradient background: `#667eea → #764ba2 → #f093fb` (indigo → purple → pink)
- Glass overlay for depth
- Three animated bokeh blobs using CSS keyframe animations (`animate-float-slow/medium/fast`)
- Responsive: Lottie illustration hidden on mobile (`hidden md:block`)

#### Greeting Logic

```ts
const hour = new Date().getHours();
if (hour < 12)  return "Good Morning";
if (hour < 18)  return "Good Afternoon";
             return "Good Evening";
```

#### GSAP Animation

1. Banner slides down from `y:-30, scaleY:0.8` — `power3.out` 700ms
2. Text children (`<p>`, `<h1>`, `<p>`) cascade in from `x:-40` with 120ms stagger
3. Lottie container scales in from `0.7` — `back.out(1.5)` 600ms

#### Lottie

- Source: hosted JSON on `lottie.host` (analytics/chart theme)
- Rendered via `<Player autoplay loop src={URL} />` from `@lottiefiles/react-lottie-player`
- Entirely decorative — does not affect functionality if the URL is unavailable

#### Props

| Prop | Type | Description |
|------|------|-------------|
| `dateText` | `string` | Formatted date shown in the subtitle |

---

### FloatingParticles

**File:** `src/components/ui/floating-particles.tsx`  
**Library:** GSAP

Renders 18 soft, semi-transparent coloured dots that drift continuously within the dashboard content area.

#### Implementation

- Particles are created as `<div>` elements appended directly to the container (avoids React re-renders)
- Each particle is positioned absolutely with `left/top` percentages
- GSAP `repeat: -1, yoyo: true` tween moves each dot to a random position over 10–20s
- Colors: muted blue, green, purple, amber, pink at 10–15% opacity

#### Positioning

Uses `position: absolute` on the container so particles stay within the dashboard scroll area and do not overlap other pages or the sidebar. (Was `fixed` originally — patched to `absolute`.)

#### Cleanup

On unmount: all GSAP tweens killed via `tweensRef`, all DOM elements removed via `removeChild`.

#### Props

None — fully self-contained.

---

### LivePulseIndicator

**File:** `src/components/ui/live-pulse-indicator.tsx`  
**Library:** Anime.js v4

Shows a pulsing green dot with an expanding ring and animated "LIVE" text. Used in card headers.

#### Animations (all `loop: true`)

- Dot: `scale 1→1.3→1, opacity 1→0.7→1` over 1500ms `inOutSine`
- Ring: `scale 1→2.5, opacity 0.5→0` over 2000ms `outExpo` — expanding ripple
- Text: `opacity 0.6→1→0.6` over 2000ms `inOutSine` — breathing glow

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `isActive` | `boolean` | `true` | When false, no animations run |

---

## CSS Additions

Added to `src/app/globals.css`:

```css
/* Three float speed variants used by WelcomeBanner bokeh blobs */
@keyframes float-slow   { ... }   /* 8s */
@keyframes float-medium { ... }   /* 6s */
@keyframes float-fast   { ... }   /* 4s */

.animate-float-slow   { animation: float-slow   8s ease-in-out infinite; }
.animate-float-medium { animation: float-medium 6s ease-in-out infinite; }
.animate-float-fast   { animation: float-fast   4s ease-in-out infinite; }

/* Utility for glass morphism cards (available globally) */
.glass-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}
```

---

## Dashboard Page Composition

**File:** `src/app/dashboard/page.tsx`

### State

Same as original: `report`, `isLoading`, `hasError`, `loadDashboard`.

Added: `loaderRef` for the GSAP skeleton loader animation.

### Loading State

- 3-block skeleton: banner shape, 4 stat card shapes, 2 panel shapes
- GSAP `repeat:-1, yoyo:true` pulsing opacity animation on all `.skeleton-block` elements
- Tween killed in useEffect cleanup

### Error State

- Shows WelcomeBanner (still functional)
- Error card with AlertTriangle icon + Retry button

### Main Layout

```
<FloatingParticles />          ← absolute, behind everything
<WelcomeBanner />              ← full-width gradient header
<grid 4-col>
  <AnimatedStatCard × 4 />    ← Total Scans, On Time, Late, Absent
</grid>
<grid 2-col>
  <Card>
    <AnimatedDonutChart />     ← distribution pie
  </Card>
  <Card>
    <AnimatedPeopleCard />     ← students + staff rings
    <AnimatedAttendanceBar × 3 /> ← breakdown bars
  </Card>
</grid>
<Card>
  <AnimatedDepartmentTable />  ← conditional on data presence
</Card>
```

---

## Bugs Found & Fixed

The following bugs were discovered during the audit pass after initial implementation:

### Bug 1 — Donut chart draw-in broken (visual glitch)

**File:** `animated-donut-chart.tsx`  
**Problem:** The original code called `path.getTotalLength()` and animated `strokeDashoffset` from `totalLength → 0`. SVG `<circle>` elements do not expose `getTotalLength()` in all browsers (it's a method on `SVGGeometryElement` but circles need the DOM to be mounted). More critically, animating `strokeDashoffset` to `0` on segments that use `strokeDashoffset` for **positioning** caused all three segments to overlap at the 12 o'clock position.  
**Fix:** Store the target length in `data-seglen`. Start each segment at `strokeDasharray="0 circumference"`. Use `onUpdate` to animate `strokeDasharray` from `"0 C"` → `"segLen (C-segLen)"` while keeping `strokeDashoffset` (position) constant.

### Bug 2 — Donut chart no animation cleanup

**File:** `animated-donut-chart.tsx`  
**Problem:** No `return` cleanup in `useEffect` — all anime.js animations kept running after component unmount.  
**Fix:** Track all `animate()` calls in `animationsRef`. Call `.pause()` on each in the cleanup function.

### Bug 3 — Floating particles memory leak

**File:** `floating-particles.tsx`  
**Problem:** Each particle's GSAP animation used `onComplete: animate` (recursive self-call). On unmount only the DOM elements were removed, but all the queued GSAP tweens continued running, accumulating indefinitely.  
**Fix:** Replaced recursive `onComplete` with `repeat: -1, yoyo: true`. All tweens stored in `tweensRef` and killed with `.kill()` in cleanup.

### Bug 4 — Particles covering the sidebar

**File:** `floating-particles.tsx`  
**Problem:** Container used `position: fixed; inset: 0` which overlaid the full viewport including the navigation sidebar.  
**Fix:** Changed to `position: absolute; inset: 0` so particles are confined to the dashboard content area (its parent is `position: relative`).

### Bug 5 — LivePulseIndicator leak

**File:** `live-pulse-indicator.tsx`  
**Problem:** Three `loop: true` anime.js animations started with no cleanup return, running forever after unmount.  
**Fix:** Added `animationsRef` tracking and cleanup.

### Bug 6 — AnimatedPeopleCard leak

**File:** `animated-people-card.tsx`  
**Problem:** All anime.js animations (stagger entrances, ring fills, counters) had no cleanup.  
**Fix:** Added `animationsRef` tracking and cleanup.

### Bug 7 — StatCard re-entrance on data refresh

**File:** `animated-stat-card.tsx`  
**Problem:** The GSAP `useEffect` had `[value, delay, colors.glow]` as dependencies. When `value` changed (e.g. after a manual Retry or future auto-refresh), the full entrance animation re-ran — card opacity snapped to `0`, slid up from `y:60`, and re-entered. This made the dashboard visually break on any refresh.  
**Fix:** Separated into **two** effects:
1. Entrance effect — deps `[delay, colors.glow]` + `hasEnteredRef` guard so it runs exactly once per mount
2. Counter effect — deps `[value]` only, animates number from current to new value smoothly

### Bug 8 — Skeleton loader tween never killed

**File:** `dashboard/page.tsx`  
**Problem:** `gsap.fromTo(skeletons, ..., { repeat: -1 })` was called but the return value was discarded — the tween was never killed when loading finished and the component re-rendered.  
**Fix:** Captured the tween in a variable, added cleanup `return () => { tween.kill(); }`.

---

## Design Decisions

### Why GSAP for layout animations?

GSAP's `gsap.fromTo` + `rotation`/`scale`/`y` transforms are GPU-accelerated and avoid layout thrashing. The timeline API makes sequencing (entrance → counter → shimmer → hover setup) clean and deterministic.

### Why Anime.js for SVG/data animations?

Anime.js v4's `animate(svgElement, { strokeDashoffset: [...] })` integrates cleanly with SVG attribute animation. GSAP can do it too but requires a plugin. For data-driven counters and ring charts, Anime.js `onUpdate` pattern with a plain object proxy is cleaner.

### Why Lottie only in the banner?

Lottie files are heavy (large JSON, runtime renderer). Using it only for the decorative banner illustration keeps the performance budget reasonable. All data-driven animations use GSAP/Anime.js instead.

### Why not CSS animations for everything?

CSS animations cannot:
- Drive number counters
- Respond to dynamic data (SVG dash arrays, variable widths)
- Be precisely sequenced with stagger timing
- Be killed mid-flight on unmount

CSS keyframes are used only for the autonomous `float-slow/medium/fast` bokeh blobs (no JS needed, no cleanup needed).

### Stat cards use `opacity: 0` initial style

Cards start with `opacity: 0` via inline style (not CSS class) so the initial invisible state is applied at SSR/paint time before GSAP loads, preventing a flash of visible-then-animated cards.

---

## File Map

| File | Lines | Added/Modified | Library |
|------|-------|---------------|---------|
| `src/app/dashboard/page.tsx` | 241 | **Modified** | GSAP |
| `src/app/globals.css` | 86 | **Modified** | CSS |
| `src/components/ui/animated-stat-card.tsx` | 243 | **New** | GSAP |
| `src/components/ui/animated-donut-chart.tsx` | ~200 | **New** | Anime.js |
| `src/components/ui/animated-attendance-bar.tsx` | 80 | **New** | GSAP |
| `src/components/ui/animated-department-table.tsx` | 160 | **New** | GSAP |
| `src/components/ui/animated-people-card.tsx` | 165 | **New** | Anime.js |
| `src/components/ui/welcome-banner.tsx` | 135 | **New** | Lottie + GSAP |
| `src/components/ui/floating-particles.tsx` | 117 | **New** | GSAP |
| `src/components/ui/live-pulse-indicator.tsx` | 72 | **New** | Anime.js |

---

*Documentation generated March 7, 2026.*
