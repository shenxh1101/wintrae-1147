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
- 支持扩展数据（ExtendedData）用于自定义业务逻辑

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
- **新增**: 灵活的适用范围配置（指定商品、类目、门店、品牌）
- **新增**: 排除范围配置（排除指定商品、类目、门店）
- **新增**: 详细的使用条件（会员等级限制、用户限制、门店限制）

### 2.4 试算可用券
- 试算单张优惠券可用性（TryApply）
- 批量试算所有可用优惠券（GetAvailableCoupons）
- 返回每张券的优惠金额和不可用原因
- **新增**: 增强试算（TryApplyEnhanced）返回详细的校验过程
- **新增**: 用户友好的解释说明（UserFriendlyExplanation）

### 2.5 最优组合计算
- 自动选择最优优惠券组合（CalculateOptimal）
- 支持多种组合策略：最大优惠、首次使用、优先高价值
- 支持排除已使用券、已锁定券
- 返回最优组合及其总优惠金额
- **新增**: 增强计算（CalculateOptimalEnhanced）返回多个候选方案
- **新增**: 方案对比（PlanComparison）包含各方案优缺点
- **新增**: 结算页面数据结构（GetSettlementPageData）

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
- 会员等级折扣率（Normal/Silver/Gold/Platinum/Diamond）
- 会员专享价
- 会员积分抵扣
- 会员权益优先使用
- 自动应用会员等级折扣

### 2.9 优惠原因解释
- 解释优惠来源（WhyApplied）
- 解释不可用原因（WhyNotApplied）
- 返回结构化的优惠说明
- **新增**: 增强解释服务（EnhancedExplanationService）
- **新增**: 用户友好的文案生成
- **新增**: 方案对比表生成

### 2.10 撤销试算
- 撤销当前试算（Rollback）
- 清除已应用的优惠券
- 重置优惠状态
- **新增**: 撤销最后一次计算（RollbackLastCalculation）
- **新增**: 使用相同参数重算（RecalculateWithSameParameters）

### 2.11 展示文案生成
- 格式化优惠明细（FormatDetails）
- 生成用户友好的展示文案
- 支持多种展示格式：简洁、详细、账单式
- **新增**: 增强格式输出（FormatEnhancedResult）

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
│   ├── Enums.cs                     # 枚举定义
│   ├── OrderContext.cs              # 订单上下文
│   ├── OrderItem.cs                 # 订单商品明细
│   ├── Coupon.cs                    # 优惠券模型（含适用范围配置）
│   ├── DiscountRule.cs              # 优惠规则模型
│   ├── MemberInfo.cs                # 会员信息
│   ├── ShippingInfo.cs              # 配送信息
│   ├── CalculationResult.cs         # 计算结果
│   ├── AppliedCoupon.cs             # 已应用优惠券
│   ├── DiscountDetail.cs            # 优惠明细
│   ├── Explanation.cs               # 解释说明
│   ├── CouponTrialResult.cs         # 试算结果
│   ├── CalculationLog.cs             # 计算日志
│   ├── ScopeConfiguration.cs        # 适用范围配置
│   ├── EnhancedCalculationResult.cs  # 增强计算结果
│   ├── EnhancedCouponTrialResult.cs  # 增强试算结果
│   ├── RuleVersion.cs               # 规则版本管理 (新增)
│   ├── ItemLevelBreakdown.cs        # 商品级分摊 (新增)
│   ├── Reconciliation.cs             # 对账模型 (新增)
│   ├── GrayRelease.cs               # 灰度发布 (新增)
│   └── TemplateAndExplanation.cs     # 口径模板 (新增)
├── Engine/
│   ├── CouponCalculatorEngine.cs     # 核心计算引擎
│   ├── IRuleProvider.cs             # 规则提供者接口
│   ├── DefaultRuleProvider.cs        # 默认规则实现
│   ├── CombinationOptimizer.cs       # 最优组合优化器
│   └── EnhancedCombinationOptimizer.cs  # 增强组合优化器
├── Services/
│   ├── ICouponValidator.cs          # 优惠券校验器接口
│   ├── CouponValidator.cs            # 优惠券校验实现
│   ├── EnhancedCouponValidator.cs    # 增强校验器
│   ├── IStackingEngine.cs           # 叠加引擎接口
│   ├── StackingEngine.cs             # 叠加规则实现
│   ├── ExplanationService.cs         # 解释服务
│   ├── EnhancedExplanationService.cs # 增强解释服务
│   ├── RuleVersionManager.cs        # 版本管理 (新增)
│   ├── ItemLevelAllocationService.cs # 商品级分摊 (新增)
│   ├── ReconciliationService.cs      # 对账服务 (新增)
│   ├── GrayReleaseManager.cs        # 灰度发布管理 (新增)
│   ├── TemplatePlaybackService.cs    # 模板回放 (新增)
│   ├── TemplateManagementService.cs  # 模板管理 (新增)
│   └── ExportService.cs             # 导出服务 (新增)
├── Formatters/
│   ├── IResultFormatter.cs           # 结果格式化接口
│   ├── SimpleFormatter.cs            # 简洁格式
│   ├── DetailedFormatter.cs          # 详细格式
│   └── BillFormatter.cs             # 账单格式
├── Rules/
│   ├── BaseRule.cs                  # 规则基类
│   ├── AmountThresholdRule.cs        # 满金额规则
│   ├── QuantityThresholdRule.cs      # 满件数规则
│   ├── DiscountRateRule.cs          # 折扣规则
│   ├── FreeShippingRule.cs           # 免运费规则
│   └── MemberDiscountRule.cs        # 会员折扣规则
└── Logs/
    └── CalculationLogger.cs          # 计算日志记录器

