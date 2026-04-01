"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/store";

export default function RootPage() {
  const router    = useRouter();
  const isLoggedIn = useAuthStore((s) => s.isLoggedIn);

  useEffect(() => {
    router.replace(isLoggedIn ? "/dashboard" : "/login");
  }, [isLoggedIn, router]);

  return null;
}
