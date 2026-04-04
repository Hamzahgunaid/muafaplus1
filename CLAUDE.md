# Muafa+ — Claude Code Project Briefing
## Platform Overview
Muafa+ (معافى بلس) is an AI-powered Arabic medical education SaaS platform
for post-diagnosis patient care in Yemen. Physicians refer patients and the
system generates personalised Arabic health education articles via Claude AI,
delivered through WhatsApp and a Flutter mobile app.

## Current State — Phase 0 Complete
Production is LIVE:
- Frontend: https://muafaplus1.vercel.app
- Backend: https://muafaplus1-production.up.railway.app
- Database: PostgreSQL on Railway (NOT SQL Server)
- GitHub: https://github.com/Hamzahgunaid/muafaplus1

## Technology Stack
- Backend: .NET 8 ASP.NET Core Web API (Railway, Docker)
- Frontend: Next.js 14 React, TypeScript, RTL Arabic (Vercel)
- Mobile: Flutter iOS + Android (future phases)
- Database: PostgreSQL 15 + pgvector extension (Railway)
- Background jobs: Hangfire with PostgreSQL storage
- AI: Anthropic Claude Sonnet 4 — SDK v5.10.0 with prompt caching ACTIVE
- Embeddings: Voyage AI voyage-3-lite (Phase 5)
- Messaging: WhatsApp Business API + SMS fallback (Phase 2)
- Auth: JWT Bearer + BCrypt (providers) / Phone+Code (patients)
- Export: QuestPDF (Arabic PDF) + OpenXml (Arabic Word)

## How to run locally
cd Backend
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update
dotnet run --urls "http://localhost:5000;https://localhost:5001"
API runs at https://localhost:5001
cd Frontend
npm install
npm run dev
Portal runs at http://localhost:3000

Test accounts after migration:
- ahmed.sana@hospital.ye / MuafaPlus2025!
- fatima.hakim@clinic.ye / MuafaPlus2025!
- mohammed.z@diabetes.ye / MuafaPlus2025!

## Architecture — 5 User Roles
1. Super Admin — Afyah Wise internal. Manages all tenants globally.
2. Hospital Admin — manages their institution, users, subscription.
3. Physician — creates referrals, test scenarios, evaluations.
4. Assistant — primary referral creator (90% of daily work), linked to physicians.
5. Patient — receives WhatsApp summary, triggers Stage 2, reads articles.

