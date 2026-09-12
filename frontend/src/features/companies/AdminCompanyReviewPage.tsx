import { useState } from 'react';
import {
  useApproveCompanyMutation,
  useGetCompanyDocumentsQuery,
  useGetPendingCompaniesQuery,
  useRejectCompanyMutation,
  type Company
} from '@/features/companies/companyApi';
import { CompanyDocumentTypeLabels } from '@/shared/enums';

function CompanyDocumentsList({ companyId }: { companyId: string }) {
  const { data, isLoading } = useGetCompanyDocumentsQuery(companyId);
  const documents = data?.data ?? [];

  if (isLoading) return <p style={{ fontSize: 'var(--fs-sm)' }}>در حال بارگذاری مدارک...</p>;
  if (documents.length === 0) return <p style={{ fontSize: 'var(--fs-sm)', color: 'var(--color-danger)' }}>این شرکت هنوز مدرکی آپلود نکرده است.</p>;

  return (
    <ul style={{ margin: '0.5rem 0 0', fontSize: 'var(--fs-sm)' }}>
      {documents.map((doc) => (
        <li key={doc.id}>
          {CompanyDocumentTypeLabels[doc.documentType] ?? doc.documentType} —{' '}
          <a href={doc.fileUrl} target="_blank" rel="noreferrer">{doc.fileName}</a>
        </li>
      ))}
    </ul>
  );
}

/** طبق تسک #81: با prop اختیاری `embedded` داخل تب «بررسی شرکت‌ها»ی پنل یکپارچه Owner رندر می‌شود. */
export default function AdminCompanyReviewPage({ embedded = false }: { embedded?: boolean }) {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading } = useGetPendingCompaniesQuery({ page, pageSize });
  const [approveCompany] = useApproveCompanyMutation();
  const [rejectCompany] = useRejectCompanyMutation();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const companies: Company[] = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;

  const content = (
    <>
      {!embedded && <h1>بررسی شرکت‌های در انتظار تایید</h1>}

      {isLoading && <p>در حال بارگذاری...</p>}

      {!isLoading && companies.length === 0 && <p>در حال حاضر شرکتی در انتظار بررسی نیست.</p>}

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {companies.map((company) => (
          <div key={company.id} className="card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
              <div>
                <strong>{company.name}</strong>
                <p style={{ margin: 0, color: 'var(--color-muted)' }}>
                  شناسه ملی: {company.nationalId} — دسته: {company.industryCategory}
                </p>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button className="btn-secondary" onClick={() => setExpandedId(expandedId === company.id ? null : company.id)}>
                  {expandedId === company.id ? 'بستن مدارک' : 'مشاهده مدارک'}
                </button>
                <button className="btn-primary" onClick={() => approveCompany(company.id)}>تایید</button>
                <button
                  className="btn-primary"
                  style={{ background: 'var(--color-danger)' }}
                  onClick={() => rejectCompany(company.id)}
                >
                  رد
                </button>
              </div>
            </div>

            {expandedId === company.id && (
              <div style={{ marginTop: '0.75rem', borderTop: '1px solid var(--color-border)', paddingTop: '0.75rem' }}>
                <CompanyDocumentsList companyId={company.id} />
              </div>
            )}
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
