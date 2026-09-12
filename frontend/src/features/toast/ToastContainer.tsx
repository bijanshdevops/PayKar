import { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { toastRemoved, type ToastItem } from '@/features/toast/toastSlice';

const AUTO_DISMISS_MS = 5000;
const LEAVE_ANIMATION_MS = 220;

const iconByType: Record<string, string> = {
  success: '✓',
  error: '!',
  info: 'ℹ'
};

export default function ToastContainer() {
  const toasts = useAppSelector((state) => state.toast.items);
  const dispatch = useAppDispatch();

  if (toasts.length === 0) return null;

  return (
    <div className="toast-container" role="status" aria-live="polite">
      {toasts.map((toast) => (
        <ToastItemView key={toast.id} toast={toast} onDismiss={() => dispatch(toastRemoved(toast.id))} />
      ))}
    </div>
  );
}

function ToastItemView({ toast, onDismiss }: { toast: ToastItem; onDismiss: () => void }) {
  const [isLeaving, setIsLeaving] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setIsLeaving(true), AUTO_DISMISS_MS);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!isLeaving) return;
    const timer = setTimeout(onDismiss, LEAVE_ANIMATION_MS);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLeaving]);

  return (
    <div
      className={`toast toast--${toast.type}${isLeaving ? ' toast--leaving' : ''}`}
      onClick={() => setIsLeaving(true)}
      role="button"
      tabIndex={0}
    >
      <span className="toast__icon">{iconByType[toast.type] ?? 'ℹ'}</span>
      <span className="toast__message">{toast.message}</span>
    </div>
  );
}
