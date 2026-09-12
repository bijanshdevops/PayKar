using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

internal static class CandidateMapper
{
    public static CandidateEducationDto ToDto(CandidateEducation education) => new(
        education.Id, education.DegreeLevel, education.FieldOfStudy, education.InstitutionName, education.GraduationYear);

    public static CandidateWorkExperienceDto ToDto(CandidateWorkExperience workExperience) => new(
        workExperience.Id, workExperience.JobTitle, workExperience.CompanyName, workExperience.StartYear, workExperience.EndYear, workExperience.Description);

    public static CandidateCertificationDto ToDto(CandidateCertification certification) => new(
        certification.Id, certification.Title, certification.IssuingOrganization, certification.YearObtained);

    public static CandidateSkillDto ToDto(CandidateSkill skill) => new(skill.Id, skill.Name, skill.Category, skill.Level);

    public static CandidateLanguageDto ToDto(CandidateLanguage language) => new(language.Id, language.Name, language.ProficiencyLevel.ToString());

    /// <summary>
    /// نگاشت به DTO کامل کارفرما — فقط باید از هندلرهایی فراخوانی شود که پیش از این مالکیت آگهی را
    /// از طریق JobAdOwnershipGuard احراز کرده‌اند (GetApplicationsForJobAdQuery, GetRecentApplicantsForCompanyQuery).
    /// </summary>
    public static EmployerApplicantDto ToEmployerDto(
        JobApplication application, Candidate candidate, string jobAdTitle,
        IReadOnlyList<CandidateEducation> educations, IReadOnlyList<CandidateWorkExperience> workExperiences) => new(
        application.Id,
        application.JobAdId,
        jobAdTitle,
        candidate.Id,
        candidate.FullName,
        candidate.AvatarUrl,
        candidate.MobileNumber,
        candidate.Email,
        candidate.City,
        candidate.EducationLevel,
        candidate.WorkExperienceSummary,
        educations.Select(ToDto).ToList(),
        workExperiences.Select(ToDto).ToList(),
        candidate.Skills,
        candidate.ResumeFileUrl,
        application.MatchScorePercent,
        application.Status.ToString(),
        application.TrackingToken,
        application.CreatedAtUtc,
        application.CompanyNotes,
        application.InterviewDateTimeUtc);

    public static CandidateDto ToDto(
        Candidate candidate, IReadOnlyList<CandidateEducation> educations, IReadOnlyList<CandidateWorkExperience> workExperiences,
        IReadOnlyList<CandidateCertification> certifications, IReadOnlyList<CandidateSkill> skills, IReadOnlyList<CandidateLanguage> languages) => new(
        candidate.Id,
        candidate.FullName,
        candidate.MilitaryServiceStatus.ToString(),
        candidate.EducationLevel,
        candidate.WorkExperienceSummary,
        candidate.Skills,
        candidate.Interests,
        candidate.PsychologyAnswers,
        candidate.ResumeFileUrl,
        candidate.IsFeePaid,
        Candidate.ResumeCreationFeeAmountInRials,
        candidate.AvatarUrl,
        candidate.Email,
        candidate.City,
        educations.Select(ToDto).ToList(),
        workExperiences.Select(ToDto).ToList(),
        candidate.JobTitle,
        candidate.ProfessionalSummary,
        candidate.LinkedInUrl,
        candidate.GitHubUrl,
        candidate.PersonalWebsiteUrl,
        candidate.PreferredWorkType?.ToString(),
        candidate.MinRequestedSalaryInToman,
        candidate.MaxRequestedSalaryInToman,
        candidate.IsActivelyLookingForJob,
        certifications.Select(ToDto).ToList(),
        skills.Select(ToDto).ToList(),
        languages.Select(ToDto).ToList());

    public static JobApplicationDto ToDto(JobApplication application) => new(
        application.Id,
        application.JobAdId,
        application.CandidateId,
        application.Status.ToString(),
        application.TrackingToken,
        application.CreatedAtUtc,
        application.MatchScorePercent);
}
