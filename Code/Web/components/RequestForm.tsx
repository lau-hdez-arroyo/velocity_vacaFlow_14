'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ApiError, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type { AbsenceType } from '@/lib/types';
import { btn, colors, font, label } from '@/lib/theme';

interface Initial {
  absenceTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
}

const TYPE_ICON: Record<string, string> = { Vacation: '●', 'Personal Leave': '▲', 'Sick Leave': '■' };

function dayCount(start: string, end: string): number | null {
  if (!start || !end) return null;
  const a = new Date(start);
  const b = new Date(end);
  if (b < a) return null;
  return Math.round((b.getTime() - a.getTime()) / 86_400_000) + 1;
}

export function RequestForm({ mode, requestId, initial }: { mode: 'new' | 'edit'; requestId?: string; initial?: Initial }) {
  const router = useRouter();
  const [types, setTypes] = useState<AbsenceType[]>([]);
  const [absenceTypeId, setAbsenceTypeId] = useState(initial?.absenceTypeId ?? '');
  const [startDate, setStartDate] = useState(initial?.startDate ?? '');
  const [endDate, setEndDate] = useState(initial?.endDate ?? '');
  const [reason, setReason] = useState(initial?.reason ?? '');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    apiGet<AbsenceType[]>('/api/absence-types')
      .then((t) => {
        setTypes(t);
        setAbsenceTypeId((current) => current || t[0]?.id || '');
      })
      .catch(() => {});
  }, []);

  async function persist(submit: boolean) {
    setFieldErrors({});
    setFormError(null);
    setBusy(true);
    const payload = { absenceTypeId, startDate, endDate, reason };
    try {
      let id = requestId;
      if (mode === 'new') {
        const created = await apiPost<{ id: string }>('/api/requests', payload);
        id = created.id;
      } else {
        await apiPut(`/api/requests/${requestId}`, payload);
      }
      if (submit && id) await apiPost(`/api/requests/${id}/submit`, {});
      router.push('/requests');
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 400 && error.errors) setFieldErrors(error.errors);
        else setFormError(error.detail ?? error.message);
      } else {
        setFormError('Something went wrong. Please try again.');
      }
    } finally {
      setBusy(false);
    }
  }

  const days = dayCount(startDate, endDate);
  const fieldError = (name: string) => fieldErrors[name]?.[0];

  return (
    <div style={{ maxWidth: 640, margin: '0 auto', padding: '48px 32px 100px' }}>
      <div style={{ background: colors.paper, border: `1px solid ${colors.paperHairline}`, borderRadius: 10, padding: 40 }}>
        <div style={{ fontSize: 22, fontWeight: 700, marginBottom: 4, letterSpacing: -0.3 }}>
          {mode === 'edit' ? 'Edit your draft' : 'Plan a new request'}
        </div>
        <div style={{ fontSize: 13, color: colors.inkMuted, marginBottom: 28, fontWeight: 500 }}>
          Only drafts can be edited — submit when ready for manager review.
        </div>

        <div style={label}>Leave type</div>
        <div style={{ display: 'flex', gap: 10, marginBottom: 6 }}>
          {types.map((t) => {
            const active = absenceTypeId === t.id;
            return (
              <div
                key={t.id}
                onClick={() => setAbsenceTypeId(t.id)}
                style={{
                  flex: 1,
                  textAlign: 'center',
                  padding: '14px 10px',
                  borderRadius: 6,
                  cursor: 'pointer',
                  fontSize: 13,
                  fontWeight: 700,
                  border: `1.5px solid ${active ? colors.accent : colors.paperHairline}`,
                  background: active ? colors.accent : 'transparent',
                  color: active ? colors.paper : colors.ink,
                }}
              >
                {TYPE_ICON[t.name] ?? '●'} {t.name}
              </div>
            );
          })}
        </div>
        {fieldError('absenceTypeId') && <ErrText>{fieldError('absenceTypeId')}</ErrText>}

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 18, margin: '22px 0' }}>
          <div>
            <div style={label}>Start date</div>
            <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} style={dateInput} />
            {fieldError('startDate') && <ErrText>{fieldError('startDate')}</ErrText>}
          </div>
          <div>
            <div style={label}>End date</div>
            <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} style={dateInput} />
            {fieldError('endDate') && <ErrText>{fieldError('endDate')}</ErrText>}
          </div>
        </div>

        {days !== null && (
          <div style={{ fontSize: 12.5, color: colors.inkMuted, marginBottom: 22, fontWeight: 500 }}>
            Duration: <span style={{ color: colors.ink, fontWeight: 700 }}>{days} day{days > 1 ? 's' : ''}</span>
          </div>
        )}

        <div style={label}>Reason for the request</div>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Add context for your manager…"
          rows={3}
          style={{ ...dateInput, resize: 'vertical', fontSize: 13.5 }}
        />
        {fieldError('reason') && <ErrText>{fieldError('reason')}</ErrText>}

        {formError && <div style={{ marginTop: 14, fontSize: 12.5, color: colors.coral, fontWeight: 600 }}>{formError}</div>}

        <div style={{ display: 'flex', gap: 10, marginTop: 30 }}>
          <button onClick={() => persist(false)} disabled={busy} style={{ ...btn.base, ...btn.secondary, padding: '13px 16px' }}>
            Save Draft
          </button>
          <button onClick={() => persist(true)} disabled={busy} style={{ ...btn.base, ...btn.primary, flex: 1, padding: '13px 16px' }}>
            Submit for Approval →
          </button>
          <button onClick={() => router.push('/requests')} disabled={busy} style={{ marginLeft: 'auto', background: 'none', border: 'none', color: colors.inkMuted, fontSize: 12.5, fontWeight: 600, cursor: 'pointer', fontFamily: font }}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

const dateInput = {
  width: '100%',
  boxSizing: 'border-box' as const,
  background: colors.paperShade,
  border: `1px solid ${colors.paperHairline}`,
  borderRadius: 6,
  padding: 12,
  fontFamily: font,
  fontSize: 14,
  color: colors.ink,
};

function ErrText({ children }: { children: React.ReactNode }) {
  return <div style={{ color: colors.coral, fontSize: 12, marginTop: 6, fontWeight: 600 }}>{children}</div>;
}
