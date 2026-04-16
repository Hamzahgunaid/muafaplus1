using System.Security.Cryptography;
using System.Text;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 2 Task 4 — Layer 1 cost reduction.
/// Generates a deterministic SHA-256 hash from canonical patient profile fields.
/// The hash is used as the ArticleLibrary lookup key.
///
/// Normalisation rules:
///   - All fields: lowercase, trim whitespace
///   - List fields (Comorbidities, CurrentMedications): split by comma,
///     trim each element, sort alphabetically, rejoin with ","
///   - Null optional fields are treated as empty string
///
/// Canonical format:
///   {diagnosis}|{ageGroup}|{comorbidities}|{medications}|{allergies}|{restrictions}
///
/// Registered as Singleton — pure computation, no state or DB dependency.
/// </summary>
public class ProfileHashService
{
    // ─────────────────────────────────────────────────────────────────────────
    // Public overloads
    // ─────────────────────────────────────────────────────────────────────────

    public string GenerateHash(PatientProfile profile) =>
        ComputeHash(
            profile.PrimaryDiagnosis,
            profile.AgeGroup,
            profile.Comorbidities,
            profile.CurrentMedications,
            profile.Allergies,
            profile.MedicalRestrictions);

    public string GenerateHash(CreateReferralRequest request) =>
        ComputeHash(
            request.PrimaryDiagnosis,
            request.AgeGroup,
            request.Comorbidities,
            request.CurrentMedications,
            request.Allergies,
            request.MedicalRestrictions);

    public string GenerateHash(PatientData data) =>
        ComputeHash(
            data.PrimaryDiagnosis,
            data.AgeGroup,
            data.Comorbidities,
            data.CurrentMedications,
            data.Allergies,
            data.MedicalRestrictions);

    // ─────────────────────────────────────────────────────────────────────────
    // Core implementation
    // ─────────────────────────────────────────────────────────────────────────

    private static string ComputeHash(
        string  primaryDiagnosis,
        string  ageGroup,
        string? comorbidities,
        string? currentMedications,
        string? allergies,
        string? medicalRestrictions)
    {
        var diagnosis    = primaryDiagnosis.ToLowerInvariant().Trim();
        var age          = ageGroup.ToLowerInvariant().Trim();
        var comorbid     = NormaliseList(comorbidities);
        var medications  = NormaliseList(currentMedications);
        var allergy      = (allergies           ?? string.Empty).ToLowerInvariant().Trim();
        var restrictions = (medicalRestrictions ?? string.Empty).ToLowerInvariant().Trim();

        var canonical =
            $"{diagnosis}|{age}|{comorbid}|{medications}|{allergy}|{restrictions}";

        using var sha  = SHA256.Create();
        var bytes      = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLower();
    }

    /// <summary>
    /// Split by comma, trim each element, sort alphabetically, rejoin.
    /// "  Hypertension , Diabetes  " → "diabetes,hypertension"
    /// </summary>
    private static string NormaliseList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return string.Join(",",
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(s => s.ToLowerInvariant().Trim())
               .Where(s => s.Length > 0)
               .Order());
    }
}
