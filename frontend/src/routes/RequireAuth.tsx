import type { PropsWithChildren } from 'react';
import { Navigate } from 'react-router-dom';
import { useAppSelector } from '@/app/hooks';
import type { UserRole } from '@/shared/types';

interface RequireAuthProps {
  allowedRoles?: UserRole[];
}

/**
 * Route Guard نقش‌محور — طبق docs/frontend/AGENT.md.
 * اگر allowedRoles مشخص نشود، فقط ورود کافی است؛ در غیر این صورت نقش کاربر باید در لیست باشد.
 *
 * طبق رفع باگ «خروج ناخواسته با رفرش صفحه (F5)»: این کامپوننت به هیچ حالت میانیِ «در حال بررسی نشست»
 * نیاز ندارد. علتش این است که authSlice.ts نشست را در localStorage نگه می‌دارد و در لحظهٔ ایمپورت
 * ماژول (پیش از ساخته‌شدن Redux Store و پیش از اولین رندر React) آن را به‌صورت کاملاً همزمان
 * (Synchronous — بر خلاف مثلاً AsyncStorage در React Native) می‌خواند. یعنی وقتی این کامپوننت برای
 * اولین بار رندر می‌شود، accessToken/user از قبل هیدریت شده‌اند و هیچ ریدایرکت زودهنگام/جعلی به
 * /login رخ نمی‌دهد. اگر accessToken واقعاً منقضی شده باشد، اولین فراخوانی RTK Query با 401 مواجه
 * می‌شود و baseApi.ts به‌صورت خودکار یک‌بار تلاش برای رفرش‌توکن انجام می‌دهد؛ فقط در صورت شکست آن هم
 * loggedOut() فراخوانی و state.auth پاک می‌شود که در رندر بعدیِ همین کامپوننت (به‌صورت Reactive) باعث
 * ریدایرکت واقعی به /login می‌شود.
 */
export default function RequireAuth({ allowedRoles, children }: PropsWithChildren<RequireAuthProps>) {
  const { accessToken, user } = useAppSelector((state) => state.auth);

  if (!accessToken || !user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !user.roles.some((role) => allowedRoles.includes(role))) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
