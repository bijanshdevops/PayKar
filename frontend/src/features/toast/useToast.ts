import { useAppDispatch } from '@/app/hooks';
import { toastAdded } from '@/features/toast/toastSlice';

/**
 * هوک نمایش توست دستی — برای اکثر عملیات، توست به‌صورت خودکار توسط toastMiddleware
 * (بر اساس فیلد message در پاسخ ApiResponse از سرور) نمایش داده می‌شود. این هوک برای
 * پیام‌های کاملاً سمت کلاینت (مثل خطای اعتبارسنجی فرم پیش از ارسال) استفاده می‌شود.
 */
export function useToast() {
  const dispatch = useAppDispatch();

  return {
    success: (message: string) => dispatch(toastAdded('success', message)),
    error: (message: string) => dispatch(toastAdded('error', message)),
    info: (message: string) => dispatch(toastAdded('info', message))
  };
}
