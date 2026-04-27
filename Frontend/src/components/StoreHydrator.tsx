'use client'
import { useEffect } from 'react'
import { useAuthStore } from '@/lib/store'

export default function StoreHydrator() {
  const hydrate = useAuthStore((s) => s.hydrate)
  useEffect(() => { hydrate() }, [hydrate])
  return null
}
