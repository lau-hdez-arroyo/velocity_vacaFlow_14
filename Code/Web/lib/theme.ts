import type { CSSProperties } from 'react';

// Navy Gold design tokens — extracted from mockups/VacaFlow - Navy Gold.dc.html
export const colors = {
  bg: 'oklch(0.97 0.01 70)',
  bgHeader: 'oklch(0.935 0.012 70)',
  hairline: 'oklch(0.4 0.02 70 / 0.14)',
  ink: 'oklch(0.26 0.02 60)',
  inkMuted: 'oklch(0.50 0.02 60)',
  paper: 'oklch(0.995 0.003 70)',
  paperShade: 'oklch(0.95 0.008 70)',
  paperHairline: 'oklch(0.86 0.01 70)',
  accent: 'oklch(0.34 0.09 250)', // navy
  violet: 'oklch(0.56 0.11 300)',
  amber: 'oklch(0.74 0.15 70)',
  coral: 'oklch(0.60 0.19 25)',
  olive: 'oklch(0.58 0.13 155)',
  slate: 'oklch(0.55 0.02 60)',
} as const;

export const font = "var(--font-space), 'Space Grotesk', system-ui, -apple-system, sans-serif";

export type RequestStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Cancelled';

export const statusMeta: Record<RequestStatus, { color: string; icon: string }> = {
  Draft: { color: colors.violet, icon: '⏱' },
  Submitted: { color: colors.amber, icon: '⏱' },
  Approved: { color: colors.olive, icon: '✓' },
  Rejected: { color: colors.coral, icon: '✕' },
  Cancelled: { color: colors.slate, icon: '✕' },
};

/** Translucent tint of a color, for badge/callout backgrounds. */
export const tint = (color: string, pct = 16) => `color-mix(in oklch, ${color} ${pct}%, transparent)`;

export const initials = (name: string) =>
  name.trim().split(/\s+/).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('') || '?';

export const btn = {
  base: {
    borderRadius: 6,
    padding: '10px 16px',
    fontSize: 13,
    fontWeight: 700,
    cursor: 'pointer',
    fontFamily: font,
    border: 'none',
  } as CSSProperties,
  primary: { background: colors.accent, color: colors.paper } as CSSProperties,
  secondary: { background: 'transparent', border: `1px solid ${colors.paperHairline}`, color: colors.ink } as CSSProperties,
  danger: { background: 'transparent', border: `1px solid ${tint(colors.coral, 50)}`, color: colors.coral } as CSSProperties,
  approve: { background: colors.olive, color: colors.paper } as CSSProperties,
};

export const input: CSSProperties = {
  width: '100%',
  boxSizing: 'border-box',
  background: colors.paperShade,
  border: `1px solid ${colors.paperHairline}`,
  borderRadius: 6,
  padding: '12px 14px',
  color: colors.ink,
  fontFamily: font,
  fontSize: 14,
  outline: 'none',
};

export const label: CSSProperties = {
  fontSize: 11,
  letterSpacing: 1,
  textTransform: 'uppercase',
  color: colors.inkMuted,
  fontWeight: 700,
  marginBottom: 6,
};

export const card: CSSProperties = {
  background: colors.paper,
  border: `1px solid ${colors.paperHairline}`,
  borderRadius: 10,
  padding: '20px 22px',
};
