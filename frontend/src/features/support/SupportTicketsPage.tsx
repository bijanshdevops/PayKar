import { useMemo, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  FileText,
  Headset,
  Paperclip,
  X,
  Send,
  Loader2,
  Eye,
  Inbox,
  RefreshCcw,
  Lock,
  Reply,
  Search as SearchIcon,
  ChevronDown
} from 'lucide-react';
import {
  useCreateSupportTicketMutation,
  useGetMySupportTicketsQuery,
  useGetMySupportTicketStatusSummaryQuery,
  type SupportTicketStatus,
  type TicketDepartment,
  type TicketPriority
} from '@/features/support/supportApi';
import {
  SupportTicketStatusColors,
  SupportTicketStatusLabels,
  TicketDepartmentLabels,
  TicketPriorityColors,
  TicketPriorityLabels
} from '@/features/support/SupportTicketStatusLabels';
import { useToast } from '@/features/toast/useToast';
import PersianDate from '@/shared/components/PersianDate';

const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.pdf', '.docx'];
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

const DEPARTMENT_OPTIONS: TicketDepartment[] = ['TechnicalSupport', 'FinancialAndInvoices', 'AccountIssues', 'General'];
const PRIORITY_OPTIONS: TicketPriority[] = ['Low', 'Medium', 'High'];

type FilterKey = 'All' | SupportTicketStatus;

const FILTER_TABS: { key: FilterKey; label: string }[] = [
  { key: 'All', label: 'همه' },
  { key: 'PendingResponse', label: SupportTicketStatusLabels.PendingResponse },
  { key: 'InProgress', label: SupportTicketStatusLabels.InProgress },
  { key: 'Answered', label: SupportTicketStatusLabels.Answered },
  { key: 'Closed', label: SupportTicketStatusLabels.Closed },
  { key: 'Reopened', label: SupportTicketStatusLabels.Reopened }
];

