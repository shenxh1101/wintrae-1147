namespace CouponCalculator.Models;

public class DiscountDetail
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> AppliedProducts { get; set; } = new();
}
