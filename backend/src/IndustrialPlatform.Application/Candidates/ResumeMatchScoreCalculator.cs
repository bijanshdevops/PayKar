namespace IndustrialPlatform.Application.Candidates;

/// <summary>
/// محاسبهٔ «درصد تطابق» (MatchScore) بین مهارت‌های کارجو و نیازمندی‌های یک آگهی — طبق تصمیم صریح
/// محصولی این فاز: «فیلد MatchScore به‌عنوان فیلد محاسباتی یا عددی در JobApplications اضافه شود».
/// این سرویس عمداً ساده، قطعی (Deterministic) و بدون وابستگی خارجی/هوش مصنوعی است — نه یک عدد
/// ساختگی: بر پایهٔ همپوشانی واقعی توکن‌های متنی که خود کاربران (کارجو در پروفایل، کارفرما در آگهی)
/// وارد کرده‌اند محاسبه می‌شود، مشابه امتیازهای ساده‌ی «تطابق کلیدواژه‌ای» رایج در سامانه‌های کاریابی.
/// خالص و بدون وابستگی (Pure) است تا به‌سادگی Unit-Test پذیر باشد.
/// </summary>
public static class ResumeMatchScoreCalculator
{
    private static readonly char[] Separators = { ',', '،', '/', '|', '-', '_', '\n', '\r', '\t', ' ' };

    /// <summary>
    /// درصد همپوشانی توکن‌های <paramref name="candidateSkills"/> با <paramref name="jobRequiredSkills"/>
    /// را بر حسب سهم مهارت‌های آگهی که در مهارت‌های کارجو نیز یافت می‌شوند محاسبه می‌کند (۰ تا ۱۰۰).
    /// اگر هرکدام از دو ورودی خالی/نامعتبر باشد، امکان محاسبهٔ معنادار وجود ندارد و null بازگردانده می‌شود
    /// (نه صفر) — چون «صفر درصد تطابق» یک ادعای صریح است، در حالی‌که «داده کافی نیست» ادعای متفاوتی است.
    /// </summary>
    public static int? Calculate(string? candidateSkills, string? jobRequiredSkills)
    {
        var candidateTokens = Tokenize(candidateSkills);
        var jobTokens = Tokenize(jobRequiredSkills);

        if (candidateTokens.Count == 0 || jobTokens.Count == 0)
            return null;

        var matchedCount = jobTokens.Count(jobToken => candidateTokens.Contains(jobToken));
        var percent = (int)Math.Round(matchedCount * 100.0 / jobTokens.Count, MidpointRounding.AwayFromZero);

        return Math.Clamp(percent, 0, 100);
    }

    private static HashSet<string> Tokenize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>();

        return raw
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length > 0)
            .ToHashSet();
    }
}
