using CouponCalculator.Formatters;
using CouponCalculator.Logs;
using CouponCalculator.Models;
using CouponCalculator.Services;

namespace CouponCalculator.Engine;

public class CouponCalculatorEngine
{
    private readonly ICouponValidator _validator;
    private readonly IStackingEngine _stackingEngine;
    private readonly ExplanationService _explanationService;
    private readonly CalculationLogger _logger;
    private readonly CombinationOptimizer _optimizer;
    private IRuleProvider _ruleProvider;
    private CalculationLog? _currentLog;

    public CouponCalculatorEngine()
    {
        _validator = new CouponValidator();
        _stackingEngine = new StackingEngine();
        _explanationService = new ExplanationService();
        _logger = new CalculationLogger();
        _optimizer = new CombinationOptimizer(_stackingEngine);
        _ruleProvider = new DefaultRuleProvider();
    }

    public CouponCalculatorEngine(IRuleProvider ruleProvider)
    {
        _validator = new CouponValidator();
        _stackingEngine = new StackingEngine();
        _explanationService = new ExplanationService();
        _logger = new CalculationLogger();
        _optimizer = new CombinationOptimizer(_stackingEngine);
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

        var optimizedCoupons = _optimizer.GreedyOptimize(
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

    public CalculationLog GetCalculationLog()
    {
        return _currentLog ?? new CalculationLog();
    }

    public void Rollback(OrderContext context)
    {
        context.ClearAppliedCoupons();
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
}
