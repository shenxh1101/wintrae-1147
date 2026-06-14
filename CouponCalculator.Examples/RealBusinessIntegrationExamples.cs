using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;

namespace CouponCalculator.Examples;

public static class RealBusinessIntegrationExamples
{
    public static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         优惠券计算SDK - 真实业务集成示例                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        CompleteBusinessWorkflowExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        RuleVersionManagementExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        ItemLevelBreakdownExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        ReconciliationAndAuditExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        RefundCalculationExample();
    }

    private static void CompleteBusinessWorkflowExample()
    {
        Console.WriteLine("【完整业务工作流示例】\n");

        var productProvider = new MockProductDataProvider();
        var memberProvider = new MockMemberDataProvider();
        var couponProvider = new MockCouponDataProvider();

        var engine = new CouponCalculatorEngine();
        var allocationService = new ItemLevelAllocationService();
        var reconciliationService = new ReconciliationService();

        var userId = "USER-12345";
        var cartItems = new List<string> { "SKU-001", "SKU-002", "SKU-003" };

        var member = memberProvider.GetMember(userId);
        Console.WriteLine($"会员信息: {member.MemberId} - {member.Level} (积分: {member.Points})");

        var context = engine.CreateOrderContext($"ORDER-{DateTime.UtcNow:yyyyMMddHHmmss}");
        foreach (var skuId in cartItems)
        {
            var product = productProvider.GetProduct(skuId);
            engine.AddItem(context, new OrderItem
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Quantity = product.Stock > 10 ? 2 : 1,
                CategoryId = product.CategoryId,
                Weight = product.Weight
            });
        }

        var shipping = new ShippingInfo
        {
            ShippingMethod = "Express",
            Province = member.Province,
            City = member.City,
            Weight = context.Items.Sum(i => i.Weight * i.Quantity),
            FirstWeight = 1m,
            FirstWeightPrice = 12m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 6m
        };
        engine.SetShipping(context, shipping);

        Console.WriteLine($"订单创建: {context.OrderId}");
        Console.WriteLine($"商品总额: ¥{context.OriginalAmount:F2}");
        Console.WriteLine($"运费: ¥{context.FreightAmount:F2}");

        var availableCoupons = couponProvider.GetUserAvailableCoupons(userId, context.OriginalAmount);
        Console.WriteLine($"可用优惠券: {availableCoupons.Count}张");

        var trialCheckpoint = CreateCheckpoint(
            engine, context, availableCoupons, allocationService, reconciliationService,
            CalculationStage.Trial, "用户试算", "User");

        Console.WriteLine("\n【试算阶段】");
        DisplayCalculationResult(trialCheckpoint);

        context.ExtendedData["Confirmed"] = "true";
        var orderCheckpoint = CreateCheckpoint(
            engine, context, availableCoupons, allocationService, reconciliationService,
            CalculationStage.OrderCreated, "下单确认", "System");

        Console.WriteLine("\n【下单阶段】");
        DisplayCalculationResult(orderCheckpoint);

        var paymentCheckpoint = CreateCheckpoint(
            engine, context, availableCoupons, allocationService, reconciliationService,
            CalculationStage.PaymentPending, "支付前复算", "PaymentSystem");

        Console.WriteLine("\n【支付前复算】");
        DisplayCalculationResult(paymentCheckpoint);

        var comparison = reconciliationService.CompareCheckpoints(
            trialCheckpoint.CheckpointId,
            paymentCheckpoint.CheckpointId);

        Console.WriteLine("\n【试算 vs 支付对比】");
        Console.WriteLine(comparison.GenerateComparisonReport());

        var report = reconciliationService.GenerateReconciliationReport(context.OrderId);
        Console.WriteLine("\n【完整对账报告】");
        Console.WriteLine(report.GenerateFullReport());
    }

    private static void RuleVersionManagementExample()
    {
        Console.WriteLine("【规则版本管理示例】\n");

        var versionManager = new RuleVersionManager();

        var currentCoupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "OLD-RULE",
                CouponName = "旧规则满100减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-30),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var currentRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                RuleId = "R1",
                RuleName = "旧满减规则",
                Type = DiscountRuleType.AmountThreshold,
                Threshold = 100m,
                DiscountValue = 20m
            }
        };

        var currentVersion = versionManager.CreateVersion(
            "V1.0-当前版本",
            "当前生产环境使用的规则版本",
            currentCoupons,
            currentRules,
            "Admin");

        Console.WriteLine($"创建当前版本: {currentVersion.VersionName} ({currentVersion.FullVersion})");
        Console.WriteLine($"状态: {currentVersion.GetStatusDescription()}");

        var newCoupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "NEW-RULE",
                CouponName = "新规则满100减25",
                Type = CouponType.AmountOff,
                DiscountValue = 25m,
                MinOrderAmount = 100m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(60)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "NEW-DISCOUNT",
                CouponName = "新增9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                MaxDiscountAmount = 50m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(60)
            }
        };

        var newRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                RuleId = "R1",
                RuleName = "新满减规则",
                Type = DiscountRuleType.AmountThreshold,
                Threshold = 100m,
                DiscountValue = 25m
            },
            new DiscountRule
            {
                RuleId = "R2",
                RuleName = "新增折扣规则",
                Type = DiscountRuleType.DiscountRate,
                DiscountValue = 0.9m
            }
        };

        var newVersion = versionManager.CreateVersion(
            "V2.0-新版本",
            "优化后的规则版本，增加折扣券",
            newCoupons,
            newRules,
            "Admin");

        Console.WriteLine($"创建新版本: {newVersion.VersionName} ({newVersion.FullVersion})");

        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var plan = versionManager.CreateSwitchPlan(
            currentVersion.VersionId,
            newVersion.VersionId,
            scheduledTime,
            "Admin");

        Console.WriteLine($"创建切换计划: {plan.PlanName}");
        Console.WriteLine($"计划执行时间: {scheduledTime:yyyy-MM-dd HH:mm}");

        Console.WriteLine("\n【版本差异预览】");
        Console.WriteLine(plan.Preview.GenerateDiffReport());

        versionManager.ApprovePlan(plan.PlanId, "Manager-A");
        versionManager.ApprovePlan(plan.PlanId, "Manager-B");

        Console.WriteLine($"审批状态: {plan.GetStatusDescription()}");

        var impactCases = versionManager.GenerateSampleImpactCases(
            currentVersion,
            newVersion,
            GenerateSampleOrders());

        if (impactCases.Any())
        {
            Console.WriteLine("\n【影响案例预览】");
            foreach (var case in impactCases.Take(3))
            {
                Console.WriteLine($"订单 {case.OrderId}:");
                Console.WriteLine($"  原优惠: ¥{case.OldDiscount:F2} → 新优惠: ¥{case.NewDiscount:F2}");
                Console.WriteLine($"  变化: ¥{case.Difference:F2} ({case.ChangeReason})");
            }
        }
    }

    private static void ItemLevelBreakdownExample()
    {
        Console.WriteLine("【商品级明细分摊示例】\n");

        var engine = new CouponCalculatorEngine();
        var allocationService = new ItemLevelAllocationService();

        var context = engine.CreateOrderContext("ORDER-BREAKDOWN-001");
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "iPhone 15 Pro",
            Price = 7999m,
            Quantity = 1,
            CategoryId = "PHONE",
            Weight = 0.5m
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "AirPods Pro",
            Price = 1499m,
            Quantity = 1,
            CategoryId = "AUDIO",
            Weight = 0.1m
        });
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-003",
            ProductName = "手机壳",
            Price = 99m,
            Quantity = 2,
            CategoryId = "ACCESSORY",
            Weight = 0.05m
        });

        context.Member = new MemberInfo
        {
            MemberId = "VIP-001",
            Level = MemberLevel.Platinum
        };
        context.FreightAmount = 15m;

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "PHONE-DISC",
                CouponName = "手机95折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.95m,
                MaxDiscountAmount = 400m,
                ApplicableScope = new ApplicableScope
                {
                    CategoryIds = new List<string> { "PHONE" }
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "AUDIO-DISC",
                CouponName = "音频85折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.85m,
                ApplicableScope = new ApplicableScope
                {
                    CategoryIds = new List<string> { "AUDIO" }
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "FREESHIP",
                CouponName = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = allocationService.CalculateItemBreakdown(context, result);

        Console.WriteLine(breakdown.GenerateBreakdownReport());

        Console.WriteLine("\n【优惠券分摊明细】");
        foreach (var allocation in breakdown.CouponAllocations)
        {
            Console.WriteLine($"优惠券: {allocation.CouponName}");
            Console.WriteLine($"  总优惠: ¥{allocation.TotalDiscountAmount:F2}");
            Console.WriteLine($"  分摊策略: {allocation.AllocationStrategy}");
            foreach (var item in allocation.ItemAllocations)
            {
                Console.WriteLine($"    {item.ProductName}: ¥{item.Amount:F2} ({item.Ratio:P2})");
            }
        }
    }

    private static void ReconciliationAndAuditExample()
    {
        Console.WriteLine("【对账与排查示例】\n");

        var engine = new CouponCalculatorEngine();
        var allocationService = new ItemLevelAllocationService();
        var reconciliationService = new ReconciliationService();

        var orderId = "ORDER-AUDIT-001";
        var context = engine.CreateOrderContext(orderId);
        context.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "商品A",
            Price = 200m,
            Quantity = 2,
            CategoryId = "CATE-001"
        });

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满400减50",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 400m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var checkpoint1 = CreateCheckpoint(
            engine, context, coupons, allocationService, reconciliationService,
            CalculationStage.Trial, "用户A", "Customer");

        context.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "商品B",
            Price = 100m,
            Quantity = 1,
            CategoryId = "CATE-002"
        });

        var checkpoint2 = CreateCheckpoint(
            engine, context, coupons, allocationService, reconciliationService,
            CalculationStage.OrderCreated, "系统", "System");

        coupons.Add(new Coupon
        {
            CouponId = "C2",
            CouponCode = "新增9折券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.9m,
            MaxDiscountAmount = 30m,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        });

        var checkpoint3 = CreateCheckpoint(
            engine, context, coupons, allocationService, reconciliationService,
            CalculationStage.PaymentPending, "支付系统", "Payment");

        var report = reconciliationService.GenerateReconciliationReport(orderId);

        Console.WriteLine(report.GenerateFullReport());

        var inconsistencies = reconciliationService.FindInconsistentCalculations(
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));

        Console.WriteLine("\n【不一致计算查询】");
        Console.WriteLine($"发现 {inconsistencies.Count} 个不一致的计算节点");
    }

    private static void RefundCalculationExample()
    {
        Console.WriteLine("【退款重算示例】\n");

        var engine = new CouponCalculatorEngine();
        var allocationService = new ItemLevelAllocationService();

        var originalContext = engine.CreateOrderContext("ORDER-REFUND-001");
        originalContext.AddItem(new OrderItem
        {
            ProductId = "SKU-001",
            ProductName = "iPhone 15 Pro",
            Price = 7999m,
            Quantity = 1,
            CategoryId = "PHONE"
        });
        originalContext.AddItem(new OrderItem
        {
            ProductId = "SKU-002",
            ProductName = "AirPods Pro",
            Price = 1499m,
            Quantity = 2,
            CategoryId = "AUDIO"
        });
        originalContext.Member = new MemberInfo
        {
            MemberId = "VIP-001",
            Level = MemberLevel.Gold
        };

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "满8000减500",
                Type = CouponType.AmountOff,
                DiscountValue = 500m,
                MinOrderAmount = 8000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var originalResult = engine.CalculateOptimalEnhanced(originalContext, coupons);
        var originalBreakdown = allocationService.CalculateItemBreakdown(originalContext, originalResult);

        Console.WriteLine("【原订单信息】");
        Console.WriteLine($"订单总额: ¥{originalBreakdown.TotalOriginalAmount:F2}");
        Console.WriteLine($"总优惠: ¥{originalBreakdown.TotalDiscountAmount:F2}");
        Console.WriteLine($"应付: ¥{originalBreakdown.TotalFinalAmount:F2}");

        var refundRequests = new List<RefundItemRequest>
        {
            new RefundItemRequest
            {
                ProductId = "SKU-002",
                RefundQuantity = 1,
                RefundReason = "质量问题"
            }
        };

        var refundCalculation = allocationService.CalculateRefund(
            originalContext,
            originalBreakdown,
            refundRequests,
            RefundMethod.Proportional);

        Console.WriteLine(refundCalculation.GenerateRefundReport());

        Console.WriteLine("\n【优惠券处理建议】");
        foreach (var coupon in refundCalculation.RefundCoupons)
        {
            Console.WriteLine($"优惠券: {coupon.CouponName}");
            Console.WriteLine($"  原优惠: ¥{coupon.OriginalDiscount:F2}");
            Console.WriteLine($"  退款优惠: ¥{coupon.RefundDiscount:F2}");
            Console.WriteLine($"  处理方式: {coupon.HandleMethod}");
            Console.WriteLine($"  是否退回: {coupon.IsReturned}");
        }
    }

    private static CalculationCheckpoint CreateCheckpoint(
        CouponCalculatorEngine engine,
        OrderContext context,
        List<Coupon> coupons,
        ItemLevelAllocationService allocationService,
        ReconciliationService reconciliationService,
        CalculationStage stage,
        string operatorName,
        string operatorRole)
    {
        var result = engine.CalculateOptimal(context, coupons);
        var enhancedResult = engine.CalculateOptimalEnhanced(context, coupons);
        var breakdown = allocationService.CalculateItemBreakdown(context, enhancedResult);

        return reconciliationService.CreateCheckpoint(
            context,
            result,
            enhancedResult,
            breakdown,
            stage,
            operatorName,
            operatorRole,
            "V1.0");
    }

    private static void DisplayCalculationResult(CalculationCheckpoint checkpoint)
    {
        Console.WriteLine($"计算时间: {checkpoint.Timestamp:HH:mm:ss}");
        Console.WriteLine($"计算阶段: {checkpoint.GetStageDescription()}");
        Console.WriteLine($"操作人: {checkpoint.Operator} ({checkpoint.OperatorRole})");
        Console.WriteLine($"原始金额: ¥{checkpoint.Result.OriginalAmount:F2}");
        Console.WriteLine($"优惠金额: ¥{checkpoint.Result.TotalDiscountAmount:F2}");
        Console.WriteLine($"应付金额: ¥{checkpoint.Result.FinalAmount:F2}");
        Console.WriteLine($"应用券数: {checkpoint.AppliedCouponIds.Count}");
    }

    private static List<OrderContext> GenerateSampleOrders()
    {
        var orders = new List<OrderContext>();
        var engine = new CouponCalculatorEngine();

        for (int i = 0; i < 5; i++)
        {
            var context = engine.CreateOrderContext($"SAMPLE-{i}");
            context.AddItem(new OrderItem
            {
                ProductId = $"SKU-{i}",
                Price = 100m + i * 50m,
                Quantity = 1 + i,
                CategoryId = "CATE-001"
            });
            orders.Add(context);
        }

        return orders;
    }
}

