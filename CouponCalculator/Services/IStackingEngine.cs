using CouponCalculator.Models;

namespace CouponCalculator.Services;

public interface IStackingEngine
{
    bool CanStack(Coupon coupon1, Coupon coupon2);
    List<Coupon> GetStackableCoupons(List<Coupon> coupons);
    List<List<Coupon>> GetValidCombinations(List<Coupon> coupons);
}
