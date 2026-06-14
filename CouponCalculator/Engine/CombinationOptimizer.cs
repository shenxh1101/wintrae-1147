using CouponCalculator.Models;

namespace CouponCalculator.Engine;

public class CombinationOptimizer
{
    private readonly IStackingEngine _stackingEngine;

    public CombinationOptimizer(IStackingEngine stackingEngine)
    {
        _stackingEngine = stackingEngine;
    }

    public List<Coupon> Optimize(List<Coupon> availableCoupons, OrderContext context, Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        if (!availableCoupons.Any())
            return new List<Coupon>();

        var validCombinations = _stackingEngine.GetValidCombinations(availableCoupons);

        var bestCombination = new List<Coupon>();
        decimal bestDiscount = 0;

        foreach (var combination in validCombinations)
        {
            decimal totalDiscount = 0;
            var tempContext = CloneContext(context);

            foreach (var coupon in combination)
            {
                var discount = calculateDiscount(coupon, tempContext);
                totalDiscount += discount;

                ApplyCouponToContext(tempContext, coupon, discount);
            }

            if (totalDiscount > bestDiscount)
            {
                bestDiscount = totalDiscount;
                bestCombination = combination;
            }
        }

        return bestCombination;
    }

    public List<Coupon> GreedyOptimize(List<Coupon> availableCoupons, OrderContext context, Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        if (!availableCoupons.Any())
            return new List<Coupon>();

        var result = new List<Coupon>();
        var tempContext = CloneContext(context);
        var remainingCoupons = availableCoupons.ToList();

        while (remainingCoupons.Any())
        {
            var bestCoupon = FindBestCoupon(remainingCoupons, tempContext, calculateDiscount);

            if (bestCoupon == null)
                break;

            var discount = calculateDiscount(bestCoupon, tempContext);

            var canAdd = true;
            foreach (var existing in result)
            {
                if (!_stackingEngine.CanStack(bestCoupon, existing))
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd && discount > 0)
            {
                result.Add(bestCoupon);
                ApplyCouponToContext(tempContext, bestCoupon, discount);
            }

            remainingCoupons.Remove(bestCoupon);
        }

        return result;
    }

    private Coupon? FindBestCoupon(List<Coupon> coupons, OrderContext context, Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        Coupon? best = null;
        decimal bestDiscount = 0;

        foreach (var coupon in coupons)
        {
            var discount = calculateDiscount(coupon, context);
            if (discount > bestDiscount)
            {
                bestDiscount = discount;
                best = coupon;
            }
        }

        return best;
    }

    private OrderContext CloneContext(OrderContext original)
    {
        return new OrderContext
        {
            OrderId = original.OrderId,
            Items = original.Items.ToList(),
            Member = original.Member,
            Shipping = original.Shipping,
            FreightAmount = original.FreightAmount,
            ExtendedData = new Dictionary<string, object>(original.ExtendedData)
        };
    }

    private void ApplyCouponToContext(OrderContext context, Coupon coupon, decimal discount)
    {
        if (coupon.Type == CouponType.FreeShipping)
        {
            context.FreightAmount = 0;
        }
    }
}
