namespace CouponCalculator.Models;

public class Coupon
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int MinQuantity { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string[] ApplicableProductIds { get; set; } = Array.Empty<string>();
    public string[] ApplicableCategoryIds { get; set; } = Array.Empty<string>();
    public string[] ExcludedProductIds { get; set; } = Array.Empty<string>();
    public int Priority { get; set; }
    public string StackingGroup { get; set; } = string.Empty;
    public bool CanStackWithSameType { get; set; }
    public MemberLevel? RequiredMemberLevel { get; set; }

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return now >= ValidFrom && now <= ValidTo;
    }

    public bool IsApplicableToProduct(string productId)
    {
        if (ExcludedProductIds.Contains(productId))
            return false;

        if (ApplicableProductIds.Length == 0 && ApplicableCategoryIds.Length == 0)
            return true;

        return ApplicableProductIds.Contains(productId);
    }

    public bool IsApplicableToCategory(string categoryId)
    {
        if (ApplicableCategoryIds.Length == 0)
            return true;

        return ApplicableCategoryIds.Contains(categoryId);
    }
}
