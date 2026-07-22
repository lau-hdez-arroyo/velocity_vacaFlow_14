import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import { Space_Grotesk } from 'next/font/google';
import { colors, font } from '@/lib/theme';

const spaceGrotesk = Space_Grotesk({
  subsets: ['latin'],
  weight: ['500', '600', '700'],
  variable: '--font-space',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'VacaFlow',
  description: 'Absence and vacation request management for IGS Solutions',
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" className={spaceGrotesk.variable}>
      <body
        style={{
          margin: 0,
          background: colors.bg,
          color: colors.ink,
          fontFamily: font,
        }}
      >
        {children}
      </body>
    </html>
  );
}
