# DonBosco AMS — Frontend

A modern Next.js frontend for the DonBosco Attendance Management System, built to work with the existing ASP.NET Core backend API.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | Next.js 16 (App Router, Turbopack) |
| Language | TypeScript (strict) |
| Styling | Tailwind CSS 4 |
| State | Zustand (persisted auth store) |
| HTTP | Axios (JWT interceptors, auto-refresh) |
| Notifications | react-hot-toast (via custom `notify` wrapper) |
| Icons | lucide-react |
| Dates | date-fns |

## Project Structure

```
frontend/src/
├── app/                        # Next.js App Router pages
│   ├── layout.tsx              # Root layout
│   ├── page.tsx                # Redirect → /dashboard
│   ├── providers.tsx           # Theme + toast provider
│   ├── error.tsx               # Global error boundary
│   ├── not-found.tsx           # 404 page
│   ├── loading.tsx             # Suspense loading state
│   ├── login/page.tsx          # Login page
│   ├── dashboard/page.tsx      # Dashboard with stats & charts
│   ├── attendance/page.tsx     # Attendance log (search/filter)
│   ├── students/page.tsx       # Student CRUD + QR codes
│   ├── staff/page.tsx          # Staff CRUD + QR codes
│   ├── departments/page.tsx    # Department management
│   ├── programs/page.tsx       # Academic program management
│   ├── sections/page.tsx       # Section management
│   ├── academic-periods/       # Academic period management
│   ├── reports/page.tsx        # Daily/Weekly/Department reports
│   ├── gate-terminals/         # Gate terminal management
│   ├── users/page.tsx          # User/role management
│   ├── business-rules/         # System configuration rules
│   ├── audit-logs/page.tsx     # System audit trail
│   └── settings/page.tsx       # Profile & password change
├── components/
│   ├── layout/                 # AppShell, Sidebar, Header, AuthGuard
│   └── ui/                     # Reusable UI components
│       ├── button.tsx          # Button with loading state
│       ├── input.tsx           # Input with label, error, aria
│       ├── select.tsx          # Select with label, error, aria
│       ├── textarea.tsx        # Textarea with label, error, aria
│       ├── checkbox.tsx        # Checkbox with label, error, aria
│       ├── modal.tsx           # Modal with focus trapping
│       ├── data-table.tsx      # Generic sortable data table
│       ├── pagination.tsx      # Pagination controls
│       ├── search-bar.tsx      # Debounced search with clear
│       ├── confirm-dialog.tsx  # Confirmation modal
│       ├── badge.tsx           # Status badge
│       ├── card.tsx            # Card + CardHeader + CardTitle
│       ├── stat-card.tsx       # Dashboard stat card
│       └── loading-spinner.tsx # Reusable spinner
├── lib/
│   ├── api-client.ts           # Axios instance with JWT interceptors
│   ├── constants.ts            # App-wide constants
│   ├── toast.ts                # Toast notification utilities
│   └── utils.ts                # Formatting & helper functions
├── services/                   # API service layer (13 services)
├── stores/
│   ├── auth-store.ts           # Zustand auth with persist + token expiry
│   └── sidebar-store.ts        # Sidebar open/collapsed state
└── types/
    ├── index.ts                # AuthUser, UserRole
    └── api.ts                  # All API request/response types
```

## Getting Started

```bash
cd frontend
npm install
cp .env.local.example .env.local  # Set NEXT_PUBLIC_API_URL
npm run dev
```

The app runs on `http://localhost:3000` by default and expects the API at the URL configured in `.env.local`.

## User Roles

| Role | Access |
|---|---|
| Admin | Full access to all modules |
| Registrar | Students, Attendance |
| DepartmentHead | Staff, Attendance, Reports |
| Guard | Attendance logging |

## Key Design Decisions

- **Centralized toast system** — All notifications go through `lib/toast.ts` (`notify.success/error/warning/info/promise`) with smart Axios error extraction
- **Token expiry** — Checked on every route change (auth guard) and every API request (axios interceptor); auto-logout on expiry
- **Token refresh** — Automatic 401 retry with refresh token in the response interceptor
- **Shared constants** — Page sizes, app name, password rules defined in `lib/constants.ts`, used everywhere
- **Accessible by default** — ARIA attributes on all form controls, focus trapping in modals, keyboard navigation, `role`/`aria-label` on interactive elements
- **Mobile responsive** — Sidebar collapses to hamburger overlay on mobile (`< md` breakpoint)
- **Error boundaries** — Global `error.tsx`, `not-found.tsx`, and `loading.tsx` for graceful failure handling
