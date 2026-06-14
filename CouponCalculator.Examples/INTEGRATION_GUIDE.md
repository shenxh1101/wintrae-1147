# 优惠券计算SDK - 完整接入指南

## 📦 快速开始

### 1. 安装SDK

将 `CouponCalculator` 项目添加到您的解决方案中，引用即可使用：

```csharp
using CouponCalculator.Engine;
using CouponCalculator.Models;
```

### 2. 创建引擎实例

```csharp
var engine = new CouponCalculatorEngine();
```

### 3. 基础使用流程

```csharp
// Step 1: 创建订单上下文
var context = engine.CreateOrderContext("ORDER-001");

// Step 2: 添加商品
context.AddItem(new OrderItem
{
    ProductId = "SKU-001",
    ProductName = "商品名称",
    Price = 100m,
    Quantity = 2,
    CategoryId = "CATE-001"
});

// Step 3: 设置会员信息 (可选)
context.Member = new MemberInfo
{
    MemberId = "USER-001",
    Level = MemberLevel.Gold
};

// Step 4: 设置配送信息 (可选)
context.FreightAmount = 15m; // 或使用 ShippingInfo 计算

// Step 5: 准备优惠券
var coupons = new List<Coupon>
{
    new Coupon
    {
        CouponId = "C1",
        CouponCode = "NEWUSER100",
        Type = CouponType.AmountOff,
        DiscountValue = 20m,
        MinOrderAmount = 100m,
        ValidFrom = DateTime.UtcNow.AddDays(-1),
        ValidTo = DateTime.UtcNow.AddDays(30)
    }
};

// Step 6: 计算最优优惠
var result = engine.CalculateOptimal(context, coupons);

// Step 7: 获取展示文案
var display = engine.FormatDetails(result, DisplayFormat.Bill);
Console.WriteLine(display);
```

---

## 🎯 完整功能清单

### 1. 订单上下文管理

```csharp
var context = engine.CreateOrderContext("ORDER-ID");

// 添加商品
engine.AddItem(context, new OrderItem { ... });

// 设置会员
engine.SetMember(context, new MemberInfo { ... });

// 设置配送
engine.SetShipping(context, new ShippingInfo { ... });

// 扩展数据 (用于自定义业务逻辑)
context.ExtendedData["StoreId"] = "STORE-001";
context.ExtendedData["OrderCount"] = 5;
```

### 2. 优惠券配置

```csharp
var coupon = new Coupon
{
    CouponId = "C1",
    CouponCode = "DISCOUNT",
    CouponName = "优惠名称",
    
    // 优惠券类型
    Type = CouponType.AmountOff,  // 满减
    // Type = CouponType.DiscountRate,  // 折扣
    // Type = CouponType.QuantityDiscount,  // 满件折
    // Type = CouponType.FreeShipping,  // 免运费
    // Type = CouponType.MemberExclusive,  // 会员专享
    
    // 优惠值
    DiscountValue = 20m,  // 减20元 或 打8折 (0.8m)
    
    // 最高优惠上限
    MaxDiscountAmount = 50m,
    
    // 适用范围配置
    ApplicableScope = new ApplicableScope
    {
        ProductIds = new List<string> { "SKU-001", "SKU-002" },
        CategoryIds = new List<string> { "CATE-001" },
        StoreIds = new List<string> { "STORE-001" },
        BrandIds = new List<string> { "BRAND-A" }
    },
    
    // 排除范围
    ExcludedScope = new ExcludedScope
    {
        ProductIds = new List<string> { "SKU-EXCLUDE" },
        CategoryIds = new List<string> { "CATE-EXCLUDE" }
    },
    
    // 使用条件
    UsageCondition = new UsageCondition
    {
        MinOrderAmount = 100m,  // 最低消费
        MinQuantity = 2,  // 最低件数
        AllowedMemberLevels = new List<MemberLevel> 
        { 
            MemberLevel.Gold, 
            MemberLevel.Platinum, 
            MemberLevel.Diamond 
        },
        ValidFrom = DateTime.UtcNow,
        ValidTo = DateTime.UtcNow.AddDays(30)
    },
    
    // 叠加规则
    StackingGroup = "GROUP-A",  // 同组互斥
    CanStackWithSameType = true,  // 是否可与同类叠加
    
    // 元数据
    Metadata = new CouponMetadata
    {
        CampaignId = "CAMPAIGN-001",
        Tag = "promotion"
    }
};
```

### 3. 试算可用券

```csharp
// 单张券试算
var trialResult = engine.TryApply(context, coupon);

// 批量试算
var trialResults = engine.GetAvailableCoupons(context, coupons);

// 增强试算 (返回详细原因)
var enhancedTrial = engine.TryApplyEnhanced(context, coupon);

// 批量增强试算
var enhancedTrials = engine.GetAvailableCouponsEnhanced(context, coupons);
```

