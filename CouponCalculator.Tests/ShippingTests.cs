using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class ShippingTests
{
    private readonly CouponCalculatorEngine _engine;

    public ShippingTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimal_WithFreeShippingCoupon_Should免除运费()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 150m,
            Quantity = 1
        });

        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            BaseFreight = 10m,
            Weight = 1m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        _engine.SetShipping(context, shipping);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(10m, result.FreightDiscount);
        Assert.Equal(0, result.FinalFreight);
    }

    [Fact]
    public void ShippingInfo_CalculateFreight_ShouldCalculateCorrectly()
    {
        var shipping = new ShippingInfo
        {
            Weight = 2.5m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };

        var freight = shipping.CalculateFreight();

        Assert.Equal(15m, freight);
    }

    [Fact]
    public void ShippingInfo_CalculateFreight_WithinFirstWeight()
    {
        var shipping = new ShippingInfo
        {
            Weight = 0.5m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };

        var freight = shipping.CalculateFreight();

        Assert.Equal(10m, freight);
    }

    [Fact]
    public void CalculateOptimal_WithMultipleWeights_ShouldCalculateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 100m,
            Quantity = 2,
            Weight = 1.5m
        });

        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            Weight = 3m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        _engine.SetShipping(context, shipping);

        Assert.Equal(20m, context.FreightAmount);
    }
}
