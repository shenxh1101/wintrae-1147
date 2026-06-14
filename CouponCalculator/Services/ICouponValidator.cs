using CouponCalculator.Models;

namespace CouponCalculator.Services;

public interface ICouponValidator
{
    ValidationResult Validate(Coupon coupon, OrderContext context);
    List<string> GetUnavailableReasons(Coupon coupon, OrderContext context);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
