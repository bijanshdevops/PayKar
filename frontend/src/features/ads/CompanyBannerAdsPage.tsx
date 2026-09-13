import { useMemo, useRef, useState, type DragEvent } from 'react';
import {
  BarChart3,
  Calendar,
  CheckCircle2,
  Clock,
  Eye,
  ImagePlus,
  Layers,
  Link2,
  MousePointerClick,
  RefreshCw,
  UploadCloud,
  Wallet,
  X,
  XCircle
} from 'lucide-react';
import {
  useCreateBannerAdMutation,
  useGetBannerSlotsQuery,
  useGetMyCompanyBannerAdsQuery,
  useRenewBannerAdMutation,
  useSubmitBannerAdForReviewMutation,
  useUploadBannerImageMutation,
  type BannerAd,
  type BannerAdStatus,
  type BannerPlacement,
  type BannerSlot
} from '@/features/ads/bannerAdApi';
import { useToast } from '@/features/toast/useToast';
import { formatToJalali } from '@/shared/utils/date';

const PLACEMENT_LABELS: Record<BannerPlacement, string> = {
  Home: 'صفحه اصلی',
  JobAdList: 'لیست آگهی‌ها',
  Both: 'صفحه اصلی + لیست آگهی‌ها'
};

const DURATION_PRESETS = [7, 15, 30, 60];
const RENEWAL_DISCOUNT_RATE = 0.2;

/** جایگاه‌های تبلیغاتی طبق تصمیم بیزینسی این فاز، فقط با تومان قیمت‌گذاری شده‌اند (نه ریال) — برخلاف مبالغ InRials بقیه سیستم. */
const formatToman = (amount: number) => Math.round(amount).toLocaleString('fa-IR');

const formatDate = (iso: string | null) => formatToJalali(iso, 'd MMMM yyyy');

const STATUS_STYLES: Record<BannerAdStatus, { bg: string; fg: string; border: string; label: string; Icon: typeof CheckCircle2 }> = {
  Draft: { bg: '#f1f5f9', fg: '#475569', border: '#e2e8f0', label: 'پیش‌نویس', Icon: Clock },
  PendingReview: { bg: '#fffbeb', fg: '#b45309', border: '#fde68a', label: 'در انتظار بررسی ادمین', Icon: Clock },
  Active: { bg: '#ecfdf5', fg: '#047857', border: '#a7f3d0', label: 'فعال (در حال نمایش)', Icon: CheckCircle2 },
  Rejected: { bg: '#fff1f2', fg: '#be123c', border: '#fecdd3', label: 'ردشده', Icon: XCircle },
  Expired: { bg: '#f8fafc', fg: '#64748b', border: '#e2e8f0', label: 'منقضی‌شده', Icon: Clock }
};

function KpiCard({ icon: Icon, iconBg, iconFg, label, value, hint }: {
  icon: typeof Wallet;
  iconBg: string;
  iconFg: string;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs text-slate-400">{label}</p>
          <p className="mt-1.5 text-lg font-bold text-slate-900">{value}</p>
          {hint && <p className="mt-0.5 text-[11px] text-slate-400">{hint}</p>}
        </div>
        <span className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl" style={{ background: iconBg, color: iconFg }}>
          <Icon className="h-5 w-5" />
        </span>
      </div>
    </div>
  );
}

function SlotCard({ slot, selected, onSelect }: { slot: BannerSlot; selected: boolean; onSelect: () => void }) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className={`w-full rounded-2xl border p-4 text-right transition-colors ${
        selected ? 'border-emerald-500 bg-emerald-50/50 ring-1 ring-emerald-500' : 'border-slate-200 bg-white hover:border-emerald-200'
      }`}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-sm font-bold text-slate-800">{slot.title}</p>
          <p className="mt-0.5 text-xs text-slate-400">{PLACEMENT_LABELS[slot.placement]} · {slot.dimensions}</p>
        </div>
        {selected && (
          <span className="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded-full bg-emerald-500 text-white">
            <CheckCircle2 className="h-3.5 w-3.5" />
          </span>
        )}
      </div>
      <p className="mt-3 text-sm font-extrabold text-emerald-700">{formatToman(slot.dailyPrice)} تومان <span className="text-xs font-medium text-slate-400">/ روز</span></p>
    </button>
  );
}

