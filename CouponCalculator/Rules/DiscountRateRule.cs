using CouponCalculator.Models;

namespace CouponCalculator.Rules;

public class DiscountRateRule : BaseRule
{
    public decimal DiscountRate { get; set; }
    public decimal? MaxDiscount { get; set; }
    public string[] ApplicableCategories { get; set; } = Array.Empty<string>();
    public string[] ApplicableProducts { get; set; } = Array.Empty<string>();

    public DiscountRateRule()
    {
        Type = DiscountRuleType.DiscountRate;
    }

    public override bool IsApplicable(OrderContext context)
    {
        if (ApplicableProducts.Length == 0 && ApplicableCategories.Length == 0)
            return true;

        return context.Items.Any(item =>
            (ApplicableProducts.Length == 0 || ApplicableProducts.Contains(item.ProductId)) &&
            (ApplicableCategories.Length == 0 || ApplicableCategories.Contains(item.CategoryId))
        );
    }

    public override decimal CalculateDiscount(OrderContext context)
    {
        if (!IsApplicable(context))
            return 0;

        var applicableItems = context.Items
            .Where(item =>
                (ApplicableProducts.Length == 0 || ApplicableProducts.Contains(item.ProductId)) &&
                (ApplicableCategories.Length == 0 || ApplicableCategories.Contains(item.CategoryId))
            )
            .ToList();

        var applicableAmount = applicableItems.Sum(item => item.TotalAmount);
        var discount = applicableAmount * (1 - DiscountRate);

        if (MaxDiscount.HasValue)
        {
            discount = Math.Min(discount, MaxDiscount.Value);
        }

        return discount;
    }

    public override List<string> GetUnmetConditions(OrderContext context)
    {
        return new List<string>();
    }
}
