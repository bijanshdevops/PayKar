import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useAppSelector } from '@/app/hooks';
import { useSetPasswordMutation } from '@/features/auth/authApi';

interface SetPasswordFormValues {
  username: string;
  password: string;
  confirmPassword: string;
}

export default function AccountSecurityPage() {
  const user = useAppSelector((state) => state.auth.user);
  const [setPassword, { isLoading }] = useSetPasswordMutation();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const { register, handleSubmit, watch, formState: { errors } } = useForm<SetPasswordFormValues>();
  const password = watch('password');

  const onSubmit = async (values: SetPasswordFormValues) => {
    setServerError(null);
    setSuccessMessage(null);

    const result = await setPassword({ username: values.username, password: values.password });

    if ('data' in result && result.data?.success) {
      setSuccessMessage('نام‌کاربری و رمز عبور شما با موفقیت تنظیم شد. از این پس می‌توانید با آن‌ها نیز وارد شوید.');
      return;
    }

    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setServerError(errorResponse?.data?.message ?? 'تنظیم رمز عبور با خطا مواجه شد.');
  };

  return (
    <div className="container">
      <div className="card" style={{ maxWidth: 480, margin: '2rem auto' }}>
        <h1>امنیت حساب کاربری</h1>
        <p style={{ color: 'var(--color-muted)' }}>
          شماره موبایل شما ({user?.mobileNumber}) همیشه برای ورود با کد پیامکی فعال می‌ماند. در صورت تمایل،
          می‌توانید یک نام‌کاربری و رمز عبور نیز تنظیم کنید تا در دفعات بعد بدون نیاز به کد پیامکی وارد شوید.
        </p>

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="field">
            <label htmlFor="username">نام‌کاربری</label>
            <input
              id="username"
              {...register('username', {
                required: 'نام‌کاربری الزامی است.',
                pattern: { value: /^[a-zA-Z0-9_]{4,30}$/, message: 'فقط حروف انگلیسی، عدد و _ ، بین ۴ تا ۳۰ کاراکتر.' }
              })}
            />
            {errors.username && <span className="error-text">{errors.username.message}</span>}
          </div>

          <div className="field">
            <label htmlFor="password">رمز عبور</label>
            <input
              id="password"
              type="password"
              {...register('password', {
                required: 'رمز عبور الزامی است.',
                minLength: { value: 8, message: 'رمز عبور باید حداقل ۸ کاراکتر باشد.' }
              })}
            />
            {errors.password && <span className="error-text">{errors.password.message}</span>}
          </div>

          <div className="field">
            <label htmlFor="confirmPassword">تکرار رمز عبور</label>
            <input
              id="confirmPassword"
              type="password"
              {...register('confirmPassword', {
                required: 'تکرار رمز عبور الزامی است.',
                validate: (value) => value === password || 'رمز عبور و تکرار آن یکسان نیستند.'
              })}
            />
            {errors.confirmPassword && <span className="error-text">{errors.confirmPassword.message}</span>}
          </div>

          {serverError && <p className="error-text">{serverError}</p>}
          {successMessage && <p style={{ color: 'var(--color-success)' }}>{successMessage}</p>}

          <button type="submit" className="btn-primary" disabled={isLoading}>
            {isLoading ? 'در حال ذخیره...' : 'تنظیم نام‌کاربری و رمز عبور'}
          </button>
        </form>
      </div>
    </div>
  );
}
