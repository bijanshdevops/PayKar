// آیکون‌های خطی سبک برای سایدبار داشبورد — هم‌سبک با AuthIcons.tsx (بدون کتابخانه خارجی).

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

export function GridIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="3.5" y="3.5" width="7" height="7" rx="1.5" />
      <rect x="13.5" y="3.5" width="7" height="7" rx="1.5" />
      <rect x="3.5" y="13.5" width="7" height="7" rx="1.5" />
      <rect x="13.5" y="13.5" width="7" height="7" rx="1.5" />
    </svg>
  );
}

export function BuildingIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M5 21V5a1.5 1.5 0 0 1 1.5-1.5h7A1.5 1.5 0 0 1 15 5v16" />
      <path d="M15 10.5h3A1.5 1.5 0 0 1 19.5 12v9" />
      <path d="M5 21h14.5" />
      <path d="M8 7h1M8 10.5h1M8 14h1M11.5 7h1M11.5 10.5h1M11.5 14h1" />
    </svg>
  );
}

export function BriefcaseIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="3" y="7.5" width="18" height="12" rx="2" />
      <path d="M8.5 7.5V6a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v1.5" />
      <path d="M3 12.5h18" />
    </svg>
  );
}

export function MegaphoneIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 10v4a1 1 0 0 0 1 1h1.5l1 4.5h2L8.5 15H10l7.5 4V5L10 9H4a1 1 0 0 0-1 1Z" />
      <path d="M20 9.5a4 4 0 0 1 0 5" />
    </svg>
  );
}

export function FileTextIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M7 3.5h7l4 4V19a1.5 1.5 0 0 1-1.5 1.5H7A1.5 1.5 0 0 1 5.5 19V5A1.5 1.5 0 0 1 7 3.5Z" />
      <path d="M14 3.5V8h4" />
      <path d="M8.5 12.5h7M8.5 15.5h7M8.5 9.5h3" />
    </svg>
  );
}

export function ShieldIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M12 3.5 5 6v6c0 4.5 3 7.5 7 8.5 4-1 7-4 7-8.5V6l-7-2.5Z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  );
}

export function ChatIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 5.5h16v10.5H9L4 20V5.5Z" />
      <path d="M8 9.5h8M8 12.5h5" />
    </svg>
  );
}

export function ChartIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 20V10M10 20V4M16 20v-7M4 20h16" />
    </svg>
  );
}

export function LockGearIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="5" y="10.5" width="14" height="9" rx="2" />
      <path d="M8 10.5V7a4 4 0 0 1 8 0v3.5" />
      <circle cx="12" cy="15" r="1.6" />
    </svg>
  );
}

export function StoreIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 9.5 5.5 4h13L20 9.5" />
      <path d="M4.5 9.5v10A1 1 0 0 0 5.5 20.5h13a1 1 0 0 0 1-1v-10" />
      <path d="M4 9.5a2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0" />
    </svg>
  );
}

export function HomeIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 11.5 12 4l8 7.5" />
      <path d="M6 10v9.5a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V10" />
      <path d="M10 20.5V14h4v6.5" />
    </svg>
  );
}

export function UsersIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="9" cy="8.5" r="3" />
      <path d="M3.5 19.5a5.5 5.5 0 0 1 11 0" />
      <circle cx="17" cy="9" r="2.5" />
      <path d="M14.8 13.2a4.5 4.5 0 0 1 5.7 4.3" />
    </svg>
  );
}

export function CreditCardIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="3" y="6" width="18" height="13" rx="2" />
      <path d="M3 10.5h18" />
      <path d="M7 14.5h4" />
    </svg>
  );
}

export function HeadsetIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 13v-1a8 8 0 0 1 16 0v1" />
      <rect x="3" y="13" width="4" height="6" rx="1.5" />
      <rect x="17" y="13" width="4" height="6" rx="1.5" />
      <path d="M19 19v1a2.5 2.5 0 0 1-2.5 2.5H13" />
    </svg>
  );
}

export function BookmarkIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M6.5 4A1.5 1.5 0 0 1 8 2.5h8A1.5 1.5 0 0 1 17.5 4v17l-5.5-3.3L6.5 21V4Z" />
    </svg>
  );
}