public class MockProductDataProvider
{
    private readonly Dictionary<string, ProductData> _products = new();

    public MockProductDataProvider()
    {
        InitializeProducts();
    }

    private void InitializeProducts()
    {
        _products["SKU-001"] = new ProductData
        {
            ProductId = "SKU-001",
            ProductName = "iPhone 15 Pro",
            Price = 7999m,
            CategoryId = "PHONE",
            BrandId = "APPLE",
            Weight = 0.5m,
            Stock = 100
        };

        _products["SKU-002"] = new ProductData
        {
            ProductId = "SKU-002",
            ProductName = "AirPods Pro",
            Price = 1499m,
            CategoryId = "AUDIO",
            BrandId = "APPLE",
            Weight = 0.1m,
            Stock = 50
        };

        _products["SKU-003"] = new ProductData
        {
            ProductId = "SKU-003",
            ProductName = "手机壳",
            Price = 99m,
            CategoryId = "ACCESSORY",
            BrandId = "GENERIC",
            Weight = 0.05m,
            Stock = 200
        };
    }

    public ProductData GetProduct(string productId)
    {
        return _products.TryGetValue(productId, out var product)
            ? product
            : new ProductData { ProductId = productId, ProductName = "未知商品", Price = 100m };
    }

    public List<ProductData> GetProductsByCategory(string categoryId)
    {
        return _products.Values.Where(p => p.CategoryId == categoryId).ToList();
    }

