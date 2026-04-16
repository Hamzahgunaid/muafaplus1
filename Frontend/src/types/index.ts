// ── Auth ──────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email:    string;
  password: string;
}

export interface LoginResponse {
  token:       string;
  userId:      string;
  role:        string;
  tenantId:    string | null;
  physicianId: string;
  fullName:    string;
  specialty:   string;
  institution: string | null;
  expiresAt:   string;
  mustResetOnNextLogin: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword:     string;
}

export interface PhysicianProfile {
  physicianId:  string;
  fullName:     string;
  specialty:    string;
  institution:  string | null;
  city:         string | null;
  totalSessions: number;
}

// ── Patient ───────────────────────────────────────────────────────────────────

export type AgeGroup =
  | "Infant" | "Toddler" | "Child"
  | "Adolescent" | "Adult" | "Elderly";

export interface PatientData {
  primaryDiagnosis:   string;
  ageGroup:           AgeGroup;
  comorbidities:      string;
  currentMedications: string;
  allergies:          string;
  medicalRestrictions: string;
}

// ── Risk ──────────────────────────────────────────────────────────────────────

export type RiskLevel = "LOW" | "MODERATE" | "HIGH" | "CRITICAL";

export interface RiskScore {
  acuteFactors:      string[];
  acutePoints:       number;
  complexityFactors: string[];
  complexityPoints:  number;
  protectiveFactors: string[];
  protectivePoints:  number;
  totalScore:        number;
  riskLevel:         RiskLevel;
  rationale:         string;
}

// ── Articles ──────────────────────────────────────────────────────────────────

export interface ArticleContent {
  articleId:        string;
  titleAr:          string;
  titleEn:          string;
  coverageCodes:    string[];
  priority:         string;
  riskLevel:        string;
  contentAr:        string;
  wordCount:        number;
  sectionsIncluded: number[];
  sources:          { title: string; url: string; year: number }[];
}

// ── Sessions ──────────────────────────────────────────────────────────────────

export interface SessionSummary {
  sessionId:     string;
  patientId:     string;
  status:        SessionStatus["status"];
  riskLevel:     RiskLevel | null;
  totalArticles: number | null;
  totalCost:     number | null;
  startedAt:     string;
  completedAt:   string | null;
}

// ── Workflow result ───────────────────────────────────────────────────────────

export interface WorkflowResult {
  sessionId:        string;
  patientId:        string;
  success:          boolean;
  cSharpRiskScore:  RiskScore | null;
  summaryArticle:   string | null;
  riskAssessment:   RiskScore | null;
  detailedArticles: ArticleContent[];
  stage1Cost:       number;
  stage2Cost:       number;
  totalCost:        number;
  errorMessage:     string | null;
}

// ── API wrapper ───────────────────────────────────────────────────────────────

export interface ApiResponse<T> {
  success:  boolean;
  data:     T | null;
  error:    string | null;
  errorType: string | null;
  metadata: Record<string, unknown> | null;
}

// ── Session detail (from GET /Session/{id}) ───────────────────────────────────

export interface ArticleRecord {
  articleId:    string;
  articleType:  "summary" | "detailed";
  coverageCodes: string;
  content:      string;
  wordCount:    number;
  costUsd:      number;
  createdAt:    string;
}

export interface SessionDetail {
  sessionId:     string;
  patientId:     string;
  physicianId:   string;
  stage:         string;
  status:        SessionStatus["status"];
  riskLevel:     RiskLevel | null;
  totalArticles: number | null;
  totalCost:     number | null;
  startedAt:     string;
  completedAt:   string | null;
  errorMessage:  string | null;
  articles:      ArticleRecord[];
}

export interface SessionStatus {
  sessionId:     string;
  status:        "pending" | "in_progress" | "complete" | "failed";
  riskLevel:     RiskLevel | null;
  totalArticles: number | null;
  totalCost:     number | null;
  completedAt:   string | null;
  errorMessage:  string | null;
}

// ── Referrals ─────────────────────────────────────────────────────────────────

export interface CreateReferralRequest {
  patientPhone:             string;
  patientNameOverride?:     string;
  primaryDiagnosis:         string;
  ageGroup:                 AgeGroup;
  comorbidities?:           string;
  currentMedications?:      string;
  allergies?:               string;
  medicalRestrictions?:     string;
  notes?:                   string;
  notificationDelayHours?:  number;
  whatsAppDelivery?:        boolean;
}

export interface ReferralResponse {
  referralId:           string;
  referralCode:         string;
  patientPhone:         string;
  patientName:          string | null;
  status:               string;
  riskLevel:            string | null;
  sessionId:            string | null;
  notes:                string | null;
  createdAt:            string;
  updatedAt:            string;
  scheduledDeliveryAt?: string | null;
  deliveredAt?:         string | null;
  chatEnabled?:         boolean;
}

export interface ReferralArticleResponse {
  articleId:     string;
  articleType:   string;
  content_ar:    string;
  coverageCodes: string | null;
  wordCount:     number;
  createdAt:     string;
}

// ── Referral Engagement ───────────────────────────────────────────────────────

