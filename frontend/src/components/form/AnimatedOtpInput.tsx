import { useEffect, useRef, useState } from 'react';

export type OtpStatus = 'idle' | 'verifying' | 'error' | 'success';

interface AnimatedOtpInputProps {
  length?: number;
  status: OtpStatus;
  onComplete: (code: string) => void;
  /** با هر افزایش این مقدار، جعبه‌ها پاک و روی اولین جعبه فوکوس می‌شود (مثلاً پس از خطا). */
  resetSignal?: number;
}

/**
 * ورودی کد تایید انیمیشنی (فوکوس خودکار، حرکت خودکار بین جعبه‌ها، پشتیبانی Paste،
 * و وضعیت‌های بصری idle/verifying/error/success) — طبق نمونهٔ ارسالی کاربر (OTP Verification V2).
 */
export default function AnimatedOtpInput({ length = 6, status, onComplete, resetSignal }: AnimatedOtpInputProps) {
  const [values, setValues] = useState<string[]>(() => Array(length).fill(''));
  const inputsRef = useRef<Array<HTMLInputElement | null>>([]);

  useEffect(() => {
    inputsRef.current[0]?.focus();
  }, []);

  useEffect(() => {
    if (resetSignal === undefined) return;
    setValues(Array(length).fill(''));
    inputsRef.current[0]?.focus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resetSignal]);

  const handleChange = (index: number, raw: string) => {
    if (status === 'verifying' || status === 'success') return;

    const digit = raw.replace(/\D/g, '').slice(-1);
    const next = [...values];
    next[index] = digit;
    setValues(next);

    if (digit && index < length - 1) {
      inputsRef.current[index + 1]?.focus();
    }

    if (digit && next.every((v) => v !== '')) {
      onComplete(next.join(''));
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !values[index] && index > 0) {
      inputsRef.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, length);
    if (!pasted) return;
    e.preventDefault();

    const next = Array(length).fill('');
    pasted.split('').forEach((ch, i) => {
      next[i] = ch;
    });
    setValues(next);

    const lastIndex = Math.min(pasted.length, length) - 1;
    inputsRef.current[lastIndex]?.focus();

    if (pasted.length === length) onComplete(pasted);
  };

  return (
    <div className={`otp-input otp-input--${status}`} dir="ltr">
      {values.map((value, index) => (
        <input
          key={index}
          ref={(el) => {
            inputsRef.current[index] = el;
          }}
          className="otp-input__box"
          inputMode="numeric"
          maxLength={1}
          value={status === 'success' && value ? '✓' : value}
          disabled={status === 'verifying' || status === 'success'}
          onChange={(e) => handleChange(index, e.target.value)}
          onKeyDown={(e) => handleKeyDown(index, e)}
          onPaste={handlePaste}
          aria-label={`رقم ${index + 1} کد تایید`}
        />
      ))}
    </div>
  );
}
