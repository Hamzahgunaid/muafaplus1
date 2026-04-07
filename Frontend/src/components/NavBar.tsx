"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/lib/store";

const NAV_LINKS = [
  { label: "الرئيسية",              href: "/dashboard" },
  { label: "الإحالات",              href: "/referrals" },
  { label: "سيناريوهات الاختبار",  href: "/test-scenarios" },
  { label: "الإدارة",               href: "/admin" },
];

export default function NavBar() {
  const pathname = usePathname();
  const router   = useRouter();
  const { physician, logout } = useAuthStore();

  const handleLogout = () => { logout(); router.push("/login"); };

  return (
    <header className="bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
      {/* Brand + nav links */}
      <div className="flex items-center gap-6">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-brand-600 flex items-center justify-center text-white font-bold text-sm">
            م+
          </div>
          {physician && (
            <div>
              <span className="font-semibold text-gray-900 text-sm">{physician.fullName}</span>
              <span className="text-gray-400 text-xs block">{physician.specialty}</span>
            </div>
          )}
        </div>

        <nav className="flex items-center gap-1">
          {NAV_LINKS.map(({ label, href }) => {
            const active = pathname === href || pathname.startsWith(href + "/");
            return (
              <Link
                key={href}
                href={href}
                className={`px-3 py-1.5 rounded-lg text-sm transition ${
                  active
                    ? "bg-brand-50 text-brand-700 font-medium"
                    : "text-gray-500 hover:text-gray-800 hover:bg-gray-50"
                }`}
              >
                {label}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-4">
        <Link
          href="/generate"
          className="px-4 py-2 rounded-xl bg-brand-600 text-white text-sm font-medium hover:bg-brand-800 transition"
        >
          + مريض جديد
        </Link>
        <button
          onClick={handleLogout}
          className="text-gray-400 hover:text-gray-700 text-sm transition"
        >
          خروج
        </button>
      </div>
    </header>
  );
}
