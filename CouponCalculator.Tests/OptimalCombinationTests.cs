using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class OptimalCombinationTests
{
    private readonly CouponCalculatorEngine _engine;

    public OptimalCombinationTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimal_ShouldSelectBestCombination()
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
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "8折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                MaxDiscountAmount = 50m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.NotEmpty(result.AppliedCoupons);
        Assert.True(result.TotalDiscountAmount > 0);
    }

    [Fact]
    public void CalculateOptimal_WithConflictingCoupons_ShouldSelectBest()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
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
                StackingGroup = "GROUP1",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                StackingGroup = "GROUP1",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Single(result.AppliedCoupons);
        Assert.Equal(20m, result.ProductDiscountAmount);
    }

    [Fact]
    public void CalculateOptimal_WithNoApplicableCoupons_ShouldReturnNoDiscount()
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
        Assert.Equal(0, result.ProductDiscountAmount);
    }

    [Fact]
    public void GetAvailableCoupons_ShouldReturnAllTrials()
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
                CouponCode = "有效券",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "过期券",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(-1)
            }
        };

        var results = _engine.GetAvailableCoupons(context, coupons).ToList();

        Assert.Equal(2, results.Count);
        Assert.True(results.First(r => r.Coupon.CouponId == "C1").IsAvailable);
        Assert.False(results.First(r => r.Coupon.CouponId == "C2").IsAvailable);
    }

    [Fact]
    public void CalculateOptimal_WithQuantityDiscount_ShouldApplyCorrectly()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 50m,
            Quantity = 3
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满3件8折",
                Type = CouponType.QuantityDiscount,
                DiscountValue = 0.8m,
                MinQuantity = 3,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.ProductDiscountAmount > 0);
        Assert.Equal(30m, result.ProductDiscountAmount);
    }
}
