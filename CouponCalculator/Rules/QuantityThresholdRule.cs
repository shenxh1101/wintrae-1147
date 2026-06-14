using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public class QuantityThresholdRule : BaseRule
{
    public int Threshold { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal? MaxDiscount { get; set; }

    public QuantityThresholdRule()
    {
        Type = DiscountRuleType.QuantityThreshold;
    }

    public override bool IsApplicable(OrderContext context)
    {
        var totalQuantity = context.Items.Sum(item => item.Quantity);
        return totalQuantity >= Threshold;
    }

    public override decimal CalculateDiscount(OrderContext context)
    {
        if (!IsApplicable(context))
            return 0;

        var discount = context.OriginalAmount * (1 - DiscountRate);

        if (MaxDiscount.HasValue)
        {
            discount = Math.Min(discount, MaxDiscount.Value);
        }

        return discount;
    }

    public override List<string> GetUnmetConditions(OrderContext context)
    {
        var conditions = new List<string>();
        var totalQuantity = context.Items.Sum(item => item.Quantity);

        if (totalQuantity < Threshold)
        {
            conditions.Add($"商品数量 {totalQuantity} 件未达到最低要求 {Threshold} 件");
        }

        return conditions;
    }
}
