# CLAUDE.md — Muafa+ Shared Memory
# Last updated: April 2026
# DO NOT modify this file manually — update via architect brief only

## Project Identity
- Platform: Muafa+ (معافى بلس) — AI-powered Arabic medical education SaaS
- Owner: Afyah Wise for Digital Solutions and Technologies
- Scope: Yemen — post-diagnosis patient education — multi-tenant B2B
- GitHub: https://github.com/Hamzahgunaid/muafaplus1

## Production URLs
- Frontend: https://muafaplus1.vercel.app
- Backend: https://muafaplus1-production.up.railway.app
- Health: https://muafaplus1-production.up.railway.app/api/v1/ContentGeneration/health

## Technology Stack
- Backend: .NET 8 ASP.NET Core — Railway (Docker)
- Frontend: Next.js 14 — React, TypeScript, RTL Arabic — Vercel
- Database: PostgreSQL 15 + pgvector — Railway
- Background Jobs: Hangfire with PostgreSQL storage
- AI: Anthropic Claude Sonnet 4 — claude-sonnet-4-20250514
- Anthropic SDK: v5.10.0 with prompt caching enabled
- Messaging: WhatsApp Business API (Meta Cloud API)
- Auth: JWT Bearer + BCrypt (providers) / Phone+Code (patients)

## Architecture Workflow
Claude (Architect) → Hamzah (Coordinator) → Claude Code (Implementer)
- Claude writes precise implementation briefs
- Hamzah pastes briefs into Claude Code
- Claude Code output returned to Claude for review before proceeding

## 14 Rules for Claude Code
1. Never modify files not explicitly listed in the brief
2. Never change existing working functionality
3. Always use object initializers — ApiResponse<T>.Fail() and .Success() do not exist
4. PhysicianId always extracted from JWT claim — never from request body
5. All data access is tenant-scoped at service layer
6. UserId in JWT is a Guid — always parse with Guid.TryParse before DB queries
7. Role claim key is "Role" — RoleClaimType = "Role" set in Program.cs
8. TenantId claim key is "TenantId"
9. Prompt files loaded once at startup — runtime changes require restart
10. Never commit secrets — all secrets in Railway environment variables
11. appsettings.Development.json excluded from git permanently
12. All schema changes applied manually via Railway SQL — EF Core migrations are NOT used in production
13. ArticleOutline fields are snake_case with [JsonPropertyName] — title_ar, title_en, key_topics
14. Make surgical changes only — modify exactly what the brief specifies, nothing adjacent, no refactoring, no reformatting, no additional improvements unless explicitly requested

## Database — PostgreSQL on Railway
### Tables (18 total)
Platform: Tenants, TenantSettings, TenantSubscriptions, InvitationCodes
Users: Users (AppUser), UserRoles, AssistantPhysicianLinks
Referrals: Referrals, PatientProfiles, PatientAccess
Content: GeneratedArticles, ArticleLibrary, GenerationSessions
Engagement: ArticleEngagement, ReferralEngagement, PatientFeedback
Quality: TestScenarios, ContentEvaluations
Messaging: MessageLog
Chat: ChatThreads, ChatMessages

