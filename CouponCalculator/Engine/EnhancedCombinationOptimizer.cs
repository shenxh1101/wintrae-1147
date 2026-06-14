using CouponCalculator.Models;
using CouponCalculator.Services;

namespace CouponCalculator.Engine;

public class EnhancedCombinationOptimizer
{
    private readonly IStackingEngine _stackingEngine;

    public EnhancedCombinationOptimizer(IStackingEngine stackingEngine)
    {
        _stackingEngine = stackingEngine;
    }

    public EnhancedCalculationResult Optimize(
        OrderContext context,
        List<Coupon> availableCoupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var result = new EnhancedCalculationResult
        {
            OrderId = context.OrderId,
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount
        };

        var startTime = DateTime.Now;
        var candidatePlans = new List<CandidatePlan>();

        var validCombinations = _stackingEngine.GetValidCombinations(availableCoupons);

        if (validCombinations.Count > 0 && validCombinations.Count <= 100)
        {
            foreach (var combination in validCombinations)
            {
                var plan = BuildPlan(context, combination, memberDiscounts, freightDiscounts, calculateDiscount);
                if (plan.TotalDiscountAmount > 0 || combination.Count > 0)
                {
                    candidatePlans.Add(plan);
                }
            }
        }
        else
        {
            candidatePlans.AddRange(GenerateGreedyPlans(context, availableCoupons, memberDiscounts, freightDiscounts, calculateDiscount));
        }

        if (memberDiscounts.Any() && !candidatePlans.Any(p => p.MemberDiscount != null))
        {
            var memberOnlyPlan = BuildMemberOnlyPlan(context, memberDiscounts, freightDiscounts);
            candidatePlans.Add(memberOnlyPlan);
        }

        var distinctPlans = candidatePlans
            .GroupBy(p => $"{p.TotalDiscountAmount:F2}-{p.AppliedCoupons.Count}")
            .Select(g => g.First())
            .ToList();

        distinctPlans = distinctPlans
            .OrderByDescending(p => p.TotalDiscountAmount)
            .ThenBy(p => p.AppliedCoupons.Count)
            .Take(5)
            .ToList();

        for (int i = 0; i < distinctPlans.Count; i++)
        {
            distinctPlans[i].Rank = i + 1;
            distinctPlans[i].IsRecommended = i == 0;
            distinctPlans[i].PlanName = $"方案{i + 1}";
        }

        result.CandidatePlans = distinctPlans;
        result.RecommendedPlan = distinctPlans.FirstOrDefault(p => p.IsRecommended);

        result.Comparison = BuildComparison(context, distinctPlans, availableCoupons);

        result.Metadata = new CalculationMetadata
        {
            TotalCouponsProvided = availableCoupons.Count,
            TotalCouponsAvailable = availableCoupons.Count,
            TotalCouponsRejected = availableCoupons.Count - candidatePlans.SelectMany(p => p.AppliedCoupons).Select(c => c.CouponId).Distinct().Count(),
            PlansGenerated = distinctPlans.Count,
            CalculationTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds
        };

        return result;
    }

    private List<CandidatePlan> GenerateGreedyPlans(
        OrderContext context,
        List<Coupon> coupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var plans = new List<CandidatePlan>();

        var sortedByDiscount = coupons.OrderByDescending(c => c.DiscountValue).ToList();
        var greedyPlan = GreedySelect(context, sortedByDiscount, memberDiscounts, freightDiscounts, calculateDiscount);
        plans.Add(greedyPlan);

        var sortedByPriority = coupons.OrderByDescending(c => c.Priority).ThenByDescending(c => c.DiscountValue).ToList();
        var priorityPlan = GreedySelect(context, sortedByPriority, memberDiscounts, freightDiscounts, calculateDiscount);
        if (IsDifferentPlan(greedyPlan, priorityPlan))
        {
            priorityPlan.PlanName = "方案2";
            plans.Add(priorityPlan);
        }

        var stackablePlan = BuildStackablePlan(context, coupons, memberDiscounts, freightDiscounts, calculateDiscount);
        if (IsDifferentPlan(greedyPlan, stackablePlan) && IsDifferentPlan(priorityPlan, stackablePlan))
        {
            stackablePlan.PlanName = "方案3";
            plans.Add(stackablePlan);
        }

        var simplestPlan = BuildSimplestPlan(context, coupons, memberDiscounts, freightDiscounts, calculateDiscount);
        if (IsDifferentPlan(greedyPlan, simplestPlan) && 
            IsDifferentPlan(priorityPlan, simplestPlan) && 
            IsDifferentPlan(stackablePlan, simplestPlan))
        {
            simplestPlan.PlanName = "方案4";
            plans.Add(simplestPlan);
        }

        return plans;
    }

