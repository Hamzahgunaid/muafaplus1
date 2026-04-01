# Muafa+ — Full-Stack Medical Education Platform
## Complete Build: Phases 1–3

---

## Project Overview

Muafa+ is an AI-powered medical education content generation system for post-diagnosis
patient care in Yemen. It uses Claude AI (Anthropic) to generate evidence-based, culturally
appropriate Arabic patient education materials, personalised by diagnosis, risk level, age,
medications, and local context.

**Stack:**
- Backend: .NET 8 ASP.NET Core Web API · SQL Server · Entity Framework Core · Hangfire
- Frontend: Next.js 14 · React · TypeScript · Tailwind CSS · RTL Arabic
- AI: Anthropic Claude Sonnet 4 (claude-sonnet-4-20250514) with prompt caching
- Auth: JWT Bearer tokens (BCrypt password hashing)
- Export: QuestPDF (Arabic RTL PDF) · DocumentFormat.OpenXml (Arabic RTL Word)

---

## Repository Structure

```
MuafaPlus/                          ← .NET 8 backend
├── Controllers/
│   ├── AuthController.cs           ← POST /auth/login, GET /auth/me
│   ├── ContentGenerationController.cs  ← POST /generate/complete (async)
│   ├── ExportController.cs         ← GET /sessions/{id}/export?format=pdf|docx
│   ├── PhysicianController.cs      ← CRUD physician profiles
│   └── SessionController.cs        ← GET /Session/{id}, GET /Session/{id}/status
├── Data/
│   └── MuafaDbContext.cs           ← EF Core context, relationships, seed data
├── Models/
│   ├── ApiModels.cs                ← GenerationSession, TokenUsage, response wrappers
│   ├── ArticleModels.cs            ← ArticleContent (content_ar fixed), Stage1/2 outputs
│   ├── AuthModels.cs               ← LoginRequest/Response, PhysicianCredential
│   └── RiskScore.cs                ← Deterministic risk output from RiskCalculatorService
├── Prompts/
│   ├── Stage1SystemPrompt.txt      ← Risk algorithm removed (now in C#)
│   └── Stage2SystemPrompt.txt      ← Full article generation instructions
├── Services/
│   ├── ExportService.cs            ← Arabic RTL PDF and Word generation
│   ├── GenerationJobService.cs     ← Hangfire Stage 2 background job
│   ├── JwtService.cs               ← JWT token generation and signing
│   ├── MuafaApiClient.cs           ← Claude API client (SDK v3.x)
│   ├── PromptBuilder.cs            ← Prompt construction with IWebHostEnvironment path
│   ├── RiskCalculatorService.cs    ← Deterministic 5-step risk algorithm in C#
│   └── WorkflowService.cs          ← Stage 1 sync + Stage 2 enqueue
├── Program.cs                      ← DI, JWT auth, Hangfire, CORS, Swagger
├── MuafaPlus.csproj
├── appsettings.json
├── appsettings.Development.json
└── setup-migrations.sh             ← One-command setup script

muafaplus-frontend/                 ← Next.js 14 frontend (RTL Arabic)
├── src/
│   ├── app/
│   │   ├── layout.tsx              ← RTL root layout, Noto Sans Arabic
│   │   ├── globals.css             ← RTL styles, article-content class, risk badges
│   │   ├── page.tsx                ← Root redirect (→ /dashboard or /login)
│   │   ├── login/page.tsx          ← Physician login form
│   │   ├── dashboard/page.tsx      ← Session list, stats, navigation
│   │   ├── generate/page.tsx       ← Patient intake form + Stage 1/2 polling
│   │   └── sessions/[id]/page.tsx  ← Article viewer + PDF/Word export
│   ├── components/
│   │   └── RiskBadge.tsx           ← Risk level badge (LOW/MODERATE/HIGH/CRITICAL)
│   ├── hooks/
│   │   └── useSessionPolling.ts    ← 3-second poll hook with timeout
│   ├── lib/
│   │   └── store.ts                ← Zustand auth store (persists to localStorage)
│   ├── services/
│   │   └── api.ts                  ← Axios client: authApi, contentApi, sessionApi
│   └── types/
│       └── index.ts                ← TypeScript types for all API entities
├── package.json
├── tailwind.config.ts
├── tsconfig.json
├── next.config.js
└── README.md
```

---

## Quick Start

### Prerequisites

