import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useVerifyOtpMutation, useRequestOtpMutation } from '@/features/auth/authApi';
import { useAppDispatch } from '@/app/hooks';
import { sessionEstablished } from '@/features/auth/authSlice';
import AuthShell from '@/components/layout/AuthShell';
import AnimatedOtpInput, { type OtpStatus } from '@/components/form/AnimatedOtpInput';

const DEFAULT_OTP_EXPIRY_SECONDS = 120;

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

function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export default function VerifyOtpPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const panelRef = useRef<HTMLDivElement>(null);

  const state = location.state as
    | { mobileNumber?: string; purpose?: 'login' | 'register'; expiresInSeconds?: number }
    | null;
  const mobileNumber = state?.mobileNumber;
  const purpose = state?.purpose ?? 'login';
  const initialExpiry = state?.expiresInSeconds ?? DEFAULT_OTP_EXPIRY_SECONDS;

  const [status, setStatus] = useState<OtpStatus>('idle');
  const [resetSignal, setResetSignal] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(initialExpiry);

  const [verifyOtp] = useVerifyOtpMutation();
  const [requestOtp, { isLoading: isResending }] = useRequestOtpMutation();

  useEffect(() => {
    const timerId = window.setInterval(() => {
      setSecondsLeft((s) => (s > 0 ? s - 1 : 0));
    }, 1000);
    return () => window.clearInterval(timerId);
  }, []);

  if (!mobileNumber) {
    navigate('/login');
    return null;
  }

  const handleComplete = async (code: string) => {
    setStatus('verifying');
    setErrorMessage(null);

    const result = await verifyOtp({ mobileNumber, otpCode: code });

    if ('data' in result && result.data?.success && result.data.data) {
      setStatus('success');
      const { accessToken, refreshToken, user } = result.data.data;
      dispatch(sessionEstablished({ accessToken, refreshToken, user }));
      window.setTimeout(() => navigate('/dashboard'), 900);
      return;
    }

    setErrorMessage(extractErrorMessage(result, 'کد تایید نادرست یا منقضی شده است.'));
    setStatus('error');
    triggerShake(panelRef.current);

    window.setTimeout(() => {
      setStatus('idle');
      setResetSignal((s) => s + 1);
    }, 650);
  };

  const handleResend = async () => {
    if (secondsLeft > 0 || isResending) return;
    setErrorMessage(null);

    const result = await requestOtp({ mobileNumber });

    if ('data' in result && result.data?.success) {
      const expiresInSeconds = result.data.data?.expiresInSeconds ?? DEFAULT_OTP_EXPIRY_SECONDS;
      setSecondsLeft(expiresInSeconds);
      setResetSignal((s) => s + 1);
      return;
    }

    setErrorMessage(extractErrorMessage(result, 'ارسال مجدد کد با خطا مواجه شد.'));
    triggerShake(panelRef.current);
  };

  return (
    <AuthShell panelRef={panelRef}>
      <div className="auth-panel-content">
        <h1 className="auth-panel__title">
          {status === 'success' ? 'شماره موبایل تایید شد!' : 'تایید کد ورود'}
        </h1>
        <p className="auth-panel__subtitle">
          {status === 'success'
            ? 'در حال انتقال به داشبورد...'
            : purpose === 'register'
              ? `کد ۶ رقمی ارسال‌شده برای تکمیل ثبت‌نام به ${mobileNumber} را وارد کنید.`
              : `کد ۶ رقمی ارسال‌شده به ${mobileNumber} را وارد کنید.`}
        </p>

        <AnimatedOtpInput status={status} onComplete={handleComplete} resetSignal={resetSignal} />

        {errorMessage && status !== 'success' && <p className="error-text auth-otp-error">{errorMessage}</p>}

        {status !== 'success' && (
          <>
            <div className="auth-otp-resend">
              {secondsLeft > 0 ? (
                <span>ارسال مجدد کد تا {formatCountdown(secondsLeft)} دیگر</span>
              ) : (
                <button type="button" onClick={handleResend} disabled={isResending}>
                  {isResending ? 'در حال ارسال...' : 'ارسال مجدد کد'}
                </button>
              )}
            </div>

            <button type="button" className="auth-otp-back" onClick={() => navigate('/login')}>
              ← بازگشت به مرحله قبل
            </button>
          </>
        )}
      </div>
    </AuthShell>
  );
}
