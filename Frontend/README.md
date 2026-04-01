# Muafa+ Physician Portal — Setup Guide

## Prerequisites

- Node.js 18+ (`node --version`)
- The .NET 8 backend running on `https://localhost:5001`

## Quick Start

```bash
# 1. Install dependencies
npm install

# 2. Create local environment file
cp .env.local.example .env.local
# Edit .env.local if your backend runs on a different port

# 3. Start the development server
npm run dev
```

The portal will be available at **http://localhost:3000**

## Default Login Credentials (development)

| Physician              | Email                        | Password         |
|------------------------|------------------------------|------------------|
| Dr. Ahmed Al-Sana      | ahmed.sana@hospital.ye       | MuafaPlus2025!   |
| Dr. Fatima Al-Hakim    | fatima.hakim@clinic.ye       | MuafaPlus2025!   |
| Dr. Mohammed Al-Zubairi| mohammed.z@diabetes.ye       | MuafaPlus2025!   |

All accounts require a password reset on first login (`MustResetOnNextLogin = true`).
The password-reset screen is a Phase 3 item.

## Page Structure

```
/              → Redirects to /dashboard or /login
/login         → Physician login form
/dashboard     → Session list, stats, navigation
/generate      → Patient intake form + content generation
/sessions/[id] → Article viewer (summary + all detailed articles)
```

## Production Build

```bash
npm run build
npm start
```

For production, set `NEXT_PUBLIC_API_URL` to your deployed backend URL.
Configure a reverse proxy (nginx / Azure Front Door) to terminate TLS and
forward `/api/*` to the .NET backend.

## RTL Notes

- All pages use `dir="rtl"` via the root layout
- Tailwind classes use logical properties where possible
- Article content is rendered with the `.article-content` CSS class
  which sets RTL text direction, Arabic line-height, and table styling
- Font: Noto Sans Arabic (loaded from Google Fonts)
