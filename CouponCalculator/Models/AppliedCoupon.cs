namespace CouponCalculator.Models;

public class AppliedCoupon
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime AppliedAt { get; } = DateTime.UtcNow;
}
