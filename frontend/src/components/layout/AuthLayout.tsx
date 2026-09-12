import { Outlet } from 'react-router-dom';
import ToastContainer from '@/features/toast/ToastContainer';

/**
 * قالب صفحات ورود/ثبت‌نام/بازیابی رمز عبور — بدون Header/Footer سراسری سایت،
 * چون این صفحات خودشان یک تجربهٔ تمام‌صفحه (شیشه‌ای تیره) دارند.
 */
export default function AuthLayout() {
  return (
    <>
      <Outlet />
      <ToastContainer />
    </>
  );
}