## Architecture — Patient Referral Workflow
1. Provider creates referral → risk calculator runs (C#, no AI)
2. Stage 1 generates → stored in DB and ArticleLibrary
3. WhatsApp sends after configurable delay (default 2h):
   - Message 1: Full summary article + app download link
   - Message 2: 4-digit access code (sent separately for clarity)
4. Patient logs in: phone number + 4-digit code
5. Patient reads Stage 1 summary (scroll tracking active)
6. Patient taps "Read More" → Stage 2 triggered on demand
7. Articles appear progressively as each Hangfire job completes
8. Patient likes/dislikes articles, submits feedback
9. Provider views engagement timeline

## Architecture — Three-Layer Cost Reduction
Layer 0: Prompt caching via SDK v5.10.0 — ALREADY IMPLEMENTED
Layer 1: SHA-256 exact profile hash → ArticleLibrary table — Phase 2
Layer 2: pgvector near-match search (threshold 0.92) — Phase 5
Layer 3: Hangfire 30s batch delay for cache warming

## Development Phases
Phase 0: COMPLETE — single-tenant baseline in production
Phase 1: Multi-tenant foundation (current target)
Phase 2: Referral workflow enrichment + WhatsApp + Layer 1 cost reduction
Phase 3: Quality system + physician-patient chat + push notifications
Phase 4: Flutter mobile app (parallel with Phase 3)
Phase 5: pgvector + Voyage AI embeddings + Layer 2 cost reduction
Phase 6: Assistant gamification (future)

## Phase 1 Target — Multi-Tenant Foundation
Add these tables via EF Core migrations (PostgreSQL):
- Tenants (TenantId, Name, NameAr, LogoUrl, IsActive, CreatedAt)
- TenantSettings (TenantId FK, PatientNamePolicy, WhatsAppSenderId,
  NotificationDelayHours, ChatEnabled, PatientChatWindowDays)
- TenantSubscriptions (SubscriptionId, TenantId FK, PlanType,
  CasesAllocated, CasesUsed, BillingCycleStart, BillingCycleEnd, IsActive)
- InvitationCodes (Code PK, TenantId FK, Role, CreatedByUserId,
  UsedByUserId, UsedAt, ExpiresAt, IsActive)
- UserRoles (UserId FK, TenantId FK, Role, AssignedAt)
- AssistantPhysicianLinks (AssistantId FK, PhysicianId FK, TenantId FK,
  LinkedAt, IsActive)
- Update Users table: add TenantId FK, AuthProvider, GoogleId columns

Invitation code formats:
- SA-XXXXXX → Super Admin
- HA-XXXXXX → Hospital Admin
- PH-XXXXXX → Physician
- AS-XXXXXX → Assistant
- 4 digits → Patient (auto-generated per referral)

## Rules that must NEVER be broken
RULE 1 — Risk calculation
Always calculated in C# by RiskCalculatorService.cs BEFORE calling Claude API.
Never put risk algorithm inside a prompt.

RULE 2 — Article content field name
Field is ContentAr with JsonPropertyName("content_ar").
Never use "content" — silent bug causing empty articles.

RULE 3 — Stage 2 is always async
Stage 2 always runs as Hangfire background job via GenerationJobService.cs.
Never block an HTTP response waiting for Stage 2.
Endpoint returns HTTP 202 immediately after Stage 1 completes.

RULE 4 — PhysicianId always from JWT
PhysicianId must always come from JWT claim in the controller.
Never trust PhysicianId from request body.

RULE 5 — Prompt file paths
Prompt files in Backend/Prompts/ loaded via IWebHostEnvironment.ContentRootPath.
Never use AppDomain.CurrentDomain.BaseDirectory.

RULE 6 — All controllers require auth
Every controller except /auth/login, /auth/patient/login, and /health
requires [Authorize]. Never add unauthenticated endpoint without approval.

RULE 7 — Database is PostgreSQL NOT SQL Server
Always use Npgsql EF Core provider. Never use SqlServer provider.
Connection string comes from environment variable on Railway.
Locally: ConnectionStrings__DefaultConnection in appsettings.Development.json.

RULE 8 — Tenant isolation
All data access must be tenant-scoped at the service layer.
Never return data across tenant boundaries.
TenantId always comes from the authenticated user's JWT claim, never from
the request body.

RULE 9 — Prompt refinement is manual only
Stage1SystemPrompt.txt and Stage2SystemPrompt.txt are modified exclusively
by Afyah Wise clinical team outside the platform.
Never add any automatic or platform-driven prompt modification feature.
Never modify prompt files in code without explicit instruction.

RULE 10 — Article library is permanent
The ArticleLibrary table has no TTL or expiry.
Never add delete or expire logic to article library entries.
TenantId = null means shared across all tenants.

## Key file locations
Backend/Controllers/AuthController.cs
Backend/Controllers/ContentGenerationController.cs
Backend/Controllers/SessionController.cs
Backend/Controllers/PhysicianController.cs
Backend/Data/MuafaDbContext.cs
Backend/Services/RiskCalculatorService.cs
Backend/Services/WorkflowService.cs
Backend/Services/GenerationJobService.cs
Backend/Services/MuafaApiClient.cs
Backend/Services/PromptBuilder.cs
Backend/Models/ApiModels.cs
Backend/Prompts/Stage1SystemPrompt.txt
Backend/Prompts/Stage2SystemPrompt.txt
Frontend/src/services/api.ts
Frontend/src/app/generate/page.tsx
Frontend/src/app/dashboard/page.tsx

## Environment variables on Railway (production)
ConnectionStrings__DefaultConnection — PostgreSQL connection string
Anthropic__ApiKey — Claude API key (sk-ant-...)
Jwt__Secret — JWT signing secret
Cors__AllowedOrigins__0 — https://muafaplus1.vercel.app

## Governance
- Clinical content changes require Afyah Wise clinical team approval
- Prompt file version comment must be incremented after any modification
- Similarity threshold (0.92) reviewed after every 100 new patients
- Patient data never leaves the platform except to Anthropic API
