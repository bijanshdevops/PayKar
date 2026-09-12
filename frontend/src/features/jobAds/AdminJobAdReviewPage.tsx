import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useApproveJobAdMutation, useGetPendingReviewJobAdsQuery, useRejectJobAdMutation } from '@/features/jobAds/jobAdApi';
import { WorkShiftLabels, ContractTypeLabels, SalaryRangeTypeLabels } from '@/shared/enums';

/// <summary>
/// پنل Owner برای استعلام/تایید/رد آگهی‌های در انتظار بررسی.
/// طبق تسک #81: با prop اختیاری `embedded` می‌تواند بدون بازکردن دوباره container/عنوان صفحه،
/// داخل تب «بررسی آگهی‌ها»ی پنل یکپارچه OwnerDashboardPage نیز رندر شود.
/// </summary>
export default function AdminJobAdReviewPage({ embedded = false }: { embedded?: boolean }) {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading } = useGetPendingReviewJobAdsQuery({ page, pageSize });
  const [approveJobAd, { isLoading: isApproving }] = useApproveJobAdMutation();
  const [rejectJobAd, { isLoading: isRejecting }] = useRejectJobAdMutation();
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [reason, setReason] = useState('');
  const [errorByAdId, setErrorByAdId] = useState<Record<string, string>>({});

  const jobAds = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;

  const onApprove = async (id: string) => {
    setErrorByAdId((prev) => ({ ...prev, [id]: '' }));
    const result = await approveJobAd(id);
    if (!('data' in result) || !result.data?.success) {
      const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
      setErrorByAdId((prev) => ({ ...prev, [id]: errorResponse?.data?.message ?? 'تایید آگهی با خطا مواجه شد.' }));
    }
  };

  const onConfirmReject = async () => {
    if (!rejectingId) return;
    if (!reason.trim()) {
      setErrorByAdId((prev) => ({ ...prev, [rejectingId]: 'ذکر دلیل رد آگهی الزامی است.' }));
      return;
    }
    const result = await rejectJobAd({ id: rejectingId, reason: reason.trim() });
    if ('data' in result && result.data?.success) {
      setRejectingId(null);
      setReason('');
      return;
    }
    const errorResponse = 'error' in result ? (result.error as { data?: { message?: string } }) : undefined;
    setErrorByAdId((prev) => ({ ...prev, [rejectingId]: errorResponse?.data?.message ?? 'رد آگهی با خطا مواجه شد.' }));
  };

  const content = (
    <>
      {!embedded && (
        <>
          <h1>استعلام و تایید آگهی‌ها</h1>
          <p className="text-muted" style={{ marginTop: '-0.5rem' }}>
            آگهی‌های زیر پس از پرداخت هزینه ثبت توسط شرکت، در انتظار بررسی و تایید نهایی شما هستند.
          </p>
        </>
      )}

      {isLoading && <p>در حال بارگذاری...</p>}
      {!isLoading && jobAds.length === 0 && <p>در حال حاضر آگهی‌ای در انتظار بررسی نیست.</p>}

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {jobAds.map((ad) => (
          <div key={ad.id} className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.75rem' }}>
              <div>
                <strong>{ad.title}</strong>
                <p style={{ margin: '0.25rem 0', color: 'var(--color-muted)', fontSize: 'var(--fs-sm)' }}>
                  {WorkShiftLabels[ad.workShift] ?? ad.workShift} — {ContractTypeLabels[ad.contractType] ?? ad.contractType} — {SalaryRangeTypeLabels[ad.salaryRange.type] ?? ad.salaryRange.type}
                </p>
                <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{ad.description}</p>
                <Link to={`/job-ads/${ad.id}`} style={{ fontSize: 'var(--fs-sm)' }}>مشاهده کامل آگهی ←</Link>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button className="btn-primary" disabled={isApproving} onClick={() => onApprove(ad.id)}>تایید و انتشار</button>
                <button
                  className="btn-primary"
                  style={{ background: 'var(--color-danger)' }}
                  onClick={() => { setRejectingId(rejectingId === ad.id ? null : ad.id); setReason(''); }}
                >
                  رد
                </button>
              </div>
            </div>

            {rejectingId === ad.id && (
              <div style={{ marginTop: '1rem', borderTop: '1px solid var(--color-border)', paddingTop: '1rem' }}>
                <div className="field">
                  <label>دلیل رد آگهی</label>
                  <textarea rows={2} value={reason} onChange={(e) => setReason(e.target.value)} placeholder="مثلاً: مدارک هویتی ناقص است یا عنوان آگهی با شرح آن مطابقت ندارد." />
                </div>
                <button className="btn-primary" style={{ background: 'var(--color-danger)' }} disabled={isRejecting} onClick={onConfirmReject}>
                  ثبت رد آگهی
                </button>
              </div>
            )}

            {errorByAdId[ad.id] && <p className="error-text" style={{ marginTop: '0.5rem' }}>{errorByAdId[ad.id]}</p>}
          </div>
        ))}
      </div>

      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1.5rem' }}>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button key={p} className="btn-primary" style={{ opacity: p === page ? 1 : 0.5 }} onClick={() => setPage(p)}>
              {p}
            </button>
          ))}
        </div>
      )}
    </>
  );

  return embedded ? content : <div className="container">{content}</div>;
}
