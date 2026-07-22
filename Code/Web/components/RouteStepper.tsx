import { colors, statusMeta, tint, type RequestStatus } from '@/lib/theme';

function order(status: RequestStatus): RequestStatus[] {
  if (status === 'Rejected') return ['Draft', 'Submitted', 'Rejected'];
  if (status === 'Cancelled') return ['Draft', 'Submitted', 'Cancelled'];
  return ['Draft', 'Submitted', 'Approved'];
}

/** Three-node lifecycle stepper (Draft → Submitted → Approved|Rejected|Cancelled). */
export function RouteStepper({ status }: { status: RequestStatus }) {
  const steps = order(status);
  const currentIdx = steps.indexOf(status);

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center' }}>
        {steps.map((label, i) => {
          const meta = statusMeta[label];
          const node =
            i === currentIdx
              ? { bg: meta.color, color: colors.paper, border: 'none', ring: `0 0 0 3px ${tint(meta.color, 25)}`, icon: meta.icon }
              : i < currentIdx
                ? { bg: colors.slate, color: colors.paper, border: 'none', ring: 'none', icon: meta.icon, opacity: 0.55 }
                : { bg: 'transparent', color: colors.paperHairline, border: `2px solid ${colors.paperHairline}`, ring: 'none', icon: '' };
          return (
            <div key={label} style={{ display: 'flex', alignItems: 'center' }}>
              <div
                style={{
                  width: 26,
                  height: 26,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 12,
                  fontWeight: 700,
                  background: node.bg,
                  color: node.color,
                  border: node.border,
                  boxShadow: node.ring,
                  opacity: (node as { opacity?: number }).opacity ?? 1,
                }}
              >
                {node.icon}
              </div>
              {i < steps.length - 1 && (
                <div
                  style={{
                    width: 40,
                    height: 2,
                    background: i < currentIdx ? colors.slate : colors.paperHairline,
                    opacity: i < currentIdx ? 0.55 : 1,
                  }}
                />
              )}
            </div>
          );
        })}
      </div>
      <div style={{ display: 'flex', marginTop: 6 }}>
        {steps.map((labelText, i) => (
          <div
            key={labelText}
            style={{
              width: 66,
              textAlign: 'center',
              fontSize: 9.5,
              fontWeight: 700,
              letterSpacing: 0.2,
              whiteSpace: 'nowrap',
              color: i <= currentIdx ? colors.inkMuted : colors.paperHairline,
            }}
          >
            {labelText}
          </div>
        ))}
      </div>
    </div>
  );
}
