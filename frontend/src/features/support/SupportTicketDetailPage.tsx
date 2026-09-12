import { useRef, useState, type ChangeEvent, type FormEvent, type ReactNode } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  ArrowRight,
  Building2,
  Calendar,
  Clock,
  Copy,
  Download,
  Hash,
  Loader2,
  Lock,
  Paperclip,
  Phone,
  RefreshCcw,
  Send,
  UserRound,
  X
} from 'lucide-react';
import { useAppSelector } from '@/app/hooks';
import {
  useAddSupportMessageMutation,
  useCloseSupportTicketMutation,
  useGetSupportTicketByIdQuery,
  useReopenSupportTicketMutation,
  useUpdateSupportTicketStatusMutation,
  type SupportTicketStatus
} from '@/features/support/supportApi';
import {
  SupportTicketStatusColors,
  SupportTicketStatusLabels,
  TicketDepartmentLabels,
  TicketPriorityColors,
  TicketPriorityLabels
} from '@/features/support/SupportTicketStatusLabels';
import { useToast } from '@/features/toast/useToast';

const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.pdf', '.docx'];
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

/** طبق SupportTicket.AllowedTransitions در بک‌اند — تا گزینه‌های نامعتبر اصلاً در دراپ‌داون Admin پیشنهاد نشوند. */
const ALLOWED_STATUS_TRANSITIONS: Record<SupportTicketStatus, SupportTicketStatus[]> = {
  PendingResponse: ['InProgress', 'Answered', 'Closed'],
  InProgress: ['Answered', 'Closed'],
  Answered: ['Closed', 'Reopened', 'PendingResponse'],
  Closed: ['Reopened'],
  Reopened: ['InProgress', 'Answered', 'Closed']
};

function StatusPill({ status }: { status: SupportTicketStatus }) {
  const colors = SupportTicketStatusColors[status];
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold" style={{ background: colors.bg, color: colors.fg }}>
      <span className="h-1.5 w-1.5 rounded-full" style={{ background: colors.dot }} />
      {SupportTicketStatusLabels[status]}
    </span>
  );
}

function PriorityPill({ priority }: { priority: 'Low' | 'Medium' | 'High' }) {
  const colors = TicketPriorityColors[priority];
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-semibold" style={{ background: colors.bg, color: colors.fg }}>
      <span className="h-1.5 w-1.5 rounded-full" style={{ background: colors.dot }} />
      {TicketPriorityLabels[priority]}
    </span>
  );
}

function InfoRow({ icon: Icon, label, children }: { icon: typeof Hash; label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-2 text-xs">
      <span className="flex items-center gap-1.5 text-slate-400">
        <Icon className="h-3.5 w-3.5" />
        {label}
      </span>
      <span className="font-semibold text-slate-700">{children}</span>
    </div>
  );
}

/**
 * جزئیات یک تیکت پشتیبانی به‌صورت رشته گفتگو (Thread) — قابل استفاده هم برای کاربر
 * صاحب تیکت و هم برای تیم پشتیبانی (Admin)، طبق ADR-007. به‌روزرسانی با Polling ساده.
 * گسترش‌یافته در فاز «مدیریت پشتیبانی و تیکت‌ها»: کارت عملیات نقش‌محور (دراپ‌داون فقط برای Admin،
 * دکمه بستن/بازگشایی برای کاربر عادی)، بازگشایی/فعال‌سازی خودکار با ارسال پیام، طراحی مطابق موکاپ.
 */
