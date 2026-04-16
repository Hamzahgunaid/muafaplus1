import axios, { AxiosInstance } from "axios";
import type {
  ApiResponse, LoginRequest, LoginResponse, ChangePasswordRequest, PatientData,
  PhysicianProfile, SessionSummary, SessionDetail, SessionStatus, WorkflowResult,
  CreateReferralRequest, ReferralResponse, ReferralEngagementResponse, ReferralArticleResponse,
  CreateTestScenarioRequest, TestScenarioResponse, SubmitEvaluationRequest, ContentEvaluationResponse,
  ChatThreadResponse, ChatMessageResponse,
  TenantResponse, TenantSubscriptionSummary, CreateTenantRequest, GenerateInvitationCodeRequest,
  UserResponse, CreateUserRequest,
  TenantSettingsResponse, UpdateTenantSettingsRequest,
  AssistantLinkResponse, CreateAssistantLinkRequest,
} from "@/types";

// Set NEXT_PUBLIC_API_URL in .env.local (dev) or Vercel environment variables (production).
const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:5001";

// ── Axios instance ────────────────────────────────────────────────────────────

const http: AxiosInstance = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: { "Content-Type": "application/json" },
});

// Attach JWT from localStorage on every request
http.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("muafa_token");
    if (token) config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Redirect to login on 401
http.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && typeof window !== "undefined") {
      localStorage.removeItem("muafa_token");
      localStorage.removeItem("muafa_user");
      window.location.href = "/login";
    }
    return Promise.reject(err);
  }
);

// ── Auth ──────────────────────────────────────────────────────────────────────

export const authApi = {
  login: async (req: LoginRequest): Promise<ApiResponse<LoginResponse>> => {
    const { data } = await http.post<ApiResponse<LoginResponse>>("/auth/login", req);
    return data;
  },

  me: async (): Promise<ApiResponse<PhysicianProfile>> => {
    const { data } = await http.get<ApiResponse<PhysicianProfile>>("/auth/me");
    return data;
  },

  changePassword: async (req: ChangePasswordRequest): Promise<ApiResponse<object>> => {
    const { data } = await http.post<ApiResponse<object>>("/auth/change-password", req);
    return data;
  },
};

// ── Physicians ────────────────────────────────────────────────────────────────

export const physicianApi = {
  getSessions: async (
    physicianId: string,
    page = 1,
    pageSize = 20
  ): Promise<ApiResponse<SessionSummary[]>> => {
    const { data } = await http.get<ApiResponse<SessionSummary[]>>(
      `/Physician/${physicianId}/sessions`,
      { params: { page, pageSize } }
    );
    return data;
  },
};

// ── Content generation ────────────────────────────────────────────────────────

export const contentApi = {
  generateComplete: async (
    physicianId: string,
    patientData: PatientData
  ): Promise<ApiResponse<WorkflowResult>> => {
    const { data } = await http.post<ApiResponse<WorkflowResult>>(
      "/ContentGeneration/generate/complete",
      { physicianId, patientData }
    );
    return data;
  },

  generateStage1: async (
    physicianId: string,
    patientData: PatientData
  ): Promise<ApiResponse<unknown>> => {
    const { data } = await http.post<ApiResponse<unknown>>(
      "/ContentGeneration/generate/stage1",
      { physicianId, patientData }
    );
    return data;
  },

  health: async (): Promise<{ status: string }> => {
    const { data } = await http.get<{ status: string }>("/ContentGeneration/health");
    return data;
  },
};

// ── Sessions ──────────────────────────────────────────────────────────────────

export const sessionApi = {
  getById: async (sessionId: string): Promise<ApiResponse<SessionDetail>> => {
    const { data } = await http.get<ApiResponse<SessionDetail>>(`/Session/${sessionId}`);
    return data;
  },

  getStatus: async (sessionId: string): Promise<ApiResponse<SessionStatus>> => {
    const { data } = await http.get<ApiResponse<SessionStatus>>(`/Session/${sessionId}/status`);
    return data;
  },

  getExportUrl: (sessionId: string, format: "pdf" | "docx") =>
    `${BASE_URL}/api/v1/sessions/${sessionId}/export?format=${format}`,
};

