using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class CouponCalculationTests
{
    private readonly CouponCalculatorEngine _engine;

    public CouponCalculationTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimal_WithAmountOffCoupon_ShouldApplyDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 150m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.ProductDiscountAmount > 0);
        Assert.Contains(result.AppliedCoupons, c => c.CouponId == "C1");
    }

    [Fact]
    public void CalculateOptimal_WithDiscountRateCoupon_ShouldApplyDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 100m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "8折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.ProductDiscountAmount > 0);
        Assert.Equal(20m, result.ProductDiscountAmount);
    }

    [Fact]
    public void CalculateOptimal_WithMaxDiscount_ShouldLimitDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 500m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "5折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.5m,
                MaxDiscountAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(100m, result.ProductDiscountAmount);
    }

    [Fact]
    public void CalculateOptimal_WithInsufficientAmount_ShouldNotApply()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 50m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Empty(result.AppliedCoupons);
        Assert.Single(result.Explanation.RejectedReasons);
    }

    [Fact]
    public void CalculateOptimal_WithExpiredCoupon_ShouldReject()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 150m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "过期券",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(-1)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Empty(result.AppliedCoupons);
        Assert.Single(result.Explanation.RejectedReasons);
    }
}
