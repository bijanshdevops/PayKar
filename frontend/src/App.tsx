import { Route, Routes } from 'react-router-dom';
import Layout from '@/components/layout/Layout';
import AuthLayout from '@/components/layout/AuthLayout';
import DashboardLayout from '@/components/layout/DashboardLayout';
import HomePage from '@/features/home/HomePage';
import LoginPage from '@/features/auth/LoginPage';
import VerifyOtpPage from '@/features/auth/VerifyOtpPage';
import ForgotPasswordPage from '@/features/auth/ForgotPasswordPage';
import ResetPasswordPage from '@/features/auth/ResetPasswordPage';
import DashboardPage from '@/features/dashboard/DashboardPage';
import AccountSecurityPage from '@/features/auth/AccountSecurityPage';
import CompanyProfilePage from '@/features/companies/CompanyProfilePage';
import AdminCompanyReviewPage from '@/features/companies/AdminCompanyReviewPage';
import AdminJobAdReviewPage from '@/features/jobAds/AdminJobAdReviewPage';
import JobAdListPage from '@/features/jobAds/JobAdListPage';
import JobAdDetailPage from '@/features/jobAds/JobAdDetailPage';
import CompanyJobListingsPage from '@/features/jobAds/CompanyJobListingsPage';
import CompanyTransactionsPage from '@/features/payments/CompanyTransactionsPage';
import ResumeFormPage from '@/features/candidates/ResumeFormPage';
import CandidateProfileView from '@/features/candidates/CandidateProfileView';
import CandidateProfileEdit from '@/features/candidates/CandidateProfileEdit';
import CandidateApplicationsPage from '@/features/candidates/CandidateApplicationsPage';
import BookmarkedJobsPage from '@/features/candidates/BookmarkedJobsPage';
import TrackApplicationPage from '@/features/applications/TrackApplicationPage';
import CompanyApplicationsPage from '@/features/applications/CompanyApplicationsPage';
import CompanyApplicantsPage from '@/features/applications/CompanyApplicantsPage';
import SupportTicketsPage from '@/features/support/SupportTicketsPage';
import SupportTicketDetailPage from '@/features/support/SupportTicketDetailPage';
import AdminSupportTicketsPage from '@/features/support/AdminSupportTicketsPage';
import CompanyBannerAdsPage from '@/features/ads/CompanyBannerAdsPage';
import AdminBannerAdReviewPage from '@/features/ads/AdminBannerAdReviewPage';
import OwnerDashboardPage from '@/features/dashboard/OwnerDashboardPage';
import PaymentResultPage from '@/features/payments/PaymentResultPage';
import AboutPage from '@/features/pages/AboutPage';
import ContactPage from '@/features/pages/ContactPage';
import TermsPage from '@/features/pages/TermsPage';
import PrivacyPage from '@/features/pages/PrivacyPage';
import FaqPage from '@/features/pages/FaqPage';
import NotFoundPage from '@/features/pages/NotFoundPage';
import RequireAuth from '@/routes/RequireAuth';

