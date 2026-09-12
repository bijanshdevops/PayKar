import { useState } from 'react';
import { useLazyTrackApplicationQuery } from '@/features/applications/applicationApi';
import { ApplicationStatusLabels, ApplicationStatusMessages } from '@/shared/enums';

export default function TrackApplicationPage() {
  const [trackingToken, setTrackingToken] = useState('');
  const [trigger, { data, isFetching, isError }] = useLazyTrackApplicationQuery();

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (trackingToken.trim()) trigger(trackingToken.trim().toUpperCase());
  };

  const application = data?.data;

  return (
    <div className="container">
      <div className="card" style={{ maxWidth: 480, margin: '3rem auto' }}>
        <h1>پیگیری درخواست همکاری</h1>
        <form onSubmit={handleSearch}>
          <div className="field">
            <label>کد رهگیری</label>
            <input
              value={trackingToken}
              onChange={(e) => setTrackingToken(e.target.value)}
              placeholder="APP-XXXXXX"
            />
          </div>
          <button type="submit" className="btn-primary" disabled={isFetching}>
            {isFetching ? 'در حال جست‌وجو...' : 'پیگیری'}
          </button>
        </form>

        {isError && <p className="error-text" style={{ marginTop: '1rem' }}>درخواستی با این کد رهگیری یافت نشد.</p>}

        {application && (
          <div className="card" style={{ marginTop: '1rem', background: '#f0fdf4' }}>
            <p style={{ margin: 0, fontWeight: 600 }}>
              {ApplicationStatusMessages[application.status] ?? ApplicationStatusLabels[application.status] ?? application.status}
            </p>
            <p style={{ margin: '0.5rem 0 0', color: 'var(--color-muted)', fontSize: 'var(--fs-sm)' }}>
              وضعیت: {ApplicationStatusLabels[application.status] ?? application.status}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
