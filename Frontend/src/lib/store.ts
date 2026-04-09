import { create } from "zustand";
import type { LoginResponse, PhysicianProfile } from "@/types";

interface AuthState {
  token:      string | null;
  physician:  PhysicianProfile | null;
  isLoggedIn: boolean;
  role:       string | null;
  userId:     string | null;
  tenantId:   string | null;

  login:  (response: LoginResponse) => void;
  logout: () => void;
  setPhysician: (p: PhysicianProfile) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token:      typeof window !== "undefined" ? localStorage.getItem("muafa_token")    : null,
  physician:  typeof window !== "undefined" ? JSON.parse(localStorage.getItem("muafa_user") ?? "null") : null,
  isLoggedIn: typeof window !== "undefined" ? !!localStorage.getItem("muafa_token") : false,
  role:       typeof window !== "undefined" ? localStorage.getItem("muafa_role")     : null,
  userId:     typeof window !== "undefined" ? localStorage.getItem("muafa_userid")   : null,
  tenantId:   typeof window !== "undefined" ? localStorage.getItem("muafa_tenantid") : null,

  login: (response) => {
    localStorage.setItem("muafa_token",    response.token);
    localStorage.setItem("muafa_role",     response.role);
    localStorage.setItem("muafa_userid",   response.userId);
    localStorage.setItem("muafa_tenantid", response.tenantId ?? "");
    localStorage.setItem("muafa_user", JSON.stringify({
      physicianId:  response.physicianId,
      fullName:     response.fullName,
      specialty:    response.specialty,
      institution:  response.institution,
      city:         null,
      totalSessions: 0,
    }));
    set({
      token:      response.token,
      physician:  {
        physicianId:  response.physicianId,
        fullName:     response.fullName,
        specialty:    response.specialty,
        institution:  response.institution,
        city:         null,
        totalSessions: 0,
      },
      isLoggedIn: true,
      role:       response.role,
      userId:     response.userId,
      tenantId:   response.tenantId ?? null,
    });
  },

  logout: () => {
    localStorage.removeItem("muafa_token");
    localStorage.removeItem("muafa_user");
    localStorage.removeItem("muafa_role");
    localStorage.removeItem("muafa_userid");
    localStorage.removeItem("muafa_tenantid");
    set({ token: null, physician: null, isLoggedIn: false, role: null, userId: null, tenantId: null });
  },

  setPhysician: (p) => set({ physician: p }),
}));