    public List<ProductData> GetProductsByBrand(string brandId)
    {
        return _products.Values.Where(p => p.BrandId == brandId).ToList();
    }
}

public class ProductData
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string BrandId { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Stock { get; set; }
}

public class MockMemberDataProvider
{
    private readonly Dictionary<string, MemberData> _members = new();

    public MockMemberDataProvider()
    {
        InitializeMembers();
    }

    private void InitializeMembers()
    {
        _members["USER-12345"] = new MemberData
        {
            MemberId = "USER-12345",
            Level = MemberLevel.Gold,
            Points = 5000,
            Province = "上海",
            City = "上海市",
            RegisterDate = DateTime.UtcNow.AddDays(-365)
        };

        _members["VIP-001"] = new MemberData
        {
            MemberId = "VIP-001",
            Level = MemberLevel.Platinum,
            Points = 10000,
            Province = "北京",
            City = "北京市",
            RegisterDate = DateTime.UtcNow.AddDays(-500)
        };
    }

    public MemberData GetMember(string memberId)
    {
        return _members.TryGetValue(memberId, out var member)
            ? member
            : new MemberData { MemberId = memberId, Level = MemberLevel.Normal };
    }

    public List<MemberData> GetMembersByLevel(MemberLevel level)
    {
        return _members.Values.Where(m => m.Level == level).ToList();
    }
}

