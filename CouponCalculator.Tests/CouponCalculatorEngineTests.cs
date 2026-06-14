using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class CouponCalculatorEngineTests
{
    private readonly CouponCalculatorEngine _engine;

    public CouponCalculatorEngineTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CreateOrderContext_ShouldReturnValidContext()
    {
        var context = _engine.CreateOrderContext("ORDER001");

        Assert.NotNull(context);
        Assert.Equal("ORDER001", context.OrderId);
        Assert.Empty(context.Items);
    }

    [Fact]
    public void AddItem_ShouldAddItemToContext()
    {
        var context = _engine.CreateOrderContext();
        var item = new OrderItem
        {
            ProductId = "PROD001",
            ProductName = "Test Product",
            Price = 100m,
            Quantity = 2
        };

        _engine.AddItem(context, item);

        Assert.Single(context.Items);
        Assert.Equal(200m, context.OriginalAmount);
    }

    [Fact]
    public void SetMember_ShouldSetMemberInfo()
    {
        var context = _engine.CreateOrderContext();
        var member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };

        _engine.SetMember(context, member);

        Assert.NotNull(context.Member);
        Assert.Equal("MB001", context.Member.MemberId);
        Assert.Equal(MemberLevel.Gold, context.Member.Level);
    }

    [Fact]
    public void SetShipping_ShouldSetShippingAndCalculateFreight()
    {
        var context = _engine.CreateOrderContext();
        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            BaseFreight = 10m,
            Weight = 2m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };

        _engine.SetShipping(context, shipping);

        Assert.NotNull(context.Shipping);
        Assert.Equal(15m, context.FreightAmount);
    }
}