export default function App() {
  return (
    <Routes>
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/verify-otp" element={<VerifyOtpPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
      </Route>

      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/job-ads" element={<JobAdListPage />} />
        <Route path="/job-ads/:id" element={<JobAdDetailPage />} />
        <Route path="/track-application" element={<TrackApplicationPage />} />
        <Route path="/payment-result" element={<PaymentResultPage />} />

        <Route path="/about" element={<AboutPage />} />
        <Route path="/contact" element={<ContactPage />} />
        <Route path="/terms" element={<TermsPage />} />
        <Route path="/privacy" element={<PrivacyPage />} />
        <Route path="/faq" element={<FaqPage />} />

        <Route path="*" element={<NotFoundPage />} />
      </Route>

      {/* طبق تسک #73: تمام صفحات پنل کاربری (داشبورد، شرکت، رزومه، پشتیبانی، مدیریت پلتفرم)
          داخل یک سایدبار مشترک نقش‌محور رندر می‌شوند. حفاظت نقش هر مسیر همچنان با RequireAuth
          داخلی همان مسیر انجام می‌شود؛ RequireAuth بیرونی فقط ورود را تضمین می‌کند. */}
      <Route
        element={
          <RequireAuth>
            <DashboardLayout />
          </RequireAuth>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/account/security" element={<AccountSecurityPage />} />

        <Route path="/support" element={<SupportTicketsPage />} />
        <Route path="/support/:ticketId" element={<SupportTicketDetailPage />} />
        <Route
          path="/admin/support-tickets"
          element={
            <RequireAuth allowedRoles={['Admin']}>
              <AdminSupportTicketsPage />
            </RequireAuth>
          }
        />

        <Route
          path="/company/banner-ads"
          element={
            <RequireAuth allowedRoles={['CompanyManager']}>
              <CompanyBannerAdsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/banner-ads"
          element={
            <RequireAuth allowedRoles={['Admin']}>
              <AdminBannerAdReviewPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/dashboard"
          element={
            <RequireAuth allowedRoles={['Admin']}>
              <OwnerDashboardPage />
            </RequireAuth>
          }
        />

        {/* طبق ADR-010: عمداً بدون allowedRoles — هر کاربر واردشده‌ای (حتی صرفاً Candidate)
            باید بتواند اولین‌بار شرکت خودش را ثبت کند و نقش CompanyManager را کسب کند. */}
        <Route path="/company" element={<CompanyProfilePage />} />
        <Route
          path="/company/job-ads"
          element={
            <RequireAuth allowedRoles={['CompanyManager']}>
              <CompanyJobListingsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/company/job-ads/:jobAdId/applications"
          element={
            <RequireAuth allowedRoles={['CompanyManager']}>
              <CompanyApplicationsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/company/applicants"
          element={
            <RequireAuth allowedRoles={['CompanyManager']}>
              <CompanyApplicantsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/company/transactions"
          element={
            <RequireAuth allowedRoles={['CompanyManager']}>
              <CompanyTransactionsPage />
            </RequireAuth>
          }
        />

        <Route
          path="/resume"
          element={
            <RequireAuth allowedRoles={['Candidate']}>
              <ResumeFormPage />
            </RequireAuth>
          }
        />
        {/* طبق درخواست اصلاحی محصولی: این دو صفحه باید داخل همان چیدمان مشترک داشبورد کارجو
            (هدر + سایدبار) رندر شوند، دقیقاً مثل بقیه مسیرهای این بلوک — نه به‌صورت مستقل. */}
        <Route
          path="/resume/view"
          element={
            <RequireAuth allowedRoles={['Candidate']}>
              <CandidateProfileView />
            </RequireAuth>
          }
        />
        <Route
          path="/resume/edit"
          element={
            <RequireAuth allowedRoles={['Candidate']}>
              <CandidateProfileEdit />
            </RequireAuth>
          }
        />
        <Route
          path="/bookmarks"
          element={
            <RequireAuth allowedRoles={['Candidate']}>
              <BookmarkedJobsPage />
            </RequireAuth>
          }
        />
        {/* طبق تصمیم صریح محصولی «قطع کامل وابستگی به رزومه‌ساز مرحله‌ای»: جدول پیگیری درخواست‌ها
            از ResumeFormPage.tsx استخراج و به این صفحهٔ اختصاصی منتقل شده است. */}
        <Route
          path="/applications"
          element={
            <RequireAuth allowedRoles={['Candidate']}>
              <CandidateApplicationsPage />
            </RequireAuth>
          }
        />

        <Route
          path="/admin/companies"
          element={
            <RequireAuth allowedRoles={['Admin']}>
              <AdminCompanyReviewPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/job-ads"
          element={
            <RequireAuth allowedRoles={['Admin']}>
              <AdminJobAdReviewPage />
            </RequireAuth>
          }
        />
      </Route>
    </Routes>
  );
}
