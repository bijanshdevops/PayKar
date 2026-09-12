import type { SupportTicketStatus, TicketDepartment, TicketPriority } from '@/features/support/supportApi';

export const SupportTicketStatusLabels: Record<SupportTicketStatus, string> = {
  PendingResponse: 'در انتظار پاسخ',
  InProgress: 'در حال بررسی',
  Answered: 'پاسخ داده شده',
  Closed: 'بسته شده',
  Reopened: 'بازشده مجدد'
};

/** رنگ‌بندی نشان (Badge) هر وضعیت — پس‌زمینه/متن/نقطه، طبق موکاپ صفحه پشتیبانی. */
export const SupportTicketStatusColors: Record<SupportTicketStatus, { bg: string; fg: string; dot: string }> = {
  PendingResponse: { bg: '#fef3c7', fg: '#92400e', dot: '#d97706' },
  InProgress: { bg: '#dbeafe', fg: '#1e40af', dot: '#2563eb' },
  Answered: { bg: '#dcfce7', fg: '#166534', dot: '#16a34a' },
  Closed: { bg: '#f1f5f9', fg: '#475569', dot: '#64748b' },
  Reopened: { bg: '#f3e8ff', fg: '#6b21a8', dot: '#9333ea' }
};

export const TicketDepartmentLabels: Record<TicketDepartment, string> = {
  TechnicalSupport: 'پشتیبانی فنی',
  FinancialAndInvoices: 'امور مالی و فاکتورها',
  AccountIssues: 'مشکلات حساب کاربری',
  General: 'عمومی'
};

export const TicketPriorityLabels: Record<TicketPriority, string> = {
  Low: 'کم',
  Medium: 'متوسط',
  High: 'فوری'
};

export const TicketPriorityColors: Record<TicketPriority, { bg: string; fg: string; dot: string }> = {
  Low: { bg: '#dcfce7', fg: '#166534', dot: '#16a34a' },
  Medium: { bg: '#fef9c3', fg: '#854d0e', dot: '#d97706' },
  High: { bg: '#ffe4e6', fg: '#9f1239', dot: '#f43f5e' }
};
