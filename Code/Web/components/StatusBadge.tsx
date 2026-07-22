import { statusMeta, tint, type RequestStatus } from '@/lib/theme';

export function StatusBadge({ status }: { status: RequestStatus }) {
  const meta = statusMeta[status];
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 5,
        fontSize: 11,
        fontWeight: 700,
        letterSpacing: 0.2,
        padding: '3px 9px',
        borderRadius: 5,
        color: meta.color,
        background: tint(meta.color),
      }}
    >
      {meta.icon} {status}
    </span>
  );
}
