import { create } from "zustand";
import type { LoginResponse, PhysicianProfile } from "@/types";

interface AuthState {
  token:      string | null;
  physician:  PhysicianProfile | null;
  isLoggedIn: boolean;

  login:  (response: LoginResponse) => void;
  logout: () => void;
  setPhysician: (p: PhysicianProfile) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token:      typeof window !== "undefined" ? localStorage.getItem("muafa_token")   : null,
  physician:  typeof window !== "undefined" ? JSON.parse(localStorage.getItem("muafa_user") ?? "null") : null,
  isLoggedIn: typeof window !== "undefined" ? !!localStorage.getItem("muafa_token") : false,

  login: (response) => {
    localStorage.setItem("muafa_token", response.token);
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
    });
  },

  logout: () => {
    localStorage.removeItem("muafa_token");
    localStorage.removeItem("muafa_user");
    set({ token: null, physician: null, isLoggedIn: false });
  },

  setPhysician: (p) => set({ physician: p }),
}));
