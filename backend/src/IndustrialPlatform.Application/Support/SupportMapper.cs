using IndustrialPlatform.Domain.Support;

namespace IndustrialPlatform.Application.Support;

internal static class SupportMapper
{
    public static SupportTicketDto ToDto(SupportTicket ticket, DateTime lastActivityAtUtc) => new(
        ticket.Id,
        ticket.TicketNumber,
        ticket.UserId,
        ticket.Subject,
        ticket.Department.ToString(),
        ticket.Priority.ToString(),
        ticket.Status.ToString(),
        ticket.CreatedAtUtc,
        lastActivityAtUtc);

    public static SupportMessageDto ToDto(SupportMessage message) => new(
        message.Id,
        message.SenderUserId,
        message.IsFromSupportTeam,
        message.Body,
        message.AttachmentUrl,
        message.AttachmentFileName,
        message.CreatedAtUtc);

    public static SupportTicketDetailDto ToDetailDto(SupportTicket ticket, IReadOnlyList<SupportMessage> messages)
    {
        var lastActivityAtUtc = messages.Count == 0
            ? ticket.LastModifiedAtUtc ?? ticket.CreatedAtUtc
            : messages.Max(m => m.CreatedAtUtc);

        if (ticket.LastModifiedAtUtc is { } lastModified && lastModified > lastActivityAtUtc)
            lastActivityAtUtc = lastModified;

        return new SupportTicketDetailDto(
            ticket.Id,
            ticket.TicketNumber,
            ticket.UserId,
            ticket.Subject,
            ticket.Department.ToString(),
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.CreatedAtUtc,
            lastActivityAtUtc,
            messages.Select(ToDto).ToList(),
            ticket.RequesterMobileNumber);
    }

    public static SupportTicketStatusSummaryDto ToSummaryDto(IReadOnlyDictionary<SupportTicketStatus, int> counts)
    {
        int Get(SupportTicketStatus status) => counts.TryGetValue(status, out var count) ? count : 0;

        var pendingResponse = Get(SupportTicketStatus.PendingResponse);
        var inProgress = Get(SupportTicketStatus.InProgress);
        var answered = Get(SupportTicketStatus.Answered);
        var closed = Get(SupportTicketStatus.Closed);
        var reopened = Get(SupportTicketStatus.Reopened);

        return new SupportTicketStatusSummaryDto(
            All: pendingResponse + inProgress + answered + closed + reopened,
            PendingResponse: pendingResponse,
            InProgress: inProgress,
            Answered: answered,
            Closed: closed,
            Reopened: reopened);
    }
}
