'use client';

import { AppShell } from '@/components/AppShell';
import { RequestForm } from '@/components/RequestForm';

export default function NewRequestPage() {
  return <AppShell navLabel="New Request">{() => <RequestForm mode="new" />}</AppShell>;
}
