# 优惠券计算SDK - CouponCalculator

## 1. 项目概述

### 项目名称
**CouponCalculator** - 统一促销计算引擎SDK

### 项目类型
.NET 类库 (Class Library)，支持 .NET 6.0 及以上版本

### 核心功能
为电商、点餐、会员系统提供统一的优惠券计算能力，支持复杂促销规则、多重叠加、运费计算和会员折扣。

### 目标用户
- 电商系统
- 点餐系统
- 会员积分系统
- 各类促销营销系统

---

## 2. 功能列表

### 2.1 订单上下文管理
- 创建订单上下文（OrderContext）
- 添加商品明细（OrderItem）
- 设置收货信息（配送方式、地址）
- 设置用户信息（用户等级、会员类型）
- 设置订单金额（商品总额、运费）

### 2.2 优惠规则引擎
- 加载优惠规则集（IRuleProvider）
- 支持规则类型：满减券、折扣券、满件折、免运费券、会员专享价
- 支持规则条件：满金额、满件数、满重量、指定品类、指定商品
- 支持规则优先级和互斥组

### 2.3 优惠券管理
- 优惠券信息建模（Coupon）
- 优惠券状态验证（有效期、已使用、门槛检查）
- 优惠券适用商品过滤
- 优惠券不可用原因返回

### 2.4 试算可用券
- 试算单张优惠券可用性（TryApply）
- 批量试算所有可用优惠券（GetAvailableCoupons）
- 返回每张券的优惠金额和不可用原因

### 2.5 最优组合计算
- 自动选择最优优惠券组合（CalculateOptimal）
- 支持多种组合策略：最大优惠、首次使用、优先高价值
- 支持排除已使用券、已锁定券
- 返回最优组合及其总优惠金额

### 2.6 优惠叠加规则
- 支持叠加的优惠类型定义
- 互斥组规则处理
- 最高优惠金额限制
- 叠加上限控制

### 2.7 运费计算
- 运费券使用（FreeShipping）
- 运费计算规则（首重、续重、满免运费）
- 运费优惠与商品优惠的协同

### 2.8 会员折扣
- 会员等级折扣率
- 会员专享价
- 会员积分抵扣
- 会员权益优先使用

### 2.9 优惠原因解释
- 解释优惠来源（WhyApplied）
- 解释不可用原因（WhyNotApplied）
- 返回结构化的优惠说明

### 2.10 撤销试算
- 撤销当前试算（Rollback）
- 清除已应用的优惠券
- 重置优惠状态

### 2.11 展示文案生成
- 格式化优惠明细（FormatDetails）
- 生成用户友好的展示文案
- 支持多种展示格式：简洁、详细、账单式

### 2.12 计算过程记录
- 记录详细计算日志（CalculationLog）
- 记录每张券的校验过程
- 支持调试和问题排查

---

## 3. 技术架构

### 3.1 项目结构
```
CouponCalculator/
├── CouponCalculator.csproj          # 类库项目文件
├── Models/
│   ├── OrderContext.cs              # 订单上下文
│   ├── OrderItem.cs                 # 订单商品明细
│   ├── Coupon.cs                    # 优惠券模型
│   ├── DiscountRule.cs              # 优惠规则模型
│   ├── MemberInfo.cs                # 会员信息
│   ├── ShippingInfo.cs              # 配送信息
│   └── CalculationResult.cs         # 计算结果
├── Engine/
│   ├── CouponCalculatorEngine.cs    # 核心计算引擎
│   ├── RuleProvider.cs              # 规则提供者接口
│   ├── DefaultRuleProvider.cs       # 默认规则实现
│   └── CombinationOptimizer.cs      # 最优组合优化器
├── Rules/
│   ├── BaseRule.cs                  # 规则基类
│   ├── AmountThresholdRule.cs       # 满金额规则
│   ├── QuantityThresholdRule.cs     # 满件数规则
│   ├── DiscountRateRule.cs          # 折扣规则
│   ├── FreeShippingRule.cs          # 免运费规则
│   └── MemberDiscountRule.cs        # 会员折扣规则
├── Services/
│   ├── ICouponValidator.cs          # 优惠券校验器接口
│   ├── CouponValidator.cs           # 优惠券校验实现
│   ├── IStackingEngine.cs           # 叠加引擎接口
│   ├── StackingEngine.cs            # 叠加规则实现
│   └── ExplanationService.cs        # 解释服务
├── Formatters/
│   ├── IResultFormatter.cs          # 结果格式化接口
│   ├── SimpleFormatter.cs           # 简洁格式
│   ├── DetailedFormatter.cs         # 详细格式
│   └── BillFormatter.cs              # 账单格式
└── Logs/
    └── CalculationLogger.cs          # 计算日志记录器
```

### 3.2 核心类设计

#### CouponCalculatorEngine
主引擎类，提供所有计算能力的入口。
```csharp
public class CouponCalculatorEngine
{
    // 订单上下文管理
    OrderContext CreateOrderContext();
    void AddItem(OrderContext context, OrderItem item);
    void SetMember(OrderContext context, MemberInfo member);
    void SetShipping(OrderContext context, ShippingInfo shipping);
    
    // 优惠计算
    CalculationResult CalculateOptimal(OrderContext context, IEnumerable<Coupon> coupons);
    CouponTrialResult TryApply(OrderContext context, Coupon coupon);
    IEnumerable<CouponTrialResult> GetAvailableCoupons(OrderContext context, IEnumerable<Coupon> coupons);
    
    // 规则管理
    void LoadRules(IRuleProvider ruleProvider);
    
    // 解释和日志
    Explanation Explain(Coupon coupon, OrderContext context);
    string FormatDetails(CalculationResult result, DisplayFormat format);
    CalculationLog GetCalculationLog();
    
    // 撤销
    void Rollback(OrderContext context);
}
```

