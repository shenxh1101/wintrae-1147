namespace CouponCalculator.Models;

public class CouponTrialResult
{
    public Coupon Coupon { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal AmountAfterDiscount { get; set; }
    public List<string> UnavailableReasons { get; set; } = new();
    public List<string> ApplicableProductIds { get; set; } = new();
}
