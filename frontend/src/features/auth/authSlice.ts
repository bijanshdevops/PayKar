import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { AuthenticatedUser } from '@/shared/types';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthenticatedUser | null;
}

interface PersistedAuthState {
  accessToken: string;
  refreshToken: string;
  user: AuthenticatedUser;
}

// طبق درخواست محصولی «حفظ نشست کاربر پس از رفرش صفحه (F5)»: بک‌اند تنها منبع حقیقتِ اعتبار توکن
// است (طول عمر ۸ ساعته + ValidateLifetime=true سمت سرور)، اما فرانت‌اند باید همان توکن صادرشده را
// طوری نگه دارد که با رفرش صفحه از بین نرود — در غیر این صورت State ری‌داکس با هر F5 به initialState
// (accessToken: null) بازمی‌گردد و RequireAuth کاربرِ هنوز واقعاً واردشده را به /login هدایت می‌کند.
// localStorage (بر خلاف sessionStorage) بین تب‌ها و پس از بستن/بازکردن مرورگر هم باقی می‌ماند و
// خواندن/نوشتن آن کاملاً همزمان (Synchronous) است — یعنی مقداردهی اولیهٔ Redux Store پیش از اولین
// رندر React از localStorage انجام می‌شود و هیچ حالت میانیِ «در حال بارگذاری نشست» لازم نیست.
const AUTH_STORAGE_KEY = 'industrialPlatform.auth';

function readPersistedAuth(): PersistedAuthState | null {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as Partial<PersistedAuthState>;
    if (!parsed.accessToken || !parsed.refreshToken || !parsed.user) return null;

    return { accessToken: parsed.accessToken, refreshToken: parsed.refreshToken, user: parsed.user };
  } catch {
    // localStorage در برخی بسترها (حالت خصوصی/ناشناس برخی مرورگرها، تنظیمات سازمانی) ممکن است در
    // دسترس نباشد یا محتوای ذخیره‌شده خراب/ناسازگار باشد — در این حالت باید کاربر را در حالت
    // خروج (initialState خالی) نگه داشت تا برنامه کرش نکند، نه اینکه استثنا را propagate کند.
    return null;
  }
}

function persistAuth(state: PersistedAuthState): void {
  try {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(state));
  } catch {
    // نوشتن ممکن است شکست بخورد (مثلاً کوتای ذخیره‌سازی پر شده) — نشست همچنان در حافظهٔ Redux برای
    // همین تب فعال است، فقط پس از رفرش صفحه از بین می‌رود؛ کرش‌کردن برنامه به‌خاطر این خطا صحیح نیست.
  }
}

function clearPersistedAuth(): void {
  try {
    localStorage.removeItem(AUTH_STORAGE_KEY);
  } catch {
    // نبود دسترسی به‌همان دلایل بالا — بی‌خطر نادیده گرفته می‌شود.
  }
}

const persisted = readPersistedAuth();

const initialState: AuthState = {
  accessToken: persisted?.accessToken ?? null,
  refreshToken: persisted?.refreshToken ?? null,
  user: persisted?.user ?? null
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    sessionEstablished: (
      state,
      action: PayloadAction<{ accessToken: string; refreshToken: string; user: AuthenticatedUser }>
    ) => {
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      state.user = action.payload.user;
      persistAuth(action.payload);
    },
    credentialsUpdated: (state, action: PayloadAction<{ accessToken: string; refreshToken: string }>) => {
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      // طبق تصمیم محصولی: رفرش‌توکن معمولاً user به‌روز را هم برمی‌گرداند و از sessionEstablished
      // استفاده می‌شود (نگاه کنید به baseApi.ts)؛ این ریدیوسر صرفاً برای سازگاری با فراخوان‌های احتمالی
      // آینده که فقط توکن را تغییر می‌دهند نگه داشته شده — در آن صورت هم باید user فعلی حفظ و پایدار شود.
      if (state.user) {
        persistAuth({ accessToken: action.payload.accessToken, refreshToken: action.payload.refreshToken, user: state.user });
      }
    },
    loggedOut: (state) => {
      state.accessToken = null;
      state.refreshToken = null;
      state.user = null;
      clearPersistedAuth();
    }
  }
});

export const { sessionEstablished, credentialsUpdated, loggedOut } = authSlice.actions;
export default authSlice.reducer;
