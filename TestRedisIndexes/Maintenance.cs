public record Maintenance
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Domain { get; init; } = string.Empty;
    public string ClientCode { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset ExpiryTime { get; init; }
    public string Message { get; init; } = string.Empty;
}
