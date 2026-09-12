import { isFulfilled, isRejectedWithValue, type Middleware } from '@reduxjs/toolkit';
import { toastAdded } from '@/features/toast/toastSlice';
import type { ApiResponse } from '@/shared/types';

interface RtkQueryActionMeta {
  arg?: { type?: 'query' | 'mutation' };
}

/**
 * میدل‌ور توست سراسری: طبق تصمیم محصولی «توست‌های جذاب در کل سایت» بدون نیاز به تغییر
 * تک‌تک صفحات. فقط برای Mutationها (نه Queryهای پس‌زمینه‌ای مثل چک‌کردن وجود رزومه که
 * ۴۰۴ آن‌ها کاملاً عادی است) و فقط وقتی سرور یک `message` معنادار در ApiResponse برگردانده باشد.
 */
export const toastMiddleware: Middleware = (store) => (next) => (action) => {
  const meta = (action as { meta?: RtkQueryActionMeta }).meta;
  const isMutation = meta?.arg?.type === 'mutation';

  if (isMutation && isFulfilled(action)) {
    const payload = (action as { payload?: ApiResponse<unknown> }).payload;
    if (payload?.success && payload.message) {
      store.dispatch(toastAdded('success', payload.message));
    }
  }

  if (isMutation && isRejectedWithValue(action)) {
    const errorPayload = (action as { payload?: { data?: ApiResponse<unknown> } }).payload;
    const message = errorPayload?.data?.message ?? 'خطایی رخ داد. لطفاً دوباره تلاش کنید.';
    store.dispatch(toastAdded('error', message));
  }

  return next(action);
};
