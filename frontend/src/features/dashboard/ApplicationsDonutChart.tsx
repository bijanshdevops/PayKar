/**
 * نمودار دونات ساده سهم رزومه‌های دریافتی به تفکیک آگهی — بدون وابستگی به کتابخانه نمودارسازی
 * خارجی (طبق ADR-009: عیناً الگوی RevenueBarChart).
 */
const SLICE_COLORS = ['#10b981', '#3b82f6', '#f59e0b', '#8b5cf6', '#ef4444', '#64748b'];

export default function ApplicationsDonutChart({
  data
}: {
  data: { jobAdTitle: string; applicationCount: number }[];
}) {
  const total = data.reduce((sum, d) => sum + d.applicationCount, 0);

  if (total === 0) {
    return <p className="text-sm text-slate-400">هنوز رزومه‌ای دریافت نشده است.</p>;
  }

  const top = [...data].sort((a, b) => b.applicationCount - a.applicationCount).slice(0, 5);
  const othersCount = total - top.reduce((sum, d) => sum + d.applicationCount, 0);
  const slices = othersCount > 0 ? [...top, { jobAdTitle: 'سایر آگهی‌ها', applicationCount: othersCount }] : top;

  const radius = 60;
  const circumference = 2 * Math.PI * radius;
  let offsetAcc = 0;

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-center sm:justify-center">
      <svg viewBox="0 0 160 160" width={160} height={160} role="img" aria-label="نمودار سهم رزومه‌های دریافتی به تفکیک آگهی">
        <g transform="translate(80,80) rotate(-90)">
          <circle r={radius} fill="none" stroke="#f1f5f9" strokeWidth={22} />
          {slices.map((slice, idx) => {
            const fraction = slice.applicationCount / total;
            const dash = fraction * circumference;
            const circle = (
              <circle
                key={slice.jobAdTitle + idx}
                r={radius}
                fill="none"
                stroke={SLICE_COLORS[idx % SLICE_COLORS.length]}
                strokeWidth={22}
                strokeDasharray={`${dash} ${circumference - dash}`}
                strokeDashoffset={-offsetAcc}
              >
                <title>{`${slice.jobAdTitle}: ${slice.applicationCount.toLocaleString('fa-IR')}`}</title>
              </circle>
            );
            offsetAcc += dash;
            return circle;
          })}
        </g>
        <text x="80" y="76" textAnchor="middle" className="fill-slate-700" style={{ fontSize: 20, fontWeight: 700 }}>
          {total.toLocaleString('fa-IR')}
        </text>
        <text x="80" y="94" textAnchor="middle" className="fill-slate-400" style={{ fontSize: 10 }}>
          رزومه دریافتی
        </text>
      </svg>

      <ul className="flex flex-col gap-1.5">
        {slices.map((slice, idx) => (
          <li key={slice.jobAdTitle + idx} className="flex items-center gap-2 text-xs text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full" style={{ background: SLICE_COLORS[idx % SLICE_COLORS.length] }} />
            <span className="max-w-[160px] truncate">{slice.jobAdTitle}</span>
            <span className="text-slate-400">({slice.applicationCount.toLocaleString('fa-IR')})</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
