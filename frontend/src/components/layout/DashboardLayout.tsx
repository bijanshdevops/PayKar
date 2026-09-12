import { useState } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { useAppDispatch, useAppSelector } from "@/app/hooks";
import { loggedOut } from "@/features/auth/authSlice";
import { useLogoutMutation } from "@/features/auth/authApi";
import { useGetMyCompanyQuery } from "@/features/companies/companyApi";
import { useGetMyResumeQuery } from "@/features/candidates/candidateApi";
import { useGetMySupportTicketsQuery } from "@/features/support/supportApi";
import {
  GridIcon,
  HomeIcon,
  BuildingIcon,
  BriefcaseIcon,
  MegaphoneIcon,
  FileTextIcon,
  ShieldIcon,
  ChartIcon,
  LockGearIcon,
  StoreIcon,
  BookmarkIcon,
  UsersIcon,
  CreditCardIcon,
  HeadsetIcon,
  LogoutIcon,
  BellIcon,
  ChevronIcon,
  MenuIcon,
} from "@/components/icons/DashboardIcons";

interface NavItem {
  to: string;
  label: string;
  icon: React.ReactNode;
  end?: boolean;
}

interface NavSection {
  title: string | null;
  items: NavItem[];
}

type DashboardContext = "company" | "candidate" | "admin" | "generic";

/**
 * تعیین «زمینهٔ فعال» داشبورد بر اساس مسیر جاری — چون سیستم چندنقشی است (ADR-010: یک کاربر می‌تواند
 * هم‌زمان CompanyManager و Candidate باشد)، بر خلاف موکاپ‌های تک‌نقشی مرجع، سایدبار باید بر اساس
 * بخشی از سایت که کاربر در آن است تصمیم بگیرد کدام لیست ۵موردی مینیمال را نشان دهد، نه صرفاً نقش.
 */
function resolveContext(pathname: string, roles: string[]): DashboardContext {
  if (pathname.startsWith("/admin")) return "admin";
  if (pathname.startsWith("/company")) return "company";
  if (
    pathname.startsWith("/resume") ||
    pathname.startsWith("/bookmarks") ||
    pathname.startsWith("/applications")
  )
    return "candidate";
  if (roles.includes("CompanyManager")) return "company";
  if (roles.includes("Candidate")) return "candidate";
  return "generic";
}

/**
 * طبق فاز بازطراحی پیکسل‌به‌پیکسل داشبورد کارفرما: سایدبار کارفرما دقیقاً ۵ آیتم اصلی، بدون هیچ
 * تیتر بخش یا گزینهٔ نامربوط کارجو. سایدبار کارجو نیز به همین فلسفهٔ مینیمال محدود شده. دسترسی به
 * صفحات باقی‌مانده (پروفایل شرکت، بنرهای تبلیغاتی، امنیت حساب، پنل مدیریت، و اکنون «درخواست‌های من»
 * در /applications) از طریق دراپ‌داون پروفایل بالای صفحه حفظ شده تا ناوبری قطع نشود و سایدبار
 * شلوغ نشود.
 */
