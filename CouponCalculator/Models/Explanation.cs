namespace CouponCalculator.Models;

public class Explanation
{
    public List<AppliedExplanation> AppliedReasons { get; set; } = new();
    public List<RejectedExplanation> RejectedReasons { get; set; } = new();
}

public class AppliedExplanation
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public List<string> AppliedConditions { get; set; } = new();
}

public class RejectedExplanation
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> FailedConditions { get; set; } = new();
}
