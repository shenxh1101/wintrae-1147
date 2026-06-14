using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class ItemLevelBreakdownTests
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ItemLevelAllocationService _allocationService;

    public ItemLevelBreakdownTests()
    {
        _engine = new CouponCalculatorEngine();
        _allocationService = new ItemLevelAllocationService();
    }

    [Fact]
    public void CalculateItemBreakdown_ShouldGenerateDetailedBreakdown()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2,
            CategoryId = "CATE-001"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "商品B",
            Price = 50m,
            Quantity = 1,
            CategoryId = "CATE-002"
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满200减30",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        Assert.NotNull(breakdown);
        Assert.Equal(context.OrderId, breakdown.OrderId);
        Assert.Equal(2, breakdown.ItemDetails.Count);
        Assert.True(breakdown.TotalDiscountAmount > 0);
    }

    [Fact]
    public void CalculateItemBreakdown_WithCategoryScope_ShouldAllocateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "适用商品",
            Price = 100m,
            Quantity = 1,
            CategoryId = "CATE-001"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "不适用商品",
            Price = 100m,
            Quantity = 1,
            CategoryId = "CATE-002"
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "类目9折",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ApplicableScope = new ApplicableScope
                {
                    CategoryIds = new List<string> { "CATE-001" }
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        Assert.Equal(2, breakdown.ItemDetails.Count);

        var applicableItem = breakdown.ItemDetails.First(i => i.ProductId == "SKU-001");
        var nonApplicableItem = breakdown.ItemDetails.First(i => i.ProductId == "SKU-002");

        Assert.True(applicableItem.TotalCouponDiscount > 0);
        Assert.Equal(0, nonApplicableItem.TotalCouponDiscount);
    }

    [Fact]
    public void CalculateItemBreakdown_WithMemberDiscount_ShouldAllocateMemberDiscount()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 1
        });
        context.Member = new MemberInfo
        {
            MemberId = "MB001",
            Level = MemberLevel.Gold
        };

        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        Assert.NotNull(breakdown.MemberDetail);
        Assert.Equal(MemberLevel.Gold, breakdown.MemberDetail.MemberLevel);
        Assert.True(breakdown.MemberDetail.DiscountAmount > 0);

        var itemDetail = breakdown.ItemDetails.First();
        Assert.True(itemDetail.MemberDiscount > 0);
    }

    [Fact]
    public void CalculateItemBreakdown_WithFreeShipping_ShouldHandleFreight()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 1
        });
        context.FreightAmount = 15m;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        Assert.NotNull(breakdown.FreightDetail);
        Assert.Equal(15m, breakdown.FreightDetail.OriginalFreight);
        Assert.Equal(15m, breakdown.FreightDetail.DiscountAmount);
        Assert.Equal(0, breakdown.FreightDetail.FinalFreight);
    }

    [Fact]
    public void CalculateRefund_ShouldCalculateRefundAmount()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "商品B",
            Price = 50m,
            Quantity = 1
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                Type = CouponType.AmountOff,
                DiscountValue = 30m,
                MinOrderAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        var refundRequests = new List<RefundItemRequest>
        {
            new RefundItemRequest
            {
                ProductId = "SKU-001",
                RefundQuantity = 1
            }
        };

        var refundCalculation = _allocationService.CalculateRefund(
            context,
            breakdown,
            refundRequests,
            RefundMethod.Proportional);

        Assert.NotNull(refundCalculation);
        Assert.Equal(context.OrderId, refundCalculation.OriginalOrderId);
        Assert.Single(refundCalculation.RefundItems);
        Assert.True(refundCalculation.RefundAmount > 0);
    }

    [Fact]
    public void CalculateRefund_WithDifferentMethods_ShouldCalculateCorrectly()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            Price = 100m,
            Quantity = 2
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        var refundRequests = new List<RefundItemRequest>
        {
            new RefundItemRequest { ProductId = "SKU-001", RefundQuantity = 1 }
        };

        var proportionalRefund = _allocationService.CalculateRefund(
            context, breakdown, refundRequests, RefundMethod.Proportional);

        var originalRatioRefund = _allocationService.CalculateRefund(
            context, breakdown, refundRequests, RefundMethod.OriginalRatio);

        Assert.True(proportionalRefund.RefundAmount > 0);
        Assert.True(originalRatioRefund.RefundAmount > 0);
    }

    [Fact]
    public void GenerateBreakdownReport_ShouldReturnReadableText()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
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

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        var report = breakdown.GenerateBreakdownReport();

        Assert.Contains("商品级优惠明细分摊报告", report);
        Assert.Contains("商品A", report);
        Assert.Contains("优惠券分摊汇总", report);
    }

    [Fact]
    public void GenerateRefundReport_ShouldReturnReadableText()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2
        });

        var result = _engine.CalculateOptimalEnhanced(context, new List<Coupon>());
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        var refundRequests = new List<RefundItemRequest>
        {
            new RefundItemRequest
            {
                ProductId = "SKU-001",
                RefundQuantity = 1,
                RefundReason = "质量问题"
            }
        };

        var refundCalculation = _allocationService.CalculateRefund(
            context, breakdown, refundRequests, RefundMethod.Proportional);

        var report = refundCalculation.GenerateRefundReport();

        Assert.Contains("退款优惠重算报告", report);
        Assert.Contains("商品A", report);
        Assert.Contains("退款汇总", report);
    }

    [Fact]
    public void CouponAllocation_ShouldTrackAllAllocations()
    {
        var context = _engine.CreateOrderContext();
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 1
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "商品B",
            Price = 100m,
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
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = _allocationService.CalculateItemBreakdown(context, result);

        Assert.NotEmpty(breakdown.CouponAllocations);

        var allocation = breakdown.CouponAllocations.First();
        Assert.Equal("C1", allocation.CouponId);
        Assert.Equal(20m, allocation.TotalDiscountAmount);
        Assert.Equal(2, allocation.ItemAllocations.Count);
    }
}