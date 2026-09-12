import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useLocation, useNavigate } from 'react-router-dom';
import { useResetPasswordMutation } from '@/features/auth/authApi';
import { useAppDispatch } from '@/app/hooks';
import { toastAdded } from '@/features/toast/toastSlice';
import AuthShell from '@/components/layout/AuthShell';
import AuthField from '@/components/form/AuthField';
import AnimatedOtpInput, { type OtpStatus } from '@/components/form/AnimatedOtpInput';
import { LockIcon, EyeIcon, EyeOffIcon } from '@/components/icons/AuthIcons';

interface ResetPasswordValues {
  newPassword: string;
  confirmPassword: string;
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

/** بازیابی رمز عبور — مرحلهٔ دوم: تایید کد پیامکی + تعیین رمز عبور جدید (طبق ADR-006). */
export default function ResetPasswordPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const panelRef = useRef<HTMLDivElement>(null);

  const mobileNumber = (location.state as { mobileNumber?: string } | null)?.mobileNumber;

  const [otpCode, setOtpCode] = useState('');
  const [otpStatus, setOtpStatus] = useState<OtpStatus>('idle');
  const [otpResetSignal, setOtpResetSignal] = useState(0);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const { register, handleSubmit, formState } = useForm<ResetPasswordValues>({ mode: 'onBlur' });
  const [resetPassword, { isLoading }] = useResetPasswordMutation();

  if (!mobileNumber) {
    navigate('/forgot-password');
    return null;
  }

  const onSubmit = async (values: ResetPasswordValues) => {
    setServerError(null);

    if (otpCode.length !== 6) {
      setServerError('کد تایید ۶ رقمی را کامل وارد کنید.');
      triggerShake(panelRef.current);
      return;
    }

    if (values.newPassword !== values.confirmPassword) {
      setServerError('رمز عبور و تکرار آن یکسان نیستند.');
      triggerShake(panelRef.current);
      return;
    }

    setOtpStatus('verifying');
    const result = await resetPassword({ mobileNumber, otpCode, newPassword: values.newPassword });

    if ('data' in result && result.data?.success) {
      setOtpStatus('success');
      dispatch(toastAdded('success', 'رمز عبور شما با موفقیت بازنشانی شد. اکنون وارد شوید.'));
      window.setTimeout(() => navigate('/login'), 900);
      return;
    }

    setOtpStatus('error');
    setServerError(extractErrorMessage(result, 'کد تایید نادرست یا منقضی شده است.'));
    triggerShake(panelRef.current);

    window.setTimeout(() => {
      setOtpStatus('idle');
      setOtpCode('');
      setOtpResetSignal((s) => s + 1);
    }, 650);
  };

  return (
    <AuthShell panelRef={panelRef}>
      <div className="auth-panel-content">
        <h1 className="auth-panel__title">تعیین رمز عبور جدید</h1>
        <p className="auth-panel__subtitle">کد ۶ رقمی ارسال‌شده به {mobileNumber} را وارد و رمز عبور جدید را تعیین کنید.</p>

        <AnimatedOtpInput status={otpStatus} onComplete={setOtpCode} resetSignal={otpResetSignal} />

        <form onSubmit={handleSubmit(onSubmit)} noValidate style={{ marginTop: 'var(--space-3)' }}>
          <AuthField
            label="رمز عبور جدید"
            htmlFor="newPassword"
            icon={<LockIcon />}
            error={formState.errors.newPassword?.message}
            trailing={
              <button
                type="button"
                className="auth-ig__eye-btn"
                onClick={() => setShowPassword((v) => !v)}
                aria-label={showPassword ? 'پنهان‌کردن رمز عبور' : 'نمایش رمز عبور'}
              >
                {showPassword ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            }
          >
            <input
              id="newPassword"
              type={showPassword ? 'text' : 'password'}
              autoComplete="new-password"
              {...register('newPassword', {
                required: 'رمز عبور جدید الزامی است.',
                minLength: { value: 8, message: 'رمز عبور باید حداقل ۸ کاراکتر باشد.' }
              })}
            />
          </AuthField>

          <AuthField
            label="تکرار رمز عبور جدید"
            htmlFor="confirmPassword"
            icon={<LockIcon />}
            error={formState.errors.confirmPassword?.message}
            trailing={
              <button
                type="button"
                className="auth-ig__eye-btn"
                onClick={() => setShowConfirmPassword((v) => !v)}
                aria-label={showConfirmPassword ? 'پنهان‌کردن رمز عبور' : 'نمایش رمز عبور'}
              >
                {showConfirmPassword ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            }
          >
            <input
              id="confirmPassword"
              type={showConfirmPassword ? 'text' : 'password'}
              autoComplete="new-password"
              {...register('confirmPassword', { required: 'تکرار رمز عبور الزامی است.' })}
            />
          </AuthField>

          {serverError && <p className="error-text">{serverError}</p>}

          <button type="submit" className={`btn-primary${isLoading ? ' is-loading' : ''}`} disabled={isLoading}>
            بازنشانی رمز عبور
          </button>
        </form>
      </div>
    </AuthShell>
  );
}
