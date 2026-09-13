import { useMemo } from "react";
import DatePicker from "react-multi-date-picker";
import type DateObject from "react-date-object";
import persian from "react-date-object/calendars/persian";
import persian_fa from "react-date-object/locales/persian_fa";
import TimePicker from "react-multi-date-picker/plugins/time_picker";
import { jalaliToUtcIso } from "@/shared/utils/date";

export interface PersianDatePickerProps {
  /** مقدار فعلی به‌صورت رشته‌ی ISO 8601 UTC (همان چیزی که از react-hook-form می‌آید) یا null. */
  value: string | null | undefined;
  /** همیشه رشته‌ی ISO 8601 UTC (یا null در صورت پاک‌شدن) برمی‌گرداند — هرگز رشته‌ی شمسی. */
  onChange: (isoUtc: string | null) => void;
  onBlur?: () => void;
  name?: string;
  id?: string;
  placeholder?: string;
  disabled?: boolean;
  /** حالت نمایش خطای ولیدیشن (کادر قرمز، هم‌راستا با بقیه‌ی فرم‌های پروژه). */
  hasError?: boolean;
  /** برای فیلدهایی مثل «زمان مصاحبه» که علاوه بر تاریخ به ساعت هم نیاز دارند. */
  showTime?: boolean;
  minDate?: string | Date;
  maxDate?: string | Date;
  className?: string;
}

/**
 * ورودی تاریخ شمسی (تقویم جلالی) با ارقام و ماه‌های فارسی — هم‌راستا با تم پروژه
 * (Navy #19263A / Amber #F59E0B). مستقیماً با react-hook-form از طریق Controller
 * یکپارچه می‌شود:
 *
 * <Controller
 *   name="applicationDeadlineUtc"
 *   control={control}
 *   render={({ field }) => (
 *     <PersianDatePicker value={field.value} onChange={field.onChange} />
 *   )}
 * />
 *
 * ورودی/خروجی این کامپوننت همیشه ISO 8601 UTC است؛ تبدیل شمسی↔میلادی به‌صورت
 * داخلی و شفاف انجام می‌شود و هرگز رشته‌ی شمسی به بیرون (و از آنجا به API) درز نمی‌کند.
 */
export default function PersianDatePicker({
  value,
  onChange,
  onBlur,
  name,
  id,
  placeholder = "انتخاب تاریخ",
  disabled,
  hasError,
  showTime = false,
  minDate,
  maxDate,
  className,
}: PersianDatePickerProps) {
  const dateValue = useMemo(
    () => (value ? new Date(value) : undefined),
    [value],
  );
  const min = useMemo(
    () => (minDate ? new Date(minDate) : undefined),
    [minDate],
  );
  const max = useMemo(
    () => (maxDate ? new Date(maxDate) : undefined),
    [maxDate],
  );

  const inputClass = [
    "w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm text-[#19263A] outline-none transition",
    "placeholder:text-slate-400",
    hasError
      ? "border-[#F43F5E] focus:border-[#F43F5E] focus:ring-2 focus:ring-[#F43F5E]/20"
      : "border-slate-200 focus:border-[#F59E0B] focus:ring-2 focus:ring-[#F59E0B]/20",
    disabled ? "cursor-not-allowed bg-slate-50 text-slate-400" : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <DatePicker
      value={dateValue}
      onChange={(selected: DateObject | null) =>
        onChange(jalaliToUtcIso(selected))
      }
      calendar={persian}
      locale={persian_fa}
      calendarPosition="bottom-right"
      inputClass={inputClass}
      containerClassName="persian-date-picker w-full"
      className="persian-date-picker-panel"
      format={showTime ? "YYYY/MM/DD HH:mm" : "YYYY/MM/DD"}
      plugins={showTime ? [<TimePicker key="time-picker" hideSeconds />] : []}
      name={name}
      id={id}
      placeholder={placeholder}
      disabled={disabled}
      editable={false}
      minDate={min}
      maxDate={max}
      onClose={() => {
        onBlur?.();
        return undefined;
      }}
    />
  );
}