/**
 * پنل شرکت برای رزرو، پرداخت و مدیریت بنرهای تبلیغاتی — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک‌های ۷ و ۸).
 * یادداشت‌های شفافیت داده واقعی:
 * ۱. بازه زمانی نمایش (StartDate/EndDate) در لحظهٔ ثبت درخواست هنوز مشخص نیست (طبق مدل واقعی بک‌اند، فقط
 *    پس از تایید ادمین قطعی می‌شود)، پس به‌جای یک انتخابگر تاریخ ساختگی، فقط «تعداد روز نمایش» گرفته می‌شود.
 * ۲. قیمت جایگاه‌ها (BannerSlot.DailyPrice / BannerAd.TotalAmount) طبق تصمیم صریح بیزینسی این فاز واحدشان
 *    تومان است (نه ریال مثل بقیهٔ مبالغ سیستم)، پس اینجا بدون تقسیم بر ۱۰ نمایش داده می‌شوند.
 * ۳. KPIهای بالای صفحه از مجموع همین ۵۰ بنر بارگذاری‌شده محاسبه می‌شوند (بدون اندپوینت خلاصهٔ آماری مجزا
 *    برای بنرها)؛ برای حجم واقعی بنرهای هر شرکت (که برخلاف تراکنش‌ها معمولاً زیاد نیست) این کافی است.
 */
