import type { DailyRevenuePoint } from '@/features/dashboard/ownerDashboardApi';
import { formatToJalali } from '@/shared/utils/date';

/**
 * نمودار میله‌ای ساده روند درآمد روزانه — بدون وابستگی به کتابخانه نمودارسازی خارجی
 * (طبق ADR-009: پرهیز از افزودن تغییر استک جدید صرفاً برای یک نمودار ساده).
 */
export default function RevenueBarChart({ data }: { data: DailyRevenuePoint[] }) {
  if (data.length === 0) return null;

  const width = 700;
  const height = 180;
  const paddingBottom = 24;
  const maxAmount = Math.max(...data.map((d) => d.amountInRials), 1);
  const barWidth = width / data.length;

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg width={width} height={height} role="img" aria-label="نمودار روند درآمد روزانه">
        {data.map((point, index) => {
          const barHeight = (point.amountInRials / maxAmount) * (height - paddingBottom - 8);
          const x = index * barWidth;
          const y = height - paddingBottom - barHeight;
          return (
            <g key={point.date}>
              <rect
                x={x + 1}
                y={y}
                width={Math.max(barWidth - 2, 1)}
                height={barHeight}
                fill="var(--color-primary, #2563eb)"
                opacity={0.85}
              >
                <title>
                  {formatToJalali(point.date)}: {point.amountInRials.toLocaleString('fa-IR')} ریال
                </title>
              </rect>
            </g>
          );
        })}
        <line x1={0} y1={height - paddingBottom} x2={width} y2={height - paddingBottom} stroke="var(--color-border)" />
      </svg>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 'var(--fs-xs, 0.7rem)', color: 'var(--color-muted)' }}>
        <span>{formatToJalali(data[0].date)}</span>
        <span>{formatToJalali(data[data.length - 1].date)}</span>
      </div>
    </div>
  );
}
