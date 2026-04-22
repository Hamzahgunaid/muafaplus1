# Muafa+ — Claude Code Project Briefing
## Platform Overview
Muafa+ (معافى بلس) is an AI-powered Arabic medical education SaaS platform
for post-diagnosis patient care in Yemen. Physicians refer patients and the
system generates personalised Arabic health education articles via Claude AI,
delivered through WhatsApp and a Flutter mobile app.

## Current State — Phase 3.7 Complete (April 2026)
Production is LIVE:
- Frontend: https://muafaplus1.vercel.app
- Backend: https://muafaplus1-production.up.railway.app
- Database: PostgreSQL on Railway (NOT SQL Server)
- GitHub: https://github.com/Hamzahgunaid/muafaplus1

### Phase 3.7 — Test Scenario Article Generation + Bug Fixes (complete)
- POST /test-scenarios/{id}/generate-article?index={n} — on-demand Stage 2 per article
  - Re-runs RiskCalculatorService (Rule 1) before every Anthropic call
  - Caches result in TestScenario.GeneratedArticlesJson keyed by article index
  - Returns cached content immediately on repeat calls (no Anthropic call)
  - SaveChangesAsync only called after confirmed Anthropic success (no partial saves)
- TestScenario entity: added GeneratedArticlesJson (nullable text column)
  - EF migration: AddGeneratedArticlesJsonToTestScenario — applied via db.Database.Migrate() on startup
- TestScenarioResponse DTO: added GeneratedArticlesJson field + mapping
- Frontend: ArticleContentViewer accepts initialContent prop to seed generatedContent +
  generatedSet state on page load — articles already generated persist across refresh
- Snake_case deserialisation fix: ArticleSpec and RiskAssessment were missing
  [JsonPropertyName] attributes causing all fields except rationale to silently map
  to empty strings from Anthropic's snake_case JSON. Fixed by adding full attribute set.
  TypeScript ArticleOutline and Stage1Output.risk_assessment updated to snake_case keys.
- ReferralResponse.ChatEnabled added — gates ChatCard display; populated from
  ChatThread.IsEnabled via Include in GetReferralAsync; returns 403 gracefully
- Tenant user management: GET/POST /api/v1/tenants/{id}/users endpoints added
- ArticleContentViewer: ReactMarkdown rendering, mounted hydration guard, three-state
  generate buttons (idle → generating → generated/cached)

### Phase 3.6 — Unified Auth + Role Enforcement (complete)
- AppUser table: single source of truth for all provider logins (replaces PhysicianCredentials)
- Login reads AppUser.PasswordHash; physician backward-compat claims populated from Physicians table
- JWT carries Role + UserId + TenantId claims; RoleClaimType = "Role" enables [Authorize(Roles)]
- ChangePassword updates AppUser.PasswordHash (previously updated PhysicianCredentials only — fixed)
- UserRole.UserId type fixed string → Guid (matches AppUser.UserId uuid in PostgreSQL)
- TenantsController: GET/POST gated to SuperAdmin; GenerateInvitationCode gated to SuperAdmin,HospitalAdmin
- Frontend: role-based redirect on login (SuperAdmin/HospitalAdmin → /admin; Physician/Assistant → /dashboard)
- Role guards on /admin (non-admins → /dashboard) and /dashboard (admins → /admin)
- TenantsCard: SuperAdmin sees all tenants; HospitalAdmin fetches own tenant only

### Test accounts (all use password: MuafaPlus2025!)
| Email | Role | PhysicianId |
|-------|------|-------------|
| ahmed.sana@hospital.ye | Physician | PHY001 |
| fatima.hakim@clinic.ye | Physician | PHY002 |
| mohammed.z@diabetes.ye | Physician | PHY003 |
| superadmin@afyahwise.com | SuperAdmin | — |

Test invitation code: PH-TEST01 (Physician role, expires 2027-01-01)

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

## Database — 19 Tables + 1 column (Phase 3.7)
TestScenario.GeneratedArticlesJson — nullable text, stores { "0": "...", "1": "..." }

## Database — 19 Tables (Phase 2 complete)

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

