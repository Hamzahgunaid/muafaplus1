import { create } from "zustand";
import type { LoginResponse, PhysicianProfile } from "@/types";

interface AuthState {
  token:      string | null;
  physician:  PhysicianProfile | null;
  isLoggedIn: boolean;
  role:       string | null;
  userId:     string | null;
  fullName:   string | null;
  tenantId:   string | null;
  hydrated:   boolean;

  login:        (response: LoginResponse) => void;
  logout:       () => void;
  setPhysician: (p: PhysicianProfile) => void;
  hydrate:      () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  // All initial values are SSR-safe — no localStorage reads at module load time
  token:      null,
  physician:  null,
  isLoggedIn: false,
  role:       null,
  userId:     null,
  fullName:   null,
  tenantId:   null,
  hydrated:   false,

  // Called once on client mount to load persisted state
  hydrate: () => {
    if (typeof window === "undefined") return;
    const token = localStorage.getItem("muafa_token");
    set({
      token,
      physician:  JSON.parse(localStorage.getItem("muafa_user") ?? "null"),
      isLoggedIn: !!token,
      role:       localStorage.getItem("muafa_role"),
      userId:     localStorage.getItem("muafa_userid"),
      fullName:   localStorage.getItem("muafa_fullname"),
      tenantId:   localStorage.getItem("muafa_tenantid"),
      hydrated:   true,
    });
  },

  login: (response) => {
    localStorage.setItem("muafa_token",    response.token);
    localStorage.setItem("muafa_role",     response.role);
    localStorage.setItem("muafa_userid",   response.userId);
    localStorage.setItem("muafa_fullname", response.fullName);
    localStorage.setItem("muafa_tenantid", response.tenantId ?? "");
    localStorage.setItem("muafa_user", JSON.stringify({
      physicianId:   response.physicianId,
      fullName:      response.fullName,
      specialty:     response.specialty,
      institution:   response.institution,
      city:          null,
      totalSessions: 0,
    }));
    set({
      token:      response.token,
      physician:  {
        physicianId:   response.physicianId,
        fullName:      response.fullName,
        specialty:     response.specialty,
        institution:   response.institution,
        city:          null,
        totalSessions: 0,
      },
      isLoggedIn: true,
      role:       response.role,
      userId:     response.userId,
      fullName:   response.fullName,
      tenantId:   response.tenantId ?? null,
      hydrated:   true,
    });
  },

  logout: () => {
    localStorage.removeItem("muafa_token");
    localStorage.removeItem("muafa_user");
    localStorage.removeItem("muafa_role");
    localStorage.removeItem("muafa_userid");
    localStorage.removeItem("muafa_fullname");
    localStorage.removeItem("muafa_tenantid");
    set({
      token: null, physician: null, isLoggedIn: false,
      role: null, userId: null, fullName: null, tenantId: null,
    });
  },

  setPhysician: (p) => set({ physician: p }),
}));
