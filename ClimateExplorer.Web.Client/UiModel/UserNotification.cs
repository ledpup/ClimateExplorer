namespace ClimateExplorer.Web.Client.UiModel;

public sealed record UserNotification
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Message { get; init; }

    public NotificationType Type { get; init; } = NotificationType.Info;

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int Count { get; set; } = 1;

    public Guid? LocationId { get; init; }

    public string? LocationName { get; init; }

    public string? ActionText { get; init; }

    public string? ActionUrl { get; init; }
}