- .NET 8 SDK (`dotnet --version` → 8.0.x)
- SQL Server 2019+ or SQL Server Express / LocalDB
- Node.js 18+ (`node --version`)
- Anthropic API key (https://console.anthropic.com)

### Backend Setup

```bash
cd MuafaPlus

# Set secrets — never put these in appsettings files
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey"  "sk-ant-api03-YOUR_KEY"
dotnet user-secrets set "Jwt:Secret"        "$(openssl rand -base64 48)"

# Install packages, create DB, apply migrations, run
bash setup-migrations.sh
```

The API starts at **https://localhost:5001**
Swagger UI is at the root: **https://localhost:5001**
Hangfire dashboard: **https://localhost:5001/hangfire**
Health check: **https://localhost:5001/health**

### Frontend Setup

```bash
cd muafaplus-frontend
npm install
cp .env.local.example .env.local   # edit if backend runs on different port
npm run dev
```

Portal at **http://localhost:3000**

### Default Login Credentials (development seed data)

| Physician               | Email                         | Password        |
|-------------------------|-------------------------------|-----------------|
| Dr. Ahmed Al-Sana       | ahmed.sana@hospital.ye        | MuafaPlus2025!  |
| Dr. Fatima Al-Hakim     | fatima.hakim@clinic.ye        | MuafaPlus2025!  |
| Dr. Mohammed Al-Zubairi | mohammed.z@diabetes.ye        | MuafaPlus2025!  |

All accounts have `MustResetOnNextLogin = true`.

---

## API Reference

### Authentication
| Method | Endpoint             | Description                        |
|--------|----------------------|------------------------------------|
| POST   | /api/v1/auth/login   | Login, returns JWT                 |
| GET    | /api/v1/auth/me      | Current physician profile          |

### Content Generation
| Method | Endpoint                                | Description                             |
|--------|-----------------------------------------|-----------------------------------------|
| POST   | /api/v1/ContentGeneration/generate/complete | Stage 1 sync + Stage 2 queued (async) |
| POST   | /api/v1/ContentGeneration/generate/stage1  | Stage 1 only (sync)                    |
| GET    | /api/v1/ContentGeneration/health           | Health check (no auth required)        |

### Sessions
| Method | Endpoint                          | Description                         |
|--------|-----------------------------------|-------------------------------------|
| GET    | /api/v1/Session/{id}              | Full session + all articles         |
| GET    | /api/v1/Session/{id}/status       | Lightweight status poll             |

### Physicians
| Method | Endpoint                          | Description                         |
|--------|-----------------------------------|-------------------------------------|
| GET    | /api/v1/Physician                 | All active physicians               |
| GET    | /api/v1/Physician/{id}            | Single physician profile            |
| GET    | /api/v1/Physician/{id}/sessions   | Physician's session history         |
| POST   | /api/v1/Physician                 | Create physician                    |
| PUT    | /api/v1/Physician/{id}            | Update physician                    |
| DELETE | /api/v1/Physician/{id}            | Soft-delete physician               |

### Export
| Method | Endpoint                                      | Description              |
|--------|-----------------------------------------------|--------------------------|
| GET    | /api/v1/sessions/{id}/export?format=pdf       | Download Arabic RTL PDF  |
| GET    | /api/v1/sessions/{id}/export?format=docx      | Download Arabic RTL Word |

---

## Generation Workflow

```
POST /generate/complete
        │
        ▼ (sync, ~10–15 s)
  C# RiskCalculatorService.Calculate()
        │
        ▼
  Claude Stage 1 API call
  → risk echo + summary article + article outlines
        │
        ▼
  Save summary article to DB
  Enqueue Stage 2 Hangfire job
        │
        ▼ HTTP 202 Accepted immediately
  { sessionId, riskScore, stage1Cost, pollUrl }
        │
        ▼ Frontend polls every 3 s
  GET /Session/{id}/status
        │
        ▼ When status = "complete"
  GET /Session/{id}
  → full session + all articles
        │
        ▼
  GET /sessions/{id}/export?format=pdf|docx
```

---

## Risk Calculator

Risk is computed in C# (`RiskCalculatorService`) before any AI call.
The algorithm is the 5-step clinical scoring model:

| Factor type     | Points | Triggers                                                   |
|-----------------|--------|------------------------------------------------------------|
| Acute danger    | +1 each| Anticoagulants, insulin/sulfonylureas, recent hospitalization, severe organ failure, active cancer, dangerous drug interaction |
| Complexity      | +0.5   | Polypharmacy (4+ meds), 2+ chronic conditions, cognitive impairment, high-risk age group, frequent monitoring |
| Protective      | −0.5   | Stable >3 months, adult age group, early-stage condition, documented good adherence |

| Total score | Risk level |
|-------------|------------|
| ≤ 0.5       | LOW        |
| 1.0 – 1.5   | MODERATE   |
| 2.0 – 2.5   | HIGH       |
| ≥ 3.0       | CRITICAL   |

---

## Cost Model (Claude Sonnet 4)

| Token type       | Rate (per 1M tokens) |
|------------------|----------------------|
| Input            | $3.00                |
| Output           | $15.00               |
| Cache write      | $3.75                |
| Cache read       | $0.30                |

**Average per patient:** Stage 1 ~$0.015 + Stage 2 ~$0.080 = **~$0.095**
At 1,000 patients/month with prompt caching: **~$95/month**

---

## Database Schema

```
Physicians          ←──┐
  PhysicianId (PK)      │ FK
  FullName, Specialty   │
  Email (unique)        │
  LicenseNumber (unique)│
                        │
PhysicianCredentials    │
  PhysicianId (PK/FK) ──┘  CASCADE delete
  PasswordHash (bcrypt)

Patients
  PatientId (PK)
  PhysicianId (FK → Physicians, RESTRICT)
  PrimaryDiagnosis, AgeGroup, Medications...

GenerationSessions
  SessionId (PK)
  PatientId (FK → Patients, CASCADE)
  PhysicianId (FK → Physicians, RESTRICT)
  Status, RiskLevel, TotalCost...

GeneratedArticles
  ArticleId (PK)
  SessionId (FK → GenerationSessions, CASCADE)
  ArticleType, Content, WordCount, CostUsd...
```

---

## Phase 4 — Production Hardening (remaining work)

The following items are scoped for Phase 4 and are not yet implemented:

**Security:**
- Restrict Hangfire dashboard behind `[Authorize]` with admin role
- Rate limiting per physician (AspNetCoreRateLimit or .NET 8 built-in)
- Password reset flow (`MustResetOnNextLogin` enforcement in frontend)
- Refresh token rotation (currently tokens expire and require re-login)
- Input sanitisation middleware for all free-text fields
- CORS locked to production domains (currently set in appsettings)

**Infrastructure:**
- Docker / docker-compose configuration
- Azure App Service or AWS ECS deployment pipeline
- SQL Server connection string via Azure Key Vault
- Anthropic API key via Key Vault (not user secrets)
- Serilog sink to Azure Application Insights or Seq
- Database backup strategy

**Frontend:**
- Password reset screen (backend `MustResetOnNextLogin` already set)
- Pagination on article tabs for sessions with many articles
- Loading skeleton states during API calls
- Progressive Web App (PWA) manifest for mobile install

**Monitoring:**
- `/health` endpoint extended with Hangfire queue depth and Claude API reachability
- Cost alert when monthly spend exceeds configurable threshold
- Session failure alerting (Hangfire job failure notifications)

---

## Key Fixes from Original Prototype

| Issue | Fix | File |
|-------|-----|------|
| `Anthropic.SDK` v0.2.0 outdated | Updated to v3.7.4 | MuafaPlus.csproj |
| Risk algorithm in AI prompt (non-deterministic) | Extracted to `RiskCalculatorService` C# class | Services/RiskCalculatorService.cs |
| `content` vs `content_ar` JSON key mismatch (silent empty articles) | `ArticleContent.ContentAr` with `[JsonPropertyName("content_ar")]` | Models/ArticleModels.cs |
| Prompt file path fails on Azure/Docker | `IWebHostEnvironment.ContentRootPath` | Services/PromptBuilder.cs |
| `DeleteBehavior` mismatch between DbContext and SQL schema | Aligned to CASCADE on Sessions→Patients | Data/MuafaDbContext.cs |
| No EF migrations (raw SQL only) | `db.Database.Migrate()` on startup + `HasData()` seed | Data/MuafaDbContext.cs |
| No authentication on any endpoint | JWT Bearer auth, `[Authorize]` on all controllers | Controllers/*, Program.cs |
| `CORS AllowAnyOrigin` | `WithOrigins()` from `appsettings Cors:AllowedOrigins` | Program.cs |
| Stage 2 blocks HTTP for 120 s | Hangfire background job, HTTP 202 Accepted immediately | Services/WorkflowService.cs |
| No physician CRUD controller | `PhysicianController` with full CRUD + sessions endpoint | Controllers/PhysicianController.cs |
| No session retrieval API | `SessionController` GET by ID and status poll | Controllers/SessionController.cs |
| No file export | `ExportService` + `ExportController` for PDF and Word | Services/ExportService.cs |
| Frontend had no UI at all | Complete Next.js 14 RTL Arabic portal | muafaplus-frontend/ |