public class MemberData
{
    public string MemberId { get; set; } = string.Empty;
    public MemberLevel Level { get; set; }
    public int Points { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisterDate { get; set; }
}

public class MockCouponDataProvider
{
    private readonly List<CouponData> _allCoupons = new();

    public MockCouponDataProvider()
    {
        InitializeCoupons();
    }

    private void InitializeCoupons()
    {
        _allCoupons.Add(new CouponData
        {
            CouponId = "C1",
            CouponCode = "NEWUSER100",
            CouponName = "新人100元券",
            Type = CouponType.AmountOff,
            DiscountValue = 100m,
            MinOrderAmount = 200m,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(30),
            TargetUserGroups = new List<string> { "NEW" }
        });

        _allCoupons.Add(new CouponData
        {
            CouponId = "C2",
            CouponCode = "VIP-DISC",
            CouponName = "VIP9折券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.9m,
            MaxDiscountAmount = 500m,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(30),
            TargetUserGroups = new List<string> { "VIP" }
        });

        _allCoupons.Add(new CouponData
        {
            CouponId = "C3",
            CouponCode = "FREESHIP",
            CouponName = "免运费券",
            Type = CouponType.FreeShipping,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(30),
            TargetUserGroups = new List<string> { "ALL" }
        });

        _allCoupons.Add(new CouponData
        {
            CouponId = "C4",
            CouponCode = "PHONE-DISC",
            CouponName = "手机95折券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.95m,
            MaxDiscountAmount = 400m,
            ApplicableCategories = new List<string> { "PHONE" },
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(30),
            TargetUserGroups = new List<string> { "ALL" }
        });
    }

