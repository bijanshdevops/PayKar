import { configureStore } from '@reduxjs/toolkit';
import { baseApi } from '@/api/baseApi';
import authReducer from '@/features/auth/authSlice';
import toastReducer from '@/features/toast/toastSlice';
import { toastMiddleware } from '@/app/toastMiddleware';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    toast: toastReducer,
    [baseApi.reducerPath]: baseApi.reducer
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware, toastMiddleware)
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
