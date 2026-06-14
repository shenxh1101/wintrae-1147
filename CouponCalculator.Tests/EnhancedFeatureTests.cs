using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class EnhancedFeatureTests
{
    private readonly CouponCalculatorEngine _engine;

    public EnhancedFeatureTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimalEnhanced_ShouldReturnMultiplePlans()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
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
                CouponCode = "满500减50",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 500m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 60m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "免运费",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        context.FreightAmount = 20m;

        var result = _engine.CalculateOptimalEnhanced(context, coupons);

        Assert.NotNull(result);
        Assert.True(result.CandidatePlans.Count >= 1);
        Assert.NotNull(result.RecommendedPlan);

        if (result.CandidatePlans.Count > 1)
        {
            var plan1 = result.CandidatePlans[0];
            var plan2 = result.CandidatePlans[1];
            Assert.NotEqual(plan1.PlanId, plan2.PlanId);
        }
    }

    [Fact]
    public void TryApplyEnhanced_ShouldReturnDetailedTrialResult()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 150m,
            Quantity = 1
        });

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "满100减20",
            Type = CouponType.AmountOff,
            DiscountValue = 20m,
            MinOrderAmount = 100m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var result = _engine.TryApplyEnhanced(context, coupon);

        Assert.NotNull(result);
        Assert.True(result.IsAvailable);
        Assert.Equal(20m, result.DiscountAmount);
        Assert.NotEmpty(result.CheckResults);
        Assert.NotNull(result.UserExplanation);
        Assert.NotEmpty(result.UserExplanation.Summary);
    }

    [Fact]
    public void TryApplyEnhanced_WithUnavailableCoupon_ShouldReturnDetailedReasons()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 50m,
            Quantity = 1
        });

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "满100减20",
            Type = CouponType.AmountOff,
            DiscountValue = 20m,
            MinOrderAmount = 100m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var result = _engine.TryApplyEnhanced(context, coupon);

        Assert.NotNull(result);
        Assert.False(result.IsAvailable);
        Assert.NotEmpty(result.UnavailableReasons);
        Assert.Contains(result.UnavailableReasons, r => r.Contains("还差") || r.Contains("未达到"));
    }

    [Fact]
    public void GetAvailableCouponsEnhanced_ShouldReturnAllTrialResults()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
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
                CouponCode = "有效券",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "过期券",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(-1)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "不满足门槛",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 500m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var results = _engine.GetAvailableCouponsEnhanced(context, coupons).ToList();

        Assert.Equal(3, results.Count);
        Assert.True(results.First(r => r.Coupon.CouponId == "C1").IsAvailable);
        Assert.False(results.First(r => r.Coupon.CouponId == "C2").IsAvailable);
        Assert.False(results.First(r => r.Coupon.CouponId == "C3").IsAvailable);
    }

    [Fact]
    public void FormatEnhancedResult_ShouldGenerateReadableOutput()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 300m,
            Quantity = 1
        });
        context.FreightAmount = 15m;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满300减30",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 300m,
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

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var formatted = _engine.FormatEnhancedResult(result, context, DisplayFormat.Simple);

        Assert.NotEmpty(formatted);
        Assert.Contains("优惠方案总结", formatted);
    }

    [Fact]
    public void GetSettlementPageData_ShouldReturnAllRequiredFields()
    {
        var context = _engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });
        context.FreightAmount = 20m;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满200减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var data = _engine.GetSettlementPageData(result, context);

        Assert.NotNull(data);
        Assert.Contains("originalAmount", data.Keys);
        Assert.Contains("finalAmount", data.Keys);
        Assert.Contains("totalDiscount", data.Keys);
        Assert.Contains("appliedCoupons", data.Keys);
        Assert.Contains("userFriendlySummary", data.Keys);
    }

    [Fact]
    public void RollbackLastCalculation_ShouldClearState()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
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
                CouponCode = "满200减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        _engine.CalculateOptimalEnhanced(context, coupons);
        
        _engine.RollbackLastCalculation();

        Assert.Empty(context.AppliedCoupons);
    }

    [Fact]
    public void RecalculateWithSameParameters_ShouldReturnConsistentResult()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
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
                CouponCode = "满300减30",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 300m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result1 = _engine.CalculateOptimalEnhanced(context, coupons);
        var result2 = _engine.RecalculateWithSameParameters();

        Assert.Equal(result1.CandidatePlans.Count, result2.CandidatePlans.Count);
        Assert.Equal(result1.RecommendedPlan?.TotalDiscountAmount, 
                     result2.RecommendedPlan?.TotalDiscountAmount);
    }

    [Fact]
    public void CalculateOptimalEnhanced_WithMember_ShouldIncludeMemberDiscount()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });
        context.Member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };

        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());

        Assert.NotNull(result.RecommendedPlan);
        Assert.True(result.RecommendedPlan.MemberDiscountAmount > 0);
    }

    [Fact]
    public void CalculateOptimalEnhanced_WithComplexScenarios_ShouldGenerateMultiplePlans()
    {
        var context = _engine.CreateOrderContext("ORDER-COMPLEX");
        context.AddItem(new OrderItem
        {
            ProductId = "LAPTOP-001",
            ProductName = "游戏笔记本",
            Price = 6999m,
            Quantity = 1,
            CategoryId = "ELECTRONICS"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "MOUSE-001",
            ProductName = "无线鼠标",
            Price = 199m,
            Quantity = 2,
            CategoryId = "ACCESSORIES"
        });
        context.Member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };
        context.FreightAmount = 30m;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满6999减500",
                Type = CouponType.AmountOff,
                DiscountValue = 500m,
                MinOrderAmount = 6999m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "数码9折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 500m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "ELECTRONICS" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "配件85折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.85m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "ACCESSORIES" } 
                },
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

        var result = _engine.CalculateOptimalEnhanced(context, coupons);

        Assert.NotNull(result);
        Assert.True(result.CandidatePlans.Count >= 1);
        Assert.NotNull(result.RecommendedPlan);
        Assert.True(result.RecommendedPlan.TotalDiscountAmount > 0);
        Assert.True(result.RecommendedPlan.FinalTotalAmount < context.OriginalAmount + context.FreightAmount);

        var plan = result.RecommendedPlan;
        Assert.Contains(plan.Advantages, a => a.Contains("节省") || a.Contains("张券"));
    }

    [Fact]
    public void Coupon_WithScopeConfiguration_ShouldApplyCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            ProductName = "电脑",
            Price = 5000m,
            CategoryId = "ELECTRONICS"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD002",
            ProductName = "书籍",
            Price = 100m,
            CategoryId = "BOOKS"
        });

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "数码专属券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.9m,
            ApplicableScope = new ApplicableScope
            {
                CategoryIds = new List<string> { "ELECTRONICS" }
            },
            ExcludedScope = new ExcludedScope
            {
                ProductIds = new List<string> { "PROD003" }
            },
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var result = _engine.TryApplyEnhanced(context, coupon);

        Assert.True(result.IsAvailable);
        Assert.Equal(500m, result.DiscountAmount);
        Assert.Contains("PROD001", result.ApplicableProductIds);
        Assert.DoesNotContain("PROD002", result.ApplicableProductIds);
    }

    [Fact]
    public void Coupon_WithMemberLevelRestriction_ShouldValidate()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 500m,
            Quantity = 1
        });
        context.Member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Normal
        };

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "金卡专享券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.9m,
            UsageCondition = new UsageCondition
            {
                AllowedMemberLevels = new List<MemberLevel> { MemberLevel.Gold, MemberLevel.Platinum, MemberLevel.Diamond }
            },
            ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var result = _engine.TryApplyEnhanced(context, coupon);

        Assert.False(result.IsAvailable);
        Assert.Contains(result.UnavailableReasons, r => r.Contains("金卡") || r.Contains("等级"));
    }
}
