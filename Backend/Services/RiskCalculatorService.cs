using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Deterministic risk calculator — extracted from Stage 1 system prompt.
/// The algorithm is now computed in C# and the result is passed to the AI prompt,
/// which removes non-determinism and makes risk decisions auditable.
/// </summary>
public class RiskCalculatorService
{
    // ── Acute Danger keywords ─────────────────────────────────────────────────
    // 1 point each. Any match triggers the factor.

    private static readonly string[] AnticoagulantKeywords =
    [
        "warfarin", "warf",
        "apixaban", "eliquis",
        "rivaroxaban", "xarelto",
        "dabigatran", "pradaxa",
        "edoxaban", "savaysa",
        "doac", "noac",
        "heparin",
        "aspirin 325", "aspirin 500", "asa 325", "asa 500",
        "clopidogrel", "ticagrelor", "prasugrel"
    ];

    private static readonly string[] HypoglycemicKeywords =
    [
        "insulin",
        "glargine", "detemir", "degludec",
        "lispro", "aspart", "glulisine",
        "glipizide", "glyburide", "glibenclamide",
        "glimepiride", "gliclazide",
        "sulfonylurea"
    ];

    private static readonly string[] RecentEventKeywords =
    [
        "recent hospitalization", "hospitalized", "hospital admission",
        "recent mi", "myocardial infarction", "heart attack",
        "recent stroke", "cva",
        "major surgery", "post-op", "post op", "postoperative",
        "recent dka", "dka",
        "recent icu", "intensive care",
        "recent procedure"
    ];

    private static readonly string[] SevereOrganFailureKeywords =
    [
        "nyha iv", "nyha class iv", "end-stage heart failure",
        "gfr<15", "gfr < 15", "esrd", "end-stage renal",
        "decompensated cirrhosis", "liver failure", "hepatic failure"
    ];

    private static readonly string[] ActiveCancerKeywords =
    [
        "chemotherapy", "chemo", "metastatic", "active cancer",
        "oncology treatment", "radiation therapy"
    ];

    // ── Complexity keywords ───────────────────────────────────────────────────
    // 0.5 points each.

    private static readonly string[] CognitivePoorAdherenceKeywords =
    [
        "cognitive impairment", "dementia", "alzheimer", "poor adherence",
        "non-adherent", "non-compliant", "forgetful", "confusion"
    ];

    private static readonly string[] FrequentMonitoringKeywords =
    [
        "daily monitoring", "weekly monitoring", "frequent checks",
        "home glucose monitoring", "continuous glucose", "cgm",
        "inr monitoring", "oxygen therapy"
    ];

    // ── Protective keywords ───────────────────────────────────────────────────
    // -0.5 points each.

    private static readonly string[] StableConditionKeywords =
    [
        "stable", "well-controlled", "controlled", "in remission",
        "no recent hospitalization", "no admissions"
    ];

    private static readonly string[] EarlyStageKeywords =
    [
        "early stage", "mild", "newly diagnosed", "stage i", "stage 1",
        "reversible", "resolving"
    ];

    private static readonly string[] GoodAdherenceKeywords =
    [
        "good adherence", "adherent", "compliant", "excellent control",
        "follows instructions", "motivated patient"
    ];

    // ── Age group scoring ─────────────────────────────────────────────────────

    private static readonly HashSet<string> HighRiskAgeGroups =
        new(StringComparer.OrdinalIgnoreCase) { "Elderly", "Infant", "Toddler" };

    private static readonly HashSet<string> ProtectiveAgeGroups =
        new(StringComparer.OrdinalIgnoreCase) { "Adult" };

    private static readonly HashSet<string> PediatricAgeGroups =
        new(StringComparer.OrdinalIgnoreCase) { "Child", "Adolescent" };

    // ─────────────────────────────────────────────────────────────────────────

