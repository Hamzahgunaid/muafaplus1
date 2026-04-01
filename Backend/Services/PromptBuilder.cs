using MuafaPlus.Models;
using Microsoft.AspNetCore.Hosting;
using System.Text;

namespace MuafaPlus.Services;

/// <summary>
/// Builds prompts for Claude API calls.
///
/// Key changes from v1:
///   - Uses IWebHostEnvironment.ContentRootPath instead of AppDomain.CurrentDomain.BaseDirectory
///     so prompt files resolve correctly in Docker / Azure App Service.
///   - Stage 1 user prompt now receives a pre-computed RiskScore block from
///     RiskCalculatorService — the algorithm has been removed from the system prompt.
///   - Stage 2 JSON schema uses "content_ar" (was "content"), matching Stage2Output model.
/// </summary>
public class PromptBuilder
{
    private readonly string _stage1SystemPrompt;
    private readonly string _stage2SystemPrompt;
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(IWebHostEnvironment env, ILogger<PromptBuilder> logger)
    {
        _logger = logger;

        // ContentRootPath resolves correctly on all deployment targets.
        var promptsPath = Path.Combine(env.ContentRootPath, "Prompts");

        _stage1SystemPrompt = File.ReadAllText(Path.Combine(promptsPath, "Stage1SystemPrompt.txt"));
        _stage2SystemPrompt = File.ReadAllText(Path.Combine(promptsPath, "Stage2SystemPrompt.txt"));

        _logger.LogInformation("Prompt templates loaded from {Path}", promptsPath);
    }

    public string GetStage1SystemPrompt() => _stage1SystemPrompt;
    public string GetStage2SystemPrompt() => _stage2SystemPrompt;

    /// <summary>
    /// Stage 1 user prompt. Receives the pre-computed risk score so the AI
    /// uses it directly rather than re-deriving it from the algorithm.
    /// </summary>
    public string BuildStage1UserPrompt(PatientData patient, RiskScore riskScore)
    {
        return $@"
{riskScore.ToPromptBlock()}

# PATIENT PROFILE

**Primary Diagnosis**: {patient.PrimaryDiagnosis}
**Age Group**: {patient.AgeGroup}
**Comorbidities**: {patient.Comorbidities}
**Current Medications**: {patient.CurrentMedications}
**Allergies**: {patient.Allergies}
**Medical Restrictions**: {patient.MedicalRestrictions}

# TASK

1. Accept the pre-computed risk level above — do NOT recalculate.
2. Generate summary article (800-1,000 words, At a Glance format) in Arabic.
3. Create 3-6 article outlines with full specifications for THIS patient.

Focus on THIS patient's critical needs based on their diagnosis, medications, and risk level.

# OUTPUT FORMAT

Return ONLY valid JSON matching this exact schema. No text before or after.

```json
{{
  ""risk_assessment"": {{
    ""acute_factors"": [""factor1""],
    ""acute_points"": {riskScore.AcutePoints},
    ""complexity_factors"": [""factor1""],
    ""complexity_points"": {riskScore.ComplexityPoints},
    ""protective_factors"": [""factor1""],
    ""protective_points"": {riskScore.ProtectivePoints},
    ""total_score"": {riskScore.TotalScore},
    ""risk_level"": ""{riskScore.RiskLevelString}"",
    ""rationale"": ""{riskScore.Rationale}""
  }},
  ""summary_article"": ""Full markdown Arabic content 800-1000 words"",
  ""article_outlines"": [
    {{
      ""article_id"": ""coverage_code_001"",
      ""title_ar"": ""Arabic title"",
      ""title_en"": ""English title"",
      ""coverage_codes"": [""code1""],
      ""priority"": ""CRITICAL|HIGH|MEDIUM|REFERENCE"",
      ""estimated_word_count"": ""XXX-XXX"",
      ""key_topics"": [""topic1"", ""topic2"", ""topic3""],
      ""rationale"": ""Why this article is needed for THIS patient""
    }}
  ],
  ""metadata"": {{
    ""total_articles"": 0,
    ""generation_timestamp"": ""ISO-8601 timestamp"",
    ""ramadan_period"": false
  }}
}}
```
";
    }

    /// <summary>
    /// Stage 2 user prompt. Uses "content_ar" in the JSON schema,
    /// matching Stage2Output.ContentAr model property.
    /// </summary>
    public string BuildStage2UserPrompt(
        PatientData patient,
        ArticleSpec articleSpec,
        RiskScore riskScore)
    {
        var coverageCodesJson = string.Join(", ",
            articleSpec.CoverageCodes.Select(c => $"\"{c}\""));

        return $@"
# PATIENT PROFILE

**Primary Diagnosis**: {patient.PrimaryDiagnosis}
**Risk Level**: {riskScore.RiskLevelString}
**Age Group**: {patient.AgeGroup}
**Comorbidities**: {patient.Comorbidities}
**Current Medications**: {patient.CurrentMedications}
**Allergies**: {patient.Allergies}
**Medical Restrictions**: {patient.MedicalRestrictions}

# ARTICLE SPECIFICATION

**Article ID**: {articleSpec.ArticleId}
**Title (Arabic)**: {articleSpec.TitleAr}
**Title (English)**: {articleSpec.TitleEn}
**Coverage Code(s)**: {string.Join(", ", articleSpec.CoverageCodes)}
**Priority**: {articleSpec.Priority}
**Target Word Count**: {articleSpec.EstimatedWordCount}
**Key Topics**:
{string.Join("\n", articleSpec.KeyTopics.Select(t => $"- {t}"))}
**Rationale**: {articleSpec.Rationale}

# TASK

Generate the complete Arabic article using the flexible section structure (Required: 1, 4, 7 | Optional: 2, 3, 5, 6).
Calibrate tone to Risk Level {riskScore.RiskLevelString}.
Stay within assigned coverage code(s).
Provide 2-3 credible sources with URLs (2020+).
Apply Yemen cultural context throughout.

# OUTPUT FORMAT

Return ONLY valid JSON matching this exact schema. No text before or after.

```json
{{
  ""article"": {{
    ""article_id"": ""{articleSpec.ArticleId}"",
    ""title_ar"": ""Arabic title"",
    ""title_en"": ""English title"",
    ""coverage_codes"": [{coverageCodesJson}],
    ""priority"": ""{articleSpec.Priority}"",
    ""risk_level"": ""{riskScore.RiskLevelString}"",
    ""content_ar"": ""Full markdown article content in Arabic"",
    ""word_count"": 0,
    ""sections_included"": [1, 4, 7],
    ""sources"": [
      {{
        ""title"": ""Source name and article title"",
        ""url"": ""https://..."",
        ""year"": 2024
      }}
    ]
  }},
  ""metadata"": {{
    ""generation_timestamp"": ""ISO-8601 timestamp"",
    ""risk_level"": ""{riskScore.RiskLevelString}"",
    ""patient_age_group"": ""{patient.AgeGroup}"",
    ""ramadan_period"": false,
    ""yemen_context_applied"": true
  }}
}}
```
";
    }
}