    private CandidatePlan GreedySelect(
        OrderContext context,
        List<Coupon> sortedCoupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var plan = new CandidatePlan
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreightAmount = context.FreightAmount,
            PlanName = "最大优惠方案"
        };

        var tempContext = CloneContext(context);
        var appliedCoupons = new List<Coupon>();

        foreach (var coupon in sortedCoupons)
        {
            var canStack = appliedCoupons.All(c => _stackingEngine.CanStack(coupon, c));
            if (canStack)
            {
                var discount = calculateDiscount(coupon, tempContext);
                if (discount > 0)
                {
                    appliedCoupons.Add(coupon);
                    var applicableAmount = tempContext.GetApplicableAmountForCoupon(coupon);
                    
                    plan.AppliedCoupons.Add(new AppliedCouponDetail
                    {
                        CouponId = coupon.CouponId,
                        CouponCode = coupon.CouponCode,
                        CouponName = coupon.CouponName,
                        Type = coupon.Type,
                        DiscountAmount = discount,
                        OriginalAmount = tempContext.OriginalAmount,
                        ApplicableAmount = applicableAmount,
                        ScopeDescription = coupon.GetScopeDescription(),
                        Reason = GetCouponApplyReason(coupon, discount, applicableAmount)
                    });

                    if (coupon.Type == CouponType.FreeShipping)
                    {
                        plan.FreightDiscountAmount = context.FreightAmount;
                        plan.FinalFreightAmount = 0;
                    }
                    else
                    {
                        plan.ProductDiscountAmount += discount;
                    }
                }
            }
        }

        if (memberDiscounts.Any())
        {
            var memberDiscount = memberDiscounts.First();
            plan.MemberDiscount = memberDiscount;
            plan.MemberDiscountAmount = memberDiscount.DiscountAmount;
        }

        if (plan.FreightDiscountAmount == 0 && freightDiscounts.Any())
        {
            var freightDiscount = freightDiscounts.First();
            plan.FreightDiscount = freightDiscount;
            plan.FreightDiscountAmount = freightDiscount.DiscountAmount;
            plan.FinalFreightAmount = freightDiscount.FinalFreight;
        }

        plan.Advantages.Add($"可节省 {plan.TotalDiscountAmount:F2} 元");
        plan.Advantages.Add($"使用 {plan.AppliedCoupons.Count} 张优惠券");

        if (plan.TotalDiscountAmount == 0)
        {
            plan.Limitations.Add("没有可用的优惠券");
        }