export function LogoutIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M9 4.5H6.5A1.5 1.5 0 0 0 5 6v12a1.5 1.5 0 0 0 1.5 1.5H9" />
      <path d="M14 15.5 18.5 12 14 8.5" />
      <path d="M18.5 12h-10" />
    </svg>
  );
}

export function ChevronIcon({ size = 16 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="m9 6 6 6-6 6" />
    </svg>
  );
}

export function MenuIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 6.5h16" />
      <path d="M4 12h16" />
      <path d="M4 17.5h16" />
    </svg>
  );
}

export function WalletIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 7.5A2.5 2.5 0 0 1 6.5 5h11A2.5 2.5 0 0 1 20 7.5V17a2.5 2.5 0 0 1-2.5 2.5h-11A2.5 2.5 0 0 1 4 17V7.5Z" />
      <path d="M15.5 12.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z" />
      <path d="M4 9.5h16" />
    </svg>
  );
}

export function DotsIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)} strokeWidth={2.4}>
      <circle cx="12" cy="5.5" r="0.6" fill="currentColor" />
      <circle cx="12" cy="12" r="0.6" fill="currentColor" />
      <circle cx="12" cy="18.5" r="0.6" fill="currentColor" />
    </svg>
  );
}

export function DownloadIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M12 3.5v11" />
      <path d="m7.5 10.5 4.5 4.5 4.5-4.5" />
      <path d="M5 19.5h14" />
    </svg>
  );
}

export function BellIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M6 10a6 6 0 0 1 12 0v4.5l1.5 2.5h-15L6 14.5V10Z" />
      <path d="M10 19.5a2 2 0 0 0 4 0" />
    </svg>
  );
}

export function CameraIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M4 8.5A1.5 1.5 0 0 1 5.5 7h2l1-2h7l1 2h2A1.5 1.5 0 0 1 20 8.5v9A1.5 1.5 0 0 1 18.5 19h-13A1.5 1.5 0 0 1 4 17.5v-9Z" />
      <circle cx="12" cy="13" r="3.3" />
    </svg>
  );
}

export function CheckCircleIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="m8.2 12.3 2.6 2.6 5-5.2" />
    </svg>
  );
}

export function ClockIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="M12 7.5V12l3 2" />
    </svg>
  );
}

export function XCircleIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="m9.3 9.3 5.4 5.4M14.7 9.3l-5.4 5.4" />
    </svg>
  );
}

export function TrashIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M5 7.5h14" />
      <path d="M9.5 7.5V5.8a1.3 1.3 0 0 1 1.3-1.3h2.4a1.3 1.3 0 0 1 1.3 1.3v1.7" />
      <path d="M7 7.5 7.8 19a1.5 1.5 0 0 0 1.5 1.4h5.4a1.5 1.5 0 0 0 1.5-1.4l.8-11.5" />
      <path d="M10.3 11v6M13.7 11v6" />
    </svg>
  );
}

export function UploadCloudIcon({ size = 22 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M7.5 17.5a4 4 0 0 1-.5-7.97A5.5 5.5 0 0 1 17.7 8a4.2 4.2 0 0 1-.7 9.5h-9.5Z" />
      <path d="M12 15.5V9.5" />
      <path d="m9.3 12.2 2.7-2.7 2.7 2.7" />
    </svg>
  );
}

export function GlobeIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <circle cx="12" cy="12" r="8.5" />
      <path d="M3.5 12h17M12 3.5a13 13 0 0 1 4 8.5 13 13 0 0 1-4 8.5 13 13 0 0 1-4-8.5 13 13 0 0 1 4-8.5Z" />
    </svg>
  );
}

export function MapPinIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <path d="M12 21s7-6.5 7-11.5A7 7 0 0 0 5 9.5C5 14.5 12 21 12 21Z" />
      <circle cx="12" cy="9.5" r="2.4" />
    </svg>
  );
}

export function ImageFileIcon({ size = 18 }: IconProps) {
  return (
    <svg {...baseProps(size)}>
      <rect x="3.5" y="4.5" width="17" height="15" rx="2" />
      <circle cx="8.5" cy="9.5" r="1.5" />
      <path d="m5 17 4.5-5 3.5 4 2.5-3 4.5 4" />
    </svg>
  );
}
