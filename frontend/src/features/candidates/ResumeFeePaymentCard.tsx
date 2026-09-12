import { useState } from 'react';
import { Wallet } from 'lucide-react';
import { useSubmitResumeFeePaymentMutation } from '@/features/candidates/candidateApi';

const toToman = (rials: number) => Math.round(rials / 10).toLocaleString('fa-IR');

/**
 * طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: جریان پرداخت هزینه
 * نهایی‌سازی رزومه اکنون مستقیماً داخل صفحات جدید (CandidateProfileView/CandidateProfileEdit)
 * قرار دارد، نه پشت یک لینک به مسیر قدیمی `/resume`. این کامپوننت مشترک همان منطق شروع
 * پرداخت (submitResumeFeePayment → هدایت به درگاه زرین‌پال) را که پیش‌تر فقط در
 * ResumeFormPage.tsx وجود داشت، اینجا بازپیاده‌سازی می‌کند تا هر دو صفحه جدید بدون تکرار کد
 * از آن استفاده کنند. بازگشت از درگاه (موفق/ناموفق) توسط PaymentResultPage.tsx به همین صفحات
 * (`/resume/view` یا `/resume/edit`) هدایت می‌شود، نه به `/resume`.
 */
export default function ResumeFeePaymentCard({ amountInRials }: { amountInRials: number }) {
  const [submitResumeFeePayment, { isLoading }] = useSubmitResumeFeePaymentMutation();
  const [error, setError] = useState<string | null>(null);

  const handlePay = async () => {
    setError(null);
    const result = await submitResumeFeePayment();

    if ('data' in result && result.data?.success) {
      const redirectUrl = result.data.data?.paymentRedirectUrl;
      if (redirectUrl) {
        window.location.href = redirectUrl;
        return;
      }
      setError('رزومه شما پیش‌تر نهایی شده است.');
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setError(errorResponse?.data?.message ?? 'شروع فرآیند پرداخت با خطا مواجه شد.');
  };

  return (
    <div className="print:hidden flex flex-col gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-5 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-start gap-3">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white text-amber-600 shadow-sm">
          <Wallet size={18} />
        </span>
        <div>
          <h2 className="m-0 mb-1 text-sm font-bold text-amber-900">پرداخت هزینه نهایی‌سازی و فعال‌سازی رزومه</h2>
          <p className="m-0 text-xs leading-6 text-amber-800">
            رزومه شما به‌صورت پیش‌نویس ذخیره شده؛ تا پرداخت هزینه ساخت رزومه (<strong>{toToman(amountInRials)} تومان</strong>) قابل
            دانلود یا ارسال برای آگهی‌ها نیست. این هزینه فقط یک‌بار است؛ ویرایش‌های بعدی رایگان خواهد بود.
          </p>
          {error && <p className="m-0 mt-2 text-xs font-semibold text-rose-600">{error}</p>}
        </div>
      </div>
      <button
        type="button"
        onClick={handlePay}
        disabled={isLoading}
        className="inline-flex shrink-0 items-center justify-center gap-1.5 whitespace-nowrap rounded-xl bg-amber-600 px-4 py-2.5 text-xs font-semibold text-white disabled:opacity-60 hover:bg-amber-700"
      >
        {isLoading ? 'در حال انتقال به درگاه پرداخت...' : `پرداخت ${toToman(amountInRials)} تومان و فعال‌سازی`}
      </button>
    </div>
  );
}
