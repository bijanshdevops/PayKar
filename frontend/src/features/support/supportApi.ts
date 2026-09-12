import { baseApi } from '@/api/baseApi';
import type { ApiResponse, PagedResult } from '@/shared/types';

// طبق فاز «مدیریت پشتیبانی و تیکت‌ها» — ۵ وضعیت مجزا (قبلاً ۴ وضعیت بود؛ Open→PendingResponse،
// Resolved→Answered، و «بازشده مجدد» اکنون یک وضعیت واقعاً مستقل است).
export type SupportTicketStatus = 'PendingResponse' | 'InProgress' | 'Answered' | 'Closed' | 'Reopened';

export type TicketDepartment = 'TechnicalSupport' | 'FinancialAndInvoices' | 'AccountIssues' | 'General';

export type TicketPriority = 'Low' | 'Medium' | 'High';

export interface SupportTicket {
  id: string;
  ticketNumber: string;
  userId: string;
  subject: string;
  department: TicketDepartment;
  priority: TicketPriority;
  status: SupportTicketStatus;
  createdAtUtc: string;
  lastActivityAtUtc: string;
}

export interface SupportMessage {
  id: string;
  senderUserId: string;
  isFromSupportTeam: boolean;
  body: string;
  attachmentUrl: string | null;
  attachmentFileName: string | null;
  createdAtUtc: string;
}

export interface SupportTicketDetail extends SupportTicket {
  messages: SupportMessage[];
  /** شماره موبایل کاربر ثبت‌کننده — برای کارت «کاربر مرتبط» صفحه جزئیات تیکت. */
  requesterMobileNumber: string;
}

/** شمارنده تیکت‌ها به تفکیک وضعیت — برای تب‌های فیلتر صفحه لیست. */
export interface SupportTicketStatusSummary {
  all: number;
  pendingResponse: number;
  inProgress: number;
  answered: number;
  closed: number;
  reopened: number;
}

export interface CreateSupportTicketValues {
  subject: string;
  message: string;
  department: TicketDepartment;
  priority: TicketPriority;
  attachment?: File;
}

const buildTicketFormData = (values: CreateSupportTicketValues) => {
  const formData = new FormData();
  formData.append('subject', values.subject);
  formData.append('message', values.message);
  formData.append('department', values.department);
  formData.append('priority', values.priority);
  if (values.attachment) formData.append('file', values.attachment);
  return formData;
};

export const supportApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    createSupportTicket: builder.mutation<ApiResponse<SupportTicketDetail>, CreateSupportTicketValues>({
      query: (values) => ({ url: '/support-tickets', method: 'POST', body: buildTicketFormData(values) }),
      invalidatesTags: ['SupportTicket']
    }),
    getMySupportTickets: builder.query<ApiResponse<PagedResult<SupportTicket>>, { status?: SupportTicketStatus; page: number; pageSize: number }>({
      query: ({ status, page, pageSize }) => ({ url: '/support-tickets/me', params: { status, page, pageSize } }),
      providesTags: ['SupportTicket']
    }),
    getMySupportTicketStatusSummary: builder.query<ApiResponse<SupportTicketStatusSummary>, void>({
      query: () => '/support-tickets/me/status-summary',
      providesTags: ['SupportTicket']
    }),
    getAllSupportTickets: builder.query<ApiResponse<PagedResult<SupportTicket>>, { status?: string; page: number; pageSize: number }>({
      query: ({ status, page, pageSize }) => ({ url: '/support-tickets', params: { status, page, pageSize } }),
      providesTags: ['SupportTicket']
    }),
    getAllSupportTicketStatusSummary: builder.query<ApiResponse<SupportTicketStatusSummary>, void>({
      query: () => '/support-tickets/status-summary',
      providesTags: ['SupportTicket']
    }),
    getSupportTicketById: builder.query<ApiResponse<SupportTicketDetail>, string>({
      query: (ticketId) => `/support-tickets/${ticketId}`,
      providesTags: ['SupportTicket']
    }),
    addSupportMessage: builder.mutation<ApiResponse<SupportMessage>, { ticketId: string; body: string; attachment?: File }>({
      query: ({ ticketId, body, attachment }) => {
        const formData = new FormData();
        formData.append('body', body);
        if (attachment) formData.append('file', attachment);
        return { url: `/support-tickets/${ticketId}/messages`, method: 'POST', body: formData };
      },
      invalidatesTags: ['SupportTicket']
    }),
    updateSupportTicketStatus: builder.mutation<ApiResponse<SupportTicket>, { ticketId: string; newStatus: SupportTicketStatus }>({
      query: ({ ticketId, newStatus }) => ({ url: `/support-tickets/${ticketId}/status`, method: 'POST', body: { newStatus } }),
      invalidatesTags: ['SupportTicket']
    }),
    reopenSupportTicket: builder.mutation<ApiResponse<SupportTicket>, string>({
      query: (ticketId) => ({ url: `/support-tickets/${ticketId}/reopen`, method: 'POST' }),
      invalidatesTags: ['SupportTicket']
    }),
    closeSupportTicket: builder.mutation<ApiResponse<SupportTicket>, string>({
      query: (ticketId) => ({ url: `/support-tickets/${ticketId}/close`, method: 'POST' }),
      invalidatesTags: ['SupportTicket']
    })
  })
});

export const {
  useCreateSupportTicketMutation,
  useGetMySupportTicketsQuery,
  useGetMySupportTicketStatusSummaryQuery,
  useGetAllSupportTicketsQuery,
  useGetAllSupportTicketStatusSummaryQuery,
  useGetSupportTicketByIdQuery,
  useAddSupportMessageMutation,
  useUpdateSupportTicketStatusMutation,
  useReopenSupportTicketMutation,
  useCloseSupportTicketMutation
} = supportApi;