export default function CompanyBannerAdsPage() {
  const { data: slotsData, isLoading: isLoadingSlots } = useGetBannerSlotsQuery();
  const { data, isLoading } = useGetMyCompanyBannerAdsQuery({ page: 1, pageSize: 50 });
  const [uploadImage, { isLoading: isUploading }] = useUploadBannerImageMutation();
  const [createBannerAd, { isLoading: isCreating }] = useCreateBannerAdMutation();
  const [submitForReview, { isLoading: isSubmitting }] = useSubmitBannerAdForReviewMutation();
  const [renewBannerAd, { isLoading: isRenewing }] = useRenewBannerAdMutation();
  const toast = useToast();

  const slots = useMemo(() => (slotsData?.data ?? []).filter((s) => s.isActive), [slotsData]);
  const banners = data?.data?.items ?? [];

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [destinationUrl, setDestinationUrl] = useState('');
  const [selectedSlotId, setSelectedSlotId] = useState<number | null>(null);
  const [durationDays, setDurationDays] = useState(30);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [renewTarget, setRenewTarget] = useState<BannerAd | null>(null);
  const [renewDurationDays, setRenewDurationDays] = useState(30);

  const selectedSlot = slots.find((s) => s.id === selectedSlotId) ?? null;
  const totalAmount = selectedSlot ? selectedSlot.dailyPrice * durationDays : 0;

  const isBusy = isUploading || isCreating || isSubmitting;

  const kpis = useMemo(() => {
    const activeCount = banners.filter((b) => b.status === 'Active').length;
    const totalImpressions = banners.reduce((sum, b) => sum + b.impressionsCount, 0);
    const totalClicks = banners.reduce((sum, b) => sum + b.clicksCount, 0);
    const ctr = totalImpressions > 0 ? (totalClicks / totalImpressions) * 100 : 0;
    return { activeCount, totalImpressions, totalClicks, ctr };
  }, [banners]);

  const resetForm = () => {
    setFile(null);
    setPreviewUrl(null);
    setDestinationUrl('');
    setSelectedSlotId(null);
    setDurationDays(30);
  };

  const pickFile = (candidate: File | null) => {
    if (!candidate) return;
    if (!candidate.type.startsWith('image/')) {
      toast.error('فقط فایل تصویری مجاز است.');
      return;
    }
    setFile(candidate);
    setPreviewUrl(URL.createObjectURL(candidate));
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    pickFile(event.dataTransfer.files?.[0] ?? null);
  };

  const handleSubmit = async () => {
    if (!file) {
      toast.error('انتخاب تصویر بنر الزامی است.');
      return;
    }
    if (!destinationUrl.trim()) {
      toast.error('لینک مقصد بنر الزامی است.');
      return;
    }
    if (!selectedSlot) {
      toast.error('انتخاب جایگاه تبلیغاتی الزامی است.');
      return;
    }
    if (durationDays <= 0) {
      toast.error('مدت زمان نمایش باید بزرگ‌تر از صفر باشد.');
      return;
    }

    const uploadResult = await uploadImage(file);
    if (!('data' in uploadResult) || !uploadResult.data?.success || !uploadResult.data.data) {
      return;
    }

    const createResult = await createBannerAd({
      imageUrl: uploadResult.data.data,
      destinationUrl: destinationUrl.trim(),
      placement: selectedSlot.placement,
      bannerSlotId: selectedSlot.id,
      durationDays
    });

    if (!('data' in createResult) || !createResult.data?.success || !createResult.data.data) {
      return;
    }

    const submitResult = await submitForReview(createResult.data.data.id);
    if ('data' in submitResult && submitResult.data?.success && submitResult.data.data?.paymentRedirectUrl) {
      window.location.href = submitResult.data.data.paymentRedirectUrl;
      return;
    }

    resetForm();
    setIsFormOpen(false);
  };

  const openRenewModal = (banner: BannerAd) => {
    setRenewTarget(banner);
    setRenewDurationDays(banner.durationDays > 0 ? banner.durationDays : 30);
  };

  // برای پیش‌نمایش تمدید، از کل کاتالوگ جایگاه‌ها (نه فقط جایگاه‌های فعال) جست‌وجو می‌کنیم؛
  // چون ممکن است جایگاه بنر فعلی، بعد از ثبت رزرو اولیه، غیرفعال شده باشد.
  const renewSlot = renewTarget ? (slotsData?.data ?? []).find((s) => s.id === renewTarget.bannerSlotId) : null;
  const renewBaseAmount = renewSlot ? renewSlot.dailyPrice * renewDurationDays : 0;
  const renewDiscountAmount = renewBaseAmount * RENEWAL_DISCOUNT_RATE;
  const renewFinalAmount = renewBaseAmount - renewDiscountAmount;

  const handleConfirmRenew = async () => {
    if (!renewTarget) return;
    const result = await renewBannerAd({ id: renewTarget.id, durationDays: renewDurationDays });
    if ('data' in result && result.data?.success) {
      setRenewTarget(null);
    }
  };

  return (
    <div className="flex flex-col gap-6" dir="rtl">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">مدیریت و رزرو بنرهای تبلیغاتی</h1>
          <p className="mt-1 text-sm text-slate-500">رزرو جایگاه تبلیغاتی، پیگیری وضعیت تایید، و مشاهدهٔ آمار نمایش/کلیک بنرهای شما</p>
        </div>
        <button
          onClick={() => setIsFormOpen((v) => !v)}
          className="inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700"
        >
          <ImagePlus className="h-4 w-4" />
          {isFormOpen ? 'بستن فرم رزرو' : 'ثبت بنر جدید'}
        </button>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <KpiCard icon={Layers} iconBg="#ecfdf5" iconFg="#059669" label="بنرهای فعال" value={kpis.activeCount.toLocaleString('fa-IR')} />
        <KpiCard icon={Eye} iconBg="#eff6ff" iconFg="#2563eb" label="مجموع نمایش" value={kpis.totalImpressions.toLocaleString('fa-IR')} />
        <KpiCard
          icon={MousePointerClick}
          iconBg="#fdf4ff"
          iconFg="#a21caf"
          label="نرخ کلیک (CTR)"
          value={`${kpis.ctr.toLocaleString('fa-IR', { maximumFractionDigits: 2 })}٪`}
          hint={`${kpis.totalClicks.toLocaleString('fa-IR')} کلیک از ${kpis.totalImpressions.toLocaleString('fa-IR')} نمایش`}
        />
      </div>

      <div className="grid transition-[grid-template-rows] duration-300 ease-in-out" style={{ gridTemplateRows: isFormOpen ? '1fr' : '0fr' }}>
        <div className="overflow-hidden">
          <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm">
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
              <div className="lg:col-span-2">
                <h2 className="mb-3 text-sm font-bold text-slate-700">۱. انتخاب جایگاه تبلیغاتی</h2>
                {isLoadingSlots && <div className="h-24 animate-pulse rounded-2xl bg-slate-100" />}
                <div className="flex flex-col gap-3">
                  {slots.map((slot) => (
                    <SlotCard key={slot.id} slot={slot} selected={slot.id === selectedSlotId} onSelect={() => setSelectedSlotId(slot.id)} />
                  ))}
                </div>
              </div>

              <div className="lg:col-span-3">
                <h2 className="mb-3 text-sm font-bold text-slate-700">۲. اطلاعات بنر</h2>

                <div
                  onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
                  onDragLeave={() => setIsDragging(false)}
                  onDrop={handleDrop}
                  onClick={() => fileInputRef.current?.click()}
                  className={`flex cursor-pointer flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed p-6 text-center transition-colors ${
                    isDragging ? 'border-emerald-500 bg-emerald-50/50' : 'border-slate-200 hover:border-emerald-300'
                  }`}
                >
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    hidden
                    onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
                  />
                  {previewUrl ? (
                    <img src={previewUrl} alt="پیش‌نمایش بنر" className="max-h-32 rounded-lg object-contain" />
                  ) : (
                    <>
                      <UploadCloud className="h-8 w-8 text-slate-300" />
                      <p className="text-sm font-semibold text-slate-600">تصویر بنر را بکشید و رها کنید یا کلیک کنید</p>
                      <p className="text-xs text-slate-400">JPG، PNG یا GIF{selectedSlot ? ` — ابعاد پیشنهادی ${selectedSlot.dimensions}` : ''}</p>
                    </>
                  )}
                </div>

                <div className="mt-4">
                  <label className="mb-1.5 block text-xs font-semibold text-slate-500">لینک مقصد (پس از کلیک کاربر به این آدرس هدایت می‌شود)</label>
                  <div className="relative">
                    <Link2 className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <input
                      type="url"
                      value={destinationUrl}
                      onChange={(e) => setDestinationUrl(e.target.value)}
                      placeholder="https://example.com"
                      className="w-full rounded-lg border border-slate-200 bg-white py-2 pl-3 pr-9 text-sm text-slate-700 focus:border-emerald-500 focus:outline-none"
                    />
                  </div>
                </div>

                <div className="mt-4">
                  <label className="mb-1.5 block text-xs font-semibold text-slate-500">مدت زمان نمایش (روز)</label>
                  <div className="flex flex-wrap items-center gap-2">
                    {DURATION_PRESETS.map((d) => (
                      <button
                        key={d}
                        type="button"
                        onClick={() => setDurationDays(d)}
                        className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors ${
                          durationDays === d ? 'bg-emerald-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                        }`}
                      >
                        {d.toLocaleString('fa-IR')} روز
                      </button>
                    ))}
                    <input
                      type="number"
                      min={1}
                      value={durationDays}
                      onChange={(e) => setDurationDays(Math.max(1, Number(e.target.value) || 0))}
                      className="w-20 rounded-lg border border-slate-200 bg-white px-2 py-1.5 text-center text-xs text-slate-700 focus:border-emerald-500 focus:outline-none"
                    />
                  </div>
                  <p className="mt-1.5 flex items-center gap-1 text-[11px] text-slate-400">
                    <Calendar className="h-3 w-3" />
                    تاریخ دقیق شروع نمایش پس از تایید ادمین مشخص می‌شود.
                  </p>
                </div>

                <div className="mt-4 rounded-xl bg-slate-50 p-3.5">
                  <div className="flex items-center justify-between text-xs text-slate-500">
                    <span>جایگاه انتخابی</span>
                    <span className="font-semibold text-slate-700">{selectedSlot ? selectedSlot.title : '—'}</span>
                  </div>
                  <div className="mt-1.5 flex items-center justify-between text-xs text-slate-500">
                    <span>قیمت روزانه × مدت نمایش</span>
                    <span className="font-semibold text-slate-700">
                      {selectedSlot ? `${formatToman(selectedSlot.dailyPrice)} × ${durationDays.toLocaleString('fa-IR')}` : '—'}
                    </span>
                  </div>
                  <div className="mt-2 flex items-center justify-between border-t border-slate-200 pt-2">
                    <span className="text-sm font-semibold text-slate-600">مبلغ کل</span>
                    <span className="text-base font-extrabold text-emerald-700">{formatToman(totalAmount)} تومان</span>
                  </div>
                </div>

                <div className="mt-4 flex items-center gap-2">
                  <button
                    onClick={handleSubmit}
                    disabled={isBusy}
                    className="inline-flex flex-1 items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isBusy ? 'در حال پردازش...' : 'ثبت و ارسال برای تایید'}
                  </button>
                  <button
                    onClick={() => { setIsFormOpen(false); resetForm(); }}
                    className="rounded-xl bg-slate-100 px-4 py-2.5 text-sm font-semibold text-slate-600 transition-colors hover:bg-slate-200"
                  >
                    انصراف
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-sm">
        {isLoading && (
          <div className="flex flex-col gap-2 p-4">
            {Array.from({ length: 3 }, (_, i) => <div key={i} className="h-14 animate-pulse rounded-xl bg-slate-100" />)}
          </div>
        )}

        {!isLoading && banners.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-2 py-16 text-center">
            <Layers className="h-10 w-10 text-slate-300" />
            <p className="font-semibold text-slate-600">شما هنوز بنری رزرو نکرده‌اید</p>
          </div>
        )}

        {!isLoading && banners.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-slate-100 text-right text-xs font-semibold text-slate-400">
                  <th className="px-4 py-3">بنر / جایگاه</th>
                  <th className="px-4 py-3">بازه نمایش</th>
                  <th className="px-4 py-3">آمار (نمایش / کلیک / CTR)</th>
                  <th className="px-4 py-3">وضعیت</th>
                  <th className="px-4 py-3">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {banners.map((banner) => {
                  const style = STATUS_STYLES[banner.status];
                  const ctr = banner.impressionsCount > 0 ? (banner.clicksCount / banner.impressionsCount) * 100 : 0;
                  const canRenew = banner.status === 'Active' || banner.status === 'Expired';
                  return (
                    <tr key={banner.id} className="border-b border-slate-50 last:border-0 hover:bg-slate-50/40">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <img src={banner.imageUrl} alt="بنر" className="h-10 w-16 flex-shrink-0 rounded-lg object-cover" />
                          <div>
                            <p className="font-semibold text-slate-700">{banner.slotTitle ?? PLACEMENT_LABELS[banner.placement]}</p>
                            {banner.rejectionReason && <p className="mt-0.5 max-w-[220px] truncate text-xs text-rose-500">دلیل رد: {banner.rejectionReason}</p>}
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-xs text-slate-500">
                        {banner.startDate ? (
                          <span>{formatDate(banner.startDate)} تا {formatDate(banner.endDate)}</span>
                        ) : (
                          <span className="text-slate-400">پس از تایید مشخص می‌شود ({banner.durationDays.toLocaleString('fa-IR')} روز)</span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3 text-xs text-slate-600">
                          <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5 text-slate-400" />{banner.impressionsCount.toLocaleString('fa-IR')}</span>
                          <span className="inline-flex items-center gap-1"><MousePointerClick className="h-3.5 w-3.5 text-slate-400" />{banner.clicksCount.toLocaleString('fa-IR')}</span>
                          <span className="inline-flex items-center gap-1 font-semibold text-slate-700">
                            <BarChart3 className="h-3.5 w-3.5 text-slate-400" />
                            {ctr.toLocaleString('fa-IR', { maximumFractionDigits: 2 })}٪
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold"
                          style={{ background: style.bg, color: style.fg, borderColor: style.border }}
                        >
                          <style.Icon className="h-3 w-3" />
                          {style.label}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        {canRenew ? (
                          <button
                            onClick={() => openRenewModal(banner)}
                            className="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-2.5 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-emerald-50 hover:text-emerald-700"
                          >
                            <RefreshCw className="h-3.5 w-3.5" />
                            تمدید
                          </button>
                        ) : (
                          <span className="text-xs text-slate-300">—</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {renewTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={() => setRenewTarget(null)}>
          <div className="w-full max-w-sm rounded-2xl bg-white p-5 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-800">تمدید بنر تبلیغاتی</h2>
              <button onClick={() => setRenewTarget(null)} className="text-slate-400 hover:text-slate-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <p className="mb-3 text-xs text-slate-500">{renewTarget.slotTitle ?? PLACEMENT_LABELS[renewTarget.placement]}</p>

            <label className="mb-1.5 block text-xs font-semibold text-slate-500">مدت زمان تمدید (روز)</label>
            <div className="mb-4 flex flex-wrap items-center gap-2">
              {DURATION_PRESETS.map((d) => (
                <button
                  key={d}
                  type="button"
                  onClick={() => setRenewDurationDays(d)}
                  className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors ${
                    renewDurationDays === d ? 'bg-emerald-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {d.toLocaleString('fa-IR')} روز
                </button>
              ))}
              <input
                type="number"
                min={1}
                value={renewDurationDays}
                onChange={(e) => setRenewDurationDays(Math.max(1, Number(e.target.value) || 0))}
                className="w-20 rounded-lg border border-slate-200 bg-white px-2 py-1.5 text-center text-xs text-slate-700 focus:border-emerald-500 focus:outline-none"
              />
            </div>

            <div className="flex flex-col gap-2 rounded-xl bg-slate-50 p-3.5 text-sm">
              <div className="flex items-center justify-between text-slate-500">
                <span>مبلغ پایه</span>
                <span className="font-semibold text-slate-700">{formatToman(renewBaseAmount)} تومان</span>
              </div>
              <div className="flex items-center justify-between text-emerald-600">
                <span>تخفیف تمدید (۲۰٪)</span>
                <span className="font-semibold">- {formatToman(renewDiscountAmount)} تومان</span>
              </div>
              <div className="flex items-center justify-between border-t border-slate-200 pt-2">
                <span className="font-semibold text-slate-600">مبلغ نهایی (از کیف پول کسر می‌شود)</span>
                <span className="text-base font-extrabold text-emerald-700">{formatToman(renewFinalAmount)} تومان</span>
              </div>
            </div>

            <button
              onClick={handleConfirmRenew}
              disabled={isRenewing}
              className="mt-4 flex w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Wallet className="h-4 w-4" />
              {isRenewing ? 'در حال پردازش...' : 'تایید و کسر از کیف پول'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
