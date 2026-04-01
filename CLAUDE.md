# Muafa+ — Claude Code Project Briefing

## What this project is
Muafa+ is an Arabic medical education platform for post-diagnosis patient
care in Yemen. Physicians enter patient data and the system generates
personalised Arabic health education articles using Claude AI.

- Backend: .NET 8 ASP.NET Core Web API
- Frontend: Next.js 14 React, RTL Arabic, Tailwind CSS
- AI: Anthropic Claude Sonnet 4 with prompt caching
- Auth: JWT Bearer tokens + BCrypt password hashing
- Background jobs: Hangfire
- Export: QuestPDF (Arabic PDF) and OpenXml (Arabic Word)
- Database: SQL Server with Entity Framework Core

## How to run locally

Start the backend:
cd Backend
dotnet user-secrets set "Anthropic:ApiKey" "your-key-here"
dotnet user-secrets set "Jwt:Secret" "any-long-random-string"
dotnet ef database update
dotnet run
API runs at https://localhost:5001
Swagger UI is at the root https://localhost:5001

Start the frontend:
cd Frontend
copy .env.local.example .env.local
npm install
npm run dev
Portal runs at http://localhost:3000

Test login after running migrations:
Email: ahmed.sana@hospital.ye
Password: MuafaPlus2025!

## Rules that must never be broken

RULE 1 — Risk calculation
Always calculated in C# by RiskCalculatorService.cs BEFORE calling Claude API.
Never put the risk algorithm inside a prompt. This was the original design flaw.

RULE 2 — Article content field name
The field is ContentAr with JsonPropertyName("content_ar").
Never use "content" — that was a silent bug causing empty articles.

RULE 3 — Stage 2 is always async
Stage 2 article generation always runs as a Hangfire background job
via GenerationJobService.cs. It must never block an HTTP response.
The endpoint returns HTTP 202 immediately after Stage 1 completes.

RULE 4 — PhysicianId always from JWT
PhysicianId must always come from the JWT claim in the controller.
Never trust PhysicianId from the request body.

RULE 5 — Prompt file paths
Prompt files in Backend/Prompts/ are loaded using
IWebHostEnvironment.ContentRootPath. Never use AppDomain.CurrentDomain.BaseDirectory.

RULE 6 — All controllers require auth
Every controller except /auth/login and /health has [Authorize].
Never add an unauthenticated endpoint without explicit approval.

## Where the key files are

Backend/Controllers/AuthController.cs          login and /me endpoints
Backend/Controllers/ContentGenerationController.cs   generate complete and stage1
Backend/Controllers/SessionController.cs       GET session by id and status poll
Backend/Controllers/PhysicianController.
