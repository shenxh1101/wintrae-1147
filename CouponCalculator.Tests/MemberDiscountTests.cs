using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class MemberDiscountTests
{
    private readonly CouponCalculatorEngine _engine;

    public MemberDiscountTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimal_WithGoldMember_ShouldApplyMemberDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });

        var member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };
        _engine.SetMember(context, member);

        var result = _engine.CalculateOptimal(context, new List<Coupon>());

        Assert.True(result.MemberDiscount > 0);
        Assert.Equal(20m, result.MemberDiscount);
    }

    [Fact]
    public void CalculateOptimal_WithDiamondMember_ShouldApplyHigherDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });

        var member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Diamond
        };
        _engine.SetMember(context, member);

        var result = _engine.CalculateOptimal(context, new List<Coupon>());

        Assert.Equal(40m, result.MemberDiscount);
    }

    [Fact]
    public void CalculateOptimal_WithMemberExclusiveCoupon_ShouldApplyExtraDiscount()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });

        var member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };
        _engine.SetMember(context, member);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "会员专享85折",
                Type = CouponType.MemberExclusive,
                DiscountValue = 0.85m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.MemberDiscount > 0);
    }

    [Fact]
    public void CalculateOptimal_WithRequiredMemberLevel_ShouldValidate()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });

        var member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Normal
        };
        _engine.SetMember(context, member);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "仅限金卡",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                RequiredMemberLevel = MemberLevel.Gold,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Single(result.Explanation.RejectedReasons);
        Assert.Empty(result.AppliedCoupons);
    }

    [Fact]
    public void MemberInfo_GetDiscountRate_ShouldReturnCorrectRate()
    {
        var normalMember = new MemberInfo { Level = MemberLevel.Normal };
        var silverMember = new MemberInfo { Level = MemberLevel.Silver };
        var goldMember = new MemberInfo { Level = MemberLevel.Gold };
        var platinumMember = new MemberInfo { Level = MemberLevel.Platinum };
        var diamondMember = new MemberInfo { Level = MemberLevel.Diamond };

        Assert.Equal(1.0m, normalMember.GetDiscountRate());
        Assert.Equal(0.95m, silverMember.GetDiscountRate());
        Assert.Equal(0.90m, goldMember.GetDiscountRate());
        Assert.Equal(0.85m, platinumMember.GetDiscountRate());
        Assert.Equal(0.80m, diamondMember.GetDiscountRate());
    }
}
