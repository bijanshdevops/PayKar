import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useRequestOtpMutation } from '@/features/auth/authApi';
import AuthShell from '@/components/layout/AuthShell';
import AuthField from '@/components/form/AuthField';
import { PhoneIcon } from '@/components/icons/AuthIcons';

interface ForgotPasswordValues {
  mobileNumber: string;
}

function triggerShake(el: HTMLElement | null) {
  if (!el) return;
  el.classList.remove('auth-shell__panel--shake');
  void el.offsetWidth;
  el.classList.add('auth-shell__panel--shake');
}

function extractErrorMessage(result: unknown, fallback: string): string {
  if (result && typeof result === 'object' && 'error' in result) {
    const err = (result as { error?: { data?: { message?: string } } }).error;
    if (err?.data?.message) return err.data.message;
  }
  return fallback;
}

/** درخواست کد بازیابی رمز عبور — مرحلهٔ اول (طبق ADR-006، از همان اندپوینت otp/request استفاده می‌شود). */
export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const panelRef = useRef<HTMLDivElement>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const { register, handleSubmit, formState } = useForm<ForgotPasswordValues>({ mode: 'onBlur' });
  const [requestOtp, { isLoading }] = useRequestOtpMutation();

  const onSubmit = async (values: ForgotPasswordValues) => {
    setServerError(null);
    const result = await requestOtp({ mobileNumber: values.mobileNumber });

    if ('data' in result && result.data?.success) {
      navigate('/reset-password', { state: { mobileNumber: values.mobileNumber } });
      return;
    }

    setServerError(extractErrorMessage(result, 'ارسال کد تایید با خطا مواجه شد.'));
    triggerShake(panelRef.current);
  };

  return (
    <AuthShell panelRef={panelRef}>
      <div className="auth-panel-content">
        <h1 className="auth-panel__title">بازیابی رمز عبور</h1>
        <p className="auth-panel__subtitle">
          شماره موبایل ثبت‌شده در حساب خود را وارد کنید تا کد تایید برایتان پیامک شود.
        </p>

        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <AuthField label="شماره موبایل" htmlFor="mobileNumber" icon={<PhoneIcon />} error={formState.errors.mobileNumber?.message}>
            <input
              id="mobileNumber"
              type="tel"
              placeholder="09121234567"
              autoComplete="tel"
              {...register('mobileNumber', {
                required: 'شماره موبایل الزامی است.',
                pattern: { value: /^09\d{9}$/, message: 'شماره موبایل معتبر نیست.' }
              })}
            />
          </AuthField>

          {serverError && <p className="error-text">{serverError}</p>}

          <button type="submit" className={`btn-primary${isLoading ? ' is-loading' : ''}`} disabled={isLoading}>
            دریافت کد تایید
          </button>
        </form>

        <div className="auth-panel__footer-link">
          <Link to="/login">بازگشت به صفحه ورود</Link>
        </div>
      </div>
    </AuthShell>
  );
}
