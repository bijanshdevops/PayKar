// ساختارهای مشترک — منطبق با backend/src/IndustrialPlatform.Shared/Api (سند 04-Api-Contract.md)

export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string | null;
  data: T | null;
  errors: Record<string, string[]> | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PageRequest {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export type UserRole = 'Admin' | 'CompanyManager' | 'Candidate';

export interface AuthenticatedUser {
  id: string;
  mobileNumber: string;
  roles: UserRole[];
}