    public List<Coupon> GetUserAvailableCoupons(string userId, decimal orderAmount)
    {
        return _allCoupons
            .Where(c => c.IsValid())
            .Where(c => c.MinOrderAmount <= orderAmount)
            .Select(c => ConvertToCoupon(c))
            .ToList();
    }

    public List<Coupon> GetCouponsByCategory(string categoryId)
    {
        return _allCoupons
            .Where(c => c.ApplicableCategories.Contains(categoryId) || c.ApplicableCategories.Count == 0)
            .Select(c => ConvertToCoupon(c))
            .ToList();
    }

    private Coupon ConvertToCoupon(CouponData data)
    {
        return new Coupon
        {
            CouponId = data.CouponId,
            CouponCode = data.CouponCode,
            CouponName = data.CouponName,
            Type = data.Type,
            DiscountValue = data.DiscountValue,
            MaxDiscountAmount = data.MaxDiscountAmount,
            MinOrderAmount = data.MinOrderAmount,
            ValidFrom = data.ValidFrom,
            ValidTo = data.ValidTo,
            ApplicableScope = new ApplicableScope
            {
                CategoryIds = data.ApplicableCategories
            }
        };
    }
}

public class CouponData
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public List<string> ApplicableCategories { get; set; } = new();
    public List<string> TargetUserGroups { get; set; } = new();

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return now >= ValidFrom && now <= ValidTo;
    }
}