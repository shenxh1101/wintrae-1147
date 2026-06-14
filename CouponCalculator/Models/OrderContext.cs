namespace CouponCalculator.Models;

public class OrderContext
{
    public string OrderId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; private set; } = new();
    public MemberInfo? Member { get; set; }
    public ShippingInfo? Shipping { get; set; }
    public decimal OriginalAmount => Items.Sum(item => item.TotalAmount);
    public decimal FreightAmount { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public Dictionary<string, object> ExtendedData { get; set; } = new();
    public List<AppliedCoupon> AppliedCoupons { get; private set; } = new();

    public void AddItem(OrderItem item)
    {
        Items.Add(item);
    }

    public void ApplyCoupon(AppliedCoupon coupon)
    {
        AppliedCoupons.Add(coupon);
    }

    public void ClearAppliedCoupons()
    {
        AppliedCoupons.Clear();
    }

    public decimal GetApplicableAmountForCoupon(Coupon coupon)
    {
        if (coupon.ApplicableProductIds.Length == 0 && coupon.ApplicableCategoryIds.Length == 0)
        {
            return OriginalAmount;
        }

        return Items
            .Where(item => coupon.IsApplicableToProduct(item.ProductId) && coupon.IsApplicableToCategory(item.CategoryId))
            .Sum(item => item.TotalAmount);
    }

    public int GetApplicableQuantityForCoupon(Coupon coupon)
    {
        if (coupon.ApplicableProductIds.Length == 0 && coupon.ApplicableCategoryIds.Length == 0)
        {
            return Items.Sum(item => item.Quantity);
        }

        return Items
            .Where(item => coupon.IsApplicableToProduct(item.ProductId) && coupon.IsApplicableToCategory(item.CategoryId))
            .Sum(item => item.Quantity);
    }
}
