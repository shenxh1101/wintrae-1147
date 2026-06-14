using CouponCalculator.Formatters;
using CouponCalculator.Logs;
using CouponCalculator.Models;
using CouponCalculator.Services;

namespace CouponCalculator.Engine;

public class CouponCalculatorEngine
{
    private readonly ICouponValidator _validator;
    private readonly EnhancedCouponValidator _enhancedValidator;
    private readonly IStackingEngine _stackingEngine;
    private readonly ExplanationService _explanationService;
    private readonly EnhancedExplanationService _enhancedExplanationService;
    private readonly CalculationLogger _logger;
    private readonly CombinationOptimizer _optimizer;
    private readonly EnhancedCombinationOptimizer _enhancedOptimizer;
    private IRuleProvider _ruleProvider;
    private CalculationLog? _currentLog;
    private OrderContext? _lastContext;
    private List<Coupon>? _lastCoupons;
    private EnhancedCalculationResult? _lastResult;

    public CouponCalculatorEngine()
    {
        _validator = new CouponValidator();
        _enhancedValidator = new EnhancedCouponValidator();
        _stackingEngine = new StackingEngine();
        _explanationService = new ExplanationService();
        _enhancedExplanationService = new EnhancedExplanationService();
        _logger = new CalculationLogger();
        _optimizer = new CombinationOptimizer(_stackingEngine);
        _enhancedOptimizer = new EnhancedCombinationOptimizer(_stackingEngine);
        _ruleProvider = new DefaultRuleProvider();
    }

    public CouponCalculatorEngine(IRuleProvider ruleProvider)
    {
        _validator = new CouponValidator();
        _enhancedValidator = new EnhancedCouponValidator();
        _stackingEngine = new StackingEngine();
        _explanationService = new ExplanationService();
        _enhancedExplanationService = new EnhancedExplanationService();
        _logger = new CalculationLogger();
        _optimizer = new CombinationOptimizer(_stackingEngine);
        _enhancedOptimizer = new EnhancedCombinationOptimizer(_stackingEngine);
        _ruleProvider = ruleProvider;
    }

    public void LoadRules(IRuleProvider ruleProvider)
    {
        _ruleProvider = ruleProvider;
    }

    public OrderContext CreateOrderContext(string orderId = "")
    {
        return new OrderContext
        {
            OrderId = string.IsNullOrEmpty(orderId) ? Guid.NewGuid().ToString() : orderId
        };
    }

    public void AddItem(OrderContext context, OrderItem item)
    {
        context.AddItem(item);
    }

    public void SetMember(OrderContext context, MemberInfo member)
    {
        context.Member = member;
    }

    public void SetShipping(OrderContext context, ShippingInfo shipping)
    {
        context.Shipping = shipping;
        context.FreightAmount = shipping.CalculateFreight();
    }

    public CouponTrialResult TryApply(OrderContext context, Coupon coupon)
    {
        _currentLog = _logger.CreateLog(context.OrderId);
        _logger.AddEntry(_currentLog, coupon.CouponId, "Start", $"开始试算优惠券 {coupon.CouponCode}", true);

        var result = new CouponTrialResult
        {
            Coupon = coupon,
            CurrentAmount = context.OriginalAmount
        };

        var validationResult = _validator.Validate(coupon, context);
        if (!validationResult.IsValid)
        {
            result.IsAvailable = false;
            result.UnavailableReasons = validationResult.Errors;
            _logger.AddEntry(_currentLog, coupon.CouponId, "Validation", "优惠券校验失败", false);
            return result;
        }

        _logger.AddEntry(_currentLog, coupon.CouponId, "Validation", "优惠券校验通过", true);

        var discountAmount = CalculateCouponDiscount(coupon, context);
        result.DiscountAmount = discountAmount;
        result.AmountAfterDiscount = context.OriginalAmount - discountAmount;
        result.IsAvailable = true;

        if (coupon.Type == CouponType.FreeShipping)
        {
            result.DiscountAmount = context.FreightAmount;
        }

        result.ApplicableProductIds = context.Items
            .Where(item => coupon.IsApplicableToProduct(item.ProductId) && coupon.IsApplicableToCategory(item.CategoryId))
            .Select(item => item.ProductId)
            .ToList();

        _logger.AddEntry(_currentLog, coupon.CouponId, "Calculate", $"计算优惠金额: {discountAmount:F2}", true, discountAmount);

        return result;
    }