### Key Type Facts
- Users.UserId: uuid (Guid in C#)
- UserRoles.UserId: uuid (after Fix_UserRole_UserId_ToGuid migration)
- UserRoles.Role: int4 (TenantRole enum — SuperAdmin=0, HospitalAdmin=1, Physician=2, Assistant=3)
- TenantId everywhere: uuid
- GenerationSessions.PatientId: nullable text (FK constraint dropped)
- GenerationSessions.PhysicianId: text (FK constraint dropped — references AppUser not Physicians)

### Manual SQL Fixes Applied to Production
- UserRoles.UserId changed from text to uuid
- GenerationSessions.PatientId made nullable (ALTER TABLE)
- FK_GenerationSessions_Patients_PatientId dropped
- FK_GenerationSessions_Physicians_PhysicianId dropped
- TenantSettings.WhatsAppEnabled added: boolean NOT NULL DEFAULT false
- Referrals.ChatEnabled added: boolean NOT NULL DEFAULT true
- Users.ChatEnabled added: boolean NOT NULL DEFAULT true

### Test Data (Production Railway)
Tenant: Sana'a General Hospital
TenantId: a0000000-0000-0000-0000-000000000001

| Email | UserId | Role |
|---|---|---|
| admin@muafaplus.com | 00000000-0000-0000-0000-000000000099 | SuperAdmin (0) |
| mohammed.z@diabetes.ye | 00000000-0000-0000-0000-000000000003 | HospitalAdmin (1) |
| ahmed.sana@hospital.ye | 00000000-0000-0000-0000-000000000001 | Physician (2) |
| fatima.hakim@clinic.ye | 00000000-0000-0000-0000-000000000002 | Assistant (3) |

Password for all: MuafaPlus2025!

## Railway Environment Variables
DATABASE_URL=<railway postgresql connection string>
ANTHROPIC_API_KEY=<anthropic api key>
JWT_SECRET=<jwt secret>
Cors__AllowedOrigins__0=https://muafaplus1.vercel.app
WHATSAPP_PHONE_NUMBER_ID=1112172131979263
WHATSAPP_ACCESS_TOKEN=<meta access token>
WHATSAPP_TEST_MODE=true

## WhatsApp Configuration
- Provider: Meta Cloud API
- PhoneNumberId: 1112172131979263
- TestMode: true (uses hello_world template only)
- Two-message delivery: Message 1 = full summary article, Message 2 = 4-digit access code

## JWT Claims Structure
```json
{
  "UserId": "guid",
  "Role": "SuperAdmin|HospitalAdmin|Physician|Assistant",
  "TenantId": "guid",
  "PhysicianId": "guid-or-empty",
  "FullName": "string",
  "sub": "email",
  "exp": "unix timestamp"
}
```

## Login Response Shape
```json
{
  "success": true,
  "data": {
    "token": "eyJ...",
    "userId": "00000000-0000-0000-0000-000000000001",
    "role": "Physician",
    "tenantId": "a0000000-0000-0000-0000-000000000001",
    "physicianId": "PHY001",
    "fullName": "Dr. Ahmed Al-Sana",
    "specialty": "Internal Medicine",
    "institution": "Sana'a General Hospital",
    "expiresAt": "2026-...",
    "mustResetOnNextLogin": false
  }
}
```

## Frontend localStorage Keys
- muafa_token — JWT token
- muafa_role — user role string
- muafa_userid — user UUID
- muafa_tenantid — tenant UUID
- muafa_fullname — display name
- muafa_user — physician profile JSON (physicianId, fullName, specialty, institution)

All keys cleared on logout.

## Role → Page Mapping
| Role | Landing Page | Admin | Referrals | Test Scenarios |
|---|---|---|---|---|
| SuperAdmin | /dashboard | ✅ /admin → /admin/tenants/[id] | ✅ all | ✅ |
| HospitalAdmin | /dashboard | ✅ /admin → /admin/tenants/[id] | ✅ own tenant | ✅ |
| Physician | /dashboard | ❌ | ✅ own | ✅ |
| Assistant | /dashboard | ❌ | ✅ own | ❌ |
| Patient | Flutter only | ❌ | ❌ | ❌ |

## NavBar Links Per Role
- SuperAdmin: الرئيسية, الإحالات, سيناريوهات الاختبار, الإدارة
- HospitalAdmin: الرئيسية, الإحالات, سيناريوهات الاختبار, الإدارة
- Physician: الرئيسية, الإحالات, سيناريوهات الاختبار
- Assistant: الرئيسية, الإحالات
- All roles: + مريض جديد button visible

## Role Labels (Arabic)
- SuperAdmin → مدير النظام
- HospitalAdmin → مدير مستشفى
- Physician → طبيب
- Assistant → مساعد

## Cost Architecture
- Layer 0: Prompt caching — ACTIVE (Anthropic SDK v5.10.0)
- Layer 1: SHA-256 exact match via ArticleLibrary — ACTIVE
- Layer 2: pgvector near-match (threshold 0.92) — PLANNED (Phase 5)
- Layer 3: Anthropic API with 30s Hangfire batch delay — ACTIVE fallback
- Current cost: ~$0.29/session for 6 Arabic articles

## Content Generation
- Model: claude-sonnet-4-20250514 (Claude Sonnet 4)
- Stage 1: Risk assessment + summary article (800-1000 words) + article outlines
- Stage 2: 3-6 detailed articles (500-750 words each) via Hangfire background jobs
- Stage 1 for test scenarios: synchronous (streaming available via SSE endpoint)
- Stage 2 for test scenarios: per-article via POST /test-scenarios/{id}/generate-article
- Risk levels: LOW(≤0.5), MODERATE(1.0-1.5), HIGH(2.0-2.5), CRITICAL(≥3.0)

## ArticleOutline JSON Shape (snake_case — Rule 13)
```json
{
  "title_ar": "عنوان المقال بالعربية",
  "title_en": "Article Title in English",
  "key_topics": ["topic1", "topic2"],
  "rationale": "Why this article is relevant"
}
```

## Key File Locations
### Backend
- Controllers: Backend/Controllers/
  - AuthController.cs — login, change-password, patient login
  - TenantsController.cs — tenant CRUD, users, settings, subscription, assistant-links
  - ReferralsController.cs — referral workflow, articles, engagement, chat
  - TestScenariosController.cs — test scenarios, evaluations, SSE stream, generate-article
  - ContentGenerationController.cs — legacy generation (Phase 0)
- Models: Backend/Models/
  - Entities/ — AppUser, UserRole, Tenant, Referral, TestScenario, ChatThread, ChatMessage etc.
  - DTOs/ — request/response objects
  - ArticleModels.cs — Stage1Output, Stage2Article, ArticleSpec, RiskAssessment (all snake_case)
  - ApiModels.cs — ReferralResponse, TenantResponse, UserSummaryResponse etc.
- Services: Backend/Services/
  - JwtService.cs — GenerateToken(AppUser) — use this overload only
  - InvitationCodeService.cs
  - TenantService.cs
- Data: Backend/Data/MuafaDbContext.cs
- Prompts: Backend/Prompts/Stage1SystemPrompt.txt, Stage2SystemPrompt.txt

### Frontend
- Pages: Frontend/src/app/
  - dashboard/page.tsx — unified role-aware dashboard
  - admin/page.tsx — SuperAdmin tenant list / HospitalAdmin auto-redirect
  - admin/tenants/[id]/page.tsx — per-tenant management (5 cards)
  - referrals/page.tsx, referrals/[id]/page.tsx, referrals/new/page.tsx
  - test-scenarios/page.tsx, test-scenarios/[id]/page.tsx, test-scenarios/new/page.tsx
- Components: Frontend/src/components/
  - NavBar.tsx — role-based navigation
  - ArticleContentViewer.tsx — shared Markdown article renderer (referral + test-scenario modes)
- State: Frontend/src/lib/store.ts — Zustand store (useAuthStore)
- API: Frontend/src/services/api.ts — all API calls
- Types: Frontend/src/types/index.ts — all TypeScript interfaces

## Completed Phases
### Phase 0 — Baseline (Complete)
- .NET 8 backend, Next.js 14 frontend, PostgreSQL on Railway
- JWT auth, BCrypt, 5-step risk calculator
- Stage 1 sync generation, Stage 2 Hangfire background jobs
- Prompt caching via Anthropic SDK v5.10.0
- Cost: ~$0.29/session verified in production

### Phase 1 — Multi-Tenant Foundation (Complete)
- 6 tables: Tenants, TenantSettings, TenantSubscriptions, InvitationCodes, UserRoles, AssistantPhysicianLinks
- Invitation code service (SA-, HA-, PH-, AS- prefixes)
- Tenant management API (7 endpoints)
- Patient Name Policy enforcement

### Phase 2 — Referral Workflow (Complete)
- 8 tables: PatientAccess, Referrals, PatientProfiles, ReferralEngagement, ArticleEngagement, PatientFeedback, MessageLog, ArticleLibrary
- WhatsApp Business API (Meta Cloud API, TestMode, hello_world template)
- Patient JWT auth (30-day expiry, Role=Patient)
- SHA-256 Layer 1 cost reduction with LIBRARY HIT/MISS logging
- Engagement tracking endpoints

### Phase 2.5 — Frontend + Auth Overhaul (Complete)
- Unified role-based routing — all roles land on /dashboard
- Role-aware dashboard — admins see referrals, physicians see sessions
- Admin page Option C — tenant list + per-tenant detail page (/admin/tenants/[id])
- 5 management cards per tenant: Subscription, Settings, Users, AssistantLinks, InvitationCodes
- Arabic role labels: مدير النظام, مدير مستشفى, طبيب, مساعد
- ArticleContentViewer shared component with Markdown rendering
- Referral detail — inline article viewer with react-markdown
- Test scenario detail — توليد button with persistent state, real Arabic titles
- Stage 2 per-article generation for test scenarios with DB persistence
- snake_case fix for ArticleOutline fields
- Chat 403 fix on referral detail page
- ChangePassword fixed to use AppUser
- GetTenantUsers fixed — returns TenantRole enum name not AppUser.Role string
- GenerationSessions FK constraints dropped (Patients + Physicians)

### Phase 3 — Physician-Patient Chat (Complete)
- Physician-patient async chat working end to end with disclaimer
- ChatEnabled wired from Referral entity into ReferralResponse
- WhatsAppEnabled boolean added to TenantSettings (admin toggle persists)
- Physician.ChatEnabled gate removed — chat gated by TenantSettings.ChatEnabled only
- Admin settings toggle saves correctly (whatsAppEnabled ?? false guard added)

## Pending — Phase 3 Remaining
- [ ] Streaming SSE frontend for test-scenarios/new page
- [ ] Read progress tracking display on referral detail
- [ ] Push notifications via Firebase (low priority)

## Pending — UX Fixes
- [ ] React hydration errors #418/#423 on admin tenant page (chat API call)
- [ ] Assistant-physician linking — test dropdown form in production
- [ ] Invitation codes — test copy button and generation in production

## Pending — Phase 5
- [ ] pgvector Layer 2 near-match vector search (after 50 patients)
- [ ] Voyage AI voyage-3-lite embeddings
- [ ] Clinical governance review of 0.92 similarity threshold

## Governance
- Prompt refinement: MANUAL ONLY — decided by Afyah Wise clinical team
- No automatic prompt modification
- Prompt files loaded at startup — changes require redeployment
- Vector search threshold (0.92) requires clinical team review after every 100 patients
- All chat sessions must display disclaimer (Arabic + English) before first message
- EF Core migrations written in code but NOT applied automatically — apply manually via Railway SQL
