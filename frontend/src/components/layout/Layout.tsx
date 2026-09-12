import { Outlet } from 'react-router-dom';
import Header from '@/components/layout/Header';
import Footer from '@/components/layout/Footer';
import ToastContainer from '@/features/toast/ToastContainer';

export default function Layout() {
  return (
    <>
      <Header />
      {/* طبق بازطراحی جدید هدر عمومی به position:fixed تبدیل شد (خارج از جریان عادی صفحه)، پس
          برای اینکه محتوای همهٔ صفحات عمومی (غیر از هوم‌پیج) زیر آن پنهان نشود، دقیقاً به‌اندازهٔ
          ارتفاع هدر (h-20 = ۸۰px) پدینگ بالا گرفته می‌شود. صفحهٔ اصلی چون بخش Hero خودش را
          می‌خواهد پشت هدر شفاف کاملاً edge-to-edge نشان دهد، همین پدینگ را با margin منفی روی
          Hero خنثی می‌کند (نگاه کنید به HomePage.tsx). */}
      <main className="pt-20">
        <Outlet />
      </main>
      <Footer />
      <ToastContainer />
    </>
  );
}