### 4. 最优组合计算

```csharp
// 标准计算 (返回单一最优方案)
var result = engine.CalculateOptimal(context, coupons);

// 增强计算 (返回多个候选方案)
var enhancedResult = engine.CalculateOptimalEnhanced(context, coupons);

// 方案列表
foreach (var plan in enhancedResult.CandidatePlans)
{
    Console.WriteLine($"{plan.PlanName}: 节省 ¥{plan.TotalDiscountAmount:F2}");
}

// 推荐方案
if (enhancedResult.RecommendedPlan != null)
{
    Console.WriteLine($"应付: ¥{enhancedResult.RecommendedPlan.FinalTotalAmount:F2}");
}
```

### 5. 撤销与重算

```csharp
// 撤销当前订单的优惠
engine.Rollback(context);

// 撤销最后一次计算
engine.RollbackLastCalculation();

// 使用相同参数重新计算
var newResult = engine.RecalculateWithSameParameters();
```

### 6. 格式化输出

```csharp
// 简单格式
var simple = engine.FormatDetails(result, DisplayFormat.Simple);

// 详细格式
var detailed = engine.FormatDetails(result, DisplayFormat.Detailed);

// 账单格式
var bill = engine.FormatDetails(result, DisplayFormat.Bill);

// 增强格式输出
var enhanced = engine.FormatEnhancedResult(enhancedResult, context, DisplayFormat.Detailed);
```

### 7. 结算页面数据

```csharp
var settlementData = engine.GetSettlementPageData(enhancedResult, context);

// 获取所需字段
var originalAmount = settlementData["originalAmount"];  // 原始金额
var finalAmount = settlementData["finalAmount"];  // 最终应付
var totalDiscount = settlementData["totalDiscount"];  // 总优惠
var appliedCoupons = settlementData["appliedCoupons"];  // 应用的券列表
```

---

## 📊 返回数据结构

### CalculationResult

```csharp
public class CalculationResult
{
    public decimal OriginalAmount { get; set; }    // 原始商品金额
    public decimal ProductDiscountAmount { get; set; }  // 商品优惠
    public decimal FreightDiscount { get; set; }     // 运费优惠
    public decimal MemberDiscount { get; set; }      // 会员折扣
    public decimal FinalAmount { get; set; }         // 最终应付金额
    public List<AppliedCoupon> AppliedCoupons { get; set; }  // 已应用优惠券
    public Explanation Explanation { get; set; }    // 解释说明
}
```

### EnhancedCalculationResult

```csharp
public class EnhancedCalculationResult
{
    public string OrderId { get; set; }
    public decimal OriginalAmount { get; set; }
    public List<CandidatePlan> CandidatePlans { get; set; }  // 候选方案列表
    public CandidatePlan? RecommendedPlan { get; set; }      // 推荐方案
    public PlanComparison Comparison { get; set; }           // 方案对比
}
```

### CandidatePlan

```csharp
public class CandidatePlan
{
    public string PlanId { get; set; }
    public string PlanName { get; set; }
    public bool IsRecommended { get; set; }
    public List<AppliedCouponDetail> AppliedCoupons { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal FinalTotalAmount { get; set; }
    public List<string> Advantages { get; set; }  // 方案优点
    public List<string> Limitations { get; set; }  // 方案限制
}
```

---

## 🎨 使用场景示例

### 场景1: 电商订单

```csharp
var engine = new CouponCalculatorEngine();
var context = engine.CreateOrderContext(orderId);

// 添加购物车商品
foreach (var item in cart.Items)
{
    engine.AddItem(context, new OrderItem
    {
        ProductId = item.SkuId,
        ProductName = item.Name,
        Price = item.Price,
        Quantity = item.Quantity,
        CategoryId = item.CategoryId
    });
}

// 设置会员
context.Member = new MemberInfo
{
    MemberId = user.Id,
    Level = (MemberLevel)user.MemberLevel
};

// 设置配送
context.FreightAmount = CalculateFreight(cart.Weight);

// 获取可用优惠券
var userCoupons = await couponService.GetUserCouponsAsync(userId, context.OriginalAmount);
var availableCoupons = userCoupons.Where(c => c.Status == CouponStatus.Available).ToList();

// 计算最优方案
var result = engine.CalculateOptimalEnhanced(context, availableCoupons);

// 返回结算数据
return new SettlementData
{
    OriginalAmount = result.RecommendedPlan.OriginalAmount,
    FinalAmount = result.RecommendedPlan.FinalTotalAmount,
    TotalDiscount = result.RecommendedPlan.TotalDiscountAmount,
    AppliedCoupons = result.RecommendedPlan.AppliedCoupons.Select(c => new 
    {
        c.CouponId,
        c.CouponCode,
        c.DiscountAmount
    }).ToList(),
    CandidatePlans = result.CandidatePlans.Select(p => new 
    {
        p.PlanName,
        p.FinalTotalAmount,
        p.TotalDiscountAmount,
        p.IsRecommended
    }).ToList()
};
```

