import { Children, cloneElement, isValidElement, type ReactNode } from 'react';

/**
 * کاروسل اسکرول خودکار افقی — طبق تسک #79.
 * محتوا یک‌بار تکرار می‌شود تا حلقه‌ی اسکرول بی‌پایان و بدون پرش ایجاد شود (transform: translateX(-50%)).
 * با هاور موس (دسکتاپ) اسکرول متوقف می‌شود؛ سرعت با متغیر CSS --carousel-duration قابل تنظیم است.
 */
export default function AutoScrollCarousel({
  children,
  durationSeconds = 32
}: {
  children: ReactNode;
  durationSeconds?: number;
}) {
  const items = Children.toArray(children);

  if (items.length === 0) return null;

  const duplicateItems = items.map((child) =>
    isValidElement(child) ? cloneElement(child, { key: `dup-${child.key}` }) : child
  );

  return (
    <div className="carousel-viewport">
      <div className="carousel-track" style={{ ['--carousel-duration' as string]: `${durationSeconds}s` }}>
        {items}
        {duplicateItems}
      </div>
    </div>
  );
}
