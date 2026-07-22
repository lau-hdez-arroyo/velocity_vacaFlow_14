'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiGet } from '@/lib/api-client';
import type { CurrentUser } from '@/lib/types';
import { colors } from '@/lib/theme';

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    apiGet<CurrentUser>('/api/auth/me')
      .then((user) => router.replace(user.role === 'Manager' ? '/team' : '/requests'))
      .catch(() => router.replace('/login'));
  }, [router]);

  return <div style={{ padding: 48, color: colors.inkMuted, fontSize: 14 }}>Loading…</div>;
}