const STATUS_ICONS: Record<SupportTicketStatus, typeof Reply> = {
  PendingResponse: SearchIcon,
  InProgress: SearchIcon,
  Answered: Reply,
  Closed: Lock,
  Reopened: RefreshCcw
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} بایت`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} کیلوبایت`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} مگابایت`;
}

function StatusBadge({ status }: { status: SupportTicketStatus }) {
  const colors = SupportTicketStatusColors[status];
  const Icon = STATUS_ICONS[status];
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold"
      style={{ background: colors.bg, color: colors.fg }}
    >
      <Icon className="h-3 w-3" />
      {SupportTicketStatusLabels[status]}
    </span>
  );
}

function PriorityBadge({ priority }: { priority: TicketPriority }) {
  const colors = TicketPriorityColors[priority];
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold" style={{ background: colors.bg, color: colors.fg }}>
      <span className="h-1.5 w-1.5 rounded-full" style={{ background: colors.dot }} />
      {TicketPriorityLabels[priority]}
    </span>
  );
}

/**
 * «پشتیبانی و تیکت‌ها» — صفحه واحد و غنی‌شده برای همه نقش‌ها (کارجو، شرکت، ادمین) طبق فاز
 * «مدیریت پشتیبانی و تیکت‌ها»: ثبت تیکت جدید (موضوع/دپارتمان/اولویت/شرح/ضمیمه فایل) + لیست
 * تیکت‌های خودِ کاربر با تب‌های فیلتر وضعیت (۵ حالت) و جدول اطلاعات.
 */
export default function SupportTicketsPage() {
  const navigate = useNavigate();
  const toast = useToast();

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [subject, setSubject] = useState('');
  const [department, setDepartment] = useState<TicketDepartment>('TechnicalSupport');
  const [priority, setPriority] = useState<TicketPriority>('High');
  const [message, setMessage] = useState('');
  const [attachment, setAttachment] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [activeFilter, setActiveFilter] = useState<FilterKey>('All');
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [createTicket, { isLoading: isCreating }] = useCreateSupportTicketMutation();
  const { data: summaryData } = useGetMySupportTicketStatusSummaryQuery();
  const { data, isLoading } = useGetMySupportTicketsQuery({
    status: activeFilter === 'All' ? undefined : activeFilter,
    page,
    pageSize
  });

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

  const validateAndSetFile = (file: File) => {
    const ext = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      toast.error('فرمت فایل مجاز نیست. فرمت‌های مجاز: JPG، PNG، PDF، DOCX.');
      return;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      toast.error('حداکثر حجم مجاز فایل ۱۰ مگابایت است.');
      return;
    }
    setAttachment(file);
  };

  const handleFileInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) validateAndSetFile(file);
    e.target.value = '';
  };

  const resetForm = () => {
    setSubject('');
    setMessage('');
    setDepartment('TechnicalSupport');
    setPriority('High');
    setAttachment(null);
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (subject.trim().length < 3) {
      toast.error('موضوع تیکت باید حداقل ۳ کاراکتر باشد.');
      return;
    }
    if (message.trim().length < 5) {
      toast.error('شرح پیام باید حداقل ۵ کاراکتر باشد.');
      return;
    }

    const result = await createTicket({
      subject: subject.trim(),
      message: message.trim(),
      department,
      priority,
      attachment: attachment ?? undefined
    });

    if ('data' in result && result.data?.success) {
      resetForm();
      setIsFormOpen(false);
      setPage(1);
      setActiveFilter('All');
    }
  };

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-6" dir="rtl">
      <div>
        <h1 className="flex items-center gap-2 text-xl font-extrabold text-slate-800 sm:text-2xl">
          <FileText className="h-6 w-6 text-emerald-600" />
          پشتیبانی و تیکت‌ها
        </h1>
        <p className="mt-1 text-sm text-slate-500">ارسال درخواست و پیگیری وضعیت تیکت‌های پشتیبانی سامانه.</p>
      </div>

      <div className="rounded-2xl border border-slate-200/80 bg-white">
        <button
          type="button"
          onClick={() => setIsFormOpen((v) => !v)}
          aria-expanded={isFormOpen}
          className="flex w-full items-center justify-between gap-3 px-5 py-3.5 text-right sm:px-6"
        >
          <span className="flex items-center gap-2 text-sm font-bold text-slate-800">
            <FileText className="h-4 w-4 text-emerald-600" />
            ثبت تیکت جدید
          </span>
          <span
            className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors ${
              isFormOpen ? 'bg-slate-100 text-slate-500' : 'bg-emerald-50 text-emerald-700'
            }`}
          >
            {isFormOpen ? 'بستن فرم' : '+ ثبت تیکت جدید'}
            <ChevronDown className={`h-3.5 w-3.5 transition-transform duration-300 ${isFormOpen ? 'rotate-180' : ''}`} />
          </span>
        </button>

        <div className="grid transition-[grid-template-rows] duration-300 ease-in-out" style={{ gridTemplateRows: isFormOpen ? '1fr' : '0fr' }}>
          <div className="overflow-hidden">
            <form onSubmit={handleSubmit} className="border-t border-slate-100 px-5 pb-4 pt-3.5 sm:px-6">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className="mb-1 block text-xs font-semibold text-slate-600">
                    اولویت <span className="text-rose-500">*</span>
                  </label>
                  <div className="flex flex-wrap gap-1.5">
                    {PRIORITY_OPTIONS.map((p) => {
                      const colors = TicketPriorityColors[p];
                      const isActive = priority === p;
                      return (
                        <button
                          key={p}
                          type="button"
                          onClick={() => setPriority(p)}
                          className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold transition-colors"
                          style={
                            isActive
                              ? { background: colors.bg, color: colors.fg, borderColor: colors.dot }
                              : { background: '#fff', color: '#64748b', borderColor: '#e2e8f0' }
                          }
                        >
                          <span className="h-1.5 w-1.5 rounded-full" style={{ background: colors.dot }} />
                          {TicketPriorityLabels[p]}
                        </button>
                      );
                    })}
                  </div>
                </div>

                <div className="sm:col-span-2">
                  <label className="mb-1 block text-xs font-semibold text-slate-600">
                    دپارتمان / دسته‌بندی <span className="text-rose-500">*</span>
                  </label>
                  <div className="relative">
                    <select
                      value={department}
                      onChange={(e) => setDepartment(e.target.value as TicketDepartment)}
                      className="w-full appearance-none rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
                    >
                      {DEPARTMENT_OPTIONS.map((d) => (
                        <option key={d} value={d}>{TicketDepartmentLabels[d]}</option>
                      ))}
                    </select>
                    <ChevronDown className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  </div>
                </div>
              </div>

              <div className="mt-3">
                <label className="mb-1 block text-xs font-semibold text-slate-600">
                  موضوع تیکت <span className="text-rose-500">*</span>
                </label>
                <input
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  maxLength={200}
                  placeholder="موضوع خود را وارد کنید"
                  className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
                />
              </div>

              <div className="mt-3">
                <label className="mb-1 block text-xs font-semibold text-slate-600">
                  شرح پیام و مشکل <span className="text-rose-500">*</span>
                </label>
                <textarea
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  maxLength={4000}
                  rows={3}
                  placeholder="لطفاً جزئیات کامل مشکل یا درخواست خود را شرح دهید..."
                  className="w-full resize-y rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
                />
              </div>

              <div className="mt-3">
                <label className="mb-1 block text-xs font-semibold text-slate-600">ضمیمه فایل یا تصویر</label>
                <div className="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    className="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-3 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-slate-200"
                  >
                    <Paperclip className="h-3.5 w-3.5" />
                    {attachment ? 'تغییر فایل' : 'افزودن ضمیمه'}
                  </button>
                  {attachment && (
                    <span className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700">
                      {attachment.name}
                      <span className="text-emerald-400">({formatFileSize(attachment.size)})</span>
                      <button
                        type="button"
                        onClick={() => setAttachment(null)}
                        className="text-emerald-500 transition-colors hover:text-rose-600"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  )}
                  <span className="text-[10px] text-slate-400">حداکثر ۱۰ مگابایت - JPG, PNG, PDF, DOCX</span>
                </div>
                <input ref={fileInputRef} type="file" accept=".jpg,.jpeg,.png,.pdf,.docx" onChange={handleFileInputChange} className="hidden" />
              </div>

              <button
                type="submit"
                disabled={isCreating}
                className="mt-4 inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-5 py-2 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                {isCreating ? 'در حال ارسال...' : 'ارسال تیکت'}
              </button>
            </form>
          </div>
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200/80 bg-white p-5 sm:p-6">
        <h2 className="mb-4 flex items-center gap-2 text-base font-bold text-slate-800">
          <Headset className="h-[18px] w-[18px] text-emerald-600" />
          لیست تیکت‌های پشتیبانی
        </h2>

        <div className="mb-4 flex flex-wrap gap-2">
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
          <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 py-14 text-center">
            <Inbox className="mb-3 h-9 w-9 text-slate-300" />
            <p className="font-semibold text-slate-600">تیکتی با این فیلتر یافت نشد.</p>
            <p className="mt-1 text-sm text-slate-400">برای ثبت درخواست جدید از فرم بالا استفاده کنید.</p>
          </div>
        )}

        {!isLoading && tickets.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-right text-xs font-semibold text-slate-400">
                  <th className="px-3 py-2">شناسه تیکت</th>
                  <th className="px-3 py-2">موضوع</th>
                  <th className="px-3 py-2">بخش</th>
                  <th className="px-3 py-2">اولویت</th>
                  <th className="px-3 py-2">آخرین به‌روزرسانی</th>
                  <th className="px-3 py-2">وضعیت</th>
                  <th className="px-3 py-2">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr
                    key={ticket.id}
                    onClick={() => navigate(`/support/${ticket.id}`)}
                    className="cursor-pointer border-b border-slate-50 transition-colors hover:bg-slate-50"
                  >
                    <td className="px-3 py-3 font-mono text-xs text-slate-500">#{ticket.ticketNumber}</td>
                    <td className="px-3 py-3 font-medium text-slate-700">{ticket.subject}</td>
                    <td className="px-3 py-3 text-slate-500">{TicketDepartmentLabels[ticket.department]}</td>
                    <td className="px-3 py-3"><PriorityBadge priority={ticket.priority} /></td>
                    <td className="px-3 py-3 text-xs text-slate-400">
                      <PersianDate date={ticket.lastActivityAtUtc} relative />
                    </td>
                    <td className="px-3 py-3"><StatusBadge status={ticket.status} /></td>
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
                ))}
              </tbody>
            </table>
          </div>
        )}

        {totalPages > 1 && (
          <div className="mt-4 flex items-center justify-center gap-2">
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
    </div>
  );
}
