import scrollbar from 'tailwind-scrollbar';

/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  // طبق تصمیم این تسک: Preflight غیرفعال است تا با سیستم CSS خالص موجود پروژه
  // (index.css و کلاس‌های سفارشی مثل .card/.btn-primary) تداخل ایجاد نکند.
  // Tailwind فقط برای کامپوننت‌های جدید/بازطراحی‌شده (هدر، هیرو، فیلتر آگهی‌ها) استفاده می‌شود.
  corePlugins: {
    preflight: false
  },
  theme: {
    extend: {
      fontFamily: {
        vazir: ['Vazirmatn', 'Tahoma', 'sans-serif']
      }
      // توجه مهم: عمداً هیچ توکن رنگی سفارشی (base/brand/danger/muted) در اینجا ثبت نمی‌شود.
      // تجربهٔ تسک قبلی نشان داد که نام‌گذاری یک رنگ سفارشی هم‌نام با کلیدهای مقیاس
      // fontSize پیش‌فرض Tailwind (مثل «base» که معادل کلاس اندازهٔ فونت پیش‌فرض text-base است)
      // باعث تولید دو قانون CSS متضاد با کلاس یکسان (.text-base) می‌شود و بسته به ترتیب
      // خروجی build، یکی از آن‌ها به‌صورت غیرقابل‌پیش‌بینی رنگ/سایز فونت را می‌شکند — دقیقاً
      // علت باگ «پس‌زمینهٔ هدر/هیرو سفید شد» بود. به همین دلیل رنگ‌های برند همه‌جا با مقدار
      // هگز صریح (مثل bg-[#19263A]) نوشته می‌شوند، نه از طریق theme.extend.colors.
    }
  },
  plugins: [scrollbar]
};
