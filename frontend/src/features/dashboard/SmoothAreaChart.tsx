import type { CompanyDashboardDailyViewsPoint } from '@/features/companies/companyApi';
import { formatToJalali } from '@/shared/utils/date';

/**
 * نمودار ناحیه‌ای/خطی روان بازدید ۳۰ روزه («گزارش بازدید آگهی‌ها») — منحنی نرم با Catmull-Rom→Bezier
 * (بدون وابستگی به کتابخانه نمودارسازی خارجی، طبق ADR-009: عیناً الگوی RevenueBarChart).
 */
export default function SmoothAreaChart({ data }: { data: CompanyDashboardDailyViewsPoint[] }) {
  if (data.length === 0) return null;

  const width = 700;
  const height = 220;
  const paddingBottom = 28;
  const paddingTop = 12;
  const maxValue = Math.max(...data.map((d) => d.viewsCount), 1);

  const points = data.map((d, index) => {
    const x = data.length > 1 ? (index / (data.length - 1)) * width : 0;
    const y = height - paddingBottom - (d.viewsCount / maxValue) * (height - paddingBottom - paddingTop);
    return { x, y, ...d };
  });

  // منحنی نرم با نقاط کنترل میانی ساده (بدون کتابخانه خارجی).
  const buildSmoothPath = (): string => {
    if (points.length < 2) return `M 0 ${points[0]?.y ?? height}`;
    let path = `M ${points[0].x.toFixed(1)} ${points[0].y.toFixed(1)}`;
    for (let i = 0; i < points.length - 1; i++) {
      const p0 = points[i];
      const p1 = points[i + 1];
      const midX = (p0.x + p1.x) / 2;
      path += ` C ${midX.toFixed(1)} ${p0.y.toFixed(1)}, ${midX.toFixed(1)} ${p1.y.toFixed(1)}, ${p1.x.toFixed(1)} ${p1.y.toFixed(1)}`;
    }
    return path;
  };

  const linePath = buildSmoothPath();
  const areaPath = `${linePath} L ${points[points.length - 1].x.toFixed(1)} ${height - paddingBottom} L 0 ${height - paddingBottom} Z`;

  // برچسب‌های محور افقی: هر ~۵ روز یک تاریخ شمسی ساده نمایش داده می‌شود تا شلوغ نشود.
  const labelEvery = Math.max(Math.ceil(points.length / 6), 1);

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} role="img" aria-label="نمودار روند بازدید ۳۰ روز اخیر">
        <defs>
          <linearGradient id="views-area-gradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#f59e0b" stopOpacity={0.32} />
            <stop offset="100%" stopColor="#f59e0b" stopOpacity={0} />
          </linearGradient>
        </defs>
        <line x1={0} y1={height - paddingBottom} x2={width} y2={height - paddingBottom} stroke="var(--color-border, #e2e8f0)" />
        <path d={areaPath} fill="url(#views-area-gradient)" stroke="none" />
        <path d={linePath} fill="none" stroke="#f59e0b" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round" />
        {points.map((p) => (
          <circle key={p.date} cx={p.x} cy={p.y} r={2.5} fill="#f59e0b">
            <title>
              {formatToJalali(p.date)}: {p.viewsCount.toLocaleString('fa-IR')} بازدید
            </title>
          </circle>
        ))}
        {points.map((p, index) =>
          index % labelEvery === 0 || index === points.length - 1 ? (
            <text
              key={`label-${p.date}`}
              x={p.x}
              y={height - 8}
              textAnchor="middle"
              style={{ fontSize: 10, fill: 'var(--color-muted, #94a3b8)' }}
            >
              {formatToJalali(p.date, 'd/M')}
            </text>
          ) : null
        )}
      </svg>
    </div>
  );
}
