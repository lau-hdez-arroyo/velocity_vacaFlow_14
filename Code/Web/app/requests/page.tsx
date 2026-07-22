'use client';

import { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiGet, apiPost } from '@/lib/api-client';
import type { RequestItem } from '@/lib/types';
import { AppShell } from '@/components/AppShell';
import { StatusBadge } from '@/components/StatusBadge';
import { RouteStepper } from '@/components/RouteStepper';
import { btn, colors, statusMeta } from '@/lib/theme';

const MONTHS = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
const fmt = (d: string) => {
  const [, m, day] = d.split('-').map(Number);
  return `${MONTHS[m - 1]} ${day}`;
};
const dayCount = (a: string, b: string) => Math.round((new Date(b).getTime() - new Date(a).getTime()) / 86_400_000) + 1;

export default function MyRequestsPage() {
  return <AppShell navLabel="My Requests">{() => <MyRequests />}</AppShell>;
}

function MyRequests() {
  const router = useRouter();
  const [items, setItems] = useState<RequestItem[]>([]);
  const [loading, setLoading] = useState(true);

  const reload = useCallback(async () => {
    const data = await apiGet<RequestItem[]>('/api/requests');
    setItems(data);
    setLoading(false);
  }, []);

  useEffect(() => {
    reload().catch(() => setLoading(false));
  }, [reload]);

  async function act(id: string, action: 'submit' | 'cancel') {
    await apiPost(`/api/requests/${id}/${action}`, {});
    await reload();
  }

  return (
    <div style={{ maxWidth: 820, margin: '0 auto', padding: '48px 32px 80px' }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 36 }}>
        <div>
          <div style={{ fontSize: 30, fontWeight: 700, letterSpacing: -0.4 }}>My Requests</div>
          <div style={{ fontSize: 13, color: colors.inkMuted, marginTop: 6, fontWeight: 500 }}>
            {items.length} request{items.length !== 1 ? 's' : ''} on file
          </div>
        </div>
        <button onClick={() => router.push('/requests/new')} style={{ ...btn.base, ...btn.primary, padding: '12px 18px' }}>
          + New Request
        </button>
      </div>

      {loading ? (
        <div style={{ color: colors.inkMuted, fontSize: 14 }}>Loading…</div>
      ) : items.length === 0 ? (
        <div style={{ color: colors.inkMuted, fontSize: 14 }}>No requests yet. Create your first one.</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {items.map((r) => (
            <div key={r.id} style={{ background: colors.paper, border: `1px solid ${colors.paperHairline}`, borderRadius: 10, padding: '20px 22px' }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <div style={{ fontSize: 17, fontWeight: 700, color: colors.ink, letterSpacing: -0.2 }}>{r.absenceType}</div>
                  <StatusBadge status={r.status} />
                </div>
                <div style={{ fontSize: 12, color: colors.inkMuted, fontWeight: 600 }}>{dayCount(r.startDate, r.endDate)} day{dayCount(r.startDate, r.endDate) > 1 ? 's' : ''}</div>
              </div>
              <div style={{ fontSize: 13, color: colors.inkMuted, fontWeight: 500, marginTop: 6 }}>
                {fmt(r.startDate)} → {fmt(r.endDate)}, {r.startDate.split('-')[0]}
              </div>

              <div style={{ marginTop: 16 }}>
                <RouteStepper status={r.status} />
              </div>

              {r.decisionComment && (
                <div style={{ marginTop: 14, padding: '10px 12px', background: colors.paperShade, borderLeft: `3px solid ${statusMeta[r.status].color}`, borderRadius: 4, fontSize: 12.5, color: colors.inkMuted, fontWeight: 500 }}>
                  {r.decisionComment} — Manager
                </div>
              )}

              {(r.status === 'Draft' || r.status === 'Submitted') && (
                <div style={{ display: 'flex', gap: 8, marginTop: 14 }}>
                  {r.status === 'Draft' && (
                    <>
                      <button onClick={() => router.push(`/requests/${r.id}/edit`)} style={{ ...btn.base, ...btn.secondary }}>Edit</button>
                      <button onClick={() => act(r.id, 'submit')} style={{ ...btn.base, ...btn.primary }}>Submit</button>
                    </>
                  )}
                  <button onClick={() => act(r.id, 'cancel')} style={{ ...btn.base, ...btn.danger }}>Cancel request</button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