### New tables (Phase 2)
- PatientAccess — phone number + 4-digit code authentication
- Referrals — core referral workflow record
- PatientProfiles — clinical profile per referral (with SHA-256 hash)
- ReferralEngagement — patient journey milestones
- ArticleEngagement — per-article scroll depth + reaction
- PatientFeedback — patient satisfaction per referral
- MessageLog — WhatsApp/SMS delivery audit log
- ArticleLibrary — Layer 1 cost reduction (SHA-256 exact-match cache)

## API Endpoints — Current State

### Authentication (AuthController)
POST /api/v1/auth/login — provider login, returns JWT [AllowAnonymous]
GET  /api/v1/auth/me — current user profile [Authorize]
POST /api/v1/auth/change-password — [Authorize]
POST /api/v1/auth/validate-code — validate invitation code [AllowAnonymous]
  → always returns 200, IsValid=false for bad codes, never 4xx
POST /api/v1/auth/patient/login — phone + 4-digit code, 30-day JWT [AllowAnonymous]
POST /api/v1/auth/invitation-codes/generate — [Authorize]

### Tenants (TenantsController)
GET  /api/v1/tenants — list all [Authorize]
POST /api/v1/tenants — create tenant, returns 201 [Authorize]
GET  /api/v1/tenants/{id} — get by ID, 404 if not found [Authorize]
GET  /api/v1/tenants/{id}/settings [Authorize]
PUT  /api/v1/tenants/{id}/settings — partial update [Authorize]
GET  /api/v1/tenants/{id}/subscription [Authorize]
POST /api/v1/tenants/{id}/assistant-links — 409 if already linked [Authorize]

### Referrals (ReferralsController)
POST /api/v1/referrals — create referral, 202 Accepted [Authorize]
GET  /api/v1/referrals — list (Patient: by PatientAccessId; Provider: by PhysicianId) [Authorize]
GET  /api/v1/referrals/{id} — single referral + engagement [Authorize]
GET  /api/v1/referrals/{id}/articles — progressive article loading [Authorize]
POST /api/v1/referrals/{id}/stage2 — patient triggers Stage 2, 202 Accepted [Authorize]
GET  /api/v1/referrals/{id}/engagement — provider engagement detail view [Authorize]

### Engagement (EngagementController)
POST /api/v1/referrals/{id}/engagement — track app_opened/summary_viewed/stage2_requested [Authorize]
POST /api/v1/articles/{articleId}/engagement — track scroll depth + reaction [Authorize]
POST /api/v1/referrals/{id}/feedback — patient feedback, 409 if duplicate [Authorize]

### Test Scenarios (TestScenariosController)
POST /api/v1/test-scenarios — create + Stage 1 generate, returns 201 [Authorize]
GET  /api/v1/test-scenarios — list by physician [Authorize]
GET  /api/v1/test-scenarios/{id} — single scenario + evaluation [Authorize]
POST /api/v1/test-scenarios/{id}/evaluation — submit evaluation, 409 if duplicate [Authorize]
POST /api/v1/test-scenarios/{id}/generate-article?index={n} — Stage 2 single article [Authorize]
  → checks GeneratedArticlesJson cache first; saves result back after generation
POST /api/v1/test-scenarios/generate/stream — SSE Stage 1 preview, does not save [Authorize]

### Tenant Users (TenantsController)
GET  /api/v1/tenants/{id}/users — list users for tenant [Authorize]
POST /api/v1/tenants/{id}/users — create user with BCrypt password [Authorize]

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
    "userId": "00000000-0000-0000-0000-000000000001",
    "physicianId": "PHY001",
    "fullName": "Dr. Ahmed Al-Sana",
    "specialty": "Internal Medicine",
    "institution": "Sana'a General Hospital",
    "role": "Physician",
    "tenantId": "00000000-0000-0000-0000-000000000010",
    "expiresAt": "2026-...",
    "mustResetOnNextLogin": false
  },
  "error": null
}

Fields added in Phase 3.6:
- userId — AppUser.UserId (Guid) — used for ChangePassword lookup
- role — string: "SuperAdmin" | "HospitalAdmin" | "Physician" | "Assistant"
- tenantId — Guid? (null for SuperAdmin)

