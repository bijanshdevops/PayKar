import { Link, useSearchParams } from 'react-router-dom';

/**
 * صفحه نمایش نتیجه پرداخت زرین‌پال — طبق docs/frontend/Tasks.md فاز ۵.
 * مرورگر کاربر توسط Api Callback Endpoint به این مسیر با کوئری‌استرینگ
 * success/refId/message هدایت می‌شود.
 */
export default function PaymentResultPage() {
  const [searchParams] = useSearchParams();
  const success = searchParams.get('success') === 'true';
  const refId = searchParams.get('refId');
  const message = searchParams.get('message');
  const bannerAdId = searchParams.get('bannerAdId');
  const candidateId = searchParams.get('candidateId');
  const isBannerPurchase = !!bannerAdId && bannerAdId !== '' && bannerAdId !== 'null';
  // طبق ADR-013 — پرداخت هزینه رزومه‌ساز.
  const isResumeFeePayment = !!candidateId && candidateId !== '' && candidateId !== 'null';

  // طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: چه پرداخت موفق باشد چه
  // ناموفق، کاربر به نمای حرفه‌ای رزومه (CandidateProfileView) بازمی‌گردد، نه به `/resume` قدیمی.
  // خود دکمهٔ «تلاش مجدد برای پرداخت» اکنون مستقیماً همان‌جا (ResumeFeePaymentCard) قرار دارد.
  const backLink = isBannerPurchase
    ? '/company/banner-ads'
    : isResumeFeePayment
      ? '/resume/view'
      : '/company/job-ads';
  const backLabel = isBannerPurchase
    ? 'بازگشت به بنرهای تبلیغاتی من'
    : isResumeFeePayment
      ? 'بازگشت به رزومه من'
      : 'بازگشت به آگهی‌های من';

  return (
    <div className="container" style={{ maxWidth: '480px', margin: '3rem auto', textAlign: 'center' }}>
      <div className="card">
        {success ? (
          <>
            <h1 style={{ color: 'var(--color-success, #16a34a)' }}>پرداخت با موفقیت انجام شد</h1>
            <p>
              {isBannerPurchase
                ? 'هزینه بنر تبلیغاتی پرداخت شد و بنر شما برای بررسی و تایید نهایی به تیم ما ارسال شد.'
                : isResumeFeePayment
                  ? 'هزینه ساخت رزومه پرداخت شد و رزومه شما نهایی شد.'
                  : 'هزینه ثبت آگهی پرداخت شد و آگهی شما برای بررسی و تایید نهایی به تیم ما ارسال شد.'}
            </p>
            {refId && <p style={{ color: 'var(--color-muted)' }}>کد پیگیری: {refId}</p>}
          </>
        ) : (
          <>
            <h1 style={{ color: 'var(--color-danger)' }}>پرداخت ناموفق بود</h1>
            <p>{message || 'در پردازش پرداخت خطایی رخ داد.'}</p>
          </>
        )}

        <Link to={backLink} className="btn-primary" style={{ textDecoration: 'none', display: 'inline-block', marginTop: '1.5rem' }}>
          {backLabel}
        </Link>
      </div>
    </div>
  );
}
