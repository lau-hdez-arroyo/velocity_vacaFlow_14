'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';

// Minimal placeholder home. It confirms the auto-established session after registration.
// The authenticated current-user endpoint and full navigation arrive with US-002.
export default function HomePage() {
  const [fullName, setFullName] = useState<string | null>(null);
  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    setFullName(sessionStorage.getItem('vacaflow.fullName'));
    setRole(sessionStorage.getItem('vacaflow.role'));
  }, []);

  return (
    <main style={styles.page}>
      <div style={styles.card}>
        {fullName ? (
          <>
            <h1 style={styles.title}>Welcome, {fullName}</h1>
            <p style={styles.text}>
              Registration successful — you are signed in{role ? ` as ${role}` : ''}.
            </p>
          </>
        ) : (
          <>
            <h1 style={styles.title}>VacaFlow</h1>
            <p style={styles.text}>
              <Link href="/register">Create an account</Link> to get started.
            </p>
          </>
        )}
      </div>
    </main>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', padding: 16 },
  card: {
    width: '100%',
    maxWidth: 480,
    background: '#fff',
    borderRadius: 12,
    padding: 32,
    boxShadow: '0 8px 24px rgba(0,0,0,0.08)',
  },
  title: { marginTop: 0, fontSize: 24 },
  text: { fontSize: 15, lineHeight: 1.5 },
};
