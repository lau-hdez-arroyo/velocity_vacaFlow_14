'use client';

import { useEffect, useState, type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { apiGet, ApiError } from '@/lib/api-client';
import type { CurrentUser } from '@/lib/types';
import { colors } from '@/lib/theme';
import { HeaderBar } from './HeaderBar';

/**
 * Auth guard + chrome for every authenticated page. Resolves the current user from the session
 * (redirecting to /login on 401), then renders the header and the page content (as a function of
 * the resolved user).
 */
export function AppShell({
  navLabel,
  children,
}: {
  navLabel: string;
  children: (user: CurrentUser) => ReactNode;
}) {
  const router = useRouter();
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [state, setState] = useState<'loading' | 'ready'>('loading');

  useEffect(() => {
    let active = true;
    apiGet<CurrentUser>('/api/auth/me')
      .then((u) => {
        if (!active) return;
        setUser(u);
        setState('ready');
      })
      .catch((error) => {
        if (error instanceof ApiError && error.status === 401) router.replace('/login');
        else router.replace('/login');
      });
    return () => {
      active = false;
    };
  }, [router]);

  if (state !== 'ready' || !user) {
    return <div style={{ padding: 48, color: colors.inkMuted, fontSize: 14 }}>Loading…</div>;
  }

  return (
    <div>
      <HeaderBar user={user} navLabel={navLabel} />
      {children(user)}
    </div>
  );
}
