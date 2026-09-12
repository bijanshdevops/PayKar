namespace IndustrialPlatform.Domain.Support;

/// <summary>دپارتمان/بخش مربوطه تیکت پشتیبانی — طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
public enum TicketDepartment
{
    /// <summary>پشتیبانی فنی.</summary>
    TechnicalSupport = 1,

    /// <summary>امور مالی و فاکتورها.</summary>
    FinancialAndInvoices = 2,

    /// <summary>مشکلات حساب کاربری.</summary>
    AccountIssues = 3,

    /// <summary>عمومی.</summary>
    General = 4
}
