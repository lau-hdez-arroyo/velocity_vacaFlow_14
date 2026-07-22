import type { RequestStatus } from './theme';

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: 'Employee' | 'Manager';
}

export interface AbsenceType {
  id: string;
  name: string;
}

export interface RequestItem {
  id: string;
  ownerEmployeeId: string;
  employeeName: string;
  absenceTypeId: string;
  absenceType: string;
  startDate: string; // yyyy-MM-dd
  endDate: string;
  reason: string;
  status: RequestStatus;
  decision: 'Approved' | 'Rejected' | null;
  decisionComment: string | null;
}