    public EnhancedCouponTrialResult TryApplyEnhanced(OrderContext context, Coupon coupon)
    {
        _currentLog = _logger.CreateLog(context.OrderId);
        _logger.AddEntry(_currentLog, coupon.CouponId, "Start", $"开始增强试算优惠券 {coupon.CouponCode}", true);

        var result = _enhancedValidator.GetDetailedTrialResult(coupon, context);

        if (result.IsAvailable)
        {
            result.DiscountAmount = CalculateCouponDiscount(coupon, context);
            
            if (coupon.Type == CouponType.FreeShipping)
            {
                result.DiscountAmount = context.FreightAmount;
            }
        }

        _logger.AddEntry(_currentLog, coupon.CouponId, "Validation", 
            result.IsAvailable ? "优惠券可用" : "优惠券不可用", result.IsAvailable, result.DiscountAmount);

        return result;
    }

    public IEnumerable<CouponTrialResult> GetAvailableCoupons(OrderContext context, IEnumerable<Coupon> coupons)
    {
        var results = new List<CouponTrialResult>();

        foreach (var coupon in coupons)
        {
            var trialResult = TryApply(context, coupon);
            results.Add(trialResult);
        }

        return results;
    }

    public IEnumerable<EnhancedCouponTrialResult> GetAvailableCouponsEnhanced(
        OrderContext context, 
        IEnumerable<Coupon> coupons)
    {
        return coupons.Select(c => TryApplyEnhanced(context, c)).ToList();
    }

    public CalculationResult CalculateOptimal(OrderContext context, IEnumerable<Coupon> coupons)
    {
        _currentLog = _logger.CreateLog(context.OrderId);
        var couponList = coupons.ToList();

        var result = new CalculationResult
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreight = context.FreightAmount
        };

        _currentLog.OriginalAmount = result.OriginalAmount;
        _currentLog.CouponsTested = couponList.Count;

        var trialResults = GetAvailableCoupons(context, couponList).ToList();
        var availableCoupons = trialResults
            .Where(r => r.IsAvailable && r.DiscountAmount > 0)
            .Select(r => r.Coupon)
            .ToList();

        var rejectedCoupons = trialResults
            .Where(r => !r.IsAvailable)
            .Select(r => new RejectedExplanation
            {
                CouponId = r.Coupon.CouponId,
                CouponCode = r.Coupon.CouponCode,
                Reason = "无法使用该优惠券",
                FailedConditions = r.UnavailableReasons
            })
            .ToList();

        result.Explanation.RejectedReasons = rejectedCoupons;

        var optimizedCoupons = _optimizer.Optimize(
            availableCoupons,
            context,
            (coupon, ctx) => CalculateCouponDiscount(coupon, ctx)
        );

        foreach (var coupon in optimizedCoupons)
        {
            _logger.AddEntry(_currentLog, coupon.CouponId, "Apply", $"应用优惠券 {coupon.CouponCode}", true);
        }

        decimal productDiscount = 0;
        decimal freightDiscount = 0;
        decimal memberDiscount = 0;