        return plan;
    }

    private CandidatePlan BuildStackablePlan(
        OrderContext context,
        List<Coupon> coupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var plan = new CandidatePlan
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreightAmount = context.FreightAmount,
            PlanName = "叠加最优方案"
        };

        var stackableCoupons = _stackingEngine.GetStackableCoupons(coupons);
        var tempContext = CloneContext(context);

        foreach (var coupon in stackableCoupons)
        {
            var discount = calculateDiscount(coupon, tempContext);
            if (discount > 0)
            {
                var applicableAmount = tempContext.GetApplicableAmountForCoupon(coupon);
                
                plan.AppliedCoupons.Add(new AppliedCouponDetail
                {
                    CouponId = coupon.CouponId,
                    CouponCode = coupon.CouponCode,
                    CouponName = coupon.CouponName,
                    Type = coupon.Type,
                    DiscountAmount = discount,
                    OriginalAmount = tempContext.OriginalAmount,
                    ApplicableAmount = applicableAmount,
                    ScopeDescription = coupon.GetScopeDescription(),
                    Reason = GetCouponApplyReason(coupon, discount, applicableAmount)
                });

                if (coupon.Type == CouponType.FreeShipping)
                {
                    plan.FreightDiscountAmount = context.FreightAmount;
                    plan.FinalFreightAmount = 0;
                }
                else
                {
                    plan.ProductDiscountAmount += discount;
                }
            }
        }

        if (memberDiscounts.Any())
        {
            var memberDiscount = memberDiscounts.First();
            plan.MemberDiscount = memberDiscount;
            plan.MemberDiscountAmount = memberDiscount.DiscountAmount;
        }

        if (plan.FreightDiscountAmount == 0 && freightDiscounts.Any())
        {
            var freightDiscount = freightDiscounts.First();
            plan.FreightDiscount = freightDiscount;
            plan.FreightDiscountAmount = freightDiscount.DiscountAmount;
            plan.FinalFreightAmount = freightDiscount.FinalFreight;
        }

        plan.Advantages.Add("最大化叠加优惠");
        plan.Advantages.Add($"共 {plan.AppliedCoupons.Count} 张券可叠加使用");

        return plan;
    }

    private CandidatePlan BuildSimplestPlan(
        OrderContext context,
        List<Coupon> coupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var plan = new CandidatePlan
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreightAmount = context.FreightAmount,
            PlanName = "简约方案"
        };

        var bestSingle = coupons
            .Select(c => new { Coupon = c, Discount = calculateDiscount(c, context) })
            .OrderByDescending(x => x.Discount)
            .FirstOrDefault();

        if (bestSingle != null && bestSingle.Discount > 0)
        {
            plan.AppliedCoupons.Add(new AppliedCouponDetail
            {
                CouponId = bestSingle.Coupon.CouponId,
                CouponCode = bestSingle.Coupon.CouponCode,
                CouponName = bestSingle.Coupon.CouponName,
                Type = bestSingle.Coupon.Type,
                DiscountAmount = bestSingle.Discount,
                OriginalAmount = context.OriginalAmount,
                ApplicableAmount = context.GetApplicableAmountForCoupon(bestSingle.Coupon),
                ScopeDescription = bestSingle.Coupon.GetScopeDescription(),
                Reason = GetCouponApplyReason(bestSingle.Coupon, bestSingle.Discount, context.GetApplicableAmountForCoupon(bestSingle.Coupon))
            });

            if (bestSingle.Coupon.Type == CouponType.FreeShipping)
            {
                plan.FreightDiscountAmount = context.FreightAmount;
                plan.FinalFreightAmount = 0;
            }
            else
            {
                plan.ProductDiscountAmount = bestSingle.Discount;
            }
        }

        if (memberDiscounts.Any())
        {
            var memberDiscount = memberDiscounts.First();
            plan.MemberDiscount = memberDiscount;
            plan.MemberDiscountAmount = memberDiscount.DiscountAmount;
        }

        if (plan.FreightDiscountAmount == 0 && freightDiscounts.Any())
        {
            var freightDiscount = freightDiscounts.First();
            plan.FreightDiscount = freightDiscount;
            plan.FreightDiscountAmount = freightDiscount.DiscountAmount;
            plan.FinalFreightAmount = freightDiscount.FinalFreight;
        }

        plan.Advantages.Add("操作简单，只需使用一张券");
        plan.Advantages.Add("订单确认更快捷");

        if (plan.AppliedCoupons.Count > 1)
        {
            plan.Limitations.Add("相比最优方案可能少省一些钱");
        }

        return plan;
    }

    private CandidatePlan BuildMemberOnlyPlan(
        OrderContext context,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts)
    {
        var plan = new CandidatePlan
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreightAmount = context.FreightAmount,
            PlanName = "会员专享方案"
        };

        if (memberDiscounts.Any())
        {
            var memberDiscount = memberDiscounts.First();
            plan.MemberDiscount = memberDiscount;
            plan.MemberDiscountAmount = memberDiscount.DiscountAmount;
        }

        if (freightDiscounts.Any())
        {
            var freightDiscount = freightDiscounts.First();
            plan.FreightDiscount = freightDiscount;
            plan.FreightDiscountAmount = freightDiscount.DiscountAmount;
            plan.FinalFreightAmount = freightDiscount.FinalFreight;
        }

        plan.Advantages.Add("自动享受会员等级优惠");
        plan.Advantages.Add("无需手动选择优惠券");

        return plan;
    }

    private CandidatePlan BuildPlan(
        OrderContext context,
        List<Coupon> coupons,
        List<MemberDiscountDetail> memberDiscounts,
        List<FreightDiscountDetail> freightDiscounts,
        Func<Coupon, OrderContext, decimal> calculateDiscount)
    {
        var plan = new CandidatePlan
        {
            OriginalAmount = context.OriginalAmount,
            OriginalFreight = context.FreightAmount,
            FinalFreightAmount = context.FreightAmount,
            PlanName = $"组合方案({string.Join(",", coupons.Select(c => c.CouponCode))})"
        };

        var tempContext = CloneContext(context);

        foreach (var coupon in coupons)
        {
            var discount = calculateDiscount(coupon, tempContext);
            var applicableAmount = tempContext.GetApplicableAmountForCoupon(coupon);

            plan.AppliedCoupons.Add(new AppliedCouponDetail
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponName = coupon.CouponName,
                Type = coupon.Type,
                DiscountAmount = discount,
                OriginalAmount = tempContext.OriginalAmount,
                ApplicableAmount = applicableAmount,
                ScopeDescription = coupon.GetScopeDescription(),
                Reason = GetCouponApplyReason(coupon, discount, applicableAmount)
            });

            if (coupon.Type == CouponType.FreeShipping)
            {
                plan.FreightDiscountAmount = context.FreightAmount;
                plan.FinalFreightAmount = 0;
            }
            else
            {
                plan.ProductDiscountAmount += discount;
            }
        }

        if (memberDiscounts.Any())
        {
            var memberDiscount = memberDiscounts.First();
            plan.MemberDiscount = memberDiscount;
            plan.MemberDiscountAmount = memberDiscount.DiscountAmount;
        }

        if (plan.FreightDiscountAmount == 0 && freightDiscounts.Any())
        {
            var freightDiscount = freightDiscounts.First();
            plan.FreightDiscount = freightDiscount;
            plan.FreightDiscountAmount = freightDiscount.DiscountAmount;
            plan.FinalFreightAmount = freightDiscount.FinalFreight;
        }

        return plan;
    }

    private PlanComparison BuildComparison(
        OrderContext context,
        List<CandidatePlan> plans,
        List<Coupon> allCoupons)
    {
        var comparison = new PlanComparison
        {
            TotalPlansGenerated = plans.Count,
            AllPlans = plans.Select(p => new PlanSummary
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                Rank = p.Rank,
                TotalDiscount = p.TotalDiscountAmount,
                FinalAmount = p.FinalTotalAmount,
                CouponCount = p.AppliedCoupons.Count,
                IsRecommended = p.IsRecommended
            }).ToList()
        };

        comparison.BestPlan = comparison.AllPlans.OrderByDescending(p => p.TotalDiscount).FirstOrDefault();
        comparison.RecommendedPlanSummary = comparison.AllPlans.FirstOrDefault(p => p.IsRecommended);

        var appliedCouponIds = plans.SelectMany(p => p.AppliedCoupons).Select(c => c.CouponId).Distinct().ToList();
        var notUsedCoupons = allCoupons.Where(c => !appliedCouponIds.Contains(c.CouponId)).ToList();

        foreach (var unused in notUsedCoupons)
        {
            comparison.ExcludedReasons.Add(new ExcludedPlanReason
            {
                PlanId = Guid.NewGuid().ToString(),
                PlanName = $"未使用-{unused.CouponCode}",
                Reason = "未选中此优惠券",
                Details = new List<string>
                {
                    $"券类型: {unused.Type}",
                    $"优惠: {unused.GetDiscountDescription()}",
                    $"适用范围: {unused.GetScopeDescription()}"
                }
            });
        }

        if (plans.Count > 1)
        {
            var best = plans.OrderByDescending(p => p.TotalDiscountAmount).First();
            var others = plans.Where(p => p.PlanId != best.PlanId).ToList();
            
            foreach (var other in others)
            {
                var diff = best.TotalDiscountAmount - other.TotalDiscountAmount;
                if (diff > 0)
                {
                    other.Limitations.Add($"比最优方案少省 {diff:F2} 元");
                }
            }
        }

        return comparison;
    }

    private string GetCouponApplyReason(Coupon coupon, decimal discount, decimal applicableAmount)
    {
        return coupon.Type switch
        {
            CouponType.AmountOff => $"订单满足{coupon.GetConditionSummary()}，直接减免{discount:F2}元",
            CouponType.DiscountRate => $"指定商品原价{applicableAmount:F2}元，享受{discount:F2}元优惠",
            CouponType.QuantityDiscount => $"购买数量满足条件，折扣后优惠{discount:F2}元",
            CouponType.FreeShipping => "免收运费",
            CouponType.MemberExclusive => $"会员专属折扣，省{discount:F2}元",
            _ => $"节省{discount:F2}元"
        };
    }

    private bool IsDifferentPlan(CandidatePlan p1, CandidatePlan p2)
    {
        if (p1 == null || p2 == null) return true;
        if (p1.AppliedCoupons.Count != p2.AppliedCoupons.Count) return true;
        
        var ids1 = p1.AppliedCoupons.Select(c => c.CouponId).OrderBy(x => x).ToList();
        var ids2 = p2.AppliedCoupons.Select(c => c.CouponId).OrderBy(x => x).ToList();
        
        return !ids1.SequenceEqual(ids2);
    }

    private OrderContext CloneContext(OrderContext original)
    {
        return new OrderContext
        {
            OrderId = original.OrderId,
            Items = original.Items.ToList(),
            Member = original.Member,
            Shipping = original.Shipping,
            FreightAmount = original.FreightAmount,
            ExtendedData = new Dictionary<string, object>(original.ExtendedData)
        };
    }
}
