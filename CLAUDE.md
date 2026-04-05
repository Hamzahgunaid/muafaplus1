# Muafa+ — Claude Code Project Briefing
## Platform Overview
Muafa+ (معافى بلس) is an AI-powered Arabic medical education SaaS platform
for post-diagnosis patient care in Yemen. Physicians refer patients and the
system generates personalised Arabic health education articles via Claude AI,
delivered through WhatsApp and a Flutter mobile app.

## Current State — Phase 1 Complete
Production is LIVE:
- Frontend: https://muafaplus1.vercel.app
- Backend: https://muafaplus1-production.up.railway.app
- Database: PostgreSQL on Railway (NOT SQL Server)
- GitHub: https://github.com/Hamzahgunaid/muafaplus1

## Technology Stack
- Backend: .NET 8 ASP.NET Core Web API (Railway, Docker)
- Frontend: Next.js 14 React, TypeScript, RTL Arabic (Vercel)
- Mobile: Flutter iOS + Android (Phase 4)
- Database: PostgreSQL 15 + pgvector extension (Phase 5)
- Background jobs: Hangfire with PostgreSQL storage
- AI: Anthropic Claude Sonnet 4 — SDK v5.10.0 with prompt caching ACTIVE
- Embeddings: Voyage AI voyage-3-lite (Phase 5)
- Messaging: WhatsApp Business API + SMS fallback (Phase 2)
- Auth: JWT Bearer + BCrypt (providers) / Phone+Code (patients, Phase 2)
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

Test accounts:
- ahmed.sana@hospital.ye / MuafaPlus2025! (Physician PHY001)
- fatima.hakim@clinic.ye / MuafaPlus2025! (Physician PHY002)
- mohammed.z@diabetes.ye / MuafaPlus2025! (Physician PHY003)

Test invitation code: PH-TEST01 (Physician role, expires 2027-01-01)

## Database — 11 Tables (Phase 1 complete)

### Original tables (Phase 0)
- Physicians — seed data: PHY001, PHY002, PHY003
- PhysicianCredentials — BCrypt hashed passwords
- GenerationSessions — AI generation session tracking
- GeneratedArticles — article content storage
- PatientData — patient profile per session (will evolve in Phase 2)

### New tables (Phase 1)
- Tenants — hospital/clinic organisations
- TenantSettings — per-tenant config (PatientNamePolicy, WhatsApp, chat)
- TenantSubscriptions — quota and billing cycle
- InvitationCodes — typed access codes for all user types
- UserRoles — role assignments per user per tenant
- AssistantPhysicianLinks — many-to-many assistant-physician linking

## API Endpoints — Current State

### Authentication (AuthController)
POST /api/v1/auth/login — provider login, returns JWT [AllowAnonymous]
GET  /api/v1/auth/me — current user profile [Authorize]
POST /api/v1/auth/change-password — [Authorize]
POST /api/v1/auth/validate-code — validate invitation code [AllowAnonymous]
  → always returns 200, IsValid=false for bad codes, never 4xx
POST /api/v1/auth/patient/login — STUB 501, Phase 2 [AllowAnonymous]
POST /api/v1/auth/invitation-codes/generate — [Authorize]

### Tenants (TenantsController)
GET  /api/v1/tenants — list all [Authorize]
POST /api/v1/tenants — create tenant, returns 201 [Authorize]
GET  /api/v1/tenants/{id} — get by ID, 404 if not found [Authorize]
GET  /api/v1/tenants/{id}/settings [Authorize]
PUT  /api/v1/tenants/{id}/settings — partial update [Authorize]
GET  /api/v1/tenants/{id}/subscription [Authorize]
POST /api/v1/tenants/{id}/assistant-links — 409 if already linked [Authorize]

### Content Generation (ContentGenerationController)
POST /api/v1/ContentGeneration/generate/complete [Authorize]
GET  /api/v1/ContentGeneration/health [AllowAnonymous]

### Sessions (SessionController)
GET /api/v1/Session/{id} [Authorize]
GET /api/v1/Physician/{id}/sessions [Authorize]

## Login response shape — IMPORTANT
Login returns ApiResponse<T> wrapper. Token is at data.data.token:
{
  "success": true,
  "data": {
    "token": "eyJ...",
    "physicianId": "PHY001",
    "fullName": "Dr. Ahmed Al-Sana",
    "specialty": "Internal Medicine",
    "institution": "Sana'a General Hospital",
    "expiresAt": "2026-...",
    "mustResetOnNextLogin": false
  },
  "error": null
}

## Key service files
Backend/Services/InvitationCodeService.cs — validate + generate codes
Backend/Services/TenantService.cs — tenant CRUD + subscription + linking
Backend/Services/RiskCalculatorService.cs — 5-step C# risk algorithm
Backend/Services/WorkflowService.cs — Stage 1 + Stage 2 orchestration
Backend/Services/GenerationJobService.cs — Hangfire Stage 2 jobs
Backend/Services/MuafaApiClient.cs — Claude API calls with prompt caching
Backend/Services/PromptBuilder.cs — system prompt assembly