## Key service files
Backend/Services/InvitationCodeService.cs — validate + generate codes
Backend/Services/TenantService.cs — tenant CRUD + subscription + linking
Backend/Services/RiskCalculatorService.cs — 5-step C# risk algorithm
Backend/Services/WorkflowService.cs — Stage 1 + Stage 2 orchestration + ArticleLibrary check
Backend/Services/GenerationJobService.cs — Hangfire Stage 2 jobs
Backend/Services/MuafaApiClient.cs — Claude API calls with prompt caching
Backend/Services/PromptBuilder.cs — system prompt assembly
Backend/Services/WhatsAppService.cs — Meta Cloud API integration (TestMode active)
Backend/Services/ReferralService.cs — referral creation + Hangfire scheduled delivery
Backend/Services/ProfileHashService.cs — SHA-256 canonical profile hashing
Backend/Services/ArticleLibraryService.cs — Layer 1 cost reduction (LIBRARY HIT/MISS logging)

## Key model files
Backend/Models/Entities/Tenant.cs
Backend/Models/Entities/TenantSettings.cs
Backend/Models/Entities/TenantSubscription.cs
Backend/Models/Entities/InvitationCode.cs
Backend/Models/Entities/UserRole.cs
Backend/Models/Entities/AssistantPhysicianLink.cs
Backend/Models/Entities/TenantRole.cs (shared enum)
Backend/Models/Entities/Referral.cs
Backend/Models/Entities/PatientAccess.cs
Backend/Models/Entities/ArticleLibrary.cs
Backend/Models/Entities/TestScenario.cs — GeneratedArticlesJson added Phase 3.7
Backend/Models/ArticleModels.cs — ArticleSpec + RiskAssessment snake_case fix Phase 3.7
Backend/Models/ApiModels.cs — all request/response DTOs
Backend/Data/MuafaDbContext.cs — EF Core context + all DbSets
Backend/Controllers/ReferralsController.cs
Backend/Controllers/EngagementController.cs
Backend/Controllers/TestScenariosController.cs

## Phase 2 — Complete
- PatientAccess table (phone + 4-digit code authentication)
- Referral, PatientProfile, ReferralEngagement tables
- ArticleEngagement, PatientFeedback, MessageLog tables
- WhatsAppService — Meta Cloud API integration (TestMode active)
  PhoneNumberId: 1112172131979263
  TestMode: true (hello_world template)
- ReferralService — referral creation + Hangfire scheduled delivery
- ReferralsController — POST/GET /api/v1/referrals
- Patient JWT authentication (30-day expiry, Role=Patient claim)
- EngagementController — referral + article engagement tracking
- ProfileHashService — SHA-256 canonical profile hashing
- ArticleLibraryService — Layer 1 cost reduction (LIBRARY HIT/MISS logging)
- WorkflowService updated — checks ArticleLibrary before every Claude API call
- ArticleLibrary table — shared across tenants (TenantId = null)

## Phase 3.5 — Web Portal Update Complete
Frontend pages added (Next.js 14, RTL Arabic):
- /referrals — referral list with engagement dots + risk badges
- /referrals/new — referral creation form (all clinical fields)
- /referrals/[id] — referral detail + engagement timeline + chat
- /test-scenarios — scenario list with status and star rating summary
- /test-scenarios/new — create scenario + preview generated content
- /test-scenarios/[id] — view generated content + submit evaluation
- /admin — tenant management + invitation code generator + chat settings
NavBar component shared across all pages.
api.ts extended: 11 new functions covering referrals,
test scenarios, chat, and tenant management.
Total frontend routes: 14

## Phase 3.7 — Frontend Components (complete)
- ArticleContentViewer (Frontend/src/components/ArticleContentViewer.tsx)
  Shared component used by test-scenarios/[id] and referrals/[id].
  Props: riskLevel, summaryArticle, articleOutlines, referralArticles,
         mode ("referral"|"test-scenario"), initialContent, onGenerate
  - initialContent: Record<number,string> — seeds generatedContent + generatedSet state
    on mount so persisted articles show correct button state without API call
  - Three-state generate buttons: idle → جارٍ التوليد... → عرض →/إخفاء ▲
  - ReactMarkdown rendering with @tailwindcss/typography prose classes
  - mounted flag guards all ReactMarkdown renders to prevent hydration mismatch
  - Article titles from outline.title_ar || outline.title_en (snake_case after fix)
