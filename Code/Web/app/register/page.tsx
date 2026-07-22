'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { ApiError, apiPost } from '@/lib/api-client';

interface RegisteredUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export default function RegisterPage() {
  const router = useRouter();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('Employee');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFieldErrors({});
    setFormError(null);
    setSubmitting(true);

    try {
      const user = await apiPost<RegisteredUser>('/api/auth/register', {
        fullName,
        email,
        password,
        role,
      });
      sessionStorage.setItem('vacaflow.fullName', user.fullName);
      sessionStorage.setItem('vacaflow.role', user.role);
      router.push('/');
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 400 && error.errors) {
          setFieldErrors(error.errors);
        } else if (error.status === 409) {
          setFieldErrors({ email: ['Email already registered.'] });
        } else {
          setFormError(error.detail ?? error.message);
        }
      } else {
        setFormError('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main style={styles.page}>
      <form style={styles.card} onSubmit={handleSubmit} noValidate>
        <h1 style={styles.title}>Create your VacaFlow account</h1>

        {formError && (
          <p role="alert" style={styles.formError}>
            {formError}
          </p>
        )}

        <Field label="Full name" htmlFor="fullName" errors={fieldErrors.fullName}>
          <input
            id="fullName"
            style={styles.input}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            autoComplete="name"
          />
        </Field>

        <Field label="Email" htmlFor="email" errors={fieldErrors.email}>
          <input
            id="email"
            type="email"
            style={styles.input}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
          />
        </Field>

        <Field label="Password" htmlFor="password" errors={fieldErrors.password}>
          <input
            id="password"
            type="password"
            style={styles.input}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
          />
        </Field>

        <Field label="Role" htmlFor="role" errors={fieldErrors.role}>
          <select id="role" style={styles.input} value={role} onChange={(e) => setRole(e.target.value)}>
            <option value="Employee">Employee</option>
            <option value="Manager">Manager</option>
          </select>
        </Field>

        <button type="submit" style={styles.button} disabled={submitting}>
          {submitting ? 'Creating account…' : 'Register'}
        </button>
      </form>
    </main>
  );
}

function Field({
  label,
  htmlFor,
  errors,
  children,
}: {
  label: string;
  htmlFor: string;
  errors?: string[];
  children: React.ReactNode;
}) {
  return (
    <div style={styles.field}>
      <label htmlFor={htmlFor} style={styles.label}>
        {label}
      </label>
      {children}
      {errors?.map((message) => (
        <span key={message} role="alert" style={styles.fieldError}>
          {message}
        </span>
      ))}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', padding: 16 },
  card: {
    width: '100%',
    maxWidth: 400,
    background: '#fff',
    borderRadius: 12,
    padding: 32,
    boxShadow: '0 8px 24px rgba(0,0,0,0.08)',
    display: 'flex',
    flexDirection: 'column',
    gap: 16,
  },
  title: { margin: 0, fontSize: 22 },
  field: { display: 'flex', flexDirection: 'column', gap: 6 },
  label: { fontSize: 13, fontWeight: 600 },
  input: { padding: '10px 12px', borderRadius: 8, border: '1px solid #cbd2d9', fontSize: 14 },
  button: {
    marginTop: 8,
    padding: '12px 16px',
    borderRadius: 8,
    border: 'none',
    background: '#1a1a2e',
    color: '#fff',
    fontSize: 15,
    fontWeight: 600,
    cursor: 'pointer',
  },
  formError: {
    margin: 0,
    padding: '10px 12px',
    borderRadius: 8,
    background: '#fdecea',
    color: '#a01818',
    fontSize: 13,
  },
  fieldError: { color: '#a01818', fontSize: 12 },
};