// ── Referrals ─────────────────────────────────────────────────────────────────

export const referralApi = {
  createReferral: async (req: CreateReferralRequest): Promise<ApiResponse<ReferralResponse>> => {
    const { data } = await http.post<ApiResponse<ReferralResponse>>("/referrals", req);
    return data;
  },

  getReferrals: async (): Promise<ApiResponse<ReferralResponse[]>> => {
    const { data } = await http.get<ApiResponse<ReferralResponse[]>>("/referrals");
    return data;
  },

  getReferral: async (id: string): Promise<ApiResponse<ReferralResponse>> => {
    const { data } = await http.get<ApiResponse<ReferralResponse>>(`/referrals/${id}`);
    return data;
  },

  getEngagement: async (id: string): Promise<ApiResponse<ReferralEngagementResponse>> => {
    const { data } = await http.get<ApiResponse<ReferralEngagementResponse>>(`/referrals/${id}/engagement`);
    return data;
  },

  getArticles: async (id: string): Promise<ApiResponse<ReferralArticleResponse[]>> => {
    const { data } = await http.get<ApiResponse<ReferralArticleResponse[]>>(`/referrals/${id}/articles`);
    return data;
  },
};

// ── Test Scenarios ────────────────────────────────────────────────────────────

export const testScenarioApi = {
  createTestScenario: async (req: CreateTestScenarioRequest): Promise<ApiResponse<TestScenarioResponse>> => {
    const { data } = await http.post<ApiResponse<TestScenarioResponse>>("/test-scenarios", req);
    return data;
  },

  getTestScenarios: async (): Promise<ApiResponse<TestScenarioResponse[]>> => {
    const { data } = await http.get<ApiResponse<TestScenarioResponse[]>>("/test-scenarios");
    return data;
  },

  getTestScenario: async (id: string): Promise<ApiResponse<TestScenarioResponse>> => {
    const { data } = await http.get<ApiResponse<TestScenarioResponse>>(`/test-scenarios/${id}`);
    return data;
  },

  submitEvaluation: async (
    scenarioId: string,
    req: SubmitEvaluationRequest
  ): Promise<ApiResponse<ContentEvaluationResponse>> => {
    const { data } = await http.post<ApiResponse<ContentEvaluationResponse>>(
      `/test-scenarios/${scenarioId}/evaluation`,
      req
    );
    return data;
  },

  generateScenarioArticle: async (id: string, index: number): Promise<string> => {
    const { data } = await http.post<ApiResponse<{ content: string }>>(
      `/test-scenarios/${id}/generate-article?index=${index}`
    );
    if (!data.success || !data.data?.content) throw new Error(data.error ?? "Generation failed");
    return data.data.content;
  },
};

// ── Chat ──────────────────────────────────────────────────────────────────────

export const chatApi = {
  getChatThread: async (referralId: string): Promise<ApiResponse<ChatThreadResponse>> => {
    const { data } = await http.get<ApiResponse<ChatThreadResponse>>(`/referrals/${referralId}/chat`);
    return data;
  },

  sendChatMessage: async (
    referralId: string,
    content: string
  ): Promise<ApiResponse<ChatMessageResponse>> => {
    const { data } = await http.post<ApiResponse<ChatMessageResponse>>(
      `/referrals/${referralId}/chat/messages`,
      { content }
    );
    return data;
  },

  updateSettings: async (
    physicianId: string,
    chatEnabled: boolean
  ): Promise<ApiResponse<object>> => {
    const { data } = await http.put<ApiResponse<object>>(
      `/physicians/${physicianId}/chat-settings`,
      { chatEnabled }
    );
    return data;
  },
};

// ── Tenants ───────────────────────────────────────────────────────────────────

export const tenantApi = {
  getTenants: async (): Promise<ApiResponse<TenantResponse[]>> => {
    const { data } = await http.get<ApiResponse<TenantResponse[]>>("/tenants");
    return data;
  },

  getTenant: async (tenantId: string): Promise<ApiResponse<TenantResponse>> => {
    const { data } = await http.get<ApiResponse<TenantResponse>>(`/tenants/${tenantId}`);
    return data;
  },

  createTenant: async (req: CreateTenantRequest): Promise<ApiResponse<TenantResponse>> => {
    const { data } = await http.post<ApiResponse<TenantResponse>>("/tenants", req);
    return data;
  },

  generateInvitationCode: async (
    req: GenerateInvitationCodeRequest
  ): Promise<ApiResponse<{ code: string; expiresAt: string }>> => {
    const { data } = await http.post<ApiResponse<{ code: string; expiresAt: string }>>(
      "/auth/invitation-codes/generate",
      req
    );
    return data;
  },
};

