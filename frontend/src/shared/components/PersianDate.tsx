import { formatRelativeJalali, formatToJalali, type DateInput } from '@/shared/utils/date';

export interface PersianDateProps {
  /** تاریخ ورودی — رشته‌ی ISO 8601 UTC آمده از بک‌اند (یا Date/عدد). */
  date: DateInput;
  /** توکن فرمت date-fns (نادیده گرفته می‌شود اگر `relative` فعال باشد). پیش‌فرض: `yyyy/MM/dd`. */
  format?: string;
  /** به‌جای تاریخ مطلق، زمان نسبی نمایش بده («۵ دقیقه پیش»، «دیروز»، ...). */
  relative?: boolean;
  className?: string;
  /** متن جایگزین برای تاریخ نامعتبر/خالی (پیش‌فرض «—»). */
  fallback?: string;
}

/**
 * کامپوننت سبک نمایش تاریخ شمسی برای جدول‌ها، کارت‌ها و متاتگ‌ها.
 *
 * <PersianDate date={jobAd.publishedAtUtc} />
 * <PersianDate date={ticket.lastActivityAtUtc} relative />
 * <PersianDate date={jobAd.applicationDeadlineUtc} format="d MMMM yyyy" />
 */
export default function PersianDate({ date, format, relative, className, fallback = '—' }: PersianDateProps) {
  const text = relative ? formatRelativeJalali(date) : formatToJalali(date, format);
  const display = text || fallback;

  if (!date) {
    return <span className={className}>{fallback}</span>;
  }

  const isoForTitle = typeof date === 'string' ? date : date instanceof Date ? date.toISOString() : undefined;

  return (
    <time dateTime={isoForTitle} className={className} title={relative ? formatToJalali(date, 'yyyy/MM/dd - HH:mm') : undefined}>
      {display}
    </time>
  );
}
