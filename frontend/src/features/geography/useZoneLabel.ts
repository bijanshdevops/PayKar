import { useMemo } from 'react';
import { useGetCitiesQuery, useGetIndustrialZonesQuery, useGetProvincesQuery } from '@/features/geography/geographyApi';

/**
 * نگاشت industrialZoneId → «شهرک صنعتی X، شهر Y» با استفاده از داده واقعی جغرافیایی
 * (بدون هیچ فیلد جدید در بک‌اند — endpointهای موجود با پارامتر خالی، همه رکوردها را برمی‌گردانند).
 */
export function useZoneLabelMap(): Map<string, string> {
  const { data: provinces } = useGetProvincesQuery();
  const { data: cities } = useGetCitiesQuery(undefined);
  const { data: zones } = useGetIndustrialZonesQuery(undefined);

  return useMemo(() => {
    const provinceNameById = new Map((provinces?.data ?? []).map((p) => [p.id, p.name]));
    const cityById = new Map((cities?.data ?? []).map((c) => [c.id, c]));
    const map = new Map<string, string>();

    for (const zone of zones?.data ?? []) {
      const city = cityById.get(zone.cityId);
      const provinceName = city ? provinceNameById.get(city.provinceId) : undefined;
      const label = city
        ? `شهرک صنعتی ${zone.name} — ${city.name}${provinceName ? `، استان ${provinceName}` : ''}`
        : `شهرک صنعتی ${zone.name}`;
      map.set(zone.id, label);
    }

    return map;
  }, [provinces, cities, zones]);
}
