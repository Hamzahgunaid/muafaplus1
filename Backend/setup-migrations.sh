#!/bin/bash
# ============================================================
# Muafa+ Phase 2 — Setup Script
# Run once after cloning. Requires .NET 8 SDK and SQL Server.
# ============================================================

echo "=== Muafa+ Phase 2 Setup ==="

# 1. Set secrets — never commit these values
echo ""
echo "Step 1: Setting user secrets..."
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey"  "sk-ant-api03-YOUR_KEY_HERE"
dotnet user-secrets set "Jwt:Secret"        "$(openssl rand -base64 48)"
echo "   Anthropic API key and JWT secret configured."
echo "   IMPORTANT: Replace the Anthropic key placeholder with your real key:"
echo "   dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\""

# 2. Restore packages
echo ""
echo "Step 2: Restoring packages..."
dotnet restore

# 3. Build
echo ""
echo "Step 3: Building..."
dotnet build --no-restore

# 4. Create EF migration (only if Migrations/ is empty)
echo ""
echo "Step 4: Creating EF migration..."
if [ ! -d "Migrations" ] || [ -z "$(ls -A Migrations 2>/dev/null)" ]; then
  dotnet ef migrations add InitialCreate --output-dir Migrations
  echo "   Migration created."
else
  echo "   Migrations already exist — skipping."
fi

# 5. Apply migration (also runs automatically on app startup in Development)
echo ""
echo "Step 5: Applying migration to database..."
dotnet ef database update
echo "   Database schema created. Seed data (PHY001-003) applied."

# 6. Run
echo ""
echo "=== Setup complete. Starting API... ==="
echo "   Swagger UI: https://localhost:5001"
echo "   Health check: https://localhost:5001/health"
echo "   Hangfire dashboard: https://localhost:5001/hangfire"
echo ""
dotnet run
