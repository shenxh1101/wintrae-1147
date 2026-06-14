using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public abstract class BaseRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public DiscountRuleType Type { get; set; }
    public int Priority { get; set; }
    public bool IsStackable { get; set; } = true;
    public string StackingGroup { get; set; } = string.Empty;

    public abstract bool IsApplicable(OrderContext context);
    public abstract decimal CalculateDiscount(OrderContext context);
    public abstract List<string> GetUnmetConditions(OrderContext context);
}
