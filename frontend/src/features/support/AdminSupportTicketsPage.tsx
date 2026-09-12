import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Eye, Headset, Inbox } from 'lucide-react';
import {
  useGetAllSupportTicketsQuery,
  useGetAllSupportTicketStatusSummaryQuery,
  type SupportTicketStatus
} from '@/features/support/supportApi';
import { SupportTicketStatusColors, SupportTicketStatusLabels, TicketDepartmentLabels, TicketPriorityColors, TicketPriorityLabels } from '@/features/support/SupportTicketStatusLabels';

type FilterKey = 'All' | SupportTicketStatus;

const FILTER_TABS: { key: FilterKey; label: string }[] = [
  { key: 'All', label: 'همه' },
  { key: 'PendingResponse', label: SupportTicketStatusLabels.PendingResponse },
  { key: 'InProgress', label: SupportTicketStatusLabels.InProgress },
  { key: 'Answered', label: SupportTicketStatusLabels.Answered },
  { key: 'Closed', label: SupportTicketStatusLabels.Closed },
  { key: 'Reopened', label: SupportTicketStatusLabels.Reopened }
];

/** پنل پشتیبانی (Admin): لیست همه تیکت‌های کاربران با فیلتر وضعیت — طبق ADR-007، گسترش‌یافته در فاز «مدیریت پشتیبانی و تیکت‌ها». */
export default function AdminSupportTicketsPage() {
  const navigate = useNavigate();
  const [activeFilter, setActiveFilter] = useState<FilterKey>('All');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useGetAllSupportTicketsQuery({
    status: activeFilter === 'All' ? undefined : activeFilter,
    page,
    pageSize
  });
  const { data: summaryData } = useGetAllSupportTicketStatusSummaryQuery();

  const summary = summaryData?.data;
  const tickets = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;

  const tabCounts = useMemo<Record<FilterKey, number>>(
    () => ({
      All: summary?.all ?? 0,
      PendingResponse: summary?.pendingResponse ?? 0,
      InProgress: summary?.inProgress ?? 0,
      Answered: summary?.answered ?? 0,
      Closed: summary?.closed ?? 0,
      Reopened: summary?.reopened ?? 0
    }),
    [summary]
  );

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-4" dir="rtl">
      <h1 className="flex items-center gap-2 text-xl font-extrabold text-slate-800 sm:text-2xl">
        <Headset className="h-6 w-6 text-emerald-600" />
        تیکت‌های پشتیبانی
      </h1>

      <div className="flex flex-wrap gap-2">
        {FILTER_TABS.map((tab) => {
          const isActive = activeFilter === tab.key;
          return (
            <button
              key={tab.key}
              type="button"
              onClick={() => {
                setActiveFilter(tab.key);
                setPage(1);
              }}
              className={`rounded-full border px-3.5 py-1.5 text-xs font-semibold transition-colors ${
                isActive ? 'border-emerald-600 bg-emerald-600 text-white shadow-sm' : 'border-slate-200 bg-white text-slate-500 hover:bg-slate-100'
              }`}
            >
              {tab.label}
              <span className={`mr-1 ${isActive ? 'text-emerald-100' : 'text-slate-400'}`}>({tabCounts[tab.key].toLocaleString('fa-IR')})</span>
            </button>
          );
        })}
      </div>

      {isLoading && <p className="text-sm text-slate-400">در حال بارگذاری...</p>}

      {!isLoading && tickets.length === 0 && (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white py-14 text-center">
          <Inbox className="mb-3 h-9 w-9 text-slate-300" />
          <p className="font-semibold text-slate-600">تیکتی با این فیلتر یافت نشد.</p>
        </div>
      )}

      {!isLoading && tickets.length > 0 && (
        <div className="overflow-x-auto rounded-2xl border border-slate-200/80 bg-white">
          <table className="w-full min-w-[760px] border-collapse text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-right text-xs font-semibold text-slate-400">
                <th className="px-3 py-3">شناسه تیکت</th>
                <th className="px-3 py-3">موضوع</th>
                <th className="px-3 py-3">بخش</th>
                <th className="px-3 py-3">اولویت</th>
                <th className="px-3 py-3">آخرین به‌روزرسانی</th>
                <th className="px-3 py-3">وضعیت</th>
                <th className="px-3 py-3">عملیات</th>
              </tr>
            </thead>
            <tbody>
              {tickets.map((ticket) => {
                const statusColors = SupportTicketStatusColors[ticket.status];
                const priorityColors = TicketPriorityColors[ticket.priority];
                return (
                  <tr
                    key={ticket.id}
                    onClick={() => navigate(`/support/${ticket.id}`)}
                    className="cursor-pointer border-b border-slate-50 transition-colors hover:bg-slate-50"
                  >
                    <td className="px-3 py-3 font-mono text-xs text-slate-500">#{ticket.ticketNumber}</td>
                    <td className="px-3 py-3 font-medium text-slate-700">{ticket.subject}</td>
                    <td className="px-3 py-3 text-slate-500">{TicketDepartmentLabels[ticket.department]}</td>
                    <td className="px-3 py-3">
                      <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold" style={{ background: priorityColors.bg, color: priorityColors.fg }}>
                        <span className="h-1.5 w-1.5 rounded-full" style={{ background: priorityColors.dot }} />
                        {TicketPriorityLabels[ticket.priority]}
                      </span>
                    </td>
                    <td className="px-3 py-3 text-xs text-slate-400">
                      {new Date(ticket.lastActivityAtUtc).toLocaleString('fa-IR', { dateStyle: 'short', timeStyle: 'short' })}
                    </td>
                    <td className="px-3 py-3">
                      <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold" style={{ background: statusColors.bg, color: statusColors.fg }}>
                        <span className="h-1.5 w-1.5 rounded-full" style={{ background: statusColors.dot }} />
                        {SupportTicketStatusLabels[ticket.status]}
                      </span>
                    </td>
                    <td className="px-3 py-3">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/support/${ticket.id}`);
                        }}
                        className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2.5 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-emerald-50 hover:text-emerald-700"
                      >
                        <Eye className="h-3.5 w-3.5" />
                        مشاهده و گفتگو
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button
              key={p}
              onClick={() => setPage(p)}
              className={`h-8 w-8 rounded-lg text-xs font-semibold transition-colors ${
                p === page ? 'bg-emerald-600 text-white' : 'bg-slate-100 text-slate-500 hover:bg-slate-200'
              }`}
            >
              {p.toLocaleString('fa-IR')}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
