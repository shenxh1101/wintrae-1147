using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class ExplanationAndLogTests
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ExplanationService _explanationService;

    public ExplanationAndLogTests()
    {
        _engine = new CouponCalculatorEngine();
        _explanationService = new ExplanationService();
    }

    [Fact]
    public void Explain_WithValidCoupon_ShouldReturnAppliedExplanation()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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

        var explanation = _engine.Explain(coupon, context);

        Assert.NotEmpty(explanation.AppliedReasons);
        Assert.Empty(explanation.RejectedReasons);
    }

    [Fact]
    public void Explain_WithInvalidCoupon_ShouldReturnRejectedExplanation()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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

        var explanation = _engine.Explain(coupon, context);

        Assert.Empty(explanation.AppliedReasons);
        Assert.NotEmpty(explanation.RejectedReasons);
    }

    [Fact]
    public void GetCalculationLog_ShouldReturnLog()
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

        _engine.CalculateOptimal(context, coupons);
        var log = _engine.GetCalculationLog();

        Assert.NotNull(log);
        Assert.Equal("ORDER001", log.OrderId);
    }

    [Fact]
    public void Rollback_ShouldClearAppliedCoupons()
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

        _engine.CalculateOptimal(context, coupons);
        Assert.NotEmpty(context.AppliedCoupons);

        _engine.Rollback(context);
        Assert.Empty(context.AppliedCoupons);
    }

    [Fact]
    public void CouponValidator_GetUnavailableReasons_ShouldReturnCorrectReasons()
    {
        var validator = new CouponValidator();
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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

        var reasons = validator.GetUnavailableReasons(coupon, context);

        Assert.NotEmpty(reasons);
        Assert.Contains(reasons, r => r.Contains("未达到最低要求"));
    }

    [Fact]
    public void ExplanationService_ExplainAppliedCoupon_ShouldReturnCorrectExplanation()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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

        var explanation = _explanationService.ExplainAppliedCoupon(coupon, context, 20m);

        Assert.Single(explanation.AppliedReasons);
        Assert.Contains("满100减20", explanation.AppliedReasons[0].Reason);
        Assert.Equal(20m, explanation.AppliedReasons[0].DiscountAmount);
    }

    [Fact]
    public void ExplanationService_ExplainRejectedCoupon_ShouldReturnCorrectExplanation()
    {
        var context = _engine.CreateOrderContext();

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "测试券",
            Type = CouponType.AmountOff,
            DiscountValue = 20m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        var reasons = new List<string> { "订单中无可适用的商品" };
        var explanation = _explanationService.ExplainRejectedCoupon(coupon, context, reasons);

        Assert.Single(explanation.RejectedReasons);
        Assert.Contains("无法使用该优惠券", explanation.RejectedReasons[0].Reason);
    }
}