- TypeScript types updated (Frontend/src/types/index.ts):
  ArticleOutline fields are now snake_case: title_ar, title_en, article_id,
  coverage_codes, estimated_word_count, key_topics, rationale
  Stage1Output.risk_assessment fields are snake_case: risk_level, acute_factors, etc.
  TestScenarioResponse now includes generatedArticlesJson: string | null
  ReferralResponse now includes chatEnabled: boolean

## Phase 4 Target — Flutter Mobile Application
See specification document Section 13 for full scope.
Backend APIs are complete and verified.
Web portal (Phase 3.5) serves as UI reference for Flutter development.

## Architecture — 5 User Roles
1. Super Admin — Afyah Wise internal. Manages all tenants globally.
2. Hospital Admin — manages their institution, users, subscription.
3. Physician — creates referrals, test scenarios, evaluations.
4. Assistant — primary referral creator (90% of daily work), linked to physicians.
5. Patient — receives WhatsApp summary, triggers Stage 2, reads articles.

## Architecture — Patient Referral Workflow (Phase 2 complete)
1. Provider creates referral → risk calculator runs (C#, no AI)
2. Stage 1 generates → ArticleLibrary checked first (Layer 1 cache hit = $0)
   → stored in DB and ArticleLibrary on miss
3. WhatsApp sends after configurable delay (default 2h):
   - Message 1: Full summary article + app download link
   - Message 2: 4-digit access code (sent separately, 2s delay)
4. Patient logs in: phone number + 4-digit code → 30-day JWT
5. Patient reads Stage 1 summary (scroll tracking → ArticleEngagement)
6. Patient taps "Read More" → Stage 2 triggered on demand (Hangfire, Rule 3)
7. Articles appear progressively as each Hangfire job completes
8. Patient likes/dislikes, submits feedback
9. Provider views engagement timeline via GET /referrals/{id}/engagement

## Architecture — Three-Layer Cost Reduction
Layer 0: Prompt caching via SDK v5.10.0 — ACTIVE IN PRODUCTION
Layer 1: SHA-256 exact profile hash → ArticleLibrary — ACTIVE IN PRODUCTION
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

RULE 12 — ArticleLibrary is the cache layer
Always check ArticleLibraryService.GetByHashAsync() before calling Claude API.
Always call ArticleLibraryService.SaveAsync() after successful generation.
Never bypass the library check. TenantId = null for all shared entries.

RULE 14 — ArticleSpec and RiskAssessment use snake_case [JsonPropertyName]
Every field on ArticleSpec and RiskAssessment has a [JsonPropertyName] attribute
mapping to the snake_case key the Stage 1 prompt instructs Anthropic to return.
Never add a new field to these classes without the corresponding attribute.
The TypeScript ArticleOutline interface and Stage1Output.risk_assessment type must
use the same snake_case keys. Stage2Output.ArticleContent follows the same pattern.

RULE 13 — WhatsApp delivery is always two messages
Message 1: full summary article + app download link
Message 2: 4-digit access code (sent separately, 2 second delay)
Never combine code and content in the same message.
TestMode uses hello_world template. Production uses text messages.

RULE 15 — Make surgical changes only
Modify exactly what the brief specifies, nothing adjacent, no refactoring,
no reformatting, no additional improvements unless explicitly requested.

## Environment variables on Railway (production)
ConnectionStrings__DefaultConnection — PostgreSQL connection string
Anthropic__ApiKey — Claude API key (sk-ant-...)
Jwt__Secret — JWT signing secret
Cors__AllowedOrigins__0 — https://muafaplus1.vercel.app
WhatsApp__PhoneNumberId = 1112172131979263
WhatsApp__BusinessAccountId = 2052346812292213
WhatsApp__AccessToken = (set in Railway — starts with EAA)
WhatsApp__TestMode = true

## Governance
- Prompt refinement: manual only, decided by Afyah Wise outside platform
- Clinical content changes require Afyah Wise clinical team approval
- Prompt file version comment incremented after any modification
- Similarity threshold (0.92) reviewed after every 100 new patients
- Patient data never leaves platform except to Anthropic API
- Article library is permanent — no expiry, no deletion
