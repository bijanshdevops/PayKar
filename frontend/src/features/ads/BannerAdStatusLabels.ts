import type { BannerAdStatus } from '@/features/ads/bannerAdApi';

export const BannerAdStatusLabels: Record<BannerAdStatus, string> = {
  Draft: 'پیش‌نویس',
  PendingReview: 'در انتظار بررسی',
  Active: 'فعال (در حال نمایش)',
  Rejected: 'ردشده',
  Expired: 'منقضی‌شده'
};
