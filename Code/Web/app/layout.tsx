import type { Metadata } from 'next';
import type { ReactNode } from 'react';

export const metadata: Metadata = {
  title: 'VacaFlow',
  description: 'Absence and vacation request management',
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body
        style={{
          fontFamily: 'system-ui, -apple-system, Segoe UI, Roboto, sans-serif',
          margin: 0,
          background: '#f4f5f7',
          color: '#1a1a2e',
        }}
      >
        {children}
      </body>
    </html>
  );
}