function buildSections(
  context: DashboardContext,
  roles: string[],
): NavSection[] {
  if (context === "company") {
    return [
      {
        title: null,
        items: [
          { to: "/dashboard", label: "داشبورد", icon: <GridIcon />, end: true },
          { to: "/company/job-ads", label: "آگهی‌ها", icon: <BriefcaseIcon /> },
          {
            to: "/company/applicants",
            label: "رزومه‌های دریافتی",
            icon: <UsersIcon />,
          },
          {
            to: "/company/transactions",
            label: "امور مالی",
            icon: <CreditCardIcon />,
          },
          // طبق درخواست اصلاحی محصولی: «بنرهای تبلیغاتی» از دراپ‌داون پروفایل حذف و به سایدبار اصلی
          // کارفرما منتقل شده تا دسترسی مستقیم و برجسته‌تر باشد.
          {
            to: "/company/banner-ads",
            label: "بنرهای تبلیغاتی",
            icon: <MegaphoneIcon />,
          },
          { to: "/support", label: "تیکت پشتیبانی", icon: <HeadsetIcon /> },
        ],
      },
    ];
  }

  if (context === "candidate") {
    return [
      {
        title: null,
        items: [
          { to: "/dashboard", label: "داشبورد", icon: <HomeIcon />, end: true },
          // طبق تصمیم صریح محصولی: کلیک روی این آیتم باید مستقیماً نمای پروفایل/رزومه را نشان دهد؛
          // ویرایش از طریق دکمه «ویرایش پروفایل» (/resume/edit) و رزومه‌ساز مرحله‌ای/پرداخت از طریق
          // لینک‌های داخلی همان صفحات در دسترس می‌مانند.
          { to: "/resume/view", label: "رزومه ساز من", icon: <FileTextIcon /> },
          {
            to: "/bookmarks",
            label: "آگهی‌های نشان‌شده",
            icon: <BookmarkIcon />,
          },
          { to: "/support", label: "تیکت پشتیبانی", icon: <HeadsetIcon /> },
        ],
      },
    ];
  }

  if (context === "admin") {
    // پنل مدیریت خارج از محدودهٔ بازطراحی پیکسل‌به‌پیکسل این فاز است — چیدمان قبلی حفظ شده.
    return [
      {
        title: "مدیریت پلتفرم",
        items: [
          {
            to: "/admin/dashboard",
            label: "پنل مدیریت (نمای کلی)",
            icon: <ChartIcon />,
            end: true,
          },
          {
            to: "/admin/dashboard?tab=jobads",
            label: "بررسی آگهی‌ها",
            icon: <BriefcaseIcon />,
          },
          {
            to: "/admin/dashboard?tab=banners",
            label: "بررسی بنرهای تبلیغاتی",
            icon: <MegaphoneIcon />,
          },
          {
            to: "/admin/dashboard?tab=companies",
            label: "بررسی شرکت‌ها",
            icon: <BuildingIcon />,
          },
          {
            to: "/admin/support-tickets",
            label: "پنل پشتیبانی",
            icon: <ShieldIcon />,
          },
        ],
      },
      {
        title: "حساب کاربری",
        items: [
          { to: "/support", label: "تیکت پشتیبانی", icon: <HeadsetIcon /> },
          {
            to: "/account/security",
            label: "امنیت حساب",
            icon: <LockGearIcon />,
          },
        ],
      },
    ];
  }

  // زمینهٔ عمومی: کاربری که هنوز نه شرکتی ثبت کرده و نه نقش کارجو دارد.
  const sections: NavSection[] = [
    {
      title: null,
      items: [
        { to: "/dashboard", label: "داشبورد", icon: <HomeIcon />, end: true },
      ],
    },
    {
      title: null,
      items: [{ to: "/company", label: "ثبت شرکت من", icon: <StoreIcon /> }],
    },
  ];
  if (!roles.includes("Candidate")) {
    sections.push({
      title: null,
      items: [
        { to: "/support", label: "تیکت پشتیبانی", icon: <HeadsetIcon /> },
      ],
    });
  }
  return sections;
}

/**
 * تعیین فعال بودن آیتم سایدبار — طبق تسک #81: NavLink استاندارد React Router هنگام مقایسه‌ی
 * isActive، Query String را نادیده می‌گیرد (فقط pathname). چون آیتم‌های تب‌های پنل یکپارچه Owner
 * (`/admin/dashboard?tab=jobads` و مشابه) همگی یک pathname مشترک دارند، بدون این مقایسه‌ی دستی
 * هر چهار آیتم هم‌زمان «فعال» نمایش داده می‌شدند.
 *
 * طبق درخواست اصلاحی محصولی «پروفایل و رزومه‌ساز کارجو»: آیتم سایدبار «رزومه ساز من» به
 * `/resume/view` اشاره می‌کند، اما باید هنگام حضور در صفحهٔ ویرایش (`/resume/edit`) نیز فعال
 * نمایش داده شود — چون هر دو صفحه از دید کاربر بخشی از همان بخش «پروفایل/رزومه» هستند.
 */
