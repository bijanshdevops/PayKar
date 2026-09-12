import { useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  useCreateCompanyMutation,
  useDeleteCompanyDocumentMutation,
  useGetCompanyDocumentsQuery,
  useGetMyCompanyQuery,
  useRequestVerificationMutation,
  useUpdateCompanyMutation,
  useUploadCompanyBannerMutation,
  useUploadCompanyDocumentMutation,
  useUploadCompanyLogoMutation,
  type CompanyDocument
} from '@/features/companies/companyApi';
import { useGetCitiesQuery, useGetIndustrialZonesQuery, useGetProvincesQuery } from '@/features/geography/geographyApi';
import { useZoneLabelMap } from '@/features/geography/useZoneLabel';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { useRefreshSessionMutation } from '@/features/auth/authApi';
import { sessionEstablished } from '@/features/auth/authSlice';
import {
  BuildingIcon,
  ShieldIcon,
  CameraIcon,
  CheckCircleIcon,
  ClockIcon,
  XCircleIcon,
  TrashIcon,
  UploadCloudIcon,
  GlobeIcon,
  MapPinIcon,
  ImageFileIcon,
  FileTextIcon,
  HeadsetIcon
} from '@/components/icons/DashboardIcons';
import { MailIcon, PhoneIcon } from '@/components/icons/AuthIcons';

interface CompanyFormValues {
  name: string;
  nationalId: string;
  registrationNumber: string;
  provinceId: string;
  cityId: string;
  industrialZoneId: string;
  addressDetail: string;
  industryCategory: string;
  contactPhoneNumber: string;
  website: string;
  email: string;
  description: string;
}

/** سه اسلات ثابت مدارک احراز هویت — طبق فاز بازطراحی صفحه پروفایل شرکت. */
const DOCUMENT_SLOTS: { type: string; title: string }[] = [
  { type: 'RegistrationCertificate', title: 'روزنامه رسمی / آگهی تاسیس' },
  { type: 'IndustrialLicense', title: 'پروانه بهره‌برداری یا جواز کسب' },
  { type: 'NationalIdCard', title: 'معرفی‌نامه نماینده / کارت ملی مدیرعامل' }
];

