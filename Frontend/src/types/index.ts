// ── Auth ──────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email:    string;
  password: string;
}

export interface LoginResponse {
  token:       string;
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

export interface TenantResponse {
  tenantId:    string;
  name:        string;
  slug:        string;
  country:     string | null;
  city:        string | null;
  isActive:    boolean;
  createdAt:   string;
}

export interface CreateTenantRequest {
  name:     string;
  slug:     string;
  country?: string;
  city?:    string;
}

export interface GenerateInvitationCodeRequest {
  role:       string;
  tenantId?:  string;
  expiresAt?: string;
  maxUses?:   number;
}