        foreach (var coupon in optimizedCoupons)
        {
            var discount = CalculateCouponDiscount(coupon, context);
            
            if (coupon.Type == CouponType.FreeShipping)
            {
                freightDiscount = context.FreightAmount;
                result.FinalFreight = 0;

                var applied = new AppliedCoupon
                {
                    CouponId = coupon.CouponId,
                    CouponCode = coupon.CouponCode,
                    Type = coupon.Type,
                    DiscountAmount = freightDiscount,
                    Description = "免运费"
                };
                result.AppliedCoupons.Add(applied);
            }
            else if (coupon.Type == CouponType.MemberExclusive)
            {
                memberDiscount += discount;

                var applied = new AppliedCoupon
                {
                    CouponId = coupon.CouponId,
                    CouponCode = coupon.CouponCode,
                    Type = coupon.Type,
                    DiscountAmount = discount,
                    Description = "会员专享折扣"
                };
                result.AppliedCoupons.Add(applied);
            }
            else
            {
                productDiscount += discount;

                var applied = new AppliedCoupon
                {
                    CouponId = coupon.CouponId,
                    CouponCode = coupon.CouponCode,
                    Type = coupon.Type,
                    DiscountAmount = discount,
                    Description = GetCouponDescription(coupon)
                };
                result.AppliedCoupons.Add(applied);
            }

            var explanation = _explanationService.ExplainAppliedCoupon(coupon, context, discount);
            result.Explanation.AppliedReasons.AddRange(explanation.AppliedReasons);
        }

        result.ProductDiscountAmount = productDiscount;
        result.FreightDiscount = freightDiscount;
        result.MemberDiscount = memberDiscount;
        result.FinalAmount = result.OriginalAmount - productDiscount - memberDiscount + result.FinalFreight;

        if (context.Member != null && memberDiscount == 0)
        {
            var memberDiscountResult = CalculateMemberDiscount(context);
            if (memberDiscountResult > 0)
            {
                result.MemberDiscount = memberDiscountResult;
                result.FinalAmount -= memberDiscountResult;
            }
        }

        _currentLog.FinalAmount = result.FinalAmount;
        _currentLog.CouponsApplied = result.AppliedCoupons.Count;

