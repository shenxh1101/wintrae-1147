using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public class AmountThresholdRule : BaseRule
{
    public decimal Threshold { get; set; }
    public decimal DiscountAmount { get; set; }

    public AmountThresholdRule()
    {
        Type = DiscountRuleType.AmountThreshold;
    }

    public override bool IsApplicable(OrderContext context)
    {
        return context.OriginalAmount >= Threshold;
    }

    public override decimal CalculateDiscount(OrderContext context)
    {
        if (!IsApplicable(context))
            return 0;

        return DiscountAmount;
    }

    public override List<string> GetUnmetConditions(OrderContext context)
    {
        var conditions = new List<string>();

        if (context.OriginalAmount < Threshold)
        {
            conditions.Add($"订单金额 {context.OriginalAmount:F2} 元未达到最低要求 {Threshold:F2} 元");
        }

        return conditions;
    }
}
