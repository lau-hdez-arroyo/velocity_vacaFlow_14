'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { ApiError, apiPost } from '@/lib/api-client';
import type { CurrentUser } from '@/lib/types';
import { RouteStepper } from '@/components/RouteStepper';
import { btn, colors, font, input, label } from '@/lib/theme';

type Mode = 'login' | 'register';
type RoleChoice = 'Employee' | 'Manager';

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<Mode>('login');
  const [role, setRole] = useState<RoleChoice>('Employee');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  function redirectByRole(userRole: string) {
    router.push(userRole === 'Manager' ? '/team' : '/requests');
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFieldErrors({});
    setFormError(null);
    setSubmitting(true);
    try {
      const user =
        mode === 'login'
          ? await apiPost<CurrentUser>('/api/auth/login', { email, password })
          : await apiPost<CurrentUser>('/api/auth/register', { fullName, email, password, role });
      redirectByRole(user.role);
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 400 && error.errors) setFieldErrors(error.errors);
        else if (error.status === 409) setFieldErrors({ email: ['Email already registered.'] });
        else if (error.status === 401) setFormError('Invalid email or password.');
        else setFormError(error.detail ?? error.message);
      } else {
        setFormError('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  const isRegister = mode === 'register';

  return (
    <div style={{ minHeight: '100vh', display: 'grid', gridTemplateColumns: '1fr 1fr' }}>
      {/* Hero */}
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: '64px 56px',
          background: colors.bgHeader,
          borderRight: `1px solid ${colors.hairline}`,
        }}
      >
        <div>
          <Brand />
          <div style={{ marginTop: 64, maxWidth: 380 }}>
            <div style={{ fontSize: 38, fontWeight: 700, lineHeight: 1.15, letterSpacing: -0.5 }}>
              Every request,
              <br />
              tracked end to end.
            </div>
            <div style={{ marginTop: 18, fontSize: 15, color: colors.inkMuted, lineHeight: 1.6, fontWeight: 500 }}>
              Submit, track and approve leave requests for IGS Solutions with full visibility at every step.
            </div>
          </div>
        </div>
        <div>
          <div
            style={{
              fontSize: 11,
              letterSpacing: 1.5,
              textTransform: 'uppercase',
              color: colors.inkMuted,
              fontWeight: 600,
              marginBottom: 14,
            }}
          >
            Request lifecycle
          </div>
          <RouteStepper status="Approved" />
        </div>
      </div>

      {/* Auth form */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 56 }}>
        <form onSubmit={handleSubmit} noValidate style={{ width: '100%', maxWidth: 380 }}>
          <div style={{ display: 'flex', gap: 22, marginBottom: 22, borderBottom: `1px solid ${colors.hairline}` }}>
            <Tab active={!isRegister} onClick={() => setMode('login')}>
              Sign in
            </Tab>
            <Tab active={isRegister} onClick={() => setMode('register')}>
              Create account
            </Tab>
          </div>

          <div style={{ fontSize: 24, fontWeight: 700, marginBottom: 24, letterSpacing: -0.3 }}>
            {isRegister ? 'Set up your account' : 'Sign in to VacaFlow'}
          </div>

          {isRegister && (
            <div style={{ display: 'flex', gap: 12, marginBottom: 22 }}>
              <RoleGate active={role === 'Employee'} onClick={() => setRole('Employee')} title="Employee" subtitle="Create & track requests" />
              <RoleGate active={role === 'Manager'} onClick={() => setRole('Manager')} title="Manager" subtitle="Review team requests" />
            </div>
          )}

          {formError && (
            <div
              role="alert"
              style={{ marginBottom: 16, padding: '10px 12px', borderRadius: 8, background: 'oklch(0.94 0.05 25)', color: colors.coral, fontSize: 13, fontWeight: 600 }}
            >
              {formError}
            </div>
          )}

          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            {isRegister && (
              <Field label="Full name" errors={fieldErrors.fullName}>
                <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Jamie Torres" autoComplete="name" style={input} />
              </Field>
            )}
            <Field label="Work email" errors={fieldErrors.email}>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@igssolutions.com" autoComplete="email" style={input} />
            </Field>
            <Field label="Password" errors={fieldErrors.password}>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" autoComplete={isRegister ? 'new-password' : 'current-password'} style={input} />
            </Field>

            <button type="submit" disabled={submitting} style={{ ...btn.base, ...btn.primary, marginTop: 8, padding: '14px 18px', fontSize: 14 }}>
              {submitting ? 'Please wait…' : isRegister ? `Create account & continue as ${role}` : 'Sign in'} →
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function Brand() {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
      <div style={{ width: 30, height: 30, borderRadius: 7, background: colors.accent, display: 'flex', alignItems: 'center', justifyContent: 'center', color: colors.paper, fontWeight: 700, fontSize: 14 }}>
        V
      </div>
      <div style={{ fontSize: 20, fontWeight: 700, letterSpacing: -0.2 }}>VacaFlow</div>
    </div>
  );
}

function Tab({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <div
      onClick={onClick}
      style={{ padding: '8px 0', cursor: 'pointer', fontSize: 13, fontWeight: 700, letterSpacing: 0.2, color: active ? colors.ink : colors.inkMuted, borderBottom: `2px solid ${active ? colors.accent : 'transparent'}` }}
    >
      {children}
    </div>
  );
}

function RoleGate({ active, onClick, title, subtitle }: { active: boolean; onClick: () => void; title: string; subtitle: string }) {
  return (
    <div
      onClick={onClick}
      style={{ flex: 1, padding: 16, borderRadius: 8, cursor: 'pointer', textAlign: 'center', border: `1.5px solid ${active ? colors.accent : colors.hairline}`, background: active ? `color-mix(in oklch, ${colors.accent} 10%, transparent)` : 'transparent' }}
    >
      <div style={{ fontFamily: font, fontWeight: 700, fontSize: 15 }}>{title}</div>
      <div style={{ fontSize: 12, color: colors.inkMuted, marginTop: 2 }}>{subtitle}</div>
    </div>
  );
}

function Field({ label: text, errors, children }: { label: string; errors?: string[]; children: React.ReactNode }) {
  return (
    <div>
      <div style={label}>{text}</div>
      {children}
      {errors?.map((message) => (
        <span key={message} role="alert" style={{ color: colors.coral, fontSize: 12, display: 'block', marginTop: 4 }}>
          {message}
        </span>
      ))}
    </div>
  );
}