        return result;
    }

    public EnhancedCalculationResult CalculateOptimalEnhanced(
        OrderContext context, 
        IEnumerable<Coupon> coupons)
    {
        _lastContext = context;
        _lastCoupons = coupons.ToList();

        _currentLog = _logger.CreateLog(context.OrderId);
        var couponList = _lastCoupons;

        _currentLog.OriginalAmount = context.OriginalAmount;
        _currentLog.CouponsTested = couponList.Count;

        var trialResults = GetAvailableCouponsEnhanced(context, couponList).ToList();
        var availableCoupons = trialResults
            .Where(r => r.IsAvailable && r.DiscountAmount > 0)
            .Select(r => r.Coupon)
            .ToList();

        var memberDiscounts = new List<MemberDiscountDetail>();
        if (context.Member != null)
        {
            var memberDiscountAmount = CalculateMemberDiscount(context);
            if (memberDiscountAmount > 0)
            {
                memberDiscounts.Add(new MemberDiscountDetail
                {
                    MemberLevel = context.Member.Level,
                    MemberLevelName = GetMemberLevelName(context.Member.Level),
                    DiscountRate = context.Member.GetDiscountRate(),
                    DiscountAmount = memberDiscountAmount,
                    Reason = $"您的{GetMemberLevelName(context.Member.Level)}可享受 {(1 - context.Member.GetDiscountRate()) * 100:F0}% 折扣"
                });
            }
        }

        var freightDiscounts = new List<FreightDiscountDetail>();
        var freeShippingCoupons = trialResults
            .Where(r => r.IsAvailable && r.Coupon.Type == CouponType.FreeShipping)
            .ToList();

        if (freeShippingCoupons.Any())
        {
            freightDiscounts.Add(new FreightDiscountDetail
            {
                OriginalFreight = context.FreightAmount,
                DiscountAmount = context.FreightAmount,
                FinalFreight = 0,
                Reason = "使用免运费券"
            });
        }

        _lastResult = _enhancedOptimizer.Optimize(
            context,
            availableCoupons,
            memberDiscounts,
            freightDiscounts,
            (coupon, ctx) => CalculateCouponDiscount(coupon, ctx)
        );

        foreach (var plan in _lastResult.CandidatePlans)
        {
            foreach (var coupon in plan.AppliedCoupons)
            {
                _logger.AddEntry(_currentLog, coupon.CouponId, "Apply", 
                    $"应用优惠券 {coupon.CouponCode} 于方案 {plan.PlanName}", true, coupon.DiscountAmount);
            }
        }

        var explanation = _enhancedExplanationService.GenerateExplanation(_lastResult, context, trialResults);
        
        return _lastResult;
    }

    public Explanation Explain(Coupon coupon, OrderContext context)
    {
        var validationResult = _validator.Validate(coupon, context);

        if (validationResult.IsValid)
        {
            var discount = CalculateCouponDiscount(coupon, context);
            return _explanationService.ExplainAppliedCoupon(coupon, context, discount);
        }
        else
        {
            return _explanationService.ExplainRejectedCoupon(coupon, context, validationResult.Errors);
        }
    }

    public EnhancedCouponTrialResult ExplainEnhanced(Coupon coupon, OrderContext context)
    {
        return TryApplyEnhanced(context, coupon);
    }

    public string FormatDetails(CalculationResult result, DisplayFormat format)
    {
        IResultFormatter formatter = format switch
        {
            DisplayFormat.Simple => new SimpleFormatter(),
            DisplayFormat.Detailed => new DetailedFormatter(),
            DisplayFormat.Bill => new BillFormatter(),
            _ => new SimpleFormatter()
        };

        return formatter.Format(result);
    }

    public string FormatEnhancedResult(EnhancedCalculationResult result, OrderContext context, DisplayFormat format)
    {
        return format switch
        {
            DisplayFormat.Simple => _enhancedExplanationService.GenerateUserFriendlySummary(result, context),
            DisplayFormat.Detailed => _enhancedExplanationService.GenerateComparisonTable(result),
            DisplayFormat.Bill => FormatEnhancedBill(result, context),
            _ => _enhancedExplanationService.GenerateUserFriendlySummary(result, context)
        };
    }

    public Dictionary<string, object> GetSettlementPageData(EnhancedCalculationResult result, OrderContext context)
    {
        return _enhancedExplanationService.GenerateSettlementPageData(result, context);
    }

    public CalculationLog GetCalculationLog()
    {
        return _currentLog ?? new CalculationLog();
    }

    public void Rollback(OrderContext context)
    {
        context.ClearAppliedCoupons();
    }

    public void RollbackLastCalculation()
    {
        if (_lastContext != null)
        {
            Rollback(_lastContext);
        }
    }

    public EnhancedCalculationResult RecalculateWithSameParameters()
    {
        if (_lastContext == null || _lastCoupons == null)
        {
            throw new InvalidOperationException("没有可重新计算的上下文，请先调用 CalculateOptimalEnhanced");
        }

        return CalculateOptimalEnhanced(_lastContext, _lastCoupons);
    }

    private decimal CalculateCouponDiscount(Coupon coupon, OrderContext context)
    {
        var applicableAmount = context.GetApplicableAmountForCoupon(coupon);

        switch (coupon.Type)
        {
            case CouponType.AmountOff:
                return coupon.DiscountValue;

            case CouponType.DiscountRate:
                var discount = applicableAmount * (1 - coupon.DiscountValue);
                if (coupon.MaxDiscountAmount.HasValue)
                {
                    discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
                }
                return discount;

            case CouponType.QuantityDiscount:
                var qtyDiscount = applicableAmount * (1 - coupon.DiscountValue);
                if (coupon.MaxDiscountAmount.HasValue)
                {
                    qtyDiscount = Math.Min(qtyDiscount, coupon.MaxDiscountAmount.Value);
                }
                return qtyDiscount;

            case CouponType.FreeShipping:
                return context.FreightAmount;

            case CouponType.MemberExclusive:
                return applicableAmount * (1 - coupon.DiscountValue);

            default:
                return 0;
        }
    }

    private decimal CalculateMemberDiscount(OrderContext context)
    {
        if (context.Member == null)
            return 0;

        var discountRate = context.Member.GetDiscountRate();
        if (discountRate >= 1.0m)
            return 0;

        return context.OriginalAmount * (1 - discountRate);
    }

    private static string GetCouponDescription(Coupon coupon)
    {
        return coupon.Type switch
        {
            CouponType.AmountOff => $"满{coupon.MinOrderAmount:F0}减{coupon.DiscountValue:F0}",
            CouponType.DiscountRate => $"{(1 - coupon.DiscountValue) * 100:F0}%折扣",
            CouponType.QuantityDiscount => $"满{coupon.MinQuantity}件{(1 - coupon.DiscountValue) * 100:F0}折",
            CouponType.MemberExclusive => "会员专享",
            _ => coupon.CouponCode
        };
    }

    private static string GetMemberLevelName(MemberLevel level)
    {
        return level switch
        {
            MemberLevel.Normal => "普通会员",
            MemberLevel.Silver => "银卡会员",
            MemberLevel.Gold => "金卡会员",
            MemberLevel.Platinum => "白金会员",
            MemberLevel.Diamond => "钻石会员",
            _ => level.ToString()
        };
    }

    private string FormatEnhancedBill(EnhancedCalculationResult result, OrderContext context)
    {
        var lines = new List<string>();
        var width = 45;

        lines.Add(AlignCenter("● 订单优惠账单 ●", width));
        lines.Add(new string('═', width));
        lines.Add($"  订单号: {result.OrderId}");
        lines.Add($"  计算时间: {result.CalculatedAt:yyyy-MM-dd HH:mm:ss}");
        lines.Add(new string('─', width));

        lines.Add(AlignLeft("【商品金额】", width));
        lines.Add(AlignRight($"¥{result.OriginalAmount:F2}", width));

        if (result.OriginalFreight > 0)
        {
            lines.Add(AlignLeft("【运费】", width));
            lines.Add(AlignRight($"¥{result.OriginalFreight:F2}", width));
        }

        lines.Add(new string('─', width));

        if (result.CandidatePlans.Any())
        {
            var plan = result.CandidatePlans.First();
            
            if (plan.ProductDiscountAmount > 0)
            {
                lines.Add(AlignLeft("【商品优惠】", width));
                foreach (var coupon in plan.AppliedCoupons.Where(c => c.Type != CouponType.FreeShipping))
                {
                    lines.Add($"  · {coupon.CouponCode}");
                    lines.Add(AlignRight($"-¥{coupon.DiscountAmount:F2}", width));
                }
            }

            if (plan.FreightDiscountAmount > 0)
            {
                lines.Add(AlignLeft("【运费优惠】", width));
                lines.Add(AlignRight($"-¥{plan.FreightDiscountAmount:F2}", width));
            }

            if (plan.MemberDiscountAmount > 0)
            {
                lines.Add(AlignLeft("【会员折扣】", width));
                lines.Add(AlignRight($"-¥{plan.MemberDiscountAmount:F2}", width));
            }
        }

        lines.Add(new string('═', width));
        lines.Add(AlignLeft("应付总额", width, true));
        lines.Add(AlignRight($"¥{result.RecommendedPlan?.FinalTotalAmount ?? (result.OriginalAmount + result.OriginalFreight):F2}", width, true));

        if (result.CandidatePlans.Count > 1)
        {
            lines.Add("");
            lines.Add(AlignCenter("【其他可用方案】", width));
            lines.Add(new string('─', width));

            foreach (var otherPlan in result.CandidatePlans.Skip(1).Take(3))
            {
                lines.Add($"{otherPlan.PlanName}: 节省 ¥{otherPlan.TotalDiscountAmount:F2} → ¥{otherPlan.FinalTotalAmount:F2}");
            }
        }

        lines.Add(new string('═', width));

        return string.Join(Environment.NewLine, lines);
    }

    private static string AlignLeft(string text, int width, bool bold = false)
    {
        var prefix = bold ? "★ " : "  ";
        var padding = width - GetDisplayLength(text) - 2;
        return prefix + text + new string(' ', Math.Max(0, padding));
    }

    private static string AlignRight(string text, int width, bool bold = false)
    {
        var prefix = bold ? "★ " : "  ";
        var padding = width - GetDisplayLength(text) - 2;
        return prefix + new string(' ', Math.Max(0, padding)) + text;
    }

    private static string AlignCenter(string text, int width)
    {
        var padding = (width - GetDisplayLength(text)) / 2;
        return new string(' ', Math.Max(0, padding)) + text;
    }

    private static int GetDisplayLength(string text)
    {
        return text.Length;
    }
}
