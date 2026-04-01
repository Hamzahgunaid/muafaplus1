namespace MuafaPlus.Models;

public enum RiskLevel
{
    Low,
    Moderate,
    High,
    Critical
}

/// <summary>
/// Deterministic risk score produced by RiskCalculatorService.
/// Passed into the Stage 1 prompt so the AI receives a pre-computed result,
/// not the algorithm itself.
/// </summary>
public class RiskScore
{
    public List<string> AcuteFactors      { get; init; } = [];
    public decimal      AcutePoints       { get; init; }

    public List<string> ComplexityFactors { get; init; } = [];
    public decimal      ComplexityPoints  { get; init; }

    public List<string> ProtectiveFactors { get; init; } = [];
    public decimal      ProtectivePoints  { get; init; }

    public decimal      TotalScore        { get; init; }
    public RiskLevel    RiskLevel         { get; init; }
    public string       Rationale         { get; init; } = string.Empty;

    /// <summary>Returns the risk level as the uppercase string the prompts expect.</summary>
    public string RiskLevelString => RiskLevel.ToString().ToUpperInvariant();

    /// <summary>Formats the score as a compact prompt injection block.</summary>
    public string ToPromptBlock()
    {
        var lines = new List<string>
        {
            "# PRE-COMPUTED RISK ASSESSMENT (calculated by system — do not recalculate)",
            $"**Risk Level:** {RiskLevelString}",
            $"**Score:** {TotalScore:0.0} (Acute {AcutePoints:0.0} + Complexity {ComplexityPoints:0.0} − Protective {ProtectivePoints:0.0})",
            $"**Rationale:** {Rationale}"
        };

        if (AcuteFactors.Count > 0)
            lines.Add("**Acute danger factors:** " + string.Join(", ", AcuteFactors));

        if (ComplexityFactors.Count > 0)
            lines.Add("**Complexity factors:** " + string.Join(", ", ComplexityFactors));

        if (ProtectiveFactors.Count > 0)
            lines.Add("**Protective factors:** " + string.Join(", ", ProtectiveFactors));

        return string.Join("\n", lines);
    }
}
