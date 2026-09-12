import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import {
  useRequestOtpMutation,
  useLoginWithPasswordMutation,
  useRegisterMutation,
  useVerifyOtpMutation,
  type RegistrationAccountType
} from '@/features/auth/authApi';
import { useAppDispatch } from '@/app/hooks';
import { sessionEstablished } from '@/features/auth/authSlice';
import AuthField from '@/components/form/AuthField';
import AnimatedOtpInput, { type OtpStatus } from '@/components/form/AnimatedOtpInput';
import { UserIcon, MailIcon, PhoneIcon, LockIcon, EyeIcon, EyeOffIcon } from '@/components/icons/AuthIcons';

type Mode = 'login' | 'signup';
type Stage = 'form' | 'otp';
type LoginMethod = 'password' | 'otp';
type OtpPurpose = 'login' | 'register';

interface LoginOtpValues {
  mobileNumber: string;
}

interface LoginPasswordValues {
  username: string;
  password: string;
}

interface SignupValues {
  username: string;
  email: string;
  mobileNumber: string;
  password: string;
  confirmPassword: string;
  accountType: RegistrationAccountType;
}

const DEFAULT_OTP_EXPIRY_SECONDS = 120;

function triggerShake(el: HTMLElement | null) {
  if (!el) return;
  el.classList.remove('auth-card2--shake');
  void el.offsetWidth;
  el.classList.add('auth-card2--shake');
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

// طبق اصلاح چیدمانی محصولی: لوگو دیگر بالای فرم‌های ورود/ثبت‌نام نیست — به پنل کناری معرفی
// (auth-card2__overlay-content) منتقل شده تا هم برند اصلی‌تر/بزرگ‌تر دیده شود و هم فرم‌ها شلوغ نشوند
// (نگاه کنید به پایین‌تر: پنل معرفی/متن با جداکنندهٔ مورب).

export default function LoginPage() {
  const [mode, setMode] = useState<Mode>('login');
  const [stage, setStage] = useState<Stage>('form');
  const [loginMethod, setLoginMethod] = useState<LoginMethod>('password');
  const [showLoginPassword, setShowLoginPassword] = useState(false);
  const [showSignupPassword, setShowSignupPassword] = useState(false);
  const [showSignupConfirm, setShowSignupConfirm] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const cardRef = useRef<HTMLDivElement>(null);
  const loginPanelRef = useRef<HTMLDivElement>(null);
  const signupPanelRef = useRef<HTMLDivElement>(null);
  const otpLayerRef = useRef<HTMLDivElement>(null);
  const [cardHeight, setCardHeight] = useState<number | undefined>(undefined);
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const otpForm = useForm<LoginOtpValues>({ mode: 'onBlur' });
  const [requestOtp, { isLoading: isRequestingOtp }] = useRequestOtpMutation();

  const passwordForm = useForm<LoginPasswordValues>({ mode: 'onBlur' });
  const [loginWithPassword, { isLoading: isLoggingIn }] = useLoginWithPasswordMutation();

  const signupForm = useForm<SignupValues>({ mode: 'onBlur' });
  const [register, { isLoading: isRegistering }] = useRegisterMutation();

  // --- مرحلهٔ OTP (اکنون به‌صورت یک لایهٔ داخلی همان کارت، بدون تغییر مسیر) ---
  const [otpMobileNumber, setOtpMobileNumber] = useState('');
  const [otpPurpose, setOtpPurpose] = useState<OtpPurpose>('login');
  const [otpStatus, setOtpStatus] = useState<OtpStatus>('idle');
  const [otpResetSignal, setOtpResetSignal] = useState(0);
  const [otpError, setOtpError] = useState<string | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(DEFAULT_OTP_EXPIRY_SECONDS);

  const [verifyOtp] = useVerifyOtpMutation();

  useEffect(() => {
    const timerId = window.setInterval(() => {
      setSecondsLeft((s) => (s > 0 ? s - 1 : 0));
    }, 1000);
    return () => window.clearInterval(timerId);
  }, []);

  // ارتفاع کارت را متناسب با محتوای پنل فعال (ورود/ثبت‌نام/OTP) تنظیم می‌کند تا هیچ‌گاه
  // فرم بلندتر از کارت نشود و اسکرول داخلی زشت ایجاد نکند.
  useLayoutEffect(() => {
    const measure = () => {
      // زیر این عرض، کارت به چیدمان تک‌ستونهٔ موبایل (ارتفاع خودکار) سوییچ می‌کند؛
      // ارتفاع محاسبه‌شدهٔ دسکتاپ نباید آنجا اعمال شود.
      if (window.innerWidth <= 760) {
        setCardHeight(undefined);
        return;
      }
      const target = stage === 'otp' ? otpLayerRef.current : mode === 'login' ? loginPanelRef.current : signupPanelRef.current;
      if (target) setCardHeight(target.scrollHeight);
    };
    measure();
    window.addEventListener('resize', measure);
    return () => window.removeEventListener('resize', measure);
  }, [mode, stage, loginMethod]);

  const switchMode = (next: Mode) => {
    if (next === mode) return;
    setMode(next);
    setServerError(null);
  };

  const switchLoginMethod = (next: LoginMethod) => {
    setLoginMethod(next);
    setServerError(null);
  };

  const goToOtpStage = (mobileNumber: string, purpose: OtpPurpose, expiresInSeconds: number) => {
    setOtpMobileNumber(mobileNumber);
    setOtpPurpose(purpose);
    setOtpStatus('idle');
    setOtpError(null);
    setSecondsLeft(expiresInSeconds);
    setOtpResetSignal((s) => s + 1);
    setStage('otp');
  };

  const onOtpSubmit = async (values: LoginOtpValues) => {
    setServerError(null);
    const result = await requestOtp({ mobileNumber: values.mobileNumber });

    if ('data' in result && result.data?.success) {
      const expiresInSeconds = result.data.data?.expiresInSeconds ?? DEFAULT_OTP_EXPIRY_SECONDS;
      goToOtpStage(values.mobileNumber, 'login', expiresInSeconds);
      return;
    }

    setServerError(extractErrorMessage(result, 'ارسال کد تایید با خطا مواجه شد.'));
    triggerShake(cardRef.current);
  };

  const onPasswordSubmit = async (values: LoginPasswordValues) => {
    setServerError(null);
    const result = await loginWithPassword(values);

    if ('data' in result && result.data?.success && result.data.data) {
      const { accessToken, refreshToken, user } = result.data.data;
      dispatch(sessionEstablished({ accessToken, refreshToken, user }));
      navigate('/dashboard');
      return;
    }

    setServerError(extractErrorMessage(result, 'نام‌کاربری یا رمز عبور نادرست است.'));
    triggerShake(cardRef.current);
  };

  const onSignupSubmit = async (values: SignupValues) => {
    setServerError(null);

    if (values.password !== values.confirmPassword) {
      setServerError('رمز عبور و تکرار آن یکسان نیستند.');
      triggerShake(cardRef.current);
      return;
    }

    const result = await register({
      username: values.username,
      email: values.email,
      mobileNumber: values.mobileNumber,
      password: values.password,
      accountType: values.accountType
    });

    if ('data' in result && result.data?.success) {
      const expiresInSeconds = result.data.data?.expiresInSeconds ?? DEFAULT_OTP_EXPIRY_SECONDS;
      goToOtpStage(values.mobileNumber, 'register', expiresInSeconds);
      return;
    }

    setServerError(extractErrorMessage(result, 'ثبت‌نام با خطا مواجه شد.'));
    triggerShake(cardRef.current);
  };

  const handleOtpComplete = async (code: string) => {
    setOtpStatus('verifying');
    setOtpError(null);

    const result = await verifyOtp({ mobileNumber: otpMobileNumber, otpCode: code });

    if ('data' in result && result.data?.success && result.data.data) {
      setOtpStatus('success');
      const { accessToken, refreshToken, user } = result.data.data;
      dispatch(sessionEstablished({ accessToken, refreshToken, user }));
      window.setTimeout(() => navigate('/dashboard'), 900);
      return;
    }

    setOtpError(extractErrorMessage(result, 'کد تایید نادرست یا منقضی شده است.'));
    setOtpStatus('error');
    triggerShake(cardRef.current);

    window.setTimeout(() => {
      setOtpStatus('idle');
      setOtpResetSignal((s) => s + 1);
    }, 650);
  };

  const handleOtpResend = async () => {
    if (secondsLeft > 0) return;
    setOtpError(null);

    const result = await requestOtp({ mobileNumber: otpMobileNumber });

    if ('data' in result && result.data?.success) {
      const expiresInSeconds = result.data.data?.expiresInSeconds ?? DEFAULT_OTP_EXPIRY_SECONDS;
      setSecondsLeft(expiresInSeconds);
      setOtpResetSignal((s) => s + 1);
      return;
    }

    setOtpError(extractErrorMessage(result, 'ارسال مجدد کد با خطا مواجه شد.'));
    triggerShake(cardRef.current);
  };

  const handleOtpBack = () => {
    setStage('form');
    setOtpStatus('idle');
    setOtpError(null);
  };

  return (
    <div className="auth-page2">
      <span className="auth-shell__glow auth-shell__glow--teal" aria-hidden="true" />
      <span className="auth-shell__glow auth-shell__glow--navy" aria-hidden="true" />

      <div
        ref={cardRef}
        className={`auth-card2${mode === 'signup' ? ' auth-card2--signup' : ''}${stage === 'otp' ? ' auth-card2--otp' : ''}`}
        style={{ height: cardHeight ? `${cardHeight}px` : undefined }}
      >
        {/* --- پنل فرم ورود (خانهٔ ثابت: راست) --- */}
        <div ref={loginPanelRef} className="auth-card2__form-panel auth-card2__form-panel--login">
          <div className="auth-card2__form-scroll">
            <h1 className="auth-panel__title">خوش آمدید!</h1>
            <p className="auth-panel__subtitle">برای ادامه وارد حساب کاربری خود شوید.</p>

            <div className="auth-method-toggle">
              <button
                type="button"
                className={loginMethod === 'password' ? 'is-active' : ''}
                onClick={() => switchLoginMethod('password')}
              >
                نام‌کاربری/رمز عبور
              </button>
              <button
                type="button"
                className={loginMethod === 'otp' ? 'is-active' : ''}
                onClick={() => switchLoginMethod('otp')}
              >
                ورود سریع با کد پیامکی
              </button>
            </div>

            {loginMethod === 'password' && (
              <form onSubmit={passwordForm.handleSubmit(onPasswordSubmit)} noValidate>
                <AuthField
                  label="نام‌کاربری"
                  htmlFor="username"
                  icon={<UserIcon />}
                  error={passwordForm.formState.errors.username?.message}
                >
                  <input
                    id="username"
                    autoComplete="username"
                    {...passwordForm.register('username', { required: 'نام‌کاربری الزامی است.' })}
                  />
                </AuthField>

                <AuthField
                  label="رمز عبور"
                  htmlFor="password"
                  icon={<LockIcon />}
                  error={passwordForm.formState.errors.password?.message}
                  trailing={
                    <button
                      type="button"
                      className="auth-ig__eye-btn"
                      onClick={() => setShowLoginPassword((v) => !v)}
                      aria-label={showLoginPassword ? 'پنهان‌کردن رمز عبور' : 'نمایش رمز عبور'}
                    >
                      {showLoginPassword ? <EyeOffIcon /> : <EyeIcon />}
                    </button>
                  }
                >
                  <input
                    id="password"
                    type={showLoginPassword ? 'text' : 'password'}
                    autoComplete="current-password"
                    {...passwordForm.register('password', { required: 'رمز عبور الزامی است.' })}
                  />
                </AuthField>

                <div className="auth-panel__forgot">
                  <Link to="/forgot-password">رمز عبور را فراموش کرده‌اید؟</Link>
                </div>

                {serverError && <p className="error-text">{serverError}</p>}

                <button type="submit" className={`btn-primary${isLoggingIn ? ' is-loading' : ''}`} disabled={isLoggingIn}>
                  ورود
                </button>
              </form>
            )}

            {loginMethod === 'otp' && (
              <form onSubmit={otpForm.handleSubmit(onOtpSubmit)} noValidate>
                <AuthField
                  label="شماره موبایل"
                  htmlFor="mobileNumber"
                  icon={<PhoneIcon />}
                  error={otpForm.formState.errors.mobileNumber?.message}
                >
                  <input
                    id="mobileNumber"
                    type="tel"
                    placeholder="09121234567"
                    autoComplete="tel"
                    {...otpForm.register('mobileNumber', {
                      required: 'شماره موبایل الزامی است.',
                      pattern: { value: /^09\d{9}$/, message: 'شماره موبایل معتبر نیست.' }
                    })}
                  />
                </AuthField>

                {serverError && <p className="error-text">{serverError}</p>}

                <button type="submit" className={`btn-primary${isRequestingOtp ? ' is-loading' : ''}`} disabled={isRequestingOtp}>
                  دریافت کد تایید
                </button>
              </form>
            )}
          </div>
        </div>

        {/* --- پنل فرم ثبت‌نام (همان خانهٔ راست؛ با سوییچ به چپ منتقل می‌شود) --- */}
        <div ref={signupPanelRef} className="auth-card2__form-panel auth-card2__form-panel--signup">
          <div className="auth-card2__form-scroll">
            <h1 className="auth-panel__title">به جمع ما بپیوندید!</h1>
            <p className="auth-panel__subtitle">برای ثبت‌نام، اطلاعات زیر را کامل کنید.</p>

            <form onSubmit={signupForm.handleSubmit(onSignupSubmit)} noValidate>
              <div className="field">
                <label>نوع حساب</label>
                <div className="auth-account-type-toggle">
                  <label className="auth-account-type-option">
                    <input
                      type="radio"
                      value="Candidate"
                      defaultChecked
                      {...signupForm.register('accountType', { required: true })}
                    />
                    <span>کارجو هستم</span>
                  </label>
                  <label className="auth-account-type-option">
                    <input type="radio" value="Company" {...signupForm.register('accountType', { required: true })} />
                    <span>نماینده یک شرکت هستم</span>
                  </label>
                </div>
              </div>

              <div className="auth-card2__grid-2">
                <AuthField
                  label="نام‌کاربری"
                  htmlFor="signup-username"
                  icon={<UserIcon />}
                  error={signupForm.formState.errors.username?.message}
                >
                  <input
                    id="signup-username"
                    autoComplete="username"
                    {...signupForm.register('username', {
                      required: 'نام‌کاربری الزامی است.',
                      pattern: { value: /^[a-zA-Z0-9_]{4,30}$/, message: 'نام‌کاربری باید ۴ تا ۳۰ کاراکتر (حروف انگلیسی، عدد، _) باشد.' }
                    })}
                  />
                </AuthField>

                <AuthField
                  label="شماره موبایل"
                  htmlFor="signup-mobile"
                  icon={<PhoneIcon />}
                  error={signupForm.formState.errors.mobileNumber?.message}
                >
                  <input
                    id="signup-mobile"
                    type="tel"
                    placeholder="09121234567"
                    autoComplete="tel"
                    {...signupForm.register('mobileNumber', {
                      required: 'شماره موبایل الزامی است.',
                      pattern: { value: /^09\d{9}$/, message: 'شماره موبایل معتبر نیست.' }
                    })}
                  />
                </AuthField>
              </div>

              <AuthField label="ایمیل" htmlFor="signup-email" icon={<MailIcon />} error={signupForm.formState.errors.email?.message}>
                <input
                  id="signup-email"
                  type="email"
                  autoComplete="email"
                  {...signupForm.register('email', {
                    required: 'ایمیل الزامی است.',
                    pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'ایمیل معتبر نیست.' }
                  })}
                />
              </AuthField>

              <div className="auth-card2__grid-2">
                <AuthField
                  label="رمز عبور"
                  htmlFor="signup-password"
                  icon={<LockIcon />}
                  error={signupForm.formState.errors.password?.message}
                  trailing={
                    <button
                      type="button"
                      className="auth-ig__eye-btn"
                      onClick={() => setShowSignupPassword((v) => !v)}
                      aria-label={showSignupPassword ? 'پنهان‌کردن رمز عبور' : 'نمایش رمز عبور'}
                    >
                      {showSignupPassword ? <EyeOffIcon /> : <EyeIcon />}
                    </button>
                  }
                >
                  <input
                    id="signup-password"
                    type={showSignupPassword ? 'text' : 'password'}
                    autoComplete="new-password"
                    {...signupForm.register('password', {
                      required: 'رمز عبور الزامی است.',
                      minLength: { value: 8, message: 'رمز عبور باید حداقل ۸ کاراکتر باشد.' }
                    })}
                  />
                </AuthField>

                <AuthField
                  label="تکرار رمز عبور"
                  htmlFor="signup-confirm-password"
                  icon={<LockIcon />}
                  error={signupForm.formState.errors.confirmPassword?.message}
                  trailing={
                    <button
                      type="button"
                      className="auth-ig__eye-btn"
                      onClick={() => setShowSignupConfirm((v) => !v)}
                      aria-label={showSignupConfirm ? 'پنهان‌کردن رمز عبور' : 'نمایش رمز عبور'}
                    >
                      {showSignupConfirm ? <EyeOffIcon /> : <EyeIcon />}
                    </button>
                  }
                >
                  <input
                    id="signup-confirm-password"
                    type={showSignupConfirm ? 'text' : 'password'}
                    autoComplete="new-password"
                    {...signupForm.register('confirmPassword', { required: 'تکرار رمز عبور الزامی است.' })}
                  />
                </AuthField>
              </div>

              {serverError && <p className="error-text">{serverError}</p>}

              <button type="submit" className={`btn-primary${isRegistering ? ' is-loading' : ''}`} disabled={isRegistering}>
                ثبت‌نام
              </button>
            </form>
          </div>
        </div>

        {/* --- پنل معرفی/متن با جداکنندهٔ مورب (خانهٔ ثابت: چپ) --- */}
        <div className="auth-card2__overlay-container">
          <div className="auth-card2__overlay-content" key={mode}>
            {/* طبق اصلاح چیدمانی محصولی: لوگو اکنون عنصر برندِ اصلیِ این پنل است — چون overlay-content
                بین دو حالت ورود/ثبت‌نام فقط محتوای متنی داخل شرط زیر را عوض می‌کند، لوگو را یک‌بار و
                خارج از شرط قرار می‌دهیم تا بدون تکرار کد در هر دو حالت (بالای «تازه‌واردید؟» و بالای
                «قبلاً ثبت‌نام کرده‌اید؟») نمایش داده شود. */}
            <Link to="/" className="auth-card2__overlay-brand" aria-label="صفحه اصلی">
              <img src="/logo.png" alt="پی کار" className="auth-card2__overlay-logo" />
            </Link>

            {mode === 'login' ? (
              <>
                <h2>تازه‌واردید؟</h2>
                <p>با ساخت یک حساب کاربری، به آگهی‌های استخدام شهرک‌های صنعتی سراسر کشور دسترسی پیدا کنید.</p>
                <button type="button" className="auth-card2__overlay-btn" onClick={() => switchMode('signup')}>
                  ثبت‌نام کنید
                </button>
              </>
            ) : (
              <>
                <h2>قبلاً ثبت‌نام کرده‌اید؟</h2>
                <p>برای دسترسی به داشبورد و آگهی‌های خود، وارد حساب کاربری‌تان شوید.</p>
                <button type="button" className="auth-card2__overlay-btn" onClick={() => switchMode('login')}>
                  ورود
                </button>
              </>
            )}
          </div>
        </div>

        {/* --- سوییچ سادهٔ موبایل (فقط در عرض‌های باریک نمایش داده می‌شود) --- */}
        <div className="auth-card2__mobile-switch">
          {mode === 'login' ? (
            <span>
              حساب کاربری ندارید؟{' '}
              <button type="button" onClick={() => switchMode('signup')}>
                ثبت‌نام کنید
              </button>
            </span>
          ) : (
            <span>
              قبلاً ثبت‌نام کرده‌اید؟{' '}
              <button type="button" onClick={() => switchMode('login')}>
                وارد شوید
              </button>
            </span>
          )}
        </div>

        {/* --- لایهٔ داخلی تایید کد OTP (روی همان کارت، با فید/اسلاید) --- */}
        <div ref={otpLayerRef} className="auth-card2__otp-layer">
          <div className="auth-panel-content">
            <h1 className="auth-panel__title">
              {otpStatus === 'success' ? 'شماره موبایل تایید شد!' : 'تایید کد ورود'}
            </h1>
            <p className="auth-panel__subtitle">
              {otpStatus === 'success'
                ? 'در حال انتقال به داشبورد...'
                : otpPurpose === 'register'
                  ? `کد ۶ رقمی ارسال‌شده برای تکمیل ثبت‌نام به ${otpMobileNumber} را وارد کنید.`
                  : `کد ۶ رقمی ارسال‌شده به ${otpMobileNumber} را وارد کنید.`}
            </p>

            <AnimatedOtpInput status={otpStatus} onComplete={handleOtpComplete} resetSignal={otpResetSignal} />

            {otpError && otpStatus !== 'success' && <p className="error-text auth-otp-error">{otpError}</p>}

            {otpStatus !== 'success' && (
              <>
                <div className="auth-otp-resend">
                  {secondsLeft > 0 ? (
                    <span>ارسال مجدد کد تا {formatCountdown(secondsLeft)} دیگر</span>
                  ) : (
                    <button type="button" onClick={handleOtpResend}>
                      ارسال مجدد کد
                    </button>
                  )}
                </div>

                <button type="button" className="auth-otp-back" onClick={handleOtpBack}>
                  ← بازگشت به مرحله قبل
                </button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
