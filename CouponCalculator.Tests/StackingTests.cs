using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class StackingTests
{
    private readonly CouponCalculatorEngine _engine;
    private readonly StackingEngine _stackingEngine;

    public StackingTests()
    {
        _engine = new CouponCalculatorEngine();
        _stackingEngine = new StackingEngine();
    }

    [Fact]
    public void StackingEngine_SameGroup_ShouldNotStack()
    {
        var coupon1 = new Coupon { CouponId = "C1", StackingGroup = "GROUP1" };
        var coupon2 = new Coupon { CouponId = "C2", StackingGroup = "GROUP1" };

        Assert.False(_stackingEngine.CanStack(coupon1, coupon2));
    }

    [Fact]
    public void StackingEngine_DifferentGroup_ShouldStack()
    {
        var coupon1 = new Coupon { CouponId = "C1", StackingGroup = "GROUP1" };
        var coupon2 = new Coupon { CouponId = "C2", StackingGroup = "GROUP2" };

        Assert.True(_stackingEngine.CanStack(coupon1, coupon2));
    }

    [Fact]
    public void StackingEngine_FreeShipping_ShouldNotStack()
    {
        var coupon1 = new Coupon { CouponId = "C1", Type = CouponType.FreeShipping };
        var coupon2 = new Coupon { CouponId = "C2", Type = CouponType.FreeShipping };

        Assert.False(_stackingEngine.CanStack(coupon1, coupon2));
    }

    [Fact]
    public void CalculateOptimal_WithStackingCoupons_ShouldApplyMultiple()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 300m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满100减10",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m,
                StackingGroup = "",
                CanStackWithSameType = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "满200减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 200m,
                StackingGroup = "",
                CanStackWithSameType = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(2, result.AppliedCoupons.Count);
    }

    [Fact]
    public void CalculateOptimal_WithFreeShipping_ShouldStackWithDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });

        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            BaseFreight = 15m,
            Weight = 1m,
            FirstWeight = 1m,
            FirstWeightPrice = 15m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        _engine.SetShipping(context, shipping);

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
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "免运费",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.FreightDiscount > 0);
        Assert.Equal(0, result.FinalFreight);
        Assert.True(result.ProductDiscountAmount > 0);
    }
}
