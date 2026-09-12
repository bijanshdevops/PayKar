import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { Building2, Briefcase, Megaphone, FileText, ClipboardList, Bookmark, Shield, LogOut, ChevronDown, User } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '@/app/hooks';
import { loggedOut } from '@/features/auth/authSlice';
import { useLogoutMutation } from '@/features/auth/authApi';
import { useGetMyCompanyQuery } from '@/features/companies/companyApi';
import { useGetMyResumeQuery } from '@/features/candidates/candidateApi';

const navLinks = [
  { to: '/job-ads', label: 'آگهی‌های شغلی' },
  { to: '/about', label: 'درباره ما' },
  { to: '/contact', label: 'تماس با ما' }
];

interface UserMenuItem {
  to: string;
  label: string;
  icon: ReactNode;
}

export default function Header() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // طبق بازطراحی جدید: هدر روی تصویر پس‌زمینهٔ هیرو fixed و ابتدا کاملاً شفاف است؛ با اسکرول
  // به شیشه‌ای تیرهٔ navy تبدیل می‌شود تا محتوای زیرین هرگز از پشتش دیده نشود.
  useEffect(() => {
    const handleScroll = () => setIsScrolled(window.scrollY > 20);
    handleScroll();
    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const user = useAppSelector((state) => state.auth.user);
  const refreshToken = useAppSelector((state) => state.auth.refreshToken);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [logout] = useLogoutMutation();

  const roles = user?.roles ?? [];
  const isCompany = roles.includes('CompanyManager');
  const isCandidate = roles.includes('Candidate');
  const isAdmin = roles.includes('Admin');

  // اولویت نمایش دراپ‌داون طبق طراحی محصولی: چون سیستم چندنقشی است (ADR-010: یک کاربر می‌تواند
  // هم‌زمان CompanyManager و Candidate باشد) و این هدر — برخلاف DashboardLayout — به «زمینهٔ» مسیر
  // جاری وابسته نیست (در همهٔ صفحات عمومی یکسان است)، باید یک اولویت ثابت انتخاب شود: کارفرما، سپس
  // کارجو، و پنل مدیریت صرفاً به‌عنوان Fallback برای کاربری که هیچ‌کدام از آن دو نقش کسب‌وکاری را ندارد.
  const primaryRole: 'company' | 'candidate' | 'admin' | 'generic' = isCompany
    ? 'company'
    : isCandidate
      ? 'candidate'
      : isAdmin
        ? 'admin'
        : 'generic';

  // نام شرکت/رزومه فقط بر اساس نقش واقعی کاربر واکشی می‌شود (نه بی‌قیدوشرط) تا برای کاربری که این
  // نقش را ندارد هزینهٔ درخواست اضافه به بک‌اند تحمیل نشود. همان endpoint/کش RTK Query که
  // DashboardLayout.tsx استفاده می‌کند، پس ورود به پنل کاربری نیازی به واکشی مجدد نیست.
  const { data: myCompanyData } = useGetMyCompanyQuery(undefined, { skip: !isCompany });
  const company = myCompanyData?.data;

  const { data: myResumeData } = useGetMyResumeQuery(undefined, { skip: !isCandidate });
  const resume = myResumeData?.data;

  useEffect(() => {
    if (!userMenuOpen) return;

    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [userMenuOpen]);

  const handleLogout = async () => {
    if (refreshToken) {
      await logout({ refreshToken });
    }
    dispatch(loggedOut());
    setMobileOpen(false);
    setUserMenuOpen(false);
    navigate('/');
  };

  const displayName =
    primaryRole === 'company'
      ? (company?.name ?? 'شرکت من')
      : primaryRole === 'candidate'
        ? (resume?.fullName || user?.mobileNumber || 'کارجو')
        : primaryRole === 'admin'
          ? 'مدیر پلتفرم'
          : (user?.mobileNumber ?? 'کاربر');

  const avatarUrl =
    primaryRole === 'company' ? (company?.logoUrl ?? null) : primaryRole === 'candidate' ? (resume?.avatarUrl ?? null) : null;
  const avatarInitial = displayName.trim().charAt(0) || '؟';

  // آیتم‌های دراپ‌داون دقیقاً طبق مرزهای نقشی درخواست‌شده: کارفرما هرگز آیتم کارجو نمی‌بیند و برعکس.
  const menuItems: UserMenuItem[] =
    primaryRole === 'company'
      ? [
          { to: '/company', label: 'مشاهده پروفایل', icon: <Building2 size={16} /> },
          { to: '/company/job-ads', label: 'آگهی‌های من', icon: <Briefcase size={16} /> },
          { to: '/company/banner-ads', label: 'بنرهای تبلیغاتی من', icon: <Megaphone size={16} /> }
        ]
      : primaryRole === 'candidate'
        ? [
            { to: '/resume/view', label: 'پروفایل من', icon: <FileText size={16} /> },
            { to: '/applications', label: 'درخواست‌های من', icon: <ClipboardList size={16} /> },
            { to: '/bookmarks', label: 'آگهی‌های نشان‌شده', icon: <Bookmark size={16} /> }
          ]
        : primaryRole === 'admin'
          ? [{ to: '/admin/dashboard', label: 'پنل مدیریت', icon: <Shield size={16} /> }]
          : [{ to: '/dashboard', label: 'پنل کاربری', icon: <User size={16} /> }];

  // طبق بازطراحی جدید: تصویر پس‌زمینه از هدر به بخش Hero منتقل شد. هدر خودش دیگر هیچ تصویری
  // ندارد — ابتدا (بالای صفحه) کاملاً شفاف است تا تصویر Hero زیرش دیده شود، و با اسکرول به یک
  // پس‌زمینهٔ شیشه‌ای تیرهٔ navy جامد تبدیل می‌شود تا محتوای زیرین هرگز از پشتش دیده نشود.
  const headerStateClass = isScrolled
    ? 'bg-[#19263A]/95 backdrop-blur-md border-b border-[#1F3049] shadow-md'
    : 'bg-transparent border-b border-transparent';

  return (
    <header className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${headerStateClass}`}>
      <div className="mx-auto flex h-20 w-full max-w-[1080px] items-center justify-between gap-8 px-6">
        <Link
          to="/"
          className="flex items-center gap-2.5 whitespace-nowrap text-lg font-extrabold text-white"
          onClick={() => setMobileOpen(false)}
        >
          {/* طبق اصلاح ظاهری محصولی: لوگو بزرگ‌تر شد و یک قاب سفید مدور پشت آن اضافه شد تا روی
              پس‌زمینهٔ تیرهٔ هدر عمومی (که در صفحهٔ اصلی هم دیده می‌شود) کاملاً واضح و برجسته باشد. */}
          <span className="flex items-center justify-center rounded-xl bg-white p-1.5 shadow-sm">
            <img src="/logo.png" alt="پی کار" className="h-10 w-10 object-contain" />
          </span>
          <span>پی کار</span>
        </Link>

        <nav className="hidden items-center gap-8 md:flex">
          {navLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) =>
                `text-sm font-medium transition-colors hover:text-[#F59E0B] ${isActive ? 'text-[#F59E0B]' : 'text-slate-200'}`
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          {user ? (
            <div className="relative" ref={userMenuRef}>
              <button
                type="button"
                onClick={() => setUserMenuOpen((v) => !v)}
                aria-haspopup="menu"
                aria-expanded={userMenuOpen}
                className={`flex items-center gap-2 rounded-xl border border-white/20 bg-white/10 py-1.5 ps-3 pe-1.5 text-sm font-medium text-white backdrop-blur-sm transition-all hover:bg-white/20 ${
                  userMenuOpen ? 'bg-white/20' : ''
                }`}
              >
                <span className="hidden max-w-[140px] truncate sm:inline">{displayName}</span>
                {avatarUrl ? (
                  <img src={avatarUrl} alt={displayName} className="h-7 w-7 rounded-full object-cover" />
                ) : (
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-white/15 text-xs font-bold">
                    {avatarInitial}
                  </span>
                )}
                <ChevronDown size={14} className={`shrink-0 transition-transform duration-200 ${userMenuOpen ? 'rotate-180' : ''}`} />
              </button>

              {userMenuOpen && (
                <div
                  role="menu"
                  className="header-usermenu-panel absolute end-0 top-[calc(100%+0.6rem)] w-60 overflow-hidden rounded-2xl border border-white/15 bg-[#19263A]/95 py-2 text-right shadow-2xl backdrop-blur-xl"
                >
                  <div className="border-b border-white/10 px-4 pb-2.5 pt-1 sm:hidden">
                    <p className="truncate text-sm font-bold text-white">{displayName}</p>
                  </div>

                  {menuItems.map((item) => (
                    <Link
                      key={item.to}
                      to={item.to}
                      role="menuitem"
                      onClick={() => setUserMenuOpen(false)}
                      className="flex items-center gap-2.5 px-4 py-2.5 text-sm font-medium text-slate-200 transition-colors hover:bg-white/10 hover:text-white"
                    >
                      {item.icon}
                      {item.label}
                    </Link>
                  ))}

                  <div className="my-1.5 border-t border-white/10" />

                  <button
                    type="button"
                    role="menuitem"
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2.5 px-4 py-2.5 text-sm font-medium text-rose-400 transition-colors hover:bg-rose-500/10 hover:text-rose-300"
                  >
                    <LogOut size={16} />
                    خروج از حساب
                  </button>
                </div>
              )}
            </div>
          ) : (
            <Link
              to="/login"
              className="rounded-xl bg-[#F59E0B] px-5 py-2 text-sm font-bold text-[#19263A] shadow-sm transition-all hover:-translate-y-0.5 hover:bg-[#D97706] active:translate-y-0"
            >
              ورود / ثبت‌نام
            </Link>
          )}

          <button
            type="button"
            className="text-2xl leading-none text-white md:hidden"
            aria-label="باز کردن منو"
            onClick={() => setMobileOpen((open) => !open)}
          >
            {mobileOpen ? '✕' : '☰'}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <nav className="flex flex-col gap-1 border-t border-white/10 bg-white px-6 py-3 md:hidden">
          {navLinks.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              onClick={() => setMobileOpen(false)}
              className="border-b border-slate-100 py-2 text-sm font-medium text-slate-700 transition-colors last:border-b-0 hover:text-[#F59E0B]"
            >
              {link.label}
            </Link>
          ))}
          {user &&
            menuItems.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                onClick={() => setMobileOpen(false)}
                className="flex items-center gap-2 border-b border-slate-100 py-2 text-sm font-medium text-slate-700"
              >
                {item.icon}
                {item.label}
              </Link>
            ))}
          {user && (
            <button
              type="button"
              onClick={handleLogout}
              className="flex items-center gap-2 py-2 text-sm font-medium text-rose-600"
            >
              <LogOut size={16} />
              خروج از حساب
            </button>
          )}
        </nav>
      )}
    </header>
  );
}
