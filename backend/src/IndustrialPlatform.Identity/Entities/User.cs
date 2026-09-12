using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Identity.Entities;

/// <summary>
/// کاربر سیستم. طبق ADR-003، مسیر سریع OTP-only (بدون فرم) هنوز پابرجاست.
/// طبق ADR-005، نام‌کاربری/رمزعبور به‌عنوان روش ورود دوم اختیاری اضافه شد.
/// طبق ADR-006، مسیر ثبت‌نام کامل (نام‌کاربری+ایمیل+موبایل+رمزعبور) نیز اضافه شده؛
/// در این مسیر شماره موبایل تا تایید با OTP، «تایید‌نشده» علامت‌گذاری می‌شود.
/// </summary>
public sealed class User : BaseEntity<Guid>
{
    public string MobileNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>نام‌کاربری اختیاری برای ورود با رمز عبور (ADR-005) — یکتا در کل سیستم، Nullable.</summary>
    public string? Username { get; private set; }

    /// <summary>ایمیل اختیاری (ADR-006) — صرفاً اطلاعات تماس، در هیچ جریان احراز هویتی استفاده نمی‌شود.</summary>
    public string? Email { get; private set; }

    /// <summary>هش BCrypt رمز عبور — هرگز رمز خام ذخیره نمی‌شود. تا زمانی‌که کاربر رمز تنظیم نکرده، Null است.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// آیا مالکیت شماره موبایل با OTP تایید شده — طبق ADR-006. برای کاربرانی که از مسیر OTP
    /// (Create) ساخته می‌شوند همیشه true است (چون OTP پیش‌نیاز ساخت حساب است)؛ برای کاربرانی
    /// که از مسیر ثبت‌نام کامل (CreateWithCredentials) ساخته می‌شوند تا زمان تایید OTP، false است.
    /// </summary>
    public bool IsMobileVerified { get; private set; }

    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { }

    private User(Guid id, string mobileNumber, bool isMobileVerified) : base(id)
    {
        MobileNumber = mobileNumber;
        IsMobileVerified = isMobileVerified;
    }

    /// <summary>مسیر سریع OTP-only (ADR-003) — همیشه پس از تایید موفق OTP فراخوانی می‌شود، پس همیشه تایید‌شده است.</summary>
    public static User Create(string mobileNumber) => new(Guid.NewGuid(), mobileNumber, isMobileVerified: true);

    /// <summary>مسیر ثبت‌نام کامل (ADR-006) — تا تایید OTP، شماره موبایل «تایید‌نشده» می‌ماند.</summary>
    public static User CreateWithCredentials(string mobileNumber, string username, string email, string passwordHash)
    {
        var user = new User(Guid.NewGuid(), mobileNumber, isMobileVerified: false);
        user.Username = username;
        user.Email = email;
        user.PasswordHash = passwordHash;
        return user;
    }

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId)) return;
        _userRoles.Add(UserRole.Create(Id, roleId));
    }

    public void Deactivate() => IsActive = false;

    /// <summary>تنظیم/تغییر نام‌کاربری و هش رمز عبور — طبق ADR-005 فقط برای کاربر از‌قبل‌احرازشده (OTP) مجاز است.</summary>
    public void SetCredentials(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
    }

    /// <summary>جایگزینی هش رمز عبور — برای جریان «فراموشی رمز عبور» (ADR-006).</summary>
    public void ResetPassword(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>علامت‌گذاری تایید مالکیت شماره موبایل — طبق ADR-006 پس از هر تایید موفق OTP فراخوانی می‌شود (Idempotent).</summary>
    public void MarkMobileVerified() => IsMobileVerified = true;
}
