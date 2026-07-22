'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { apiGet } from '@/lib/api-client';
import type { RequestItem } from '@/lib/types';
import { AppShell } from '@/components/AppShell';
import { RequestForm } from '@/components/RequestForm';
import { colors } from '@/lib/theme';

export default function EditRequestPage() {
  const params = useParams();
  const id = params.id as string;
  const [request, setRequest] = useState<RequestItem | null>(null);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    apiGet<RequestItem>(`/api/requests/${id}`).then(setRequest).catch(() => setNotFound(true));
  }, [id]);

  return (
    <AppShell navLabel="Edit Draft">
      {() =>
        notFound ? (
          <div style={{ padding: 48, color: colors.inkMuted, fontSize: 14 }}>Request not found.</div>
        ) : !request ? (
          <div style={{ padding: 48, color: colors.inkMuted, fontSize: 14 }}>Loading…</div>
        ) : (
          <RequestForm
            mode="edit"
            requestId={request.id}
            initial={{ absenceTypeId: request.absenceTypeId, startDate: request.startDate, endDate: request.endDate, reason: request.reason }}
          />
        )
      }
    </AppShell>
  );
}
