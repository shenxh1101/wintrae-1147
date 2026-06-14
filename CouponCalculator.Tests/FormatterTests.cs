using CouponCalculator.Engine;
using CouponCalculator.Formatters;
using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class FormatterTests
{
    private readonly CouponCalculatorEngine _engine;

    public FormatterTests()
    {
        _engine = new CouponCalculatorEngine();
    }

    [Fact]
    public void FormatDetails_SimpleFormat_ShouldReturnSimpleText()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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
                CouponCode = "满100减10",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);
        var formatted = _engine.FormatDetails(result, DisplayFormat.Simple);

        Assert.Contains("商品总额", formatted);
        Assert.Contains("应付总额", formatted);
    }

    [Fact]
    public void FormatDetails_DetailedFormat_ShouldReturnDetailedText()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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
                CouponCode = "满100减10",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);
        var formatted = _engine.FormatDetails(result, DisplayFormat.Detailed);

        Assert.Contains("订单优惠明细", formatted);
        Assert.Contains("优惠券信息", formatted);
    }

    [Fact]
    public void FormatDetails_BillFormat_ShouldReturnBillText()
    {
        var context = _engine.CreateOrderContext();
        _engine.AddItem(context, new OrderItem
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
                CouponCode = "满100减10",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = _engine.CalculateOptimal(context, coupons);
        var formatted = _engine.FormatDetails(result, DisplayFormat.Bill);

        Assert.Contains("订单账单", formatted);
        Assert.Contains("已使用优惠券", formatted);
    }

    [Fact]
    public void SimpleFormatter_FormatLines_ShouldReturnCorrectLines()
    {
        var result = new CalculationResult
        {
            OriginalAmount = 100m,
            ProductDiscountAmount = 10m,
            FreightDiscount = 0m,
            MemberDiscount = 0m,
            FinalAmount = 90m
        };

        var formatter = new SimpleFormatter();
        var lines = formatter.FormatLines(result);

        Assert.Contains("商品总额: ¥100.00", lines);
        Assert.Contains("商品优惠: -¥10.00", lines);
        Assert.Contains("应付总额: ¥90.00", lines);
    }
}
