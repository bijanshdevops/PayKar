/**
 * لایه کمکی تاریخ شمسی (Jalali) — تک‌نقطه‌ی تبدیل بین دنیای بک‌اند (ISO 8601 UTC)
 * و دنیای نمایش/ورودی کاربر (تقویم جلالی با ارقام فارسی).
 *
 * خط قرمز معماری: بک‌اند (.NET 9) فقط و فقط ISO 8601 UTC می‌فرستد و می‌گیرد.
 * هیچ استرینگ شمسی نباید مستقیماً به عنوان تاریخ به API ارسال شود — همیشه از
 * `jalaliToUtcIso` برای تبدیل قبل از ارسال در Payload استفاده کنید.
 */
import {
  differenceInCalendarDays,
  format as formatJalali,
  formatDistanceToNowStrict,
  isValid,
  isToday,
  isYesterday,
} from 'date-fns-jalali';
import { faIR } from 'date-fns-jalali/locale';
import type DateObject from 'react-date-object';

/** هر مقداری که ممکن است به‌عنوان «تاریخ» از جایی در اپ به این توابع برسد. */
export type DateInput = string | number | Date | null | undefined;

const PERSIAN_DIGITS = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'] as const;

/** جایگزینی ارقام لاتین یک رشته با ارقام فارسی (هم‌راستا با الگوی toLocaleString('fa-IR') در کل پروژه). */
export function toPersianDigits(value: string | number): string {
  return String(value).replace(/[0-9]/g, (digit) => PERSIAN_DIGITS[Number(digit)]);
}

/** تبدیل امن ورودی به یک شیء Date معتبر؛ در صورت نامعتبر بودن null برمی‌گرداند. */
function toSafeDate(date: DateInput): Date | null {
  if (date === null || date === undefined || date === '') return null;
  const parsed = date instanceof Date ? date : new Date(date);
  return isValid(parsed) ? parsed : null;
}

/**
 * تبدیل تاریخ ISO UTC (یا هر ورودی قابل‌قبول Date) به رشته‌ی تاریخ شمسی با ارقام فارسی.
 *
 * @param date تاریخ ورودی — ترجیحاً رشته‌ی ISO 8601 UTC آمده از بک‌اند.
 * @param formatStr توکن‌های date-fns (پیش‌فرض: `yyyy/MM/dd`). برای نمایش توصیفی از `d MMMM yyyy` استفاده کنید.
 * @returns رشته‌ی شمسی با ارقام فارسی، یا «—» در صورت نامعتبر بودن تاریخ.
 */
export function formatToJalali(date: DateInput, formatStr: string = 'yyyy/MM/dd'): string {
  const safe = toSafeDate(date);
  if (!safe) return '—';
  return toPersianDigits(formatJalali(safe, formatStr, { locale: faIR }));
}

/**
 * تبدیل تاریخ به همراه ساعت (مثلاً برای لاگ‌ها و پیام‌ها) — پیش‌فرض: `yyyy/MM/dd - HH:mm`.
 */
export function formatToJalaliDateTime(date: DateInput, formatStr: string = 'yyyy/MM/dd - HH:mm'): string {
  return formatToJalali(date, formatStr);
}

/**
 * نمایش زمان نسبی به فارسی (مثلاً «۵ دقیقه پیش»، «دیروز»، «امروز - ۱۴:۳۰»).
 * برای بازه‌های خیلی نزدیک از فاصله‌ی نسبی و برای دیروز/امروز از برچسب‌های آشنا استفاده می‌کند.
 */
export function formatRelativeJalali(date: DateInput): string {
  const safe = toSafeDate(date);
  if (!safe) return '—';

  const now = new Date();

  if (isToday(safe)) {
    return `امروز - ${toPersianDigits(formatJalali(safe, 'HH:mm', { locale: faIR }))}`;
  }

  if (isYesterday(safe)) {
    return `دیروز - ${toPersianDigits(formatJalali(safe, 'HH:mm', { locale: faIR }))}`;
  }

  const dayDiff = Math.abs(differenceInCalendarDays(now, safe));
  if (dayDiff > 30) {
    // برای گذشته‌ی دورتر، تاریخ دقیق شمسی خواناتر از «چند ماه پیش» است.
    return formatToJalali(safe, 'yyyy/MM/dd');
  }

  return toPersianDigits(
    formatDistanceToNowStrict(safe, { addSuffix: true, locale: faIR })
  );
}

/**
 * تبدیل مقدار انتخاب‌شده در PersianDatePicker (شیء DateObject از react-date-object، یا
 * Date/رشته‌ی معمولی) به رشته‌ی استاندارد ISO 8601 UTC جهت ارسال در Payload درخواست‌های API.
 *
 * @returns رشته‌ی ISO UTC، یا null اگر ورودی خالی/نامعتبر باشد.
 */
export function jalaliToUtcIso(jalaliDate: DateObject | Date | string | null | undefined): string | null {
  if (jalaliDate === null || jalaliDate === undefined || jalaliDate === '') return null;

  // خروجی react-multi-date-picker معمولاً یک DateObject است که متد toDate() دارد
  // و معادل میلادیِ لحظه‌ی انتخاب‌شده را به‌صورت یک Date واقعی جاوااسکریپت برمی‌گرداند.
  const maybeDateObject = jalaliDate as { toDate?: () => Date };
  const native =
    typeof maybeDateObject?.toDate === 'function'
      ? maybeDateObject.toDate()
      : jalaliDate instanceof Date
        ? jalaliDate
        : new Date(jalaliDate as string);

  return isValid(native) ? native.toISOString() : null;
}
