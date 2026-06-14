using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Examples;

public static class UsageExamples
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== 优惠券计算SDK使用示例 ===\n");

        BasicUsageExample();
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        EcommerceScenario();
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        RestaurantScenario();
    }

    private static void BasicUsageExample()
    {
        Console.WriteLine("【基础使用示例】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ORDER-001");
        context.AddItem(new OrderItem
        {
            ProductId = "PROD-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2,
            CategoryId = "CATE-001"
        });

        Console.WriteLine($"商品总额: ¥{context.OriginalAmount:F2}");

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

        var result = engine.CalculateOptimal(context, coupons);

        Console.WriteLine($"\n应用优惠券:");
        foreach (var coupon in result.AppliedCoupons)
        {
            Console.WriteLine($"  - {coupon.CouponCode}: -¥{coupon.DiscountAmount:F2}");
        }

        Console.WriteLine($"\n优惠总额: -¥{result.TotalDiscountAmount:F2}");
        Console.WriteLine($"应付金额: ¥{result.FinalAmount:F2}");
    }

    private static void EcommerceScenario()
    {
        Console.WriteLine("【电商场景示例】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ECOM-20230614-001");

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
            ProductName = "机械键盘",
            Price = 399m,
            Quantity = 1,
            CategoryId = "ACCESSORIES"
        });

        context.Member = new MemberInfo
        {
            MemberId = "USER-12345",
            Level = MemberLevel.Gold
        };

        context.Shipping = new ShippingInfo
        {
            ShippingMethod = "顺丰速运",
            Province = "广东",
            City = "深圳",
            Weight = 4.5m,
            FirstWeight = 1m,
            FirstWeightPrice = 15m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 5m
        };
        context.FreightAmount = context.Shipping.CalculateFreight();

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
                CouponCode = "数码9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 300m,
                ApplicableCategoryIds = new[] { "ELECTRONICS" },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                StackingGroup = "CATEGORY"
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimal(context, coupons);

        Console.WriteLine("商品明细:");
        foreach (var item in context.Items)
        {
            Console.WriteLine($"  - {item.ProductName}: ¥{item.Price:F2} x {item.Quantity}");
        }

        Console.WriteLine($"\n商品总额: ¥{result.OriginalAmount:F2}");
        Console.WriteLine($"运费: ¥{result.OriginalFreight:F2}");

        Console.WriteLine("\n已应用优惠券:");
        foreach (var coupon in result.AppliedCoupons)
        {
            Console.WriteLine($"  ✓ {coupon.CouponCode} ({coupon.Description}): -¥{coupon.DiscountAmount:F2}");
        }

        Console.WriteLine($"\n未使用优惠券原因:");
        foreach (var rejected in result.Explanation.RejectedReasons)
        {
            Console.WriteLine($"  ✗ {rejected.CouponCode}");
            foreach (var reason in rejected.FailedConditions)
            {
                Console.WriteLine($"    - {reason}");
            }
        }

        Console.WriteLine($"\n优惠明细:");
        if (result.ProductDiscountAmount > 0)
            Console.WriteLine($"  商品优惠: -¥{result.ProductDiscountAmount:F2}");
        if (result.FreightDiscount > 0)
            Console.WriteLine($"  运费优惠: -¥{result.FreightDiscount:F2}");
        if (result.MemberDiscount > 0)
            Console.WriteLine($"  会员折扣: -¥{result.MemberDiscount:F2}");

        Console.WriteLine($"\n应付总额: ¥{result.FinalAmount:F2}");

        Console.WriteLine("\n--- 详细格式展示 ---");
        var detailedText = engine.FormatDetails(result, DisplayFormat.Detailed);
        Console.WriteLine(detailedText);
    }

    private static void RestaurantScenario()
    {
        Console.WriteLine("【点餐场景示例】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("REST-20230614-001");

        context.AddItem(new OrderItem
        {
            ProductId = "MEAL-001",
            ProductName = "双人套餐",
            Price = 128m,
            Quantity = 1,
            CategoryId = "MEALS"
        });

        context.AddItem(new OrderItem
        {
            ProductId = "SNACK-001",
            ProductName = "小食拼盘",
            Price = 38m,
            Quantity = 1,
            CategoryId = "SNACKS"
        });

        context.AddItem(new OrderItem
        {
            ProductId = "DRINK-001",
            ProductName = "招牌奶茶 x2",
            Price = 18m,
            Quantity = 2,
            CategoryId = "DRINKS"
        });

        context.Member = new MemberInfo
        {
            MemberId = "VIP-888",
            Level = MemberLevel.Platinum
        };

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
                CouponCode = "饮品8折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                ApplicableCategoryIds = new[] { "DRINKS" },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "R3",
                CouponCode = "新客专享满50减10",
                Type = CouponType.AmountOff,
                DiscountValue = 10m,
                MinOrderAmount = 50m,
                RequiredMemberLevel = MemberLevel.Normal,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimal(context, coupons);

        Console.WriteLine("菜品明细:");
        foreach (var item in context.Items)
        {
            Console.WriteLine($"  - {item.ProductName}: ¥{item.Price:F2} x {item.Quantity} = ¥{item.TotalAmount:F2}");
        }

        Console.WriteLine($"\n消费总额: ¥{result.OriginalAmount:F2}");

        Console.WriteLine("\n已应用优惠:");
        foreach (var coupon in result.AppliedCoupons)
        {
            Console.WriteLine($"  ✓ {coupon.CouponCode}: -¥{coupon.DiscountAmount:F2}");
        }

        Console.WriteLine($"\n应付金额: ¥{result.FinalAmount:F2}");

        Console.WriteLine("\n--- 账单格式展示 ---");
        var billText = engine.FormatDetails(result, DisplayFormat.Bill);
        Console.WriteLine(billText);
    }
}