CouponCalculator.Tests/               # 单元测试项目
├── CouponCalculatorEngineTests.cs
├── CouponCalculationTests.cs
├── StackingTests.cs
├── MemberDiscountTests.cs
├── ShippingTests.cs
├── FormatterTests.cs
├── OptimalCombinationTests.cs
├── ExplanationAndLogTests.cs
├── EdgeCaseTests.cs
├── EnhancedFeatureTests.cs
├── RuleVersionTests.cs              # 版本管理测试 (新增)
├── ReconciliationTests.cs            # 对账测试 (新增)
├── BatchReconciliationTests.cs       # 批量对账测试 (新增)
├── GrayReleaseTests.cs               # 灰度发布测试 (新增)
└── TemplatePlaybackTests.cs          # 模板回放测试 (新增)

CouponCalculator.Examples/            # 示例项目
├── UsageExamples.cs                  # 基础使用示例
├── CompleteWorkflowExamples.cs       # 完整工作流示例
├── RealBusinessIntegrationExamples.cs # 真实业务联调示例 (新增)
├── RealIntegrationWithErrorHandling.cs # 异常处理示例 (新增)
└── INTEGRATION_GUIDE.md             # 接入指南
```

### 3.2 核心类设计

#### CouponCalculatorEngine
主引擎类，提供所有计算能力的入口。

```csharp
public class CouponCalculatorEngine
{
    // 订单上下文管理
    OrderContext CreateOrderContext(string orderId = "");
    void AddItem(OrderContext context, OrderItem item);
    void SetMember(OrderContext context, MemberInfo member);
    void SetShipping(OrderContext context, ShippingInfo shipping);
    
    // 基础优惠计算
    CalculationResult CalculateOptimal(OrderContext context, IEnumerable<Coupon> coupons);
    CouponTrialResult TryApply(OrderContext context, Coupon coupon);
    IEnumerable<CouponTrialResult> GetAvailableCoupons(OrderContext context, IEnumerable<Coupon> coupons);
    
    // 增强优惠计算 (新增)
    EnhancedCalculationResult CalculateOptimalEnhanced(OrderContext context, IEnumerable<Coupon> coupons);
    EnhancedCouponTrialResult TryApplyEnhanced(OrderContext context, Coupon coupon);
    IEnumerable<EnhancedCouponTrialResult> GetAvailableCouponsEnhanced(OrderContext context, IEnumerable<Coupon> coupons);
    
    // 规则管理
    void LoadRules(IRuleProvider ruleProvider);
    
    // 解释和日志
    Explanation Explain(Coupon coupon, OrderContext context);
    EnhancedCouponTrialResult ExplainEnhanced(Coupon coupon, OrderContext context);
    string FormatDetails(CalculationResult result, DisplayFormat format);
    string FormatEnhancedResult(EnhancedCalculationResult result, OrderContext context, DisplayFormat format);
    Dictionary<string, object> GetSettlementPageData(EnhancedCalculationResult result, OrderContext context);
    CalculationLog GetCalculationLog();
    