### 场景2: 点餐系统

```csharp
var engine = new CouponCalculatorEngine();
var context = engine.CreateOrderContext();

// 添加菜品
foreach (var dish in order.Dishes)
{
    engine.AddItem(context, new OrderItem
    {
        ProductId = dish.Id,
        ProductName = dish.Name,
        Price = dish.Price,
        Quantity = dish.Quantity,
        CategoryId = dish.CategoryId,
        IsShippingRequired = false
    });
}

// 计算优惠
var result = engine.CalculateOptimal(context, availableCoupons);

// 生成账单
var bill = engine.FormatEnhancedResult(result, context, DisplayFormat.Bill);
```

### 场景3: 会员专享

```csharp
// 金卡及以上会员专享券
var vipCoupon = new Coupon
{
    CouponId = "VIP-001",
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
    }
};

var trial = engine.TryApplyEnhanced(context, vipCoupon);
if (trial.IsAvailable)
{
    Console.WriteLine($"可享受 {trial.DiscountAmount:F2} 元优惠");
}
else
{
    foreach (var reason in trial.UnavailableReasons)
    {
        Console.WriteLine(reason);
    }
}
```

---

## ⚠️ 注意事项

### 1. 金额精度
- 所有金额使用 `decimal` 类型，避免浮点数精度问题
- 金额显示时使用 `F2` 格式保留两位小数

### 2. 优惠券有效期
- 使用 `DateTime.UtcNow` 进行时间比较
- 确保优惠券的 `ValidFrom` 和 `ValidTo` 设置正确

### 3. 会员等级
- 会员等级从 `Normal(0)` 到 `Diamond(4)`
- 使用 `MemberLevel` 枚举进行比较

### 4. 适用范围
- 当 `ApplicableScope` 和 `ExcludedScope` 同时配置时，先判断排除再判断适用
- 空范围表示全场可用

### 5. 撤销与重算
- 修改订单后必须调用 `Rollback` 清除之前的优惠
- 使用 `RecalculateWithSameParameters` 可以快速重算

---

## 🔧 扩展点

### 1. 自定义规则提供者

```csharp
public class CustomRuleProvider : IRuleProvider
{
    public IEnumerable<DiscountRule> GetRules()
    {
        // 从数据库或配置加载规则
        return LoadRulesFromDatabase();
    }
}

engine.LoadRules(new CustomRuleProvider());
```

### 2. 自定义验证器

```csharp
public class CustomCouponValidator : ICouponValidator
{
    // 实现自定义验证逻辑
}
```

### 3. 自定义格式化器

```csharp
public class CustomFormatter : IResultFormatter
{
    public string Format(CalculationResult result)
    {
        // 返回自定义格式
    }
}
```

---

## 📝 最佳实践

1. **预验证**: 在调用 `CalculateOptimal` 前，先使用 `TryApplyEnhanced` 验证优惠券
2. **错误处理**: 捕获 `InvalidOperationException` 处理重算失败的情况
3. **日志记录**: 使用 `GetCalculationLog` 记录计算过程，便于问题排查
4. **缓存结果**: 在用户确认订单前，可以缓存计算结果，避免重复计算
5. **超时处理**: 对于大量优惠券，可以设置超时机制

---

## 🎯 常见问题

**Q: 为什么优惠券试算显示可用但计算时没有应用？**
A: 可能存在叠加规则冲突，检查 `StackingGroup` 和 `CanStackWithSameType` 设置。

**Q: 如何实现部分商品参与优惠？**
A: 使用 `ApplicableScope` 配置指定商品或类目，使用 `ExcludedScope` 排除不参与的商品。

**Q: 会员折扣和优惠券可以叠加吗？**
A: 可以。会员折扣和优惠券（除会员专享券外）可以叠加使用。

**Q: 如何查看计算过程？**
A: 使用 `GetCalculationLog()` 获取详细的计算日志。

---

## 📞 技术支持

如有问题，请检查：
1. 优惠券的 `ValidFrom` 和 `ValidTo` 是否正确
2. 订单金额是否满足 `MinOrderAmount`
3. 会员等级是否满足要求
4. 商品是否在适用范围和排除范围之外

更多示例代码请参考 `CouponCalculator.Examples` 项目。
