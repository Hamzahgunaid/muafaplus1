'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useAuthStore } from '@/lib/store'

type NavLink = { label: string; href: string }

function getNavLinks(role: string | null): NavLink[] {
  switch (role) {
    case 'SuperAdmin':
    case 'HospitalAdmin':
      return [
        { label: 'الرئيسية',            href: '/dashboard' },
        { label: 'الإحالات',            href: '/referrals' },
        { label: 'سيناريوهات الاختبار', href: '/test-scenarios' },
        { label: 'الإدارة',             href: '/admin' },
      ]
    case 'Physician':
      return [
        { label: 'الرئيسية',            href: '/dashboard' },
        { label: 'الإحالات',            href: '/referrals' },
        { label: 'سيناريوهات الاختبار', href: '/test-scenarios' },
      ]
    case 'Assistant':
      return [
        { label: 'الرئيسية', href: '/dashboard' },
        { label: 'الإحالات', href: '/referrals' },
      ]
    default:
      return [{ label: 'الرئيسية', href: '/dashboard' }]
  }
}

export default function NavBar() {
  const pathname = usePathname()
  const router   = useRouter()
  const { physician, fullName, role, logout } = useAuthStore()
  const [hydrated, setHydrated] = useState(false)

  useEffect(() => { setHydrated(true) }, [])

  const handleLogout = () => { logout(); router.push('/login') }

  if (!hydrated) return (
    <nav
      className="sticky top-0 z-50 bg-white border-b"
      style={{ borderColor: '#EEF0F5', height: '64px' }}
    />
  )

  const navLinks   = getNavLinks(role)
  const displayName = fullName ?? physician?.fullName ?? ''
  const displaySub  = role === 'Physician' || role === 'Assistant'
    ? (physician?.specialty ?? '')
    : role ?? ''

  const initials = displayName
    .split(' ')
    .map((n: string) => n[0])
    .join('')
    .slice(0, 2) || 'م'

  return (
    <nav
      className="sticky top-0 z-50 bg-white border-b"
      style={{ borderColor: '#EEF0F5', height: '64px', fontFamily: 'IBM Plex Sans Arabic, system-ui' }}
      dir="rtl"
    >
      <div className="max-w-7xl mx-auto px-6 h-full flex items-center justify-between">

        {/* Logo */}
        <div className="flex items-center">
          <img src="/muafa-logo.png" alt="معافى+" className="h-8 w-auto object-contain" />
        </div>

        {/* Nav links */}
        <div className="flex items-center gap-8">
          {navLinks.map(link => {
            const isActive = pathname === link.href || pathname.startsWith(link.href + '/')
            return (
              <Link
                key={link.href}
                href={link.href}
                className="text-sm font-medium pb-1 transition-colors"
                style={{
                  color: isActive ? '#1E3A72' : '#5A6478',
                  borderBottom: isActive ? '2px solid #1E3A72' : '2px solid transparent',
                  fontFamily: 'IBM Plex Sans Arabic, system-ui',
                }}
              >
                {link.label}
              </Link>
            )
          })}
        </div>

        {/* User area */}
        <div className="flex items-center gap-3">
          {displayName && (
            <div className="text-right">
              <div className="text-sm font-semibold" style={{ color: '#0E1726' }}>
                {displayName}
              </div>
              <div className="text-xs" style={{ color: '#5A6478' }}>
                {displaySub}
              </div>
            </div>
          )}
          <div
            className="w-9 h-9 rounded-full flex items-center justify-center text-white text-sm font-bold"
            style={{ background: 'linear-gradient(135deg, #1E3A72, #17305F)' }}
          >
            {initials}
          </div>
          {role !== null && (
            <Link
              href="/referrals/new"
              className="text-sm font-semibold px-4 py-2 rounded-xl text-white transition-all"
              style={{ background: '#1E3A72', fontFamily: 'IBM Plex Sans Arabic, system-ui' }}
            >
              + مريض جديد
            </Link>
          )}
          <button
            onClick={handleLogout}
            className="text-xs px-3 py-1.5 rounded-lg transition-colors"
            style={{
              color: '#5A6478',
              background: '#F6F7FB',
              border: '1px solid #EEF0F5',
              fontFamily: 'IBM Plex Sans Arabic, system-ui',
            }}
          >
            خروج
          </button>
        </div>
      </div>
    </nav>
  )
}