    // 撤销与重算 (新增)
    void Rollback(OrderContext context);
    void RollbackLastCalculation();
    EnhancedCalculationResult RecalculateWithSameParameters();
}
```

### 3.3 增强的数据模型

#### ApplicableScope - 适用范围配置
```csharp
public class ApplicableScope
{
    public List<string> ProductIds { get; set; }     // 指定商品ID列表
    public List<string> CategoryIds { get; set; }    // 指定类目ID列表
    public List<string> StoreIds { get; set; }       // 指定门店ID列表
    public List<string> BrandIds { get; set; }       // 指定品牌ID列表
    public List<string> TagIds { get; set; }         // 指定标签ID列表
    
    public bool IsAllApplicable { get; }  // 是否全场适用
    public string GetScopeDescription();  // 获取范围描述
}
```

#### ExcludedScope - 排除范围配置
```csharp
public class ExcludedScope
{
    public List<string> ProductIds { get; set; }     // 排除商品ID列表
    public List<string> CategoryIds { get; set; }   // 排除类目ID列表
    public List<string> StoreIds { get; set; }      // 排除门店ID列表
    
    public bool HasAnyExclusion { get; }  // 是否有排除项
    public string GetExclusionDescription();  // 获取排除描述
}
```

#### UsageCondition - 使用条件
```csharp
public class UsageCondition
{
    public decimal? MinOrderAmount { get; set; }           // 最低消费金额
    public int? MinQuantity { get; set; }                 // 最低购买数量
    public decimal? MaxOrderAmount { get; set; }          // 最高消费金额
    public int? MaxQuantity { get; set; }                 // 最高购买数量
    public List<MemberLevel>? AllowedMemberLevels { get; set; }  // 允许的会员等级
    public List<string>? AllowedUserIds { get; set; }     // 允许的用户ID
    public List<string>? AllowedStoreIds { get; set; }    // 允许的门店ID
    public DateTime? ValidFrom { get; set; }              // 开始时间
    public DateTime? ValidTo { get; set; }                // 结束时间
    public int? MaxUsageCount { get; set; }               // 最大使用次数
    public int? MaxUsagePerUser { get; set; }             // 每用户最大使用次数
    public bool? RequireFirstOrder { get; set; }           // 是否限首单
    public decimal? MinDistanceKm { get; set; }           // 最小配送距离
    public decimal? MaxDistanceKm { get; set; }           // 最大配送距离
}
```

#### EnhancedCalculationResult - 增强计算结果
```csharp
public class EnhancedCalculationResult
{
    public string OrderId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal OriginalFreight { get; set; }
    public List<CandidatePlan> CandidatePlans { get; set; }  // 候选方案列表
    public CandidatePlan? RecommendedPlan { get; set; }     // 推荐方案
    public PlanComparison Comparison { get; set; }           // 方案对比
    public CalculationMetadata Metadata { get; set; }        // 计算元数据
}

