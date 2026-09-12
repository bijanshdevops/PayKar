namespace IndustrialPlatform.Shared.Extensions;

/// <summary>
/// الحاقات رشته‌ای فارسی طبق سند 01-Architecture.md (بخش لایه Shared):
/// نرمال‌سازی حروف عربی/فارسی (ک/ی) و تبدیل ارقام.
/// </summary>
public static class PersianStringExtensions
{
    public static string NormalizePersian(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        return input
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Replace('‌', ' ')
            .Trim();
    }

    public static string ToPersianDigits(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var persianDigits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
                chars[i] = persianDigits[chars[i] - '0'];
        }

        return new string(chars);
    }

    public static bool IsValidIranianMobileNumber(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = input.NormalizePersian();
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^09\d{9}$");
    }

    public static bool IsValidNationalId(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var normalized = input.NormalizePersian();
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d{10,11}$");
    }
}