## Key model files
Backend/Models/Entities/Tenant.cs
Backend/Models/Entities/TenantSettings.cs
Backend/Models/Entities/TenantSubscription.cs
Backend/Models/Entities/InvitationCode.cs
Backend/Models/Entities/UserRole.cs
Backend/Models/Entities/AssistantPhysicianLink.cs
Backend/Models/Entities/TenantRole.cs (shared enum)
Backend/Models/ApiModels.cs — all request/response DTOs
Backend/Data/MuafaDbContext.cs — EF Core context + all DbSets

## Phase 2 Target — Referral Workflow Enrichment
Next tasks in order:
1. PatientAccess table — phone number + 4-digit code authentication
2. Complete POST /api/v1/auth/patient/login (remove 501 stub)
3. Referrals table — replaces/extends current GenerationSessions
4. Patient-triggered Stage 2: POST /api/v1/referrals/{id}/stage2
5. Progressive article loading — return partial results during generation
6. ReferralEngagement + ArticleEngagement tracking tables
7. PatientFeedback table and endpoint
8. QR code generation per referral
9. ArticleLibrary table with SHA-256 profile hash (Layer 1 cost reduction)
10. WhatsApp Business API integration with 2-hour smart delay

## Architecture — 5 User Roles
1. Super Admin — Afyah Wise internal. Manages all tenants globally.
2. Hospital Admin — manages their institution, users, subscription.
3. Physician — creates referrals, test scenarios, evaluations.
4. Assistant — primary referral creator (90% of daily work), linked to physicians.
5. Patient — receives WhatsApp summary, triggers Stage 2, reads articles.

## Architecture — Patient Referral Workflow (Phase 2 target)
1. Provider creates referral → risk calculator runs (C#, no AI)
2. Stage 1 generates → stored in DB and ArticleLibrary
3. WhatsApp sends after configurable delay (default 2h):
   - Message 1: Full summary article + app download link
   - Message 2: 4-digit access code (sent separately)
4. Patient logs in: phone number + 4-digit code
5. Patient reads Stage 1 summary (scroll tracking)
6. Patient taps "Read More" → Stage 2 triggered on demand
7. Articles appear progressively as each Hangfire job completes
8. Patient likes/dislikes, submits feedback
9. Provider views engagement timeline

## Architecture — Three-Layer Cost Reduction
Layer 0: Prompt caching via SDK v5.10.0 — ACTIVE IN PRODUCTION
Layer 1: SHA-256 exact profile hash → ArticleLibrary — Phase 2 Task 9
Layer 2: pgvector near-match search (threshold 0.92) — Phase 5
Layer 3: Hangfire 30s batch delay for cache warming — Phase 2

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
Every controller except /auth/login, /auth/validate-code,
/auth/patient/login, and /health requires [Authorize].
Never add unauthenticated endpoint without explicit approval.

RULE 7 — Database is PostgreSQL NOT SQL Server
Always use Npgsql EF Core provider. Never use SqlServer provider.
Connection string from Railway environment variable in production.
Locally: ConnectionStrings__DefaultConnection in appsettings.Development.json.

RULE 8 — Tenant isolation
All data access must be tenant-scoped at the service layer.
Never return data across tenant boundaries.
TenantId always from authenticated user JWT claim, never from request body.

RULE 9 — Prompt refinement is manual only
Stage1SystemPrompt.txt and Stage2SystemPrompt.txt modified exclusively
by Afyah Wise clinical team outside the platform.
Never add automatic or platform-driven prompt modification.
Never modify prompt files in code without explicit instruction.

RULE 10 — Article library is permanent
ArticleLibrary table has no TTL or expiry.
Never add delete or expire logic to article library entries.
TenantId = null means shared across all tenants.

RULE 11 — ApiResponse wrapper
All endpoints return ApiResponse<T> wrapper:
{ "success": bool, "data": T, "error": string|null, "errorType": string|null }
Never return raw objects from controllers.
Token in login response is at data.data.token (double-wrapped).

## Environment variables on Railway (production)
ConnectionStrings__DefaultConnection — PostgreSQL connection string
Anthropic__ApiKey — Claude API key (sk-ant-...)
Jwt__Secret — JWT signing secret
Cors__AllowedOrigins__0 — https://muafaplus1.vercel.app

## Governance
- Prompt refinement: manual only, decided by Afyah Wise outside platform
- Clinical content changes require Afyah Wise clinical team approval
- Prompt file version comment incremented after any modification
- Similarity threshold (0.92) reviewed after every 100 new patients
- Patient data never leaves platform except to Anthropic API
- Article library is permanent — no expiry, no deletion
