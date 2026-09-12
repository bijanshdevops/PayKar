/**
 * ریزنمودار خطی نرم (Smooth Curve) درون کارت‌های KPI — بدون وابستگی به کتابخانه نمودارسازی خارجی
 * (طبق ADR-009: همان تکنیک منحنی‌سازی Bezier در SmoothAreaChart.tsx). عرض با `width="100%"` و
 * `preserveAspectRatio="none"` نسبت به عرض واقعی کارت واکنش‌گرا است؛ مختصات داخلی (viewBox) ثابت
 * می‌مانند تا منحنی همیشه نرم و باکیفیت رندر شود.
 */
export default function Sparkline({
  data,
  color = '#2563eb',
  height = 40
}: {
  data: number[];
  color?: string;
  height?: number;
}) {
  // اگر داده ورودی خالی باشد (مثلاً حساب تازه یا خطای موقت API)، به‌جای مخفی‌کردن کامل نمودار،
  // یک خط پایهٔ صاف (Flat Baseline) رسم می‌شود تا چیدمان کارت همیشه دیده شود.
  const safeData = data.length > 0 ? data : new Array(14).fill(0);

  const width = 300;
  const max = Math.max(...safeData, 1);
  const min = Math.min(...safeData, 0);
  const range = max - min || 1;
  const paddingY = 4;

  const points = safeData.map((value, index) => {
    const x = safeData.length > 1 ? (index / (safeData.length - 1)) * width : 0;
    const y = height - paddingY - ((value - min) / range) * (height - paddingY * 2);
    return { x, y };
  });

  const buildSmoothPath = (): string => {
    if (points.length < 2) return `M 0 ${(points[0]?.y ?? height).toFixed(1)}`;
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
  const areaPath = `${linePath} L ${points[points.length - 1].x.toFixed(1)} ${height} L 0 ${height} Z`;
  const gradientId = `sparkline-gradient-${color.replace('#', '')}`;

  return (
    <svg
      width="100%"
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      preserveAspectRatio="none"
      role="img"
      aria-label="روند ۱۴ روز اخیر"
      style={{ display: 'block' }}
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity={0.32} />
          <stop offset="100%" stopColor={color} stopOpacity={0} />
        </linearGradient>
      </defs>
      <path d={areaPath} fill={`url(#${gradientId})`} stroke="none" />
      <path d={linePath} fill="none" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
    </svg>
  );
}