function formatFileSize(bytes: number): string {
  if (!bytes || bytes <= 0) return 'حجم نامشخص';
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024)).toLocaleString('fa-IR')} کیلوبایت`;
  return `${(bytes / (1024 * 1024)).toLocaleString('fa-IR', { maximumFractionDigits: 1 })} مگابایت`;
}

function formatUploadDate(iso: string): string {
  return new Date(iso).toLocaleDateString('fa-IR');
}

function isImageFile(fileName: string): boolean {
  return /\.(png|jpe?g|webp|gif)$/i.test(fileName);
}

// طبق بررسی فنی «چرا آپلود لوگو/بنر/مدارک با خطا مواجه می‌شود»: اندپوینت‌های بک‌اند و مسیر
// FormData این صفحه از قبل کاملاً درست سیم‌کشی شده بودند (دقیقاً هم‌الگو با UploadCandidateAvatar و
// CreateSupportTicket که پیش‌تر تایید شد). نقطهٔ ضعف واقعی این بود که برخلاف آن دو، هیچ اعتبارسنجی
// سمت کلاینتِ فرمت/حجم اینجا وجود نداشت — نه در کلیک-انتخاب و نه در Drag&Drop اسلات مدارک — با اینکه
// متن راهنما به کارجو «حداکثر حجم ۵ مگابایت» را وعده می‌داد. این ثابت‌ها و اعتبارسنجی زیر دقیقاً
// همان قانون بک‌اند (که اکنون واقعاً اعمال می‌شود) را پیش از ارسال درخواست شبکه بازخورد می‌دهند.
//
// طبق دومین دور اصلاح «هماهنگ‌سازی اعتبارسنجی فایل»:
// ۱. پیش‌تر خصیصهٔ accept ورودی‌های فایل (image/*  یا  image/*,.pdf) خیلی وسیع‌تر از فهرست واقعی
//    مجاز در این تابع‌ها بود — یعنی مرورگر اجازهٔ انتخاب gif/bmp/tiff/svg و... را می‌داد، ولی همین
//    اعتبارسنجی بلافاصله بعد از انتخاب آن را رد می‌کرد (دقیقاً همان تجربهٔ کاربری «انتخاب کردم ولی خطا
//    گرفتم» که گزارش شده بود). اکنون هر دو منبع (خصیصهٔ accept ورودی HTML و این تابع‌ها) از یک فهرست
//    واحد (ALLOWED_IMAGE_ACCEPT/ALLOWED_DOCUMENT_ACCEPT) ساخته می‌شوند و هرگز از هم عقب نمی‌مانند.
// ۲. پسوند فایل همیشه با toLowerCase() نرمال می‌شود (فایل‌های .JPG/.PNG با حروف بزرگ هم درست کار
//    می‌کنند) و اعتبارسنجی هم MIME Type و هم پسوند فایل را می‌پذیرد (کافی است یکی از این دو معتبر
//    باشد) — چون برخی مرورگرها/سیستم‌عامل‌ها برای فرمت‌هایی مثل webp یا فایل‌های Drag&Drop از منابع
//    غیرمعمول، file.type را خالی یا نامتعارف گزارش می‌کنند؛ تکیه‌ی صرف بر MIME Type در آن حالت‌ها
//    فایل کاملاً معتبر را به‌اشتباه رد می‌کرد.
const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024;
const ALLOWED_IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
const ALLOWED_IMAGE_MIME_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
const ALLOWED_IMAGE_ACCEPT = [...ALLOWED_IMAGE_MIME_TYPES, ...ALLOWED_IMAGE_EXTENSIONS].join(',');

const ALLOWED_DOCUMENT_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.pdf'];
const ALLOWED_DOCUMENT_MIME_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
const ALLOWED_DOCUMENT_ACCEPT = [...ALLOWED_DOCUMENT_MIME_TYPES, ...ALLOWED_DOCUMENT_EXTENSIONS].join(',');

function getFileExtension(fileName: string): string {
  return `.${fileName.split('.').pop()?.toLowerCase() ?? ''}`;
}

function validateImageFile(file: File): string | null {
  const extensionOk = ALLOWED_IMAGE_EXTENSIONS.includes(getFileExtension(file.name));
  const mimeOk = ALLOWED_IMAGE_MIME_TYPES.includes(file.type.toLowerCase());
  if (!extensionOk && !mimeOk) return 'فرمت تصویر باید jpg، jpeg، png یا webp باشد.';
  if (file.size > MAX_IMAGE_SIZE_BYTES) return 'حجم تصویر نباید بیشتر از ۵ مگابایت باشد.';
  return null;
}

function validateDocumentFile(file: File): string | null {
  const extensionOk = ALLOWED_DOCUMENT_EXTENSIONS.includes(getFileExtension(file.name));
  const mimeOk = ALLOWED_DOCUMENT_MIME_TYPES.includes(file.type.toLowerCase());
  if (!extensionOk && !mimeOk) return 'فرمت مدرک باید pdf، jpg، jpeg یا png باشد.';
  if (file.size > MAX_IMAGE_SIZE_BYTES) return 'حجم مدرک نباید بیشتر از ۵ مگابایت باشد.';
  return null;
}

/** بج وضعیت احراز هویت روی کارت هدر — طبق سه حالت صریح خواسته‌شده در فاز بازطراحی. */
function VerificationStatusPill({ status }: { status: string }) {
  if (status === 'Verified') {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-200 bg-emerald-50 px-3 py-1.5 text-xs font-bold text-emerald-700">
        <CheckCircleIcon size={15} /> احراز هویت شده
      </span>
    );
  }
  if (status === 'PendingVerification') {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-bold text-amber-700">
        <ClockIcon size={15} /> در حال بررسی مدارک
      </span>
    );
  }
  if (status === 'Rejected') {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-bold text-rose-700">
        <XCircleIcon size={15} /> رد شده — نیاز به اصلاح مدارک
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-100 px-3 py-1.5 text-xs font-bold text-slate-600">
      <ClockIcon size={15} /> احراز هویت نشده
    </span>
  );
}

/** بج وضعیت هر مدرک — چون بررسی در سطح کل شرکت انجام می‌شود (نه هر مدرک جدا)، از وضعیت شرکت مشتق می‌شود. */
function DocumentStatusBadge({ companyStatus }: { companyStatus: string }) {
  if (companyStatus === 'Verified') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-bold text-emerald-700">
        <CheckCircleIcon size={12} /> تایید شده
      </span>
    );
  }
  if (companyStatus === 'Rejected') {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-rose-50 px-2 py-0.5 text-[11px] font-bold text-rose-700">
        <XCircleIcon size={12} /> رد شده
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-bold text-amber-700">
      <ClockIcon size={12} /> در انتظار بررسی
    </span>
  );
}

/** یک اسلات مدرک — یا Dropzone خالی برای آپلود، یا کارت فایل آپلودشده با پیش‌نمایش/حذف. */
function DocumentSlotCard({
  slot,
  document,
  companyStatus,
  disabled,
  isUploading,
  onUpload,
  onDelete
}: {
  slot: { type: string; title: string };
  document: CompanyDocument | undefined;
  companyStatus: string;
  disabled: boolean;
  isUploading: boolean;
  onUpload: (file: File) => void;
  onDelete: () => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);

  const handleFiles = (files: FileList | null) => {
    const file = files?.[0];
    if (file) onUpload(file);
  };

  if (document) {
    return (
      <div className="flex flex-col rounded-2xl border border-slate-100 bg-white p-4 shadow-sm">
        <div className="mb-2 flex items-start justify-between gap-2">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-slate-50 text-slate-500">
            {isImageFile(document.fileName) ? <ImageFileIcon size={22} /> : <FileTextIcon size={22} />}
          </span>
          <DocumentStatusBadge companyStatus={companyStatus} />
        </div>
        <div className="min-w-0">
          <div className="truncate text-xs font-bold text-slate-500">{slot.title}</div>
          <div className="truncate text-sm font-semibold text-slate-700" title={document.fileName}>
            {document.fileName}
          </div>
          <div className="mt-0.5 text-[11px] text-slate-400">
            {formatFileSize(document.fileSizeBytes)} · {formatUploadDate(document.uploadedAtUtc)}
          </div>
        </div>
        <div className="mt-3 flex items-center gap-2 border-t border-slate-50 pt-2.5">
          <a
            href={document.fileUrl}
            target="_blank"
            rel="noreferrer"
            className="flex-1 rounded-lg py-1.5 text-center text-xs font-semibold text-slate-500 transition hover:bg-slate-100"
            style={{ textDecoration: 'none' }}
          >
            پیش‌نمایش
          </a>
          <button
            type="button"
            onClick={onDelete}
            className="flex items-center justify-center rounded-lg p-1.5 text-slate-400 transition hover:bg-rose-50 hover:text-rose-600"
            aria-label="حذف مدرک"
          >
            <TrashIcon size={16} />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div
      onDragOver={(e) => {
        e.preventDefault();
        if (!disabled) setIsDragOver(true);
      }}
      onDragLeave={() => setIsDragOver(false)}
      onDrop={(e) => {
        e.preventDefault();
        setIsDragOver(false);
        if (!disabled) handleFiles(e.dataTransfer.files);
      }}
      onClick={() => !disabled && inputRef.current?.click()}
      className={`flex flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed p-4 text-center transition ${
        disabled
          ? 'cursor-not-allowed border-slate-200 bg-slate-50/50 opacity-60'
          : `cursor-pointer bg-slate-50/50 hover:border-blue-400 ${isDragOver ? 'border-blue-400 bg-blue-50/40' : 'border-slate-200'}`
      }`}
    >
      <input
        ref={inputRef}
        type="file"
        accept={ALLOWED_DOCUMENT_ACCEPT}
        className="hidden"
        disabled={disabled}
        onChange={(e) => {
          handleFiles(e.target.files);
          e.target.value = '';
        }}
      />
      <UploadCloudIcon size={26} />
      <div className="text-xs font-bold text-slate-600">{slot.title}</div>
      <div className="text-[11px] text-slate-400">
        {isUploading ? 'در حال آپلود...' : 'کشیدن و رها کردن فایل یا کلیک برای انتخاب'}
      </div>
      <div className="text-[10px] text-slate-300">PDF، JPG یا PNG — حداکثر ۵ مگابایت</div>
    </div>
  );
}

export default function CompanyProfilePage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { refreshToken, user } = useAppSelector((state) => state.auth);
  const wasCompanyManagerBeforeCreate = user?.roles.includes('CompanyManager') ?? false;
  const { data: myCompany, isLoading: isLoadingCompany } = useGetMyCompanyQuery();
  const [createCompany, { isLoading: isCreating }] = useCreateCompanyMutation();
  const [updateCompany, { isLoading: isUpdating }] = useUpdateCompanyMutation();
  const [requestVerification] = useRequestVerificationMutation();
  const [refreshSession] = useRefreshSessionMutation();
  const [uploadCompanyLogo, { isLoading: isUploadingLogo }] = useUploadCompanyLogoMutation();
  const [uploadCompanyBanner, { isLoading: isUploadingBanner }] = useUploadCompanyBannerMutation();
  const [uploadCompanyDocument] = useUploadCompanyDocumentMutation();
  const [deleteCompanyDocument] = useDeleteCompanyDocumentMutation();

  const { register, handleSubmit, watch, formState: { errors } } = useForm<CompanyFormValues>();
  const [serverError, setServerError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [uploadingSlotType, setUploadingSlotType] = useState<string | null>(null);
  const [documentError, setDocumentError] = useState<string | null>(null);

  const logoInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);

  const provinceId = watch('provinceId');
  const cityId = watch('cityId');

  const { data: provinces } = useGetProvincesQuery();
  const { data: cities } = useGetCitiesQuery(provinceId || undefined, { skip: !provinceId });
  const { data: zones } = useGetIndustrialZonesQuery(cityId || undefined, { skip: !cityId });
  const zoneLabelById = useZoneLabelMap();

  const company = myCompany?.data;

  const { data: documentsData } = useGetCompanyDocumentsQuery(company?.id ?? '', { skip: !company });
  const documents = documentsData?.data ?? [];
  const documentBySlotType = new Map(documents.map((d) => [d.documentType, d]));

  const onSubmit = async (values: CompanyFormValues) => {
    setServerError(null);
    setSuccessMessage(null);

    const result = company
      ? await updateCompany({
          id: company.id,
          body: {
            name: values.name,
            addressDetail: values.addressDetail,
            industryCategory: values.industryCategory,
            industrialZoneId: values.industrialZoneId,
            contactPhoneNumber: values.contactPhoneNumber,
            website: values.website,
            email: values.email,
            description: values.description
          }
        })
      : await createCompany({
          name: values.name,
          nationalId: values.nationalId,
          registrationNumber: values.registrationNumber,
          industrialZoneId: values.industrialZoneId,
          addressDetail: values.addressDetail,
          industryCategory: values.industryCategory,
          contactPhoneNumber: values.contactPhoneNumber,
          website: values.website,
          email: values.email,
          description: values.description
        });

    if (!('data' in result && result.data?.success)) {
      const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
      setServerError(errorResponse?.data?.message ?? 'ثبت اطلاعات شرکت با خطا مواجه شد.');
      return;
    }

    // طبق ADR-010: اولین ثبت موفق شرکت، نقش CompanyManager را در Backend اعطا می‌کند؛
    // با فراخوانی refreshSession توکن/نقش‌های تازه را می‌گیریم تا بدون خروج/ورود مجدد،
    // ناوبری سایدبار/داشبورد بلافاصله دسترسی پنل شرکت را نشان دهد.
    if (!company && !wasCompanyManagerBeforeCreate && refreshToken) {
      const refreshResult = await refreshSession({ refreshToken });
      if ('data' in refreshResult && refreshResult.data?.success && refreshResult.data.data) {
        const { accessToken, refreshToken: newRefreshToken, user: refreshedUser } = refreshResult.data.data;
        dispatch(sessionEstablished({ accessToken, refreshToken: newRefreshToken, user: refreshedUser }));
      }
      setSuccessMessage('شرکت با موفقیت ثبت شد! دسترسی پنل شرکت برای شما فعال شد.');
      window.setTimeout(() => navigate('/dashboard'), 1200);
      return;
    }

    // طبق موکاپ بازطراحی: دکمهٔ اصلی هم ذخیره می‌کند و هم (در صورت نیاز) درخواست احراز هویت را
    // ارسال می‌کند — یک اکشن ترکیبی واحد به‌جای دو دکمهٔ جدا. RequestVerification در بک‌اند فقط
    // وقتی شرکت از قبل «Verified» باشد خطا می‌دهد، پس فراخوانی آن برای بقیهٔ وضعیت‌ها بی‌خطر است.
    if (company && company.verificationStatus !== 'Verified') {
      await requestVerification(company.id);
    }

    setSuccessMessage('اطلاعات شرکت با موفقیت ذخیره و برای بررسی ارسال شد.');
  };

  const handleUploadSlotDocument = async (documentType: string, file: File) => {
    setDocumentError(null);

    // اعتبارسنجی سمت کلاینت پیش از ارسال — هم برای انتخاب با کلیک و هم Drag&Drop (هر دو مسیر به همین
    // تابع واحد ختم می‌شوند)؛ بک‌اند هم مستقل همین دو قانون را دوباره اعتبارسنجی می‌کند.
    const validationError = validateDocumentFile(file);
    if (validationError) {
      setDocumentError(validationError);
      return;
    }

    setUploadingSlotType(documentType);
    const result = await uploadCompanyDocument({ file, documentType });
    setUploadingSlotType(null);
    if (!('data' in result && result.data?.success)) {
      const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
      setDocumentError(errorResponse?.data?.message ?? 'آپلود مدرک با خطا مواجه شد.');
    }
  };

  const handleDeleteDocument = async (documentId: string) => {
    if (!window.confirm('آیا از حذف این مدرک مطمئن هستید؟')) return;
    setDocumentError(null);
    const result = await deleteCompanyDocument(documentId);
    if ('error' in result) {
      const errorResponse = result.error as { data?: { message?: string } };
      setDocumentError(errorResponse?.data?.message ?? 'حذف مدرک با خطا مواجه شد.');
    }
  };

  const handleLogoFileSelected = async (file: File) => {
    setServerError(null);
    const validationError = validateImageFile(file);
    if (validationError) {
      setServerError(validationError);
      return;
    }
    const result = await uploadCompanyLogo(file);
    if ('error' in result) {
      const errorResponse = result.error as { data?: { message?: string } };
      setServerError(errorResponse?.data?.message ?? 'آپلود لوگو با خطا مواجه شد.');
    }
  };

  const handleBannerFileSelected = async (file: File) => {
    setServerError(null);
    const validationError = validateImageFile(file);
    if (validationError) {
      setServerError(validationError);
      return;
    }
    const result = await uploadCompanyBanner(file);
    if ('error' in result) {
      const errorResponse = result.error as { data?: { message?: string } };
      setServerError(errorResponse?.data?.message ?? 'آپلود تصویر بنر با خطا مواجه شد.');
    }
  };

  if (isLoadingCompany) return <p className="text-sm text-slate-500">در حال بارگذاری...</p>;

  const locationLabel = company ? zoneLabelById.get(company.industrialZoneId) ?? null : null;

  return (
    <div className="mx-auto max-w-6xl">
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[7fr_3fr] lg:items-start">
        {/* ===================== ستون اصلی (۷۰٪) ===================== */}
        <div className="flex flex-col gap-5">
          {/* ---------- کارت هدر پروفایل ---------- */}
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3 }}
            className="overflow-hidden rounded-2xl border border-slate-100 bg-white shadow-sm"
          >
            <div
              className="relative h-40 w-full sm:h-48"
              style={{
                backgroundImage: company?.bannerUrl
                  ? `url(${company.bannerUrl})`
                  : 'linear-gradient(135deg, #19263a 0%, #1f3049 60%, #f59e0b 100%)',
                backgroundSize: 'cover',
                backgroundPosition: 'center'
              }}
            >
              <input
                ref={bannerInputRef}
                type="file"
                accept={ALLOWED_IMAGE_ACCEPT}
                className="hidden"
                disabled={!company || isUploadingBanner}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) handleBannerFileSelected(file);
                  e.target.value = '';
                }}
              />
              {company && (
                <button
                  type="button"
                  onClick={() => bannerInputRef.current?.click()}
                  disabled={isUploadingBanner}
                  className="absolute top-3 end-3 flex items-center gap-1.5 rounded-lg bg-black/40 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur transition hover:bg-black/55 disabled:opacity-60"
                >
                  <CameraIcon size={15} /> {isUploadingBanner ? 'در حال آپلود...' : 'تغییر تصویر بنر'}
                </button>
              )}
              {company && (
                <span className="absolute bottom-2 end-3 rounded-md bg-black/35 px-2 py-0.5 text-[10px] font-medium text-white/90 backdrop-blur">
                  JPG، PNG یا WEBP — حداکثر ۵ مگابایت
                </span>
              )}
            </div>

            <div className="px-5 pb-5 sm:px-6">
              <div className="flex flex-wrap items-end justify-between gap-3">
                <div className="flex items-end gap-4">
                  <div className="relative -mt-12 shrink-0">
                    <span className="flex h-24 w-24 items-center justify-center overflow-hidden rounded-full border-4 border-white bg-slate-100 text-slate-400 shadow-md">
                      {company?.logoUrl ? (
                        <img src={company.logoUrl} alt={company.name} className="h-full w-full object-cover" />
                      ) : (
                        <BuildingIcon size={34} />
                      )}
                    </span>
                    <input
                      ref={logoInputRef}
                      type="file"
                      accept={ALLOWED_IMAGE_ACCEPT}
                      className="hidden"
                      disabled={!company || isUploadingLogo}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (file) handleLogoFileSelected(file);
                        e.target.value = '';
                      }}
                    />
                    {company && (
                      <button
                        type="button"
                        onClick={() => logoInputRef.current?.click()}
                        disabled={isUploadingLogo}
                        className="absolute bottom-0 end-0 flex h-8 w-8 items-center justify-center rounded-full border-2 border-white bg-emerald-500 text-white shadow transition hover:bg-emerald-600 disabled:opacity-60"
                        aria-label="تغییر لوگو"
                        title="فرمت لوگو: JPG، PNG یا WEBP — حداکثر ۵ مگابایت"
                      >
                        <CameraIcon size={14} />
                      </button>
                    )}
                  </div>

                  <div className="pb-1">
                    <h1 className="text-lg font-extrabold text-slate-800 sm:text-xl">
                      {company?.name || 'ثبت پروفایل شرکت'}
                    </h1>
                    <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                      {company?.industryCategory && (
                        <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600">
                          {company.industryCategory}
                        </span>
                      )}
                      {locationLabel && (
                        <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-600">
                          <MapPinIcon size={12} /> {locationLabel}
                        </span>
                      )}
                    </div>
                    {company && (
                      <p className="mt-1.5 text-[10px] text-slate-400">لوگو: JPG، PNG یا WEBP — حداکثر ۵ مگابایت</p>
                    )}
                  </div>
                </div>

                <div className="pb-1">{company && <VerificationStatusPill status={company.verificationStatus} />}</div>
              </div>
            </div>
          </motion.div>

          {/* ---------- فرم اطلاعات پایه شرکت ---------- */}
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.05 }}
            className="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm sm:p-6"
          >
            <div className="mb-4 flex items-center gap-2">
              <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-800 text-xs font-bold text-white">۱</span>
              <h2 className="text-sm font-bold text-slate-700">اطلاعات پایه شرکت</h2>
            </div>

            <form onSubmit={handleSubmit(onSubmit)}>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <FormField label="نام رسمی شرکت" error={errors.name?.message}>
                  <input
                    className={inputClass}
                    defaultValue={company?.name}
                    {...register('name', { required: 'نام شرکت الزامی است.' })}
                  />
                </FormField>

                <FormField label="شناسه ملی" error={errors.nationalId?.message}>
                  <input
                    className={inputClass}
                    disabled={!!company}
                    defaultValue={company?.nationalId}
                    placeholder="شناسه‌ای ۱۱ رقمی"
                    {...register('nationalId', { required: !company ? 'شناسه ملی الزامی است.' : false })}
                  />
                </FormField>

                {!company && (
                  <FormField label="شماره ثبت شرکت" error={errors.registrationNumber?.message}>
                    <input
                      className={inputClass}
                      placeholder="شماره ثبت در ادارهٔ ثبت شرکت‌ها"
                      {...register('registrationNumber', { required: 'شماره ثبت الزامی است.' })}
                    />
                  </FormField>
                )}

                <FormField label="صنعت / حوزه فعالیت" error={errors.industryCategory?.message}>
                  <input
                    className={inputClass}
                    defaultValue={company?.industryCategory}
                    placeholder="نام دقیق دسته‌بندی صنعتی"
                    {...register('industryCategory', { required: 'دسته‌بندی صنعتی الزامی است.' })}
                  />
                </FormField>

                <FormField label="استان">
                  <select className={inputClass} {...register('provinceId', { required: !company })}>
                    <option value="">انتخاب کنید...</option>
                    {provinces?.data?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                </FormField>

                <FormField label="شهر">
                  <select className={inputClass} disabled={!provinceId} {...register('cityId', { required: !company })}>
                    <option value="">انتخاب کنید...</option>
                    {cities?.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </FormField>

                <FormField label="شهرک صنعتی" error={errors.industrialZoneId?.message}>
                  <select
                    className={inputClass}
                    defaultValue={company?.industrialZoneId}
                    disabled={!cityId && !company}
                    {...register('industrialZoneId', { required: 'انتخاب شهرک صنعتی الزامی است.' })}
                  >
                    <option value="">انتخاب کنید...</option>
                    {zones?.data?.map((z) => <option key={z.id} value={z.id}>{z.name}</option>)}
                  </select>
                </FormField>

                <FormField label="وب‌سایت شرکت" error={errors.website?.message}>
                  <div className="relative">
                    <span className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-slate-400">
                      <GlobeIcon size={16} />
                    </span>
                    <input
                      className={`${inputClass} ps-9`}
                      defaultValue={company?.website ?? ''}
                      placeholder="example.com"
                      {...register('website')}
                    />
                  </div>
                </FormField>

                <FormField label="ایمیل سازمانی" error={errors.email?.message}>
                  <div className="relative">
                    <span className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-slate-400">
                      <MailIcon size={16} />
                    </span>
                    <input
                      type="email"
                      className={`${inputClass} ps-9`}
                      defaultValue={company?.email ?? ''}
                      placeholder="info@example.com"
                      {...register('email')}
                    />
                  </div>
                </FormField>

                <FormField label="شماره تماس ثابت" error={errors.contactPhoneNumber?.message}>
                  <div className="relative">
                    <span className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-slate-400">
                      <PhoneIcon size={16} />
                    </span>
                    <input
                      type="tel"
                      className={`${inputClass} ps-9`}
                      placeholder="۰۹xxxxxxxxx یا ۰۲۱xxxxxxxx"
                      defaultValue={company?.contactPhoneNumber ?? ''}
                      {...register('contactPhoneNumber', {
                        required: 'شماره تماس شرکت الزامی است.',
                        pattern: { value: /^0\d{9,10}$/, message: 'شماره تماس باید با صفر شروع شود و ۱۰ یا ۱۱ رقم باشد.' }
                      })}
                    />
                  </div>
                </FormField>

                <div className="md:col-span-2">
                  <FormField label="آدرس دقیق" error={errors.addressDetail?.message}>
                    <textarea
                      className={inputClass}
                      rows={2}
                      defaultValue={company?.addressDetail}
                      {...register('addressDetail', { required: 'آدرس دقیق الزامی است.' })}
                    />
                  </FormField>
                </div>

                <div className="md:col-span-2">
                  <FormField label="درباره شرکت / معرفی کوتاه" error={errors.description?.message}>
                    <textarea
                      className={inputClass}
                      rows={3}
                      maxLength={1000}
                      placeholder="چند جمله دربارهٔ فعالیت، محصولات و سابقهٔ شرکت..."
                      defaultValue={company?.description ?? ''}
                      {...register('description')}
                    />
                  </FormField>
                </div>
              </div>

              {serverError && <p className="mt-3 text-sm font-medium text-rose-600">{serverError}</p>}
              {successMessage && <p className="mt-3 text-sm font-medium text-emerald-600">{successMessage}</p>}

              {/* ---------- بخش مدارک KYC — داخل همان فرم تا submit واحد ذخیره+ارسال کار کند ---------- */}
              <div className="mt-6 border-t border-slate-100 pt-5">
                <div className="mb-1 flex items-center gap-2">
                  <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-800 text-xs font-bold text-white">۲</span>
                  <h2 className="text-sm font-bold text-slate-700">اسناد و مدارک احراز هویت</h2>
                </div>
                <p className="mb-4 text-xs text-slate-400">
                  برای دریافت نشان تایید و انتشار آگهی، مدارک زیر را با فرمت PDF یا تصویر (JPG، PNG) و حداکثر حجم ۵ مگابایت بارگذاری کنید.
                </p>

                {!company && (
                  <p className="mb-3 rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-medium text-amber-700">
                    ابتدا اطلاعات پایه شرکت را ذخیره کنید تا بتوانید مدارک احراز هویت را بارگذاری کنید.
                  </p>
                )}

                {documentError && <p className="mb-3 text-sm font-medium text-rose-600">{documentError}</p>}

                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {DOCUMENT_SLOTS.map((slot) => (
                    <DocumentSlotCard
                      key={slot.type}
                      slot={slot}
                      document={documentBySlotType.get(slot.type)}
                      companyStatus={company?.verificationStatus ?? 'PendingVerification'}
                      disabled={!company}
                      isUploading={uploadingSlotType === slot.type}
                      onUpload={(file) => handleUploadSlotDocument(slot.type, file)}
                      onDelete={() => {
                        const doc = documentBySlotType.get(slot.type);
                        if (doc) handleDeleteDocument(doc.id);
                      }}
                    />
                  ))}
                </div>
              </div>

              {/* ---------- نوار اکشن پایین ---------- */}
              <div className="mt-6 flex flex-wrap items-center justify-end gap-3 border-t border-slate-100 pt-5">
                <button
                  type="button"
                  onClick={() => navigate(-1)}
                  className="rounded-xl border border-slate-200 px-6 py-2.5 text-sm font-medium text-slate-600 transition hover:bg-slate-100"
                >
                  انصراف
                </button>
                <button
                  type="submit"
                  disabled={isCreating || isUpdating}
                  className="rounded-xl bg-[#19263A] px-6 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-[#1F3049] disabled:opacity-60"
                >
                  {isCreating || isUpdating ? 'در حال ذخیره...' : 'ذخیره و ارسال جهت تایید'}
                </button>
              </div>
            </form>
          </motion.div>
        </div>

        {/* ===================== ستون کناری (۳۰٪) — راهنمای احراز هویت ===================== */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, delay: 0.1 }}
          className="sticky top-6 flex flex-col gap-4 rounded-2xl border border-blue-100 bg-gradient-to-b from-blue-50/50 to-white p-6 shadow-sm"
        >
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-bold text-slate-800">راهنمای احراز هویت</h2>
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-blue-100 text-blue-600">
              <ShieldIcon size={18} />
            </span>
          </div>

          <ol className="flex flex-col gap-4">
            {[
              { title: 'اطلاعات شرکت را وارد کنید', desc: 'اطلاعات پایه شرکت خود را به‌صورت دقیق و کامل وارد نمایید.' },
              { title: 'مدارک قانونی را بارگذاری کنید', desc: 'مدارک مورد نیاز را با کیفیت مناسب بارگذاری کنید.' },
              { title: 'بررسی توسط کارشناسان (حداکثر ۲۴ ساعت)', desc: 'تیم ما مدارک شما را بررسی کرده و نتیجه را از طریق پیامک و ایمیل اعلام می‌کند.' }
            ].map((step, idx) => (
              <li key={step.title} className="flex items-start gap-3">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-blue-600 text-xs font-bold text-white">
                  {(idx + 1).toLocaleString('fa-IR')}
                </span>
                <div className="min-w-0">
                  <div className="text-sm font-bold text-slate-700">{step.title}</div>
                  <div className="mt-0.5 text-xs leading-relaxed text-slate-500">{step.desc}</div>
                </div>
              </li>
            ))}
          </ol>

          <div className="flex items-start gap-3 rounded-xl border border-blue-100 bg-white p-3.5">
            <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-emerald-50 text-emerald-600">
              <ShieldIcon size={16} />
            </span>
            <div>
              <div className="text-xs font-bold text-slate-700">نکته امنیتی</div>
              <p className="mt-0.5 text-[11px] leading-relaxed text-slate-500">
                اطلاعات و مدارک شما به‌صورت کاملاً امن نگهداری می‌شود و فقط برای اهداف احراز هویت استفاده خواهد شد.
              </p>
            </div>
          </div>

          <div className="border-t border-blue-100 pt-4 text-center">
            <p className="mb-2 text-xs text-slate-500">سوالی دارید؟ با پشتیبانی ما در تماس باشید.</p>
            <Link
              to="/support"
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-blue-200 bg-white py-2.5 text-xs font-bold text-blue-700 transition hover:bg-blue-50"
              style={{ textDecoration: 'none' }}
            >
              <HeadsetIcon size={15} /> تماس با پشتیبانی
            </Link>
          </div>
        </motion.div>
      </div>
    </div>
  );
}

const inputClass =
  'w-full rounded-xl border border-slate-200 bg-white px-3.5 py-2.5 text-sm text-slate-700 outline-none transition focus:border-[#F59E0B] focus:ring-2 focus:ring-[#F59E0B]/20';

function FormField({
  label,
  error,
  children
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1.5 block text-xs font-bold text-slate-600">{label}</label>
      {children}
      {error && <span className="mt-1 block text-[11px] font-medium text-rose-600">{error}</span>}
    </div>
  );
}
