'use client';

import { useRouter } from 'next/navigation';
import { apiPost } from '@/lib/api-client';
import { colors, font, initials } from '@/lib/theme';

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export function HeaderBar({ user, navLabel }: { user: CurrentUser; navLabel: string }) {
  const router = useRouter();

  async function handleLogout() {
    try {
      await apiPost('/api/auth/logout', {});
    } catch {
      /* ignore — redirect regardless */
    }
    router.push('/login');
  }

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '18px 32px',
        background: colors.bgHeader,
        borderBottom: `1px solid ${colors.hairline}`,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 28 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
          <div
            style={{
              width: 26,
              height: 26,
              borderRadius: 6,
              background: colors.accent,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: colors.paper,
              fontWeight: 700,
              fontSize: 12,
            }}
          >
            V
          </div>
          <div style={{ fontSize: 17, fontWeight: 700, letterSpacing: -0.2 }}>VacaFlow</div>
        </div>
        <div
          style={{
            fontSize: 13,
            fontWeight: 700,
            color: colors.ink,
            borderBottom: `2px solid ${colors.accent}`,
            paddingBottom: 4,
          }}
        >
          {navLabel}
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
          <div
            style={{
              width: 28,
              height: 28,
              borderRadius: '50%',
              background: colors.accent,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 12,
              fontWeight: 700,
              color: colors.paper,
            }}
          >
            {initials(user.fullName)}
          </div>
          <div>
            <div style={{ fontSize: 13, fontWeight: 700 }}>{user.fullName}</div>
            <div style={{ fontSize: 11, color: colors.inkMuted }}>{user.role}</div>
          </div>
        </div>
        <button
          onClick={handleLogout}
          style={{
            background: 'none',
            border: `1px solid ${colors.hairline}`,
            color: colors.inkMuted,
            borderRadius: 6,
            padding: '7px 12px',
            fontSize: 12,
            fontWeight: 600,
            cursor: 'pointer',
            fontFamily: font,
          }}
        >
          Log out
        </button>
      </div>
    </div>
  );
}
