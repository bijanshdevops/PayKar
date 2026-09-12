// آیکون‌های خطی سبک (Stroke-based) برای صفحات ورود/ثبت‌نام — بدون وابستگی به کتابخانه خارجی،
// طبق سبک بصری پروژه (بدون کتابخانه آیکون در package.json فعلی).

interface IconProps {
  size?: number;
}

const baseProps = (size: number) => ({
  width: size,
  height: size,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const
});

export function UserIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="12" cy="8" r="3.5" />
      <path d="M4.5 20c1.4-3.5 4.3-5.5 7.5-5.5s6.1 2 7.5 5.5" />
    </svg>
  );
}

export function MailIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="3" y="5" width="18" height="14" rx="2.5" />
      <path d="m4 7 8 6 8-6" />
    </svg>
  );
}

export function PhoneIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M6.5 3.5h3l1.2 4.4-2 1.6a12 12 0 0 0 5.8 5.8l1.6-2 4.4 1.2v3a2 2 0 0 1-2.2 2A16.5 16.5 0 0 1 4.5 5.7a2 2 0 0 1 2-2.2Z" />
    </svg>
  );
}

export function LockIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="4.5" y="10.5" width="15" height="9.5" rx="2.2" />
      <path d="M7.5 10.5V7.2a4.5 4.5 0 0 1 9 0v3.3" />
    </svg>
  );
}

export function EyeIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

export function EyeOffIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M3.5 3.5l17 17" />
      <path d="M10.6 5.7A10.6 10.6 0 0 1 12 5.5c6 0 9.5 6.5 9.5 6.5a13.6 13.6 0 0 1-3.1 3.9M6.4 6.9C4 8.5 2.5 12 2.5 12s3.5 6.5 9.5 6.5c1.3 0 2.5-.3 3.6-.8" />
      <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2" />
    </svg>
  );
}