    public RiskScore Calculate(PatientData patient)
    {
        var factors = new RiskFactors();

        var allText = BuildSearchText(patient);

        // ── Step 1: Acute Danger factors (1 pt each) ─────────────────────────

        if (ContainsAny(allText, AnticoagulantKeywords))
        {
            factors.AcuteFactors.Add("Anticoagulant or antiplatelet therapy");
            factors.AcutePoints += 1.0m;
        }

        if (ContainsAny(allText, HypoglycemicKeywords))
        {
            factors.AcuteFactors.Add("Hypoglycaemia-risk medication (insulin or sulfonylurea)");
            factors.AcutePoints += 1.0m;
        }

        if (ContainsAny(allText, RecentEventKeywords))
        {
            factors.AcuteFactors.Add("Recent high-acuity event within 6 weeks");
            factors.AcutePoints += 1.0m;
        }

        if (ContainsAny(allText, SevereOrganFailureKeywords))
        {
            factors.AcuteFactors.Add("Severe organ failure (NYHA IV / GFR < 15 / decompensated liver)");
            factors.AcutePoints += 1.0m;
        }

        if (ContainsAny(allText, ActiveCancerKeywords))
        {
            factors.AcuteFactors.Add("Active cancer or cytotoxic treatment");
            factors.AcutePoints += 1.0m;
        }

        // Dangerous drug interaction: warfarin + NSAIDs or warfarin + aspirin high-dose
        if (ContainsAny(allText, ["warfarin", "warf"]) &&
            ContainsAny(allText, ["nsaid", "ibuprofen", "naproxen", "diclofenac", "indomethacin"]))
        {
            factors.AcuteFactors.Add("Dangerous drug interaction: warfarin + NSAID");
            factors.AcutePoints += 1.0m;
        }

        // ── Step 2: Complexity factors (0.5 pt each) ─────────────────────────

        var medicationCount = CountMedications(patient.CurrentMedications);
        if (medicationCount >= 4)
        {
            factors.ComplexityFactors.Add($"Polypharmacy ({medicationCount} medications)");
            factors.ComplexityPoints += 0.5m;
        }

        var comorbidityCount = CountComorbidities(patient.Comorbidities);
        if (comorbidityCount >= 2)
        {
            factors.ComplexityFactors.Add($"Multiple chronic conditions ({comorbidityCount} comorbidities)");
            factors.ComplexityPoints += 0.5m;
        }

        if (ContainsAny(allText, CognitivePoorAdherenceKeywords))
        {
            factors.ComplexityFactors.Add("Cognitive impairment or documented poor adherence");
            factors.ComplexityPoints += 0.5m;
        }

        if (HighRiskAgeGroups.Contains(patient.AgeGroup))
        {
            factors.ComplexityFactors.Add($"Age group with elevated risk ({patient.AgeGroup})");
            factors.ComplexityPoints += 0.5m;
        }

        if (PediatricAgeGroups.Contains(patient.AgeGroup))
        {
            factors.ComplexityFactors.Add($"Paediatric patient ({patient.AgeGroup}) — requires age-appropriate adaptation");
            factors.ComplexityPoints += 0.5m;
        }

        if (ContainsAny(allText, FrequentMonitoringKeywords))
        {
            factors.ComplexityFactors.Add("Requires frequent (daily or weekly) monitoring");
            factors.ComplexityPoints += 0.5m;
        }

        // ── Step 3: Protective factors (-0.5 pt each) ─────────────────────────

        if (ContainsAny(allText, StableConditionKeywords))
        {
            factors.ProtectiveFactors.Add("Condition documented as stable > 3 months");
            factors.ProtectivePoints += 0.5m;
        }

        if (ProtectiveAgeGroups.Contains(patient.AgeGroup))
        {
            factors.ProtectiveFactors.Add("Adult age group (18–70), functionally independent");
            factors.ProtectivePoints += 0.5m;
        }

        if (ContainsAny(allText, EarlyStageKeywords))
        {
            factors.ProtectiveFactors.Add("Early-stage or mild condition");
            factors.ProtectivePoints += 0.5m;
        }

        if (ContainsAny(allText, GoodAdherenceKeywords))
        {
            factors.ProtectiveFactors.Add("Documented good adherence history");
            factors.ProtectivePoints += 0.5m;
        }

        // ── Step 4: Calculate total score ─────────────────────────────────────

        var totalScore = factors.AcutePoints + factors.ComplexityPoints - factors.ProtectivePoints;

        // ── Step 5: Assign risk level ─────────────────────────────────────────

        var riskLevel = totalScore switch
        {
            <= 0.5m              => RiskLevel.Low,
            <= 1.5m              => RiskLevel.Moderate,
            <= 2.5m              => RiskLevel.High,
            _                    => RiskLevel.Critical
        };

        return new RiskScore
        {
            AcuteFactors        = factors.AcuteFactors,
            AcutePoints         = factors.AcutePoints,
            ComplexityFactors   = factors.ComplexityFactors,
            ComplexityPoints    = factors.ComplexityPoints,
            ProtectiveFactors   = factors.ProtectiveFactors,
            ProtectivePoints    = factors.ProtectivePoints,
            TotalScore          = totalScore,
            RiskLevel           = riskLevel,
            Rationale           = BuildRationale(factors, totalScore, riskLevel, medicationCount, comorbidityCount)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildSearchText(PatientData patient)
    {
        return string.Join(" ",
            patient.PrimaryDiagnosis,
            patient.Comorbidities,
            patient.CurrentMedications,
            patient.Allergies,
            patient.MedicalRestrictions
        ).ToLowerInvariant();
    }

    private static bool ContainsAny(string text, IEnumerable<string> keywords)
        => keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static int CountMedications(string medications)
    {
        if (string.IsNullOrWhiteSpace(medications) ||
            medications.Equals("none", StringComparison.OrdinalIgnoreCase))
            return 0;

        // Split on common delimiters: comma, semicolon, newline, "and"
        var parts = medications
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 2)
            .ToList();

        return parts.Count;
    }

    private static int CountComorbidities(string comorbidities)
    {
        if (string.IsNullOrWhiteSpace(comorbidities) ||
            comorbidities.Equals("none", StringComparison.OrdinalIgnoreCase))
            return 0;

        return comorbidities
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Count(p => p.Length > 2);
    }

    private static string BuildRationale(
        RiskFactors factors,
        decimal totalScore,
        RiskLevel level,
        int medicationCount,
        int comorbidityCount)
    {
        var parts = new List<string>();

        if (factors.AcutePoints > 0)
            parts.Add($"Acute danger: {factors.AcutePoints:0.0} pt ({factors.AcuteFactors.Count} factor(s))");

        if (factors.ComplexityPoints > 0)
            parts.Add($"Complexity: {factors.ComplexityPoints:0.0} pt ({factors.ComplexityFactors.Count} factor(s))");

        if (factors.ProtectivePoints > 0)
            parts.Add($"Protective: -{factors.ProtectivePoints:0.0} pt ({factors.ProtectiveFactors.Count} factor(s))");

        parts.Add($"Net score: {totalScore:0.0} → {level.ToString().ToUpperInvariant()}");

        return string.Join(". ", parts) + ".";
    }

    private sealed class RiskFactors
    {
        public List<string> AcuteFactors      { get; } = [];
        public decimal      AcutePoints       { get; set; }
        public List<string> ComplexityFactors { get; } = [];
        public decimal      ComplexityPoints  { get; set; }
        public List<string> ProtectiveFactors { get; } = [];
        public decimal      ProtectivePoints  { get; set; }
    }
}
