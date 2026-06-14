namespace CouponCalculator.Models;

public class DiscountRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public DiscountRuleType Type { get; set; }
    public decimal Threshold { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int Priority { get; set; }
    public string[] ApplicableCategories { get; set; } = Array.Empty<string>();
    public string[] ApplicableProducts { get; set; } = Array.Empty<string>();
    public bool IsStackable { get; set; } = true;
    public string StackingGroup { get; set; } = string.Empty;
}

public enum DiscountRuleType
{
    AmountThreshold,
    QuantityThreshold,
    DiscountRate,
    FreeShipping,
    MemberDiscount
}
