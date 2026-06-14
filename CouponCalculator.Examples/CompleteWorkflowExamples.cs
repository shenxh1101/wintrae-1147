using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Examples;

public static class CompleteWorkflowExamples
{
    public static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         优惠券计算SDK - 完整工作流演示                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        BasicWorkflowExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        EnhancedWorkflowWithMultiplePlans();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        EcommerceCompleteScenario();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        RestaurantOrderScenario();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        AdvancedScopeConfigurationExample();
        Console.WriteLine("\n" + new string('=', 70) + "\n");

        BoundaryConditionsExample();
    }

    private static void BasicWorkflowExample()
    {
        Console.WriteLine("【基础工作流示例】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ORDER-BASIC-001");

        context.AddItem(new OrderItem
        {
            ProductId = "PROD-001",
            ProductName = "商品A",
            Price = 100m,
            Quantity = 2,
            CategoryId = "CATE-001"
        });

        Console.WriteLine($"✓ 订单创建成功: {context.OrderId}");
        Console.WriteLine($"✓ 商品总额: ¥{context.OriginalAmount:F2}");

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "NEWUSER50",
                CouponName = "新人50元券",
                Type = CouponType.AmountOff,
                DiscountValue = 50m,
                MinOrderAmount = 200m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        Console.WriteLine($"✓ 已加载 {coupons.Count} 张优惠券\n");

        var trialResult = engine.TryApply(context, coupons[0]);
        Console.WriteLine($"试算结果: {(trialResult.IsAvailable ? "可用 ✓" : "不可用 ✗")}");
        if (trialResult.IsAvailable)
        {
            Console.WriteLine($"  优惠金额: ¥{trialResult.DiscountAmount:F2}");
            Console.WriteLine($"  优惠后金额: ¥{trialResult.AmountAfterDiscount:F2}");
        }
        else
        {
            Console.WriteLine($"  不可用原因: {string.Join(", ", trialResult.UnavailableReasons)}");
        }

        var result = engine.CalculateOptimal(context, coupons);

        Console.WriteLine($"\n计算结果:");
        Console.WriteLine($"  原始金额: ¥{result.OriginalAmount:F2}");
        Console.WriteLine($"  优惠金额: -¥{result.ProductDiscountAmount:F2}");
        Console.WriteLine($"  最终金额: ¥{result.FinalAmount:F2}");

        var display = engine.FormatDetails(result, DisplayFormat.Bill);
        Console.WriteLine($"\n展示文案:\n{display}");
    }

    private static void EnhancedWorkflowWithMultiplePlans()
    {
        Console.WriteLine("【增强工作流 - 多方案对比】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ORDER-MULTI-001");
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
            MemberId = "USER-001",
            Level = MemberLevel.Gold
        };
        context.FreightAmount = 30m;

        Console.WriteLine($"订单金额: ¥{context.OriginalAmount:F2}");
        Console.WriteLine($"会员等级: {context.Member.Level} (金卡会员)");
        Console.WriteLine($"运费: ¥{context.FreightAmount:F2}\n");

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "C1",
                CouponCode = "数码券",
                CouponName = "数码专属9折券",
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
                CouponId = "C2",
                CouponCode = "配件券",
                CouponName = "配件85折券",
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
                CouponId = "C3",
                CouponCode = "免运费",
                CouponName = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var enhancedResult = engine.CalculateOptimalEnhanced(context, coupons);

        Console.WriteLine($"生成了 {enhancedResult.CandidatePlans.Count} 个候选方案:\n");

        foreach (var plan in enhancedResult.CandidatePlans.Take(3))
        {
            var marker = plan.IsRecommended ? "⭐" : "  ";
            Console.WriteLine($"{marker} {plan.PlanName}");
            Console.WriteLine($"   节省: ¥{plan.TotalDiscountAmount:F2}");
            Console.WriteLine($"   最终: ¥{plan.FinalTotalAmount:F2}");
            Console.WriteLine($"   优惠券: {string.Join(", ", plan.AppliedCoupons.Select(c => c.CouponCode))}");
            
            if (plan.MemberDiscountAmount > 0)
            {
                Console.WriteLine($"   会员折扣: ¥{plan.MemberDiscountAmount:F2}");
            }
            Console.WriteLine();
        }

        if (enhancedResult.RecommendedPlan != null)
        {
            Console.WriteLine("推荐方案详情:");
            Console.WriteLine($"  {enhancedResult.RecommendedPlan.PlanName}");
            Console.WriteLine($"  应付总额: ¥{enhancedResult.RecommendedPlan.FinalTotalAmount:F2}");
            
            foreach (var coupon in enhancedResult.RecommendedPlan.AppliedCoupons)
            {
                Console.WriteLine($"  ✓ {coupon.CouponName}: -¥{coupon.DiscountAmount:F2}");
                Console.WriteLine($"    {coupon.Reason}");
            }
        }

        var comparison = engine.FormatEnhancedResult(enhancedResult, context, DisplayFormat.Detailed);
        Console.WriteLine($"\n方案对比表:\n{comparison}");

        var settlementData = engine.GetSettlementPageData(enhancedResult, context);
        Console.WriteLine("\n结算页面数据结构:");
        Console.WriteLine($"  originalAmount: {settlementData["originalAmount"]}");
        Console.WriteLine($"  finalAmount: {settlementData["finalAmount"]}");
        Console.WriteLine($"  totalDiscount: {settlementData["totalDiscount"]}");
        Console.WriteLine($"  couponCount: {settlementData["couponCount"]}");
    }

    private static void EcommerceCompleteScenario()
    {
        Console.WriteLine("【电商完整场景】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ECOM-20230614-001");
        context.ExtendedData["StoreId"] = "STORE-SH-001";
        context.ExtendedData["OrderCount"] = 5;

        var cartItems = new List<OrderItem>
        {
            new OrderItem 
            { 
                ProductId = "SKU-001", 
                ProductName = "iPhone 15 Pro", 
                Price = 7999m, 
                Quantity = 1, 
                CategoryId = "PHONE" 
            },
            new OrderItem 
            { 
                ProductId = "SKU-002", 
                ProductName = "AirPods Pro", 
                Price = 1499m, 
                Quantity = 1, 
                CategoryId = "AUDIO" 
            },
            new OrderItem 
            { 
                ProductId = "SKU-003", 
                ProductName = "手机壳", 
                Price = 99m, 
                Quantity = 2, 
                CategoryId = "ACCESSORY" 
            }
        };

        foreach (var item in cartItems)
        {
            engine.AddItem(context, item);
        }

        context.Member = new MemberInfo
        {
            MemberId = "VIP-USER-001",
            Level = MemberLevel.Platinum,
            Points = 5000
        };

        context.Shipping = new ShippingInfo
        {
            ShippingMethod = "顺丰速运",
            Province = "上海",
            City = "上海市",
            Weight = 0.8m,
            FirstWeight = 1m,
            FirstWeightPrice = 12m,
            ContinueWeight = 1m,
            ContinueWeightPrice = 6m
        };
        context.FreightAmount = context.Shipping.CalculateFreight();

        Console.WriteLine("【购物车】");
        foreach (var item in context.Items)
        {
            Console.WriteLine($"  {item.ProductName}: ¥{item.Price:F2} x {item.Quantity} = ¥{item.TotalAmount:F2}");
        }
        Console.WriteLine($"  商品总额: ¥{context.OriginalAmount:F2}");
        Console.WriteLine($"  运费: ¥{context.FreightAmount:F2}");
        Console.WriteLine($"  会员: {context.Member.Level} (白金会员)");

        var availableCoupons = new List<Coupon>
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
                ValidTo = DateTime.UtcNow.AddDays(7)
            },
            new Coupon
            {
                CouponId = "C2",
                CouponCode = "AUDIO-DISC",
                CouponName = "耳机85折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.85m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "AUDIO" } 
                },
                ExcludedScope = new ExcludedScope 
                { 
                    ProductIds = new List<string> { "SKU-999" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "C3",
                CouponCode = "MEMBER-VIP",
                CouponName = "会员专享9折",
                Type = CouponType.MemberExclusive,
                DiscountValue = 0.9m,
                UsageCondition = new UsageCondition
                {
                    AllowedMemberLevels = new List<MemberLevel> 
                    { 
                        MemberLevel.Gold, 
                        MemberLevel.Platinum, 
                        MemberLevel.Diamond 
                    }
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(365)
            },
            new Coupon
            {
                CouponId = "C4",
                CouponCode = "FREESHIP",
                CouponName = "免运费券",
                Type = CouponType.FreeShipping,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        Console.WriteLine($"\n【可用优惠券】({availableCoupons.Count}张)");
        foreach (var coupon in availableCoupons)
        {
            Console.WriteLine($"  · {coupon.CouponName}");
            Console.WriteLine($"    {coupon.GetDiscountDescription()} | {coupon.GetScopeDescription()}");
        }

        var trialResults = engine.GetAvailableCouponsEnhanced(context, availableCoupons).ToList();
        
        Console.WriteLine($"\n【试算结果】");
        foreach (var trial in trialResults)
        {
            if (trial.IsAvailable)
            {
                Console.WriteLine($"  ✓ {trial.Coupon.CouponName}: 可省 ¥{trial.DiscountAmount:F2}");
                Console.WriteLine($"    {trial.UserExplanation.Summary}");
            }
            else
            {
                Console.WriteLine($"  ✗ {trial.Coupon.CouponName}: 不可用");
                foreach (var reason in trial.UnavailableReasons.Take(2))
                {
                    Console.WriteLine($"    - {reason}");
                }
            }
        }

        var result = engine.CalculateOptimalEnhanced(context, availableCoupons);

        Console.WriteLine($"\n【最优方案】");
        if (result.RecommendedPlan != null)
        {
            Console.WriteLine($"  {result.RecommendedPlan.PlanName}");
            Console.WriteLine($"  应付总额: ¥{result.RecommendedPlan.FinalTotalAmount:F2}");
            Console.WriteLine($"  共节省: ¥{result.RecommendedPlan.TotalDiscountAmount:F2}");
            
            Console.WriteLine($"\n  优惠明细:");
            foreach (var coupon in result.RecommendedPlan.AppliedCoupons)
            {
                Console.WriteLine($"    ✓ {coupon.CouponName}: -¥{coupon.DiscountAmount:F2}");
                Console.WriteLine($"      {coupon.Reason}");
            }
            
            if (result.RecommendedPlan.MemberDiscountAmount > 0)
            {
                Console.WriteLine($"    ✓ 会员折扣: -¥{result.RecommendedPlan.MemberDiscountAmount:F2}");
            }
            
            if (result.RecommendedPlan.FreightDiscountAmount > 0)
            {
                Console.WriteLine($"    ✓ 运费优惠: -¥{result.RecommendedPlan.FreightDiscountAmount:F2}");
            }
        }

        Console.WriteLine($"\n【其他方案对比】");
        foreach (var plan in result.CandidatePlans.Where(p => !p.IsRecommended).Take(2))
        {
            Console.WriteLine($"  {plan.PlanName}: 节省 ¥{plan.TotalDiscountAmount:F2}");
        }

        Console.WriteLine($"\n【结算页面数据】");
        var settlementData = engine.GetSettlementPageData(result, context);
        Console.WriteLine($"  originalTotal: ¥{settlementData["originalTotal"]}");
        Console.WriteLine($"  finalAmount: ¥{settlementData["finalAmount"]}");
        Console.WriteLine($"  totalDiscount: ¥{settlementData["totalDiscount"]}");
        Console.WriteLine($"  couponCount: {settlementData["couponCount"]}");
    }

    private static void RestaurantOrderScenario()
    {
        Console.WriteLine("【点餐完整场景】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("REST-20230614-001");

        var menuItems = new List<OrderItem>
        {
            new OrderItem 
            { 
                ProductId = "MEAL-001", 
                ProductName = "招牌套餐", 
                Price = 68m, 
                Quantity = 2, 
                CategoryId = "MEALS",
                IsShippingRequired = false
            },
            new OrderItem 
            { 
                ProductId = "SNACK-001", 
                ProductName = "小食拼盘", 
                Price = 28m, 
                Quantity = 1, 
                CategoryId = "SNACKS",
                IsShippingRequired = false
            },
            new OrderItem 
            { 
                ProductId = "DRINK-001", 
                ProductName = "招牌奶茶", 
                Price = 22m, 
                Quantity = 2, 
                CategoryId = "DRINKS",
                IsShippingRequired = false
            }
        };

        foreach (var item in menuItems)
        {
            engine.AddItem(context, item);
        }

        context.Member = new MemberInfo
        {
            MemberId = "MEMBER-001",
            Level = MemberLevel.Gold
        };

        Console.WriteLine("【已选菜品】");
        foreach (var item in context.Items)
        {
            Console.WriteLine($"  {item.ProductName}: ¥{item.Price:F2} x {item.Quantity}");
        }
        Console.WriteLine($"  消费总额: ¥{context.OriginalAmount:F2}");
        Console.WriteLine($"  会员: {context.Member.Level}");

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                CouponId = "R1",
                CouponCode = "MEAL-DISC",
                CouponName = "套餐9折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.9m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "MEALS" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "R2",
                CouponCode = "DRINK-DISC",
                CouponName = "饮品8折券",
                Type = CouponType.DiscountRate,
                DiscountValue = 0.8m,
                ApplicableScope = new ApplicableScope 
                { 
                    CategoryIds = new List<string> { "DRINKS" } 
                },
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            },
            new Coupon
            {
                CouponId = "R3",
                CouponCode = "FULL-DISC",
                CouponName = "满150减20",
                Type = CouponType.AmountOff,
                DiscountValue = 20m,
                MinOrderAmount = 150m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30)
            }
        };

        var result = engine.CalculateOptimalEnhanced(context, coupons);

        Console.WriteLine($"\n【推荐方案】");
        if (result.RecommendedPlan != null)
        {
            Console.WriteLine($"  应付金额: ¥{result.RecommendedPlan.FinalTotalAmount:F2}");
            Console.WriteLine($"  节省: ¥{result.RecommendedPlan.TotalDiscountAmount:F2}");
            
            foreach (var coupon in result.RecommendedPlan.AppliedCoupons)
            {
                Console.WriteLine($"  ✓ {coupon.CouponName}: -¥{coupon.DiscountAmount:F2}");
            }
        }

        var bill = engine.FormatEnhancedResult(result, context, DisplayFormat.Bill);
        Console.WriteLine($"\n【账单】\n{bill}");
    }

    private static void AdvancedScopeConfigurationExample()
    {
        Console.WriteLine("【高级适用范围配置】\n");

        var engine = new CouponCalculatorEngine();

        var context = engine.CreateOrderContext("ORDER-SCOPE-001");
        context.AddItem(new OrderItem
        {
            ProductId = "PROD-001",
            ProductName = "指定商品A",
            Price = 100m,
            Quantity = 1,
            CategoryId = "CATE-A"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD-002",
            ProductName = "指定商品B",
            Price = 200m,
            Quantity = 1,
            CategoryId = "CATE-B"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD-003",
            ProductName = "排除商品",
            Price = 50m,
            Quantity = 1,
            CategoryId = "CATE-A"
        });
        context.AddItem(new OrderItem
        {
            ProductId = "PROD-004",
            ProductName = "全场通用",
            Price = 150m,
            Quantity = 1,
            CategoryId = "CATE-C"
        });

        Console.WriteLine("【商品列表】");
        foreach (var item in context.Items)
        {
            Console.WriteLine($"  {item.ProductId}: {item.ProductName} (¥{item.Price:F2})");
        }
        Console.WriteLine($"  总计: ¥{context.OriginalAmount:F2}");

        var coupon = new Coupon
        {
            CouponId = "C1",
            CouponCode = "MIXED-SCOPE",
            CouponName = "混合范围券",
            Type = CouponType.DiscountRate,
            DiscountValue = 0.8m,
            ApplicableScope = new ApplicableScope
            {
                ProductIds = new List<string> { "PROD-001", "PROD-002" },
                CategoryIds = new List<string> { "CATE-C" }
            },
            ExcludedScope = new ExcludedScope
            {
                ProductIds = new List<string> { "PROD-003" },
                CategoryIds = new List<string> { "CATE-B" }
            },
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        Console.WriteLine($"\n【优惠券配置】");
        Console.WriteLine($"  名称: {coupon.CouponName}");
        Console.WriteLine($"  适用范围: {coupon.GetScopeDescription()}");
        Console.WriteLine($"  折扣: {coupon.GetDiscountDescription()}");

        var trialResult = engine.TryApplyEnhanced(context, coupon);

        Console.WriteLine($"\n【试算结果】");
        Console.WriteLine($"  状态: {(trialResult.IsAvailable ? "可用" : "不可用")}");
        Console.WriteLine($"  优惠金额: ¥{trialResult.DiscountAmount:F2}");
        Console.WriteLine($"  适用商品: {string.Join(", ", trialResult.ApplicableProductIds)}");
        
        if (trialResult.ExcludedProductIds.Any())
        {
            Console.WriteLine($"  排除商品: {string.Join(", ", trialResult.ExcludedProductIds)}");
        }

        foreach (var check in trialResult.CheckResults)
        {
            var icon = check.Passed ? "✓" : "✗";
            Console.WriteLine($"  {icon} {check.CheckName}: {check.UserFriendlyMessage}");
        }
    }

    private static void BoundaryConditionsExample()
    {
        Console.WriteLine("【边界条件测试】\n");

        var engine = new CouponCalculatorEngine();

        var testCases = new List<(string Name, Action Test)>
        {
            ("空订单", () =>
            {
                var context = engine.CreateOrderContext("ORDER-EMPTY");
                var result = engine.CalculateOptimal(context, new List<Coupon>());
                Console.WriteLine($"  原始金额: ¥{result.OriginalAmount:F2}");
                Console.WriteLine($"  最终金额: ¥{result.FinalAmount:F2}");
            }),
            ("零金额商品", () =>
            {
                var context = engine.CreateOrderContext("ORDER-ZERO");
                context.AddItem(new OrderItem { ProductId = "P1", Price = 0m, Quantity = 1 });
                var result = engine.CalculateOptimal(context, new List<Coupon>());
                Console.WriteLine($"  原始金额: ¥{result.OriginalAmount:F2}");
            }),
            ("刚好满足门槛", () =>
            {
                var context = engine.CreateOrderContext("ORDER-THRESHOLD");
                context.AddItem(new OrderItem { ProductId = "P1", Price = 100m, Quantity = 1 });
                var coupons = new List<Coupon>
                {
                    new Coupon 
                    { 
                        CouponId = "C1", 
                        Type = CouponType.AmountOff, 
                        DiscountValue = 10m, 
                        MinOrderAmount = 100m,
                        ValidFrom = DateTime.UtcNow.AddDays(-1),
                        ValidTo = DateTime.UtcNow.AddDays(30)
                    }
                };
                var result = engine.CalculateOptimal(context, coupons);
                Console.WriteLine($"  订单金额: ¥100.00 (刚好等于门槛)");
                Console.WriteLine($"  优惠: ¥{result.ProductDiscountAmount:F2} (应该可用)");
            }),
            ("撤销后重算", () =>
            {
                var context = engine.CreateOrderContext("ORDER-ROLLBACK");
                context.AddItem(new OrderItem { ProductId = "P1", Price = 200m, Quantity = 1 });
                var coupons = new List<Coupon>
                {
                    new Coupon 
                    { 
                        CouponId = "C1", 
                        Type = CouponType.AmountOff, 
                        DiscountValue = 20m, 
                        MinOrderAmount = 100m,
                        ValidFrom = DateTime.UtcNow.AddDays(-1),
                        ValidTo = DateTime.UtcNow.AddDays(30)
                    }
                };
                var r1 = engine.CalculateOptimal(context, coupons);
                engine.Rollback(context);
                var r2 = engine.CalculateOptimal(context, coupons);
                Console.WriteLine($"  第一次: ¥{r1.FinalAmount:F2}");
                Console.WriteLine($"  撤销后: ¥{r2.FinalAmount:F2}");
                Console.WriteLine($"  一致性: {(r1.FinalAmount == r2.FinalAmount ? "✓" : "✗")}");
            })
        };

        foreach (var (name, test) in testCases)
        {
            Console.WriteLine($"【{name}】");
            test();
            Console.WriteLine();
        }
    }
}
