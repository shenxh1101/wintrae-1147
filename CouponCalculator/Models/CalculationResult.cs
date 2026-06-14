namespace CouponCalculator.Models;

public class CalculationResult
{
    public decimal OriginalAmount { get; set; }
    public decimal ProductDiscountAmount { get; set; }
    public decimal FreightDiscount { get; set; }
    public decimal MemberDiscount { get; set; }
    public decimal TotalDiscountAmount => ProductDiscountAmount + FreightDiscount + MemberDiscount;
    public decimal FinalAmount { get; set; }
    public decimal OriginalFreight { get; set; }
    public decimal FinalFreight { get; set; }
    public List<AppliedCoupon> AppliedCoupons { get; set; } = new();
    public List<DiscountDetail> DiscountDetails { get; set; } = new();
    public Explanation Explanation { get; set; } = new();
    public DateTime CalculatedAt { get; } = DateTime.UtcNow;
}
