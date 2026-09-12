namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>
/// پورت خروجی برای دریافت آدرس Callback درگاه پرداخت — پیاده‌سازی در لایه Api چون به
/// پیکربندی میزبانی (Base URL عمومی) وابسته است و Application نباید مستقیم IConfiguration بخواند.
/// </summary>
public interface IPaymentCallbackUrlProvider
{
    string GetCallbackUrl();
}
