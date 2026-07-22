'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiGet, apiPost } from '@/lib/api-client';
import type { RequestItem } from '@/lib/types';
import { AppShell } from '@/components/AppShell';
import { StatusBadge } from '@/components/StatusBadge';
import { btn, colors, font, initials } from '@/lib/theme';

const MONTHS = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
const fmt = (d: string) => {
  const [, m, day] = d.split('-').map(Number);
  return `${MONTHS[m - 1]} ${day}`;
};
const dayCount = (a: string, b: string) => Math.round((new Date(b).getTime() - new Date(a).getTime()) / 86_400_000) + 1;

export default function TeamPage() {
  return <AppShell navLabel="Team Requests">{() => <Team />}</AppShell>;
}

function Team() {
  const [items, setItems] = useState<RequestItem[]>([]);
  const [comments, setComments] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    const data = await apiGet<RequestItem[]>('/api/requests');
    setItems(data);
    setLoading(false);
  }, []);

  useEffect(() => {
    reload().catch(() => setLoading(false));
  }, [reload]);

  async function decide(id: string, action: 'approve' | 'reject') {
    setBusyId(id);
    try {
      await apiPost(`/api/requests/${id}/${action}`, { comment: comments[id] ?? '' });
      await reload();
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div style={{ maxWidth: 860, margin: '0 auto', padding: '48px 32px 80px' }}>
      <div style={{ marginBottom: 32 }}>
        <div style={{ fontSize: 30, fontWeight: 700, letterSpacing: -0.4 }}>Team Requests</div>
        <div style={{ fontSize: 13, color: colors.inkMuted, marginTop: 6, fontWeight: 500 }}>
          {items.length} request{items.length !== 1 ? 's' : ''} awaiting your decision
        </div>
      </div>

      {loading ? (
        <div style={{ color: colors.inkMuted, fontSize: 14 }}>Loading…</div>
      ) : items.length === 0 ? (
        <div style={{ color: colors.inkMuted, fontSize: 14 }}>No requests awaiting your decision.</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {items.map((r) => (
            <div key={r.id} style={{ background: colors.paper, border: `1px solid ${colors.paperHairline}`, borderRadius: 10, padding: '22px 24px', display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                  <div style={{ width: 36, height: 36, borderRadius: '50%', background: colors.accent, color: colors.paper, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, fontWeight: 700 }}>
                    {initials(r.employeeName)}
                  </div>
                  <div>
                    <div style={{ fontSize: 15, fontWeight: 700, color: colors.ink }}>{r.employeeName}</div>
                    <div style={{ fontSize: 12, color: colors.inkMuted, fontWeight: 500 }}>
                      {r.absenceType} · {fmt(r.startDate)} → {fmt(r.endDate)} · {dayCount(r.startDate, r.endDate)} day{dayCount(r.startDate, r.endDate) > 1 ? 's' : ''}
                    </div>
                  </div>
                </div>
                <StatusBadge status={r.status} />
              </div>

              {r.reason && (
                <div style={{ padding: '10px 12px', background: colors.paperShade, borderLeft: `3px solid ${colors.amber}`, borderRadius: 4, fontSize: 12.5, color: colors.inkMuted, fontWeight: 500 }}>
                  {r.reason}
                </div>
              )}

              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <input
                  value={comments[r.id] ?? ''}
                  onChange={(e) => setComments((c) => ({ ...c, [r.id]: e.target.value }))}
                  placeholder="Optional comment to employee…"
                  style={{ flex: 1, boxSizing: 'border-box', background: colors.paperShade, border: `1px solid ${colors.paperHairline}`, borderRadius: 6, padding: '9px 12px', fontFamily: font, fontSize: 12.5, color: colors.ink }}
                />
                <button onClick={() => decide(r.id, 'reject')} disabled={busyId === r.id} style={{ ...btn.base, ...btn.danger, padding: '9px 16px' }}>Reject</button>
                <button onClick={() => decide(r.id, 'approve')} disabled={busyId === r.id} style={{ ...btn.base, ...btn.approve, padding: '9px 16px' }}>Approve</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
