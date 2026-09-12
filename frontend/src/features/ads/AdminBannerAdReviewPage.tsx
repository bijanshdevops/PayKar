import { useState } from 'react';
import { useApproveBannerAdMutation, useGetPendingBannerAdsQuery, useRejectBannerAdMutation } from '@/features/ads/bannerAdApi';

/**
 * پنل Owner برای بررسی/تایید/رد بنرهای تبلیغاتی — طبق ADR-008.
 * طبق تسک #81: با prop اختیاری `embedded` داخل تب «بررسی بنرها»ی پنل یکپارچه رندر می‌شود.
 */
export default function AdminBannerAdReviewPage({ embedded = false }: { embedded?: boolean }) {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading } = useGetPendingBannerAdsQuery({ page, pageSize });
  const [approveBannerAd] = useApproveBannerAdMutation();
  const [rejectBannerAd] = useRejectBannerAdMutation();
  const [reasonDrafts, setReasonDrafts] = useState<Record<string, string>>({});

  const banners = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;

  const handleReject = (id: string) => {
    const reason = (reasonDrafts[id] ?? '').trim();
    if (!reason) return;
    rejectBannerAd({ id, reason });
  };

  const content = (
    <>
      {!embedded && <h1>بررسی بنرهای تبلیغاتی در انتظار تایید</h1>}

      {isLoading && <p>در حال بارگذاری...</p>}
      {!isLoading && banners.length === 0 && <p>در حال حاضر بنری در انتظار بررسی نیست.</p>}

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {banners.map((banner) => (
          <div key={banner.id} className="card">
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
              <img src={banner.imageUrl} alt="بنر" style={{ maxHeight: '80px', maxWidth: '240px', borderRadius: '4px' }} />
              <div style={{ flex: 1, minWidth: '200px' }}>
                <p style={{ margin: 0 }}>
                  <a href={banner.destinationUrl} target="_blank" rel="noreferrer">{banner.destinationUrl}</a>
                </p>
                <p style={{ margin: 0, color: 'var(--color-muted)', fontSize: 'var(--fs-sm)' }}>
                  محل نمایش: {banner.placement}
                </p>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem', flexWrap: 'wrap', alignItems: 'center' }}>
              <button className="btn-primary" onClick={() => approveBannerAd(banner.id)}>تایید</button>
              <input
                type="text"
                placeholder="دلیل رد..."
                value={reasonDrafts[banner.id] ?? ''}
                onChange={(e) => setReasonDrafts((prev) => ({ ...prev, [banner.id]: e.target.value }))}
                style={{ flex: 1, minWidth: '160px' }}
              />
              <button
                className="btn-primary"
                style={{ background: 'var(--color-danger)' }}
                onClick={() => handleReject(banner.id)}
              >
                رد
              </button>
            </div>
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