export default function SupportTicketDetailPage() {
  const { ticketId } = useParams<{ ticketId: string }>();
  const navigate = useNavigate();
  const { user } = useAppSelector((state) => state.auth);
  const isAdmin = user?.roles.includes('Admin') ?? false;

  const { data, isLoading } = useGetSupportTicketByIdQuery(ticketId ?? '', {
    skip: !ticketId,
    pollingInterval: 15000
  });
  const [addMessage, { isLoading: isSending }] = useAddSupportMessageMutation();
  const [updateStatus] = useUpdateSupportTicketStatusMutation();
  const [reopenTicket, { isLoading: isReopening }] = useReopenSupportTicketMutation();
  const [closeTicket, { isLoading: isClosing }] = useCloseSupportTicketMutation();
  const toast = useToast();

  const [body, setBody] = useState('');
  const [attachment, setAttachment] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const ticket = data?.data;

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;

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

  const handleSend = async (event: FormEvent) => {
    event.preventDefault();
    if (!ticketId) return;

    if (body.trim().length < 1) {
      toast.error('متن پیام نمی‌تواند خالی باشد.');
      return;
    }

    const result = await addMessage({ ticketId, body: body.trim(), attachment: attachment ?? undefined });
    if ('data' in result && result.data?.success) {
      setBody('');
      setAttachment(null);
    }
  };

  const handleStatusChange = async (newStatus: SupportTicketStatus) => {
    if (!ticketId) return;
    await updateStatus({ ticketId, newStatus });
  };

  const handleReopen = async () => {
    if (!ticketId) return;
    const result = await reopenTicket(ticketId);
    if ('data' in result && result.data?.success) {
      toast.success('تیکت دوباره باز شد. می‌توانید پیام جدید ارسال کنید.');
    } else {
      const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
      toast.error(errorResponse?.data?.message ?? 'بازگشایی تیکت با خطا مواجه شد.');
    }
  };

  const handleClose = async () => {
    if (!ticketId) return;
    const result = await closeTicket(ticketId);
    if ('data' in result && result.data?.success) {
      toast.success('تیکت بسته شد.');
    } else {
      const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
      toast.error(errorResponse?.data?.message ?? 'بستن تیکت با خطا مواجه شد.');
    }
  };

  const handleCopyTicketNumber = async () => {
    if (!ticket) return;
    try {
      await navigator.clipboard.writeText(ticket.ticketNumber);
      toast.success('شماره تیکت کپی شد.');
    } catch {
      // کپی خودکار روی برخی مرورگرها/بستر‌ها ممکن است مجاز نباشد — بی‌صدا نادیده گرفته می‌شود.
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl py-10 text-center text-sm text-slate-400" dir="rtl">
        در حال بارگذاری...
      </div>
    );
  }

  if (!ticket) {
    return (
      <div className="mx-auto flex max-w-6xl flex-col items-center gap-3 py-10 text-center" dir="rtl">
        <p className="text-sm text-slate-500">این تیکت یافت نشد یا شما به آن دسترسی ندارید.</p>
        <button
          onClick={() => navigate(-1)}
          className="inline-flex items-center gap-1.5 rounded-xl bg-white px-3.5 py-2 text-xs font-semibold text-slate-600 shadow-sm ring-1 ring-slate-200 transition-colors hover:bg-slate-50"
        >
          بازگشت
        </button>
      </div>
    );
  }

  const statusOptions = ALLOWED_STATUS_TRANSITIONS[ticket.status];
  const composerBlockedForAdmin = isAdmin && ticket.status === 'Closed';
  const reactivationHint =
    !isAdmin && ticket.status === 'Closed'
      ? 'با ارسال پیام، تیکت به‌صورت خودکار بازگشایی می‌شود.'
      : !isAdmin && ticket.status === 'Answered'
        ? 'با ارسال پیام، تیکت به وضعیت «در انتظار پاسخ» بازمی‌گردد.'
        : null;

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-4" dir="rtl">
      <button
        onClick={() => navigate(isAdmin ? '/admin/support-tickets' : '/support')}
        className="inline-flex w-fit items-center gap-1.5 rounded-xl bg-white px-3.5 py-2 text-xs font-semibold text-slate-600 shadow-sm ring-1 ring-slate-200 transition-colors hover:bg-slate-50"
      >
        <ArrowRight className="h-3.5 w-3.5" />
        بازگشت به لیست تیکت‌ها
      </button>

      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200/80 bg-white px-5 py-4 sm:px-6">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-lg font-extrabold text-slate-800 sm:text-xl">{ticket.subject}</h1>
          <span className="font-mono text-xs text-slate-400">#{ticket.ticketNumber}</span>
        </div>
        <div className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
          <PriorityPill priority={ticket.priority} />
          <span>
            دپارتمان: <span className="font-semibold text-slate-600">{TicketDepartmentLabels[ticket.department]}</span>
          </span>
          <span>وضعیت:</span>
          <StatusPill status={ticket.status} />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-[300px_1fr]">
        <div className="flex flex-col gap-4">
          <div className="rounded-2xl border border-slate-200/80 bg-white p-4">
            <h2 className="mb-3 text-sm font-bold text-slate-800">عملیات تیکت</h2>

            {isAdmin ? (
              <div>
                <label className="mb-1.5 block text-xs font-semibold text-slate-600">تغییر وضعیت</label>
                <select
                  value=""
                  onChange={(e) => {
                    if (e.target.value) handleStatusChange(e.target.value as SupportTicketStatus);
                  }}
                  disabled={statusOptions.length === 0}
                  className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none disabled:bg-slate-50 disabled:text-slate-400"
                >
                  <option value="">انتخاب وضعیت جدید...</option>
                  {statusOptions.map((s) => (
                    <option key={s} value={s}>{SupportTicketStatusLabels[s]}</option>
                  ))}
                </select>
              </div>
            ) : ticket.status === 'Closed' ? (
              <button
                onClick={handleReopen}
                disabled={isReopening}
                className="flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <RefreshCcw className="h-4 w-4" />
                {isReopening ? 'در حال بازگشایی...' : 'بازگشایی تیکت'}
              </button>
            ) : (
              <button
                onClick={handleClose}
                disabled={isClosing}
                className="flex w-full items-center justify-center gap-2 rounded-xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-sm font-bold text-rose-600 transition-colors hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60"
              >
                <Lock className="h-4 w-4" />
                {isClosing ? 'در حال بستن...' : 'بستن تیکت'}
              </button>
            )}
          </div>

          <div className="rounded-2xl border border-slate-200/80 bg-white p-4">
            <h2 className="mb-3 text-sm font-bold text-slate-800">اطلاعات تیکت</h2>
            <div className="flex flex-col gap-3">
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="flex items-center gap-1.5 text-slate-400">
                  <Hash className="h-3.5 w-3.5" />
                  شماره تیکت
                </span>
                <button
                  onClick={handleCopyTicketNumber}
                  className="inline-flex items-center gap-1 font-mono font-semibold text-slate-700 transition-colors hover:text-emerald-600"
                  title="کپی شماره تیکت"
                >
                  #{ticket.ticketNumber}
                  <Copy className="h-3 w-3" />
                </button>
              </div>
              <InfoRow icon={Building2} label="دپارتمان">{TicketDepartmentLabels[ticket.department]}</InfoRow>
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="text-slate-400">وضعیت</span>
                <StatusPill status={ticket.status} />
              </div>
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="text-slate-400">اولویت</span>
                <PriorityPill priority={ticket.priority} />
              </div>
              <InfoRow icon={Calendar} label="تاریخ ایجاد">
                {new Date(ticket.createdAtUtc).toLocaleDateString('fa-IR')}
              </InfoRow>
              <InfoRow icon={Clock} label="آخرین بروزرسانی">
                {new Date(ticket.lastActivityAtUtc).toLocaleString('fa-IR', { dateStyle: 'short', timeStyle: 'short' })}
              </InfoRow>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200/80 bg-white p-4">
            <h2 className="mb-3 text-sm font-bold text-slate-800">کاربر ثبت‌کننده</h2>
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <UserRound className="h-5 w-5" />
              </span>
              <div>
                <p className="text-sm font-semibold text-slate-700">صاحب تیکت</p>
                {ticket.requesterMobileNumber ? (
                  <p className="mt-0.5 flex items-center gap-1 text-xs text-slate-400">
                    <Phone className="h-3 w-3" />
                    {ticket.requesterMobileNumber}
                  </p>
                ) : (
                  <p className="mt-0.5 text-xs text-slate-400">شماره تماس ثبت نشده</p>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="flex flex-col rounded-2xl border border-slate-200/80 bg-white">
          <div className="flex max-h-[520px] flex-col gap-3 overflow-y-auto p-4 sm:p-5">
            {ticket.messages.map((message) =>
              message.isFromSupportTeam ? (
                <div key={message.id} className="flex justify-start" dir="ltr">
                  <div className="flex max-w-[80%] items-end gap-2">
                    <span className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <UserRound className="h-4 w-4" />
                    </span>
                    <div dir="rtl" className="rounded-2xl rounded-br-none border border-slate-200 bg-white px-4 py-3">
                      <p className="mb-1 text-xs font-bold text-slate-700">کارشناس پشتیبانی</p>
                      <p className="whitespace-pre-wrap text-sm text-slate-700">{message.body}</p>
                      {message.attachmentUrl && (
                        <a
                          href={message.attachmentUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="mt-2 inline-flex items-center gap-1.5 rounded-lg bg-slate-50 px-2.5 py-1.5 text-xs font-medium text-emerald-700 transition-colors hover:bg-slate-100"
                        >
                          <Paperclip className="h-3.5 w-3.5" />
                          {message.attachmentFileName ?? 'دانلود ضمیمه'}
                          <Download className="h-3 w-3" />
                        </a>
                      )}
                      <p className="mt-1.5 text-[11px] text-slate-400">
                        {new Date(message.createdAtUtc).toLocaleString('fa-IR', { dateStyle: 'short', timeStyle: 'short' })}
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                <div key={message.id} className="flex justify-start">
                  <div className="flex max-w-[80%] items-end gap-2">
                    <span className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
                      <UserRound className="h-4 w-4" />
                    </span>
                    <div className="rounded-2xl rounded-bl-none bg-emerald-50 px-4 py-3">
                      <p className="mb-1 text-xs font-bold text-slate-700">کاربر</p>
                      <p className="whitespace-pre-wrap text-sm text-slate-700">{message.body}</p>
                      {message.attachmentUrl && (
                        <a
                          href={message.attachmentUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="mt-2 inline-flex items-center gap-1.5 rounded-lg bg-white/70 px-2.5 py-1.5 text-xs font-medium text-emerald-700 transition-colors hover:bg-white"
                        >
                          <Paperclip className="h-3.5 w-3.5" />
                          {message.attachmentFileName ?? 'دانلود ضمیمه'}
                          <Download className="h-3 w-3" />
                        </a>
                      )}
                      <p className="mt-1.5 text-[11px] text-slate-400">
                        {new Date(message.createdAtUtc).toLocaleString('fa-IR', { dateStyle: 'short', timeStyle: 'short' })}
                      </p>
                    </div>
                  </div>
                </div>
              )
            )}
          </div>

          <div className="border-t border-slate-100 p-4 sm:p-5">
            {composerBlockedForAdmin ? (
              <p className="rounded-xl bg-slate-50 px-4 py-3 text-center text-xs text-slate-500">
                این تیکت بسته شده است. برای پاسخ، ابتدا وضعیت را از بخش «عملیات تیکت» تغییر دهید.
              </p>
            ) : (
              <form onSubmit={handleSend} className="flex flex-col gap-2">
                <div className="flex items-end gap-2">
                  <textarea
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    maxLength={4000}
                    rows={2}
                    placeholder="متن پیام خود را اینجا بنویسید..."
                    className="flex-1 resize-y rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
                  />
                  <button
                    type="submit"
                    disabled={isSending}
                    className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isSending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                    {isSending ? 'در حال ارسال...' : 'ارسال پیام'}
                  </button>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    className="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-3 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-slate-200"
                  >
                    <Paperclip className="h-3.5 w-3.5" />
                    {attachment ? 'تغییر ضمیمه' : 'افزودن ضمیمه'}
                  </button>
                  {attachment && (
                    <span className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-50 px-2.5 py-1 text-[11px] font-medium text-emerald-700">
                      {attachment.name}
                      <button type="button" onClick={() => setAttachment(null)} className="text-emerald-500 transition-colors hover:text-rose-600">
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  )}
                  <input ref={fileInputRef} type="file" accept=".jpg,.jpeg,.png,.pdf,.docx" onChange={handleFileChange} className="hidden" />
                </div>

                {reactivationHint && (
                  <p className="rounded-lg bg-amber-50 px-3 py-1.5 text-[11px] font-medium text-amber-700">💡 {reactivationHint}</p>
                )}
              </form>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
