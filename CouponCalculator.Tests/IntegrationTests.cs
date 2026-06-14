using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class IntegrationTests
{
    [Fact]
    public void CompleteScenario_EcommerceOrder_ShouldCalculateCorrectly()
    {
        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ECOM-ORDER-001");

        engine.AddItem(context, new OrderItem
        {
            ProductId = "LAPTOP-001",
            ProductName = "笔记本电脑",
            Price = 5000m,
            Quantity = 1,
            CategoryId = "ELECTRONICS"
        });

        engine.AddItem(context, new OrderItem
        {
            ProductId = "MOUSE-001",
            ProductName = "无线鼠标",
            Price = 100m,
            Quantity = 2,
            CategoryId = "ACCESSORIES"
        });

        var member = new MemberInfo
        {
            MemberId = "USER-12345",
            Level = MemberLevel.Gold
        };
        engine.SetMember(context, member);

        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            Province = "广东",
            City = "深圳",
            BaseFreight = 20m,
            Weight = 3m,
            FirstWeight = 1m,
            FirstWeightPrice = 15m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        engine.SetShipping(context, shipping);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满5000减500",
                Type = CouponType.AmountOff,
                DiscountValue = 500m,
                MinOrderAmount = 5000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                Priority = 1
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "9折电子券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 200m,
                ApplicableCategoryIds = new[] { "ELECTRONICS" },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                StackingGroup = "DISCOUNT"
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                IsStackable = true
            }
        };

        var result = engine.CalculateOptimal(context, coupons);

        Assert.True(result.OriginalAmount > 0);
        Assert.True(result.TotalDiscountAmount > 0);
        Assert.True(result.FinalAmount < result.OriginalAmount);

        var simpleFormat = engine.FormatDetails(result, DisplayFormat.Simple);
        Assert.NotEmpty(simpleFormat);

        var detailedFormat = engine.FormatDetails(result, DisplayFormat.Detailed);
        Assert.Contains("订单优惠明细", detailedFormat);

        var billFormat = engine.FormatDetails(result, DisplayFormat.Bill);
        Assert.Contains("订单账单", billFormat);
    }

    [Fact]
    public void CompleteScenario_RestaurantOrder_ShouldCalculateCorrectly()
    {
        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("REST-ORDER-001");

        engine.AddItem(context, new OrderItem
        {
            ProductId = "MEAL-001",
            ProductName = "招牌套餐",
            Price = 58m,
            Quantity = 2,
            CategoryId = "MEALS",
            IsShippingRequired = false
        });

        engine.AddItem(context, new OrderItem
        {
            ProductId = "DRINK-001",
            ProductName = "奶茶",
            Price = 18m,
            Quantity = 2,
            CategoryId = "DRINKS",
            IsShippingRequired = false
        });

        var member = new MemberInfo
        {
            MemberId = "VIP-USER-001",
            Level = MemberLevel.Platinum
        };
        engine.SetMember(context, member);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "R1",
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "R2",
                CouponCode = "饮品8折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                ApplicableCategoryIds = new[] { "DRINKS" },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimal(context, coupons);

        Assert.True(result.OriginalAmount > 0);
        Assert.True(result.FinalAmount < result.OriginalAmount);
    }

    [Fact]
    public void CompleteScenario_MemberSystem_ShouldApplyMembershipDiscount()
    {
        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("MEMBER-ORDER-001");

        engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD-001",
            ProductName = "会员专享商品",
            Price = 1000m,
            Quantity = 1,
            CategoryId = "GENERAL"
        });

        var levels = new[] { MemberLevel.Normal, MemberLevel.Silver, MemberLevel.Gold, MemberLevel.Platinum, MemberLevel.Diamond };
        var expectedDiscounts = new[] { 0m, 50m, 100m, 150m, 200m };

        for (int i = 0; i < levels.Length; i++)
        {
            var testContext = engine.CreateOrderContext();
            engine.AddItem(testContext, new OrderItem
            {
                ProductId = "PROD-001",
                Price = 1000m,
                Quantity = 1
            });

            var member = new MemberInfo { Level = levels[i] };
            engine.SetMember(testContext, member);

            var result = engine.CalculateOptimal(testContext, new List<Coupon>());

            Assert.Equal(expectedDiscounts[i], result.MemberDiscount);
        }
    }

    [Fact]
    public void TryApply_ShouldReturnDetailedTrialResult()
    {
        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext();
        engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD-001",
            Price = 200m,
            Quantity = 2
        });

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "测试券",
            Type = CouponType.AmountOff,
            DiscountValue = 30m,
            MinOrderAmount = 200m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var trialResult = engine.TryApply(context, coupon);

        Assert.True(trialResult.IsAvailable);
        Assert.Equal(30m, trialResult.DiscountAmount);
        Assert.Equal(400m, trialResult.CurrentAmount);
        Assert.Equal(370m, trialResult.AmountAfterDiscount);
    }

    [Fact]
    public void MultipleCouponScenario_ShouldHandleComplexStacking()
    {
        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext();
        engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD-001",
            Price = 1000m,
            Quantity = 1,
            CategoryId = "CATE-A"
        });
        engine.AddItem(context, new OrderItem
        {
            ProductId = "PROD-002",
            Price = 500m,
            Quantity = 1,
            CategoryId = "CATE-B"
        });

        var shipping = new ShippingInfo
        {
            Weight = 2m,
            FirstWeight = 1m,
            FirstWeightPrice = 10m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        engine.SetShipping(context, shipping);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "全场8折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                MaxDiscountAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "A类满500减100",
                Type = CouponType.AmountOff,
                DiscountValue = 100m,
                MinOrderAmount = 500m,
                ApplicableCategoryIds = new[] { "CATE-A" },
                StackingGroup = "CATEA",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "B类满300减50",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 300m,
                ApplicableCategoryIds = new[] { "CATE-B" },
                StackingGroup = "CATEB",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C4",
                CouponCode = "免运费",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimal(context, coupons);

        Assert.True(result.AppliedCoupons.Count >= 1);
        Assert.True(result.FinalAmount < context.OriginalAmount + context.FreightAmount);
    }
}