public class CandidatePlan
{
    public string PlanId { get; set; }
    public string PlanName { get; set; }
    public int Rank { get; set; }
    public bool IsRecommended { get; set; }
    public List<AppliedCouponDetail> AppliedCoupons { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal FinalTotalAmount { get; set; }
    public List<string> Advantages { get; set; }   // 方案优点
    public List<string> Limitations { get; set; }  // 方案限制
}

public class AppliedCouponDetail
{
    public string CouponId { get; set; }
    public string CouponCode { get; set; }
    public string CouponName { get; set; }
    public CouponType Type { get; set; }
    public decimal DiscountAmount { get; set; }
    public string ScopeDescription { get; set; }   // 适用范围描述
    public string Reason { get; set; }              // 应用原因
    public List<string> AppliedConditions { get; set; }
}
```

#### EnhancedCouponTrialResult - 增强试算结果
```csharp
public class EnhancedCouponTrialResult
{
    public Coupon Coupon { get; set; }
    public bool IsAvailable { get; set; }
    public TrialStatus Status { get; set; }
    public string StatusMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public List<TrialCheckResult> CheckResults { get; set; }  // 详细校验结果
    public List<string> UnavailableReasons { get; set; }
    public UserFriendlyExplanation UserExplanation { get; set; }  // 用户友好的解释
}

public class TrialCheckResult
{
    public string CheckName { get; set; }
    public CheckType Type { get; set; }
    public bool Passed { get; set; }
    public string Message { get; set; }
    public string UserFriendlyMessage { get; set; }  // 用户可见的消息
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

### 5.2 增强算法
- **多方案生成**: 生成多个候选方案（最大优惠、简约、叠加最优等）
- **方案对比**: 提供各方案的优缺点说明
- **推荐方案**: 自动推荐最优方案

### 5.3 约束条件
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
- **新增**: 边界场景测试（空订单、零金额、刚好门槛、撤销重算）
- **新增**: 增强功能测试（多方案、详细试算、适用范围）

### 6.2 测试项目
创建 `CouponCalculator.Tests` 项目，使用 xUnit 框架。

---

## 7. API 使用示例

### 7.1 基础使用

```csharp
var engine = new CouponCalculatorEngine();

var context = engine.CreateOrderContext("ORDER123456");
context.AddItem(new OrderItem 
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

var coupons = new List<Coupon>
{
    new Coupon 
    { 
        CouponId = "C1", 
        Type = CouponType.AmountOff, 
        DiscountValue = 20, 
        MinOrderAmount = 100 
    },
    new Coupon 
    { 
        CouponId = "C2", 
        Type = CouponType.DiscountRate, 
        DiscountValue = 0.9m, 
        MinOrderAmount = 50 
    },
    new Coupon 
    { 
        CouponId = "C3", 
        Type = CouponType.FreeShipping 
    }
};

var available = engine.GetAvailableCoupons(context, coupons);
var result = engine.CalculateOptimal(context, coupons);
var display = engine.FormatDetails(result, DisplayFormat.Detailed);
var log = engine.GetCalculationLog();
```

### 7.2 增强使用（多方案对比）

```csharp
var result = engine.CalculateOptimalEnhanced(context, coupons);

// 获取所有候选方案
foreach (var plan in result.CandidatePlans)
{
    Console.WriteLine($"{plan.PlanName}: 节省 ¥{plan.TotalDiscountAmount:F2}");
}

// 获取推荐方案
var recommended = result.RecommendedPlan;

// 获取结算页面数据
var settlementData = engine.GetSettlementPageData(result, context);

// 格式化输出
var formatted = engine.FormatEnhancedResult(result, context, DisplayFormat.Bill);
```

### 7.3 灵活适用范围配置

```csharp
var coupon = new Coupon
{
    CouponId = "C1",
    CouponCode = "MIXED-SCOPE",
    Type = CouponType.DiscountRate,
    DiscountValue = 0.8m,
    
    // 适用范围
    ApplicableScope = new ApplicableScope
    {
        ProductIds = new List<string> { "SKU-001", "SKU-002" },
        CategoryIds = new List<string> { "CATE-001" }
    },
    
    // 排除范围
    ExcludedScope = new ExcludedScope
    {
        ProductIds = new List<string> { "SKU-EXCLUDE" }
    },
    
    // 使用条件
    UsageCondition = new UsageCondition
    {
        MinOrderAmount = 100m,
        AllowedMemberLevels = new List<MemberLevel> 
        { 
            MemberLevel.Gold, 
            MemberLevel.Platinum, 
            MemberLevel.Diamond 
        }
    }
};
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
- [x] **新增**: 支持灵活的适用范围配置
- [x] **新增**: 返回多个候选方案
- [x] **新增**: 提供方案对比说明
- [x] **新增**: 生成用户友好的解释文案
- [x] **新增**: 边界条件正确处理
- [x] **新增**: 撤销后重算保持一致

### 8.2 质量验收
- [x] 代码编译无错误
- [x] 核心功能有单元测试覆盖
- [x] API 设计清晰易用
- [x] 注释完整规范
- [x] 支持扩展规则和提供者
- [x] **新增**: 完整的工作流示例
- [x] **新增**: 详细的接入指南

---

## 9. 扩展性设计

### 9.1 规则扩展
- 实现 `IRuleProvider` 接口添加自定义规则
- 继承 `BaseRule` 创建新规则类型

### 9.2 格式化扩展
- 实现 `IResultFormatter` 接口自定义展示格式

### 9.3 验证扩展
- 实现 `ICouponValidator` 接口自定义验证逻辑

### 9.4 优化器扩展
- 实现自定义的组合优化算法

---

## 10. 文档

### 10.1 示例项目
`CouponCalculator.Examples` 项目包含：
- `UsageExamples.cs` - 基础使用示例
- `CompleteWorkflowExamples.cs` - 完整工作流演示

### 10.2 接入指南
`INTEGRATION_GUIDE.md` 包含：
- 快速开始指南
- 完整功能清单
- 数据结构说明
- 使用场景示例
- 最佳实践
- 常见问题

---

## 11. 高级运营功能

### 11.1 规则版本管理
- 版本切换：支持同一批券规则多个版本共存
- 定时切换：配置定时生效计划
- 差异预览：切换前后查看规则差异
- 审批流程：支持发布审批流程
- **新增模型**: `RuleVersion`, `VersionSchedule`, `VersionDiff`

### 11.2 灰度发布
- 多维度灰度：支持按店铺、人群、渠道灰度放量
- 百分比灰度：按用户Hash实现精准百分比控制
- 标签灰度：支持特定标签用户优先体验
- 区域灰度：按地域进行灰度发布
- 发布前回放：支持批量回放历史订单验证效果
- 差异对比：查看不同灰度范围的结算差异
- **新增模型**: `GrayReleasePolicy`, `GrayScaleConfig`, `GrayTarget`, `GrayPlaybackResult`, `BatchPlaybackResult`
- **新增服务**: `GrayReleaseManager`

### 11.3 口径模板与回放
- 快照保存：支持保存试算、下单、退款完整口径
- 一键回放：同类订单可快速回放历史口径
- 多视角说明：统一口径供客服、运营、财务查看
- 相似模板：自动查找相似订单模板
- 模板对比：对比两个模板的差异
- **新增模型**: `CalculationTemplate`, `TemplateSnapshot`, `UnifiedExplanation`, `TemplatePlaybackResult`
- **新增服务**: `TemplatePlaybackService`, `TemplateManagementService`

### 11.4 批量对账
- 按天对账：支持每日批量订单对账
- 按活动对账：支持按营销活动批量对账
- 差异筛选：筛选结算前后差异最大的订单
- 异常检测：自动识别异常订单
- 分组统计：按渠道、店铺、会员等级分组统计
- 运营导出：支持CSV/Excel/JSON/HTML/PDF多种导出格式
- **新增模型**: `BatchReconciliationRequest`, `BatchReconciliationResult`, `ReconciliationSummary`, `AnomalyOrder`
- **新增服务**: `BatchReconciliationService`, `ExportService`

### 11.5 商品级分摊
- 商品级明细：每件商品分摊优惠券、会员折扣
- 退款回放：退款时按同一口径重算
- 多时点对比：试算、下单、支付前复算对比
- 差异说明：生成运营可懂的差异报告
- **新增模型**: `ItemLevelDiscountBreakdown`, `ItemDiscountAllocation`, `ReconciliationRecord`

---

## 12. 真实联调示例

### 12.1 异常处理示例
`RealIntegrationWithErrorHandling.cs` 包含：
- 正常流程：完整结算流程
- 接口超时：超时降级处理
- 字段缺失：缺失字段检测和警告
- 券状态变化：券状态变化检测
- 部分失败：批量处理部分失败处理
- 重试机制：失败自动重试

### 12.2 真实业务示例
`RealBusinessIntegrationExamples.cs` 包含：
- 从外部加载商品数据
- 从外部加载会员信息
- 从外部加载优惠券
- 完整结算流程集成

---

## 13. 版本历史

### v2.0 (当前版本)
#### Phase 1 - 基础能力
- **增强规则配置**: 支持商品、类目、门店、品牌、标签等多维度配置
- **增强适用范围**: 支持正向指定和反向排除
- **增强最优组合**: 返回多个候选方案及对比说明
- **增强试算结果**: 返回详细的校验过程和用户友好的解释
- **增强撤销重算**: 支持撤销和相同参数重算
- **完整边界测试**: 覆盖空订单、零金额、刚好门槛等场景
- **完整示例文档**: 提供工作流示例和接入指南

#### Phase 2 - 规则管理
- **规则版本管理**: 支持版本切换、定时生效、差异预览
- **商品级分摊**: 支持优惠券、会员折扣分摊到商品明细
- **多时点对账**: 试算、下单、支付前复算对比

#### Phase 3 - 灰度与模板
- **灰度发布**: 支持按店铺、人群、渠道灰度放量
- **批量回放**: 支持发布前批量回放历史订单
- **口径模板**: 支持保存快照模板和一键回放
- **统一口径**: 客服、运营、财务同一套说明

#### Phase 4 - 批量对账与联调
- **批量对账**: 支持按天/按活动批量对账
- **异常检测**: 自动识别差异最大的异常订单
- **运营导出**: CSV/Excel/JSON/HTML/PDF多种格式
- **异常联调**: 接口超时、字段缺失、券状态变化处理
- **真实业务示例**: 外部数据加载完整示例