// ── Users ─────────────────────────────────────────────────────────────────────

export const userApi = {
  getUsersByTenant: async (tenantId: string): Promise<ApiResponse<UserResponse[]>> => {
    const { data } = await http.get<ApiResponse<UserResponse[]>>(`/tenants/${tenantId}/users`);
    return data;
  },

  createUser: async (req: CreateUserRequest): Promise<ApiResponse<UserResponse>> => {
    const { data } = await http.post<ApiResponse<UserResponse>>(`/tenants/${req.tenantId}/users`, req);
    return data;
  },
};

// ── Tenant Settings ───────────────────────────────────────────────────────────

export const tenantSettingsApi = {
  getSettings: async (tenantId: string): Promise<ApiResponse<TenantSettingsResponse>> => {
    const { data } = await http.get<ApiResponse<TenantSettingsResponse>>(`/tenants/${tenantId}/settings`);
    return data;
  },

  updateSettings: async (
    tenantId: string,
    req: UpdateTenantSettingsRequest
  ): Promise<ApiResponse<TenantSettingsResponse>> => {
    const { data } = await http.put<ApiResponse<TenantSettingsResponse>>(`/tenants/${tenantId}/settings`, req);
    return data;
  },
};

// ── Assistant Links ───────────────────────────────────────────────────────────

export const assistantLinkApi = {
  getLinks: async (tenantId: string): Promise<ApiResponse<AssistantLinkResponse[]>> => {
    const { data } = await http.get<ApiResponse<AssistantLinkResponse[]>>(`/tenants/${tenantId}/assistant-links`);
    return data;
  },

  createLink: async (
    tenantId: string,
    req: CreateAssistantLinkRequest
  ): Promise<ApiResponse<AssistantLinkResponse>> => {
    const { data } = await http.post<ApiResponse<AssistantLinkResponse>>(
      `/tenants/${tenantId}/assistant-links`,
      req
    );
    return data;
  },
};

// ── Subscription ──────────────────────────────────────────────────────────────

export const subscriptionApi = {
  getSubscription: async (tenantId: string): Promise<ApiResponse<TenantSubscriptionSummary>> => {
    const { data } = await http.get<ApiResponse<TenantSubscriptionSummary>>(`/tenants/${tenantId}/subscription`);
    return data;
  },
};

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Masks a phone number, keeping only the last 4 digits visible.
 *  "+967782705557" → "+967***5557" */
export function maskPhone(phone: string): string {
  if (phone.length <= 4) return phone;
  const last4 = phone.slice(-4);
  const prefix = phone.slice(0, phone.length - 7); // keep country code area
  return `${prefix}***${last4}`;
}

/** Returns Arabic relative time for a date string.
 *  e.g. "منذ 3 ساعات", "منذ يومين", "الآن" */
export function formatRelativeTime(dateString: string | null): string {
  if (!dateString) return "";
  const diff = Date.now() - new Date(dateString).getTime();
  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours   = Math.floor(minutes / 60);
  const days    = Math.floor(hours   / 24);
  const weeks   = Math.floor(days    / 7);
  const months  = Math.floor(days    / 30);

  if (seconds < 60)  return "الآن";
  if (minutes < 60)  return minutes === 1 ? "منذ دقيقة" : `منذ ${minutes} دقائق`;
  if (hours   < 24)  return hours   === 1 ? "منذ ساعة"  : `منذ ${hours} ساعات`;
  if (days    < 7)   return days    === 1 ? "منذ يوم"   : `منذ ${days} أيام`;
  if (weeks   < 4)   return weeks   === 1 ? "منذ أسبوع" : `منذ ${weeks} أسابيع`;
  return months      === 1 ? "منذ شهر"   : `منذ ${months} أشهر`;
}