export interface ReferralEngagementResponse {
  referralId:           string;
  messageSentAt:        string | null;
  appOpenedAt:          string | null;
  summaryViewedAt:      string | null;
  stage2RequestedAt:    string | null;
  feedbackSubmittedAt:  string | null;
}

// ── Stage1Output — matches Backend/Models/ArticleModels.cs Stage1Output ──────
// JSON keys match [JsonPropertyName] attributes exactly.
// Fields without [JsonPropertyName] serialize as PascalCase (C# default).

export interface ArticleOutline {
  ArticleId:          string;
  TitleAr:            string;
  TitleEn:            string;
  CoverageCodes:      string[];
  Priority:           string;
  EstimatedWordCount: string;
  KeyTopics:          string[];
  Rationale:          string;
}

export interface Stage1Output {
  risk_assessment: {
    AcuteFactors:      string[];
    AcutePoints:       number;
    ComplexityFactors: string[];
    ComplexityPoints:  number;
    ProtectiveFactors: string[];
    ProtectivePoints:  number;
    TotalScore:        number;
    RiskLevel:         string;
    Rationale:         string;
  };
  summary_article:  string;
  article_outlines: ArticleOutline[];
  metadata: {
    total_articles:       number;
    generation_timestamp: string;
    ramadan_period:       boolean;
  };
}

// ── Test Scenarios ────────────────────────────────────────────────────────────

export interface CreateTestScenarioRequest {
  primaryDiagnosis:    string;
  ageGroup:            AgeGroup;
  comorbidities?:      string;
  currentMedications?: string;
  allergies?:          string;
  medicalRestrictions?: string;
}

export interface ContentEvaluationResponse {
  evaluationId:          string;
  scenarioId:            string;
  physicianId:           string;
  accuracyRating:        number;
  clarityRating:         number;
  relevanceRating:       number;
  completenessRating:    number;
  isAppropriate:         boolean;
  isCulturallySensitive: boolean;
  isArabicQuality:       boolean;
  whatWorked:            string | null;
  needsImprovement:      string | null;
  comments:              string | null;
  submittedAt:           string;
}

export interface TestScenarioResponse {
  scenarioId:           string;
  physicianId:          string;
  tenantId:             string;
  status:               string;
  patientDataJson:      string;
  generatedContentJson: string | null;
  createdAt:            string;
  evaluation:           ContentEvaluationResponse | null;
}

export interface SubmitEvaluationRequest {
  accuracyRating:        number;
  clarityRating:         number;
  relevanceRating:       number;
  completenessRating:    number;
  isAppropriate:         boolean;
  isCulturallySensitive: boolean;
  isArabicQuality:       boolean;
  whatWorked?:           string;
  needsImprovement?:     string;
  comments?:             string;
}

// ── Chat ──────────────────────────────────────────────────────────────────────

export interface ChatMessageResponse {
  messageId:  string;
  senderRole: "Physician" | "Patient";
  content:    string;
  sentAt:     string;
  isRead:     boolean;
}

export interface ChatThreadResponse {
  threadId:      string;
  referralId:    string;
  isEnabled:     boolean;
  expiresAt:     string;
  messageCount:  number;
  createdAt:     string;
  messages:      ChatMessageResponse[];
  disclaimerAr:  string;
  disclaimerEn:  string;
}

// ── Tenants ───────────────────────────────────────────────────────────────────

export interface TenantSubscriptionSummary {
  planType:         string;
  casesAllocated:   number;
  casesUsed:        number;
  usagePercentage:  number;
  billingCycleEnd:  string;
}

export interface TenantResponse {
  tenantId:            string;
  name:                string;
  nameAr?:             string | null;
  slug:                string;
  country:             string | null;
  city:                string | null;
  isActive:            boolean;
  createdAt:           string;
  activeSubscription?: TenantSubscriptionSummary | null;
}

export interface CreateTenantRequest {
  name:             string;
  nameAr?:          string;
  slug?:            string;
  country?:         string;
  city?:            string;
  adminEmail?:      string;
  planType?:        string;
  casesAllocated?:  number;
}

export interface GenerateInvitationCodeRequest {
  role:       string;
  tenantId?:  string;
  expiresAt?: string;
  maxUses?:   number;
}

// ── Users ─────────────────────────────────────────────────────────────────────

export interface UserResponse {
  userId:    string;
  email:     string;
  fullName:  string;
  role:      string;
  isActive:  boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email:    string;
  fullName: string;
  role:     string;
  tenantId: string;
}

// ── Tenant Settings ───────────────────────────────────────────────────────────

export interface TenantSettingsResponse {
  tenantId:               string;
  patientNamePolicy:      string;
  whatsAppEnabled:        boolean;
  chatEnabled:            boolean;
  notificationDelayHours: number;
}

export interface UpdateTenantSettingsRequest {
  patientNamePolicy?:      string;
  whatsAppEnabled?:        boolean;
  chatEnabled?:            boolean;
  notificationDelayHours?: number;
}

// ── Assistant Links ───────────────────────────────────────────────────────────

export interface AssistantLinkResponse {
  linkId:        string;
  assistantId:   string;
  assistantName: string;
  physicianId:   string;
  physicianName: string;
  createdAt:     string;
}

export interface CreateAssistantLinkRequest {
  assistantId: string;
  physicianId: string;
}
