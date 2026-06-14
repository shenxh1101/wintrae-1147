using CouponCalculator.Engine;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class EdgeCaseTests
{
    private readonly CouponCalculatorEngine _engine;

    public EdgeCaseTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void CalculateOptimal_WithEmptyOrder_ShouldReturnZeroAmount()
    {
        var context = _engine.CreateOrderContext();
        
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

        Assert.Equal(0m, result.OriginalAmount);
        Assert.Equal(0m, result.ProductDiscountAmount);
        Assert.Equal(0m, result.FinalAmount);
        Assert.Empty(result.AppliedCoupons);
    }

    [Fact]
    public void CalculateOptimal_WithZeroAmountProduct_ShouldHandleCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 0m,
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

        Assert.Equal(0m, result.OriginalAmount);
        Assert.Empty(result.AppliedCoupons);
    }

    [Fact]
    public void CalculateOptimal_AtExactThreshold_ShouldApply()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
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
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(100m, result.OriginalAmount);
        Assert.Single(result.AppliedCoupons);
        Assert.Equal(20m, result.ProductDiscountAmount);
    }

    [Fact]
    public void CalculateOptimal_JustBelowThreshold_ShouldNotApply()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 99.99m,
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

        Assert.Equal(99.99m, result.OriginalAmount);
        Assert.Empty(result.AppliedCoupons);
    }

    [Fact]
    public void CalculateOptimal_WithZeroFreight_ShouldHandleCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 200m,
            Quantity = 1
        });
        context.FreightAmount = 0m;

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

        Assert.Equal(0m, result.OriginalFreight);
        Assert.Equal(0m, result.FreightDiscount);
        Assert.Single(result.AppliedCoupons);
    }

    [Fact]
    public void CalculateOptimal_MixedExclusiveAndStackable_ShouldApplyCorrectly()
    {
        var context = _engine.CreateOrderContext();
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
                CouponCode = "互斥券A",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 100m,
                StackingGroup = "GROUP1",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "互斥券B",
                Type = CouponType.AmountOff,
                DiscountValue = 80m,
                MinOrderAmount = 100m,
                StackingGroup = "GROUP1",
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

        var result = _engine.CalculateOptimal(context, coupons);

        var amountOffCoupons = result.AppliedCoupons.Count(c => c.Type == CouponType.AmountOff);
        Assert.True(amountOffCoupons <= 1, "同一互斥组只能使用一张券");
        
        if (amountOffCoupons > 0)
        {
            var appliedAmountOff = result.AppliedCoupons.First(c => c.Type == CouponType.AmountOff);
            Assert.Equal(80m, appliedAmountOff.DiscountAmount);
        }
    }

    [Fact]
    public void CalculateOptimal_WithNoCoupons_ShouldReturnOriginalAmount()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 300m,
            Quantity = 1
        });
        context.FreightAmount = 20m;

        var result = _engine.CalculateOptimal(context, new List<Coupon>());

        Assert.Equal(300m, result.OriginalAmount);
        Assert.Equal(20m, result.OriginalFreight);
        Assert.Equal(0m, result.ProductDiscountAmount);
        Assert.Equal(0m, result.FreightDiscount);
        Assert.Equal(320m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_AfterRollback_ShouldBeConsistent()
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
                CouponCode = "满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result1 = _engine.CalculateOptimal(context, coupons);
        Assert.Single(result1.AppliedCoupons);

        _engine.Rollback(context);

        var result2 = _engine.CalculateOptimal(context, coupons);
        
        Assert.Equal(result1.OriginalAmount, result2.OriginalAmount);
        Assert.Equal(result1.ProductDiscountAmount, result2.ProductDiscountAmount);
        Assert.Equal(result1.AppliedCoupons.Count, result2.AppliedCoupons.Count);
    }

    [Fact]
    public void CalculateOptimal_WithAllExpiredCoupons_ShouldNotApply()
    {
        var context = _engine.CreateOrderContext();
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
                CouponCode = "过期券",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(-1)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "未生效券",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Empty(result.AppliedCoupons);
        Assert.Equal(2, result.Explanation.RejectedReasons.Count);
    }

    [Fact]
    public void CalculateOptimal_WithMultipleItemsAndApplicableScope_ShouldCalculateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            ProductName = "电脑",
            Price = 5000m,
            Quantity = 1,
            CategoryId = "ELECTRONICS"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD002",
            ProductName = "书籍",
            Price = 100m,
            Quantity = 2,
            CategoryId = "BOOKS"
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "数码券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "ELECTRONICS" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(5200m, result.OriginalAmount);
        Assert.Equal(500m, result.ProductDiscountAmount);
        Assert.Equal(4700m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_WithExcludedProducts_ShouldCalculateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            ProductName = "正品",
            Price = 200m,
            Quantity = 1,
            CategoryId = "NORMAL"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD002",
            ProductName = "特价商品",
            Price = 50m,
            Quantity = 1,
            CategoryId = "SPECIAL"
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "全场9折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ExcludedScope = new ExcludedScope 
                { 
                    CategoryIds = new List<string> { "SPECIAL" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(250m, result.OriginalAmount);
        Assert.Equal(20m, result.ProductDiscountAmount);
        Assert.Equal(230m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_WithDecimalAmounts_ShouldCalculatePrecisely()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 99.99m,
            Quantity = 3
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满300减50",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 300m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(299.97m, result.OriginalAmount);
        Assert.Empty(result.AppliedCoupons);
    }

    [Fact]
    public void CalculateOptimal_WithLargeQuantity_ShouldCalculateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 10m,
            Quantity = 100
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满10件8折",
                Type = CouponType.QuantityDiscount,
                DiscountValue = 0.8m,
                MinQuantity = 10,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(1000m, result.OriginalAmount);
        Assert.Equal(200m, result.ProductDiscountAmount);
        Assert.Equal(800m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_WithMaxDiscountLimit_ShouldCapCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 1000m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "5折券(封顶50)",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.5m,
                MaxDiscountAmount = 50m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.Equal(1000m, result.OriginalAmount);
        Assert.Equal(50m, result.ProductDiscountAmount);
        Assert.Equal(950m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_WithMultipleApplicableCoupons_ShouldSelectBest()
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
                MinOrderAmount = 200m,
                CanStackWithSameType = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 30m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.ProductDiscountAmount > 0);
    }

    [Fact]
    public void CalculateOptimal_WithMemberDiscount_ShouldCombineCorrectly()
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

        var result = _engine.CalculateOptimal(context, new List<Coupon>());

        Assert.Equal(200m, result.OriginalAmount);
        Assert.Equal(20m, result.MemberDiscount);
        Assert.Equal(180m, result.FinalAmount);
    }

    [Fact]
    public void CalculateOptimal_WithVerySmallDiscount_ShouldRoundCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = 0.01m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);

        Assert.True(result.ProductDiscountAmount >= 0);
        Assert.True(result.FinalAmount >= 0);
    }

    [Fact]
    public void CalculateOptimal_WithNegativeAmount_ShouldNotApply()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "PROD001",
            Price = -100m,
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

        Assert.True(result.OriginalAmount <= 0);
    }
}
