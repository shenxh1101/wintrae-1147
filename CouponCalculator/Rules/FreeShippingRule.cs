using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public class FreeShippingRule : BaseRule
{
    public decimal? MinOrderAmount { get; set; }

    public FreeShippingRule()
    {
        Type = DiscountRuleType.FreeShipping;
        IsStackable = true;
    }

    public override bool IsApplicable(OrderContext context)
    {
        if (MinOrderAmount.HasValue)
        {
            return context.OriginalAmount >= MinOrderAmount.Value;
        }

        return true;
    }

    public override decimal CalculateDiscount(OrderContext context)
    {
        if (!IsApplicable(context))
            return 0;

        return context.FreightAmount;
    }

    public override List<string> GetUnmetConditions(OrderContext context)
    {
        var conditions = new List<string>();

        if (MinOrderAmount.HasValue && context.OriginalAmount < MinOrderAmount.Value)
        {
            conditions.Add($"订单金额 {context.OriginalAmount:F2} 元未达到免运费的最低要求 {MinOrderAmount:F2} 元");
        }

        return conditions;
    }
}
