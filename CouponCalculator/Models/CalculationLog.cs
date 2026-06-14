namespace CouponCalculator.Models;

public class CalculationLog
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public List<LogEntry> Entries { get; set; } = new();
    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int CouponsTested { get; set; }
    public int CouponsApplied { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string CouponId { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public decimal? DiscountAmount { get; set; }
}
