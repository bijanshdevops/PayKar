import { createSlice, nanoid, type PayloadAction } from '@reduxjs/toolkit';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
}

interface ToastState {
  items: ToastItem[];
}

const initialState: ToastState = {
  items: []
};

const toastSlice = createSlice({
  name: 'toast',
  initialState,
  reducers: {
    toastAdded: {
      reducer: (state, action: PayloadAction<ToastItem>) => {
        state.items.push(action.payload);
      },
      prepare: (type: ToastType, message: string) => ({
        payload: { id: nanoid(), type, message }
      })
    },
    toastRemoved: (state, action: PayloadAction<string>) => {
      state.items = state.items.filter((item) => item.id !== action.payload);
    }
  }
});

export const { toastAdded, toastRemoved } = toastSlice.actions;
export default toastSlice.reducer;