function isNavItemActive(
  item: NavItem,
  pathname: string,
  search: string,
): boolean {
  const [itemPath, itemQuery] = item.to.split("?");

  if (itemQuery !== undefined) {
    return itemPath === pathname && `?${itemQuery}` === search;
  }

  if (item.end) {
    return itemPath === pathname && search === "";
  }

  if (
    itemPath === "/resume/view" &&
    (pathname === "/resume/edit" || pathname.startsWith("/resume/edit/"))
  ) {
    return true;
  }

  // مطابق رفتار پیش‌فرض NavLink بدون `end`: تطبیق دقیق یا زیرمسیر (مثلاً صفحات تودرتوی آگهی/درخواست‌ها).
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}

interface ProfileMenuLink {
  to: string;
  label: string;
}

export default function DashboardLayout() {
  const { user, refreshToken } = useAppSelector((state) => state.auth);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const [logout] = useLogoutMutation();
  // mobileOpen: کشوی سایدبار روی موبایل (کمتر از ۹۰۰px). desktopCollapsed: حالت جمع‌شده/باریک
  // سایدبار روی دسکتاپ. هر دو با یک دکمهٔ همبرگر واحد در نوار بالا کنترل می‌شوند؛ چون CSS هرکدام
  // را فقط در بازهٔ خودش فعال می‌کند، تغییر هم‌زمان هر دو مشکلی ایجاد نمی‌کند.
  const [mobileOpen, setMobileOpen] = useState(false);
  const [desktopCollapsed, setDesktopCollapsed] = useState(false);
  const [profileMenuOpen, setProfileMenuOpen] = useState(false);

  const roles = user?.roles ?? [];
  const context = resolveContext(location.pathname, roles);
  const sections = buildSections(context, roles);
  const accent = context === "company" ? "gold" : "teal";

  // نام شرکت فقط در زمینهٔ کارفرما واکشی می‌شود (بدون هزینهٔ درخواست اضافه برای سایر زمینه‌ها).
  const { data: myCompanyData } = useGetMyCompanyQuery(undefined, {
    skip: context !== "company",
  });
  const company = myCompanyData?.data;

  // رزومهٔ کارجو فقط در زمینهٔ کارجو واکشی می‌شود — طبق درخواست اصلاحی محصولی «آواتار پویا»، برای
  // نمایش تصویر پروفایل واقعی کارجو (avatarUrl) در دایرهٔ کاربر بالای صفحه، نه یک آیکون ثابت/خالی.
  const { data: myResumeData } = useGetMyResumeQuery(undefined, {
    skip: context !== "candidate",
  });
  const resume = myResumeData?.data;

  // شمارهٔ بج زنگولهٔ اعلان‌ها — تعداد واقعی تیکت‌های پشتیبانی باز/در حال بررسی کاربر (نه عدد جعلی)؛
  // این پلتفرم هنوز سامانهٔ اعلان‌های عمومی ندارد، پس نزدیک‌ترین معادل صادقانهٔ «چیزی نیاز به توجه دارد».
  const { data: myTicketsData } = useGetMySupportTicketsQuery({
    page: 1,
    pageSize: 50,
  });
  const openTicketsCount = (myTicketsData?.data?.items ?? []).filter(
    (t) => t.status === "PendingResponse" || t.status === "InProgress",
  ).length;

  const handleToggleSidebar = () => {
    setMobileOpen((v) => !v);
    setDesktopCollapsed((v) => !v);
  };

  // در زمینهٔ کارجو، بلوک برند بالای سایدبار طبق درخواست اصلاحی محصولی دیگر نام پلتفرم را نشان
  // نمی‌دهد — چون این ناحیه در پنل کارجو عملاً محل معرفی «حساب کاربری جاری» است، نه برند سایت؛
  // بنابراین نام واقعی کارجو (از رزومهٔ او) و زیرش شمارهٔ تماسش نمایش داده می‌شود. اگر کارجو هنوز
  // نامی برای رزومهٔ خود ثبت نکرده، به شمارهٔ موبایل به‌عنوان نام بازمی‌گردیم تا زیرنویس با مقدار
  // تکراری پر نشود و در عوض توضیح پیش‌فرض پلتفرم را نشان دهیم.
  const candidateDisplayName = resume?.fullName || null;
  const brandName =
    context === "company" && company
      ? company.name
      : context === "candidate"
        ? (candidateDisplayName ?? (user?.mobileNumber ?? "کارجو"))
        : "پی کار";
  const brandSubtitle =
    context === "company"
      ? "پرتال کارفرمایان"
      : context === "candidate"
        ? (candidateDisplayName && user?.mobileNumber
            ? user.mobileNumber
            : "سامانه فرصت‌های شغلی")
        : context === "admin"
          ? "پنل مدیریت پلتفرم"
          : "پنل کاربری";

  const pageTitle =
    context === "company"
      ? "داشبورد کارفرما"
      : context === "candidate"
        ? "داشبورد کارجو"
        : context === "admin"
          ? "پنل مدیریت"
          : "داشبورد";
  const welcomeSubtitle =
    context === "company"
      ? `خوش آمدید : شرکت ${company?.name ?? ""}`.trim()
      : context === "admin"
        ? "خوش آمدید : مدیر پلتفرم"
        : `خوش آمدید${user?.mobileNumber ? ` : ${user.mobileNumber}` : ""}`;

  const profileBadgeLabel =
    context === "company"
      ? "حساب کاربری کارفرما"
      : context === "candidate"
        ? "حساب کاربری کارجو"
        : context === "admin"
          ? "مدیر پلتفرم"
          : "کاربر پلتفرم";
  const profileDisplayName =
    context === "company" && company
      ? company.name
      : (user?.mobileNumber ?? "");
  const profileInitial = profileDisplayName
    ? profileDisplayName.charAt(0)
    : "؟";

  // تصویر آواتار دایرهٔ کاربر بالای صفحه — به‌جای آیکون ثابت/خالی قبلی، اکنون از تصویر واقعی
  // کارفرما (لوگوی شرکت) یا کارجو (avatarUrl رزومه) استفاده می‌شود؛ در نبود هر دو، حروف اول نام
  // به‌عنوان جایگزین مدرن نمایش داده می‌شود (منطق قبلی fallback).
  const profileAvatarUrl =
    context === "company"
      ? (company?.logoUrl ?? null)
      : context === "candidate"
        ? (resume?.avatarUrl ?? null)
        : null;

  // دراپ‌داون پروفایل — طبق درخواست اصلاحی محصولی «دراپ‌داون کاربر مبتنی بر نقش»: ناوبری هر نقش
  // اکیداً به آیتم‌های مخصوص همان نقش محدود شده (بدون آیتم نامربوط از نقش دیگر). چون سیستم چندنقشی
  // است (ADR-010: کاربر می‌تواند هم‌زمان CompanyManager و Candidate باشد)، کاربری با هر دو نقش می‌تواند
  // هر دو بخش را (در جای درست خودش) ببیند.
  // طبق دومین دور اصلاح محصولی: دراپ‌داون کارفرما اکیداً به «پروفایل شرکت» محدود شده — «مدیریت
  // آگهی‌ها»، «رزومه‌های دریافتی» و «بنرهای تبلیغاتی» حذف شدند (دو مورد اول همیشه در سایدبار اصلی
  // کارفرما موجود بودند؛ بنرهای تبلیغاتی هم اکنون به همان سایدبار منتقل شده — به بالا مراجعه کنید).
  // طبق سومین دور اصلاح محصولی: برای کاربر دونقشی (هم کارفرما هم کارجو)، آیتم‌های کارجو («رزومه من»،
  // «درخواست‌های من»، «آگهی‌های نشان‌شده») دیگر در پنل کارفرما (context === "company") نشت پیدا
  // نمی‌کنند — بر خلاف نسخهٔ قبلی که صرفاً بر اساس نقش (roles.includes) بود و مستقل از صفحهٔ جاری هر
  // دو دسته را هم‌زمان نمایش می‌داد، اکنون هر بخش فقط وقتی نمایش داده می‌شود که کاربر واقعاً در همان
  // زمینه/پنل (context) باشد — دراپ‌داون همیشه منعکس‌کنندهٔ پنلی است که کاربر هم‌اکنون در آن قرار دارد.
  const profileMenuLinks: ProfileMenuLink[] = [];
  if (context === "company" && roles.includes("CompanyManager")) {
    profileMenuLinks.push({ to: "/company", label: "پروفایل شرکت" });
  }
  if (context === "candidate" && roles.includes("Candidate")) {
    profileMenuLinks.push(
      // طبق فاز «پروفایل و رزومه‌ساز کارجو» — این آیتم قصد «مشاهده» رزومه را دارد، نه ساخت/ویرایش آن؛
      // بنابراین به نمای فقط-خواندنی رزومه (CandidateProfileView) هدایت می‌شود، نه رزومه‌ساز مرحله‌ای
      // (که همچنان از طریق آیتم سایدبار اصلی «رزومه ساز من» و لینک‌های داخلی صفحات جدید در دسترس است).
      { to: "/resume/view", label: "رزومه من" },
      { to: "/applications", label: "درخواست‌های من" },
      { to: "/bookmarks", label: "آگهی‌های نشان‌شده" },
    );
  }
  if (!roles.includes("CompanyManager") && !roles.includes("Candidate")) {
    profileMenuLinks.push({ to: "/company", label: "ثبت شرکت من" });
  }
  if (roles.includes("Admin")) {
    profileMenuLinks.push({ to: "/admin/dashboard", label: "پنل مدیریت" });
  }
  profileMenuLinks.push({ to: "/account/security", label: "امنیت حساب" });

  const handleLogout = async () => {
    if (refreshToken) await logout({ refreshToken });
    dispatch(loggedOut());
    navigate("/");
  };

  return (
    <div className="dash-layout">
      <AnimatePresence>
        {(mobileOpen || true) && (
          <motion.aside
            className={`dash-sidebar${mobileOpen ? " dash-sidebar--open" : ""}${desktopCollapsed ? " dash-sidebar--collapsed" : ""}`}
            data-accent={accent}
            initial={false}
          >
            <div className="dash-sidebar__brand">
              <span className="dash-sidebar__brand-dot" data-accent={accent}>
                {company?.logoUrl && context === "company" ? (
                  <img src={company.logoUrl} alt={brandName} />
                ) : context === "candidate" && resume?.avatarUrl ? (
                  <img src={resume.avatarUrl} alt={brandName} />
                ) : context === "candidate" ? (
                  <span className="dash-sidebar__brand-dot-initial">
                    {brandName.charAt(0)}
                  </span>
                ) : (
                  <BuildingIcon size={18} />
                )}
              </span>
              <div className="dash-sidebar__brand-text">
                <div className="dash-sidebar__brand-name">{brandName}</div>
                <div className="dash-sidebar__brand-subtitle">
                  {brandSubtitle}
                </div>
              </div>
            </div>

            <nav className="dash-sidebar__nav">
              {sections.map((section, idx) => (
                <motion.div
                  key={section.title ?? `section-${idx}`}
                  className="dash-sidebar__section"
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{
                    delay: idx * 0.05,
                    duration: 0.35,
                    ease: "easeOut",
                  }}
                >
                  {section.title && (
                    <div className="dash-sidebar__section-title">
                      {section.title}
                    </div>
                  )}
                  {section.items.map((item) => (
                    <Link
                      key={item.to}
                      to={item.to}
                      className={`dash-sidebar__link${isNavItemActive(item, location.pathname, location.search) ? " is-active" : ""}`}
                      onClick={() => setMobileOpen(false)}
                      title={item.label}
                    >
                      <span className="dash-sidebar__link-icon">
                        {item.icon}
                      </span>
                      <span className="dash-sidebar__link-label">
                        {item.label}
                      </span>
                    </Link>
                  ))}
                </motion.div>
              ))}
            </nav>

            <button
              type="button"
              className="dash-sidebar__logout"
              onClick={handleLogout}
              title="خروج از حساب"
            >
              <LogoutIcon />{" "}
              <span className="dash-sidebar__logout-label">خروج از حساب</span>
            </button>
          </motion.aside>
        )}
      </AnimatePresence>

      <div
        className="dash-sidebar__scrim"
        data-open={mobileOpen}
        onClick={() => setMobileOpen(false)}
      />

      <div className="dash-content-wrap">
        <div className="dash-topbar">
          <div className="dash-topbar__start">
            <button
              type="button"
              className="dash-topbar__hamburger"
              onClick={handleToggleSidebar}
              aria-label="باز/بسته‌کردن منوی داشبورد"
            >
              <MenuIcon size={20} />
            </button>

            <div className="dash-topbar__title">
              <div className="dash-topbar__title-main">{pageTitle}</div>
              <div className="dash-topbar__title-sub">{welcomeSubtitle}</div>
            </div>
          </div>

          {/* طبق درخواست محصولی: لوگوی پلتفرم در فضای خالی وسط هدر — چون DashboardLayout یک کامپوننت
              مشترک بین همه نقش‌ها (کارجو/کارفرما/ادمین/عمومی) است، همین یک نقطه برای همیشه کافی است
              و نیازی به تکرار کد در صفحات هر نقش نیست. */}
          <div className="dash-topbar__center">
            <Link to="/" aria-label="بازگشت به صفحه اصلی">
              <img src="/logo.png" alt="پی کار" className="dash-topbar__center-logo" />
            </Link>
          </div>

          <div className="dash-topbar__end">
            <Link
              to="/support"
              className="dash-topbar__bell"
              aria-label="اعلان‌ها و پشتیبانی"
            >
              <BellIcon size={19} />
              {openTicketsCount > 0 && (
                <span className="dash-topbar__bell-badge">
                  {openTicketsCount.toLocaleString("fa-IR")}
                </span>
              )}
            </Link>

            <div className="dash-topbar__profile-wrap">
              <button
                type="button"
                className="dash-topbar__profile"
                onClick={() => setProfileMenuOpen((v) => !v)}
                onBlur={() =>
                  window.setTimeout(() => setProfileMenuOpen(false), 150)
                }
              >
                {profileAvatarUrl ? (
                  <img
                    src={profileAvatarUrl}
                    alt={profileDisplayName}
                    className="dash-topbar__profile-avatar"
                  />
                ) : (
                  <span className="dash-topbar__profile-avatar dash-topbar__profile-avatar--initial">
                    {profileInitial}
                  </span>
                )}
                <span className="dash-topbar__profile-text">
                  <span className="dash-topbar__profile-name-row">
                    <span className="dash-topbar__profile-name">
                      {profileDisplayName || "کاربر"}
                    </span>
                    <ChevronIcon size={13} />
                  </span>
                  <span className="dash-topbar__profile-badge">
                    {profileBadgeLabel}
                  </span>
                </span>
              </button>

              {profileMenuOpen && (
                <div className="dash-topbar__dropdown">
                  {profileMenuLinks.map((link) => (
                    <Link
                      key={link.to}
                      to={link.to}
                      className="dash-topbar__dropdown-item"
                    >
                      {link.label}
                    </Link>
                  ))}
                  <button
                    type="button"
                    className="dash-topbar__dropdown-item dash-topbar__dropdown-item--danger"
                    onClick={handleLogout}
                  >
                    خروج از حساب
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        <main className="dash-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
