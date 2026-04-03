import axios, { AxiosInstance } from "axios";
import type {
  ApiResponse, LoginRequest, LoginResponse, ChangePasswordRequest, PatientData,
  PhysicianProfile, SessionSummary, SessionDetail, SessionStatus, WorkflowResult,
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