### 3.3 数据模型

#### OrderContext
```csharp
public class OrderContext
{
    public string OrderId { get; set; }
    public List<OrderItem> Items { get; set; }
    public MemberInfo Member { get; set; }
    public ShippingInfo Shipping { get; set; }
    public decimal OriginalAmount { get; }
    public decimal FreightAmount { get; set; }
    public DateTime CreatedAt { get; }
    public Dictionary<string, object> ExtendedData { get; }
}
```

#### Coupon
```csharp
public class Coupon
{
    public string CouponId { get; set; }
    public string CouponCode { get; set; }
    public CouponType Type { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int MinQuantity { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string[] ApplicableProductIds { get; set; }
    public string[] ApplicableCategoryIds { get; set; }
    public string[] ExcludedProductIds { get; set; }
    public int Priority { get; set; }
    public string StackingGroup { get; set; }
    public bool CanStackWithSameType { get; set; }
    public MemberLevel? RequiredMemberLevel { get; set; }
}
```

#### CalculationResult
```csharp
public class CalculationResult
{
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FreightDiscount { get; set; }
    public decimal FinalAmount { get; set; }
    public List<AppliedCoupon> AppliedCoupons { get; set; }
    public List<DiscountDetail> DiscountDetails { get; set; }
    public Explanation Explanation { get; set; }
}
```

### 3.4 枚举定义

```csharp
public enum CouponType
{
    AmountOff,          // 满减券（如满100减10）
    DiscountRate,      // 折扣券（如8折）
    QuantityDiscount,  // 满件折（如买2件8折）
    FreeShipping,      // 免运费券
    MemberExclusive,   // 会员专享价
}

public enum MemberLevel
{
    Normal = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3,
    Diamond = 4
}

public enum DisplayFormat
{
    Simple,     // 简洁格式
    Detailed,   // 详细格式
    Bill        // 账单格式
}
```

---

## 4. 叠加规则定义

### 4.1 可叠加类型
- 免运费券可与商品折扣叠加
- 会员折扣可与满减券叠加
- 不同互斥组的优惠券不可叠加

### 4.2 互斥规则
- 同一互斥组的优惠券只能使用一张
- 优先级高的优先生效
- 部分折扣券可能限制叠加数量

### 4.3 优惠上限
- 单张券最高优惠金额限制
- 订单整体优惠上限
- 运费优惠单独计算

---

## 5. 最优组合算法

### 5.1 算法策略
采用贪心算法 + 动态规划的混合策略：
1. 先按优惠力度排序所有可用券
2. 贪心选择能叠加且优惠最大的券
3. 动态规划处理互斥组选择
4. 回溯验证最优解

### 5.2 约束条件
- 满足所有优惠券的门槛条件
- 遵守叠加规则和互斥限制
- 不超过最大优惠金额限制

---

## 6. 单元测试

### 6.1 测试覆盖
- 满减券计算测试
- 折扣券计算测试
- 叠加规则测试
- 互斥规则测试
- 门槛验证测试
- 最优组合测试
- 会员折扣测试
- 运费计算测试

### 6.2 测试项目
创建 `CouponCalculator.Tests` 项目，使用 xUnit 框架。

---

## 7. API 使用示例

```csharp
// 1. 创建引擎
var engine = new CouponCalculatorEngine();

// 2. 加载优惠规则
engine.LoadRules(new DefaultRuleProvider());

// 3. 创建订单上下文
var context = engine.CreateOrderContext();
context.OrderId = "ORDER123456";

engine.AddItem(context, new OrderItem 
{
    ProductId = "PROD001",
    ProductName = "商品A",
    Price = 100m,
    Quantity = 2,
    CategoryId = "CATE001"
});

engine.SetMember(context, new MemberInfo 
{
    MemberId = "MB001",
    Level = MemberLevel.Gold
});

// 4. 试算可用优惠券
var coupons = new List<Coupon>
{
    new Coupon { CouponId = "C1", Type = CouponType.AmountOff, DiscountValue = 20, MinOrderAmount = 100 },
    new Coupon { CouponId = "C2", Type = CouponType.DiscountRate, DiscountValue = 0.9m, MinOrderAmount = 50 },
    new Coupon { CouponId = "C3", Type = CouponType.FreeShipping }
};

var available = engine.GetAvailableCoupons(context, coupons);

// 5. 计算最优组合
var result = engine.CalculateOptimal(context, coupons);

// 6. 格式化展示
var displayText = engine.FormatDetails(result, DisplayFormat.Detailed);

// 7. 获取计算日志
var log = engine.GetCalculationLog();
```

---

## 8. 验收标准

### 8.1 功能验收
- [x] 能正确创建订单上下文并添加商品
- [x] 能正确计算满减券优惠
- [x] 能正确计算折扣券优惠
- [x] 能正确处理叠加规则
- [x] 能正确处理互斥规则
- [x] 能正确计算最优组合
- [x] 能正确应用会员折扣
- [x] 能正确处理运费券
- [x] 能返回不可用原因
- [x] 能生成展示文案
- [x] 能记录计算过程

### 8.2 质量验收
- [x] 代码编译无错误
- [x] 核心功能有单元测试覆盖
- [x] API 设计清晰易用
- [x] 注释完整规范
- [x] 支持扩展规则和提供者

---

## 9. 扩展性设计

### 9.1 规则扩展
- 实现 `IRuleProvider` 接口添加自定义规则
- 继承 `BaseRule` 创建新规则类型

### 9.2 格式化扩展
- 实现 `IResultFormatter` 接口自定义展示格式

### 9.3 验证扩展
- 实现 `ICouponValidator` 接口自定义验证逻辑
