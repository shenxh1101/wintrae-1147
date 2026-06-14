using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class EnhancedCouponValidator : ICouponValidator
{
    public ValidationResult Validate(Coupon coupon, OrderContext context)
    {
        var result = new ValidationResult { IsValid = true };
        var errors = GetUnavailableReasons(coupon, context);

        if (errors.Any())
        {
            result.IsValid = false;
            result.Errors = errors;
        }

        return result;
    }

    public List<string> GetUnavailableReasons(Coupon coupon, OrderContext context)
    {
        var trialResult = GetDetailedTrialResult(coupon, context);
        return trialResult.UnavailableReasons;
    }

    public EnhancedCouponTrialResult GetDetailedTrialResult(Coupon coupon, OrderContext context)
    {
        var result = new EnhancedCouponTrialResult
        {
            Coupon = coupon,
            OriginalAmount = context.OriginalAmount,
            ApplicableAmount = coupon.GetApplicableAmount(context)
        };

        AddValidityCheck(coupon, context, result);
        
        if (!result.IsAvailable)
        {
            result.Status = TrialStatus.Unavailable;
            result.StatusMessage = "优惠券已过期或尚未生效";
            return result;
        }

        AddThresholdChecks(coupon, context, result);
        AddMemberLevelCheck(coupon, context, result);
        AddScopeChecks(coupon, context, result);
        AddStackingChecks(coupon, context, result);

        result.IsAvailable = !result.UnavailableReasons.Any();
        result.RemainingAmount = context.OriginalAmount - result.DiscountAmount;
        result.UserExplanation = GenerateUserExplanation(coupon, context, result);

        return result;
    }

    private void AddValidityCheck(Coupon coupon, OrderContext context, EnhancedCouponTrialResult result)
    {
        var now = DateTime.UtcNow;
        var check = new TrialCheckResult
        {
            CheckName = "有效期校验",
            Type = CheckType.Validity
        };

        if (now < coupon.ValidFrom)
        {
            check.Passed = false;
            check.Message = $"优惠券尚未生效，开始时间: {coupon.ValidFrom:yyyy-MM-dd HH:mm}";
            check.UserFriendlyMessage = $"优惠券将在 {coupon.ValidFrom:MM月dd日 HH:mm} 开始生效，请耐心等待";
            check.Expected = coupon.ValidFrom;
            check.Actual = now;
            result.Status = TrialStatus.NotYetValid;
            result.UnavailableReasons.Add(check.UserFriendlyMessage);
        }
        else if (now > coupon.ValidTo)
        {
            check.Passed = false;
            check.Message = $"优惠券已过期，结束时间: {coupon.ValidTo:yyyy-MM-dd HH:mm}";
            check.UserFriendlyMessage = $"优惠券已于 {coupon.ValidTo:MM月dd日} 到期，不可用";
            check.Expected = coupon.ValidTo;
            check.Actual = now;
            result.Status = TrialStatus.Expired;
            result.UnavailableReasons.Add(check.UserFriendlyMessage);
        }
        else
        {
            check.Passed = true;
            check.Message = "优惠券在有效期内";
            check.UserFriendlyMessage = $"有效期至 {coupon.ValidTo:MM月dd日}，可以使用";
        }

        result.CheckResults.Add(check);
        result.ScopeDescription = coupon.GetScopeDescription();
        result.ConditionDescription = coupon.GetConditionSummary();
    }

    private void AddThresholdChecks(Coupon coupon, OrderContext context, EnhancedCouponTrialResult result)
    {
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom || now > coupon.ValidTo)
            return;

        if (coupon.MinOrderAmount > 0)
        {
            var check = new TrialCheckResult
            {
                CheckName = "最低消费金额",
                Type = CheckType.Threshold,
                Expected = coupon.MinOrderAmount,
                Actual = context.OriginalAmount
            };

            if (context.OriginalAmount < coupon.MinOrderAmount)
            {
                check.Passed = false;
                var remaining = coupon.MinOrderAmount - context.OriginalAmount;
                check.Message = $"订单金额 {context.OriginalAmount:F2} 元未达到最低要求 {coupon.MinOrderAmount:F2} 元";
                check.UserFriendlyMessage = $"还差 {remaining:F2} 元即可使用此券，去挑点商品吧~";
                result.UnavailableReasons.Add(check.UserFriendlyMessage);
                
                if (result.Status == TrialStatus.Available)
                    result.Status = TrialStatus.ThresholdNotMet;
            }
            else
            {
                check.Passed = true;
                check.Message = "订单金额满足最低要求";
                check.UserFriendlyMessage = $"已满 {coupon.MinOrderAmount:F0} 元门槛，可使用";
            }

            result.CheckResults.Add(check);
        }

        if (coupon.MinQuantity > 0)
        {
            var applicableQty = coupon.GetApplicableQuantity(context);
            var check = new TrialCheckResult
            {
                CheckName = "最低购买数量",
                Type = CheckType.Quantity,
                Expected = coupon.MinQuantity,
                Actual = applicableQty
            };

            if (applicableQty < coupon.MinQuantity)
            {
                check.Passed = false;
                var remaining = coupon.MinQuantity - applicableQty;
                check.Message = $"适用商品数量 {applicableQty} 件未达到最低要求 {coupon.MinQuantity} 件";
                check.UserFriendlyMessage = $"还需要再买 {remaining} 件指定商品才能使用此券";
                result.UnavailableReasons.Add(check.UserFriendlyMessage);
                
                if (result.Status == TrialStatus.Available)
                    result.Status = TrialStatus.ThresholdNotMet;
            }
            else
            {
                check.Passed = true;
                check.Message = "商品数量满足最低要求";
                check.UserFriendlyMessage = $"已购 {applicableQty} 件，满足 {coupon.MinQuantity} 件门槛";
            }

            result.CheckResults.Add(check);
        }
    }

    private void AddMemberLevelCheck(Coupon coupon, OrderContext context, EnhancedCouponTrialResult result)
    {
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom || now > coupon.ValidTo)
            return;

        if (coupon.UsageCondition.AllowedMemberLevels != null && coupon.UsageCondition.AllowedMemberLevels.Count > 0)
        {
            var check = new TrialCheckResult
            {
                CheckName = "会员等级限制",
                Type = CheckType.MemberLevel,
                Expected = string.Join(",", coupon.UsageCondition.AllowedMemberLevels)
            };

            if (context.Member == null)
            {
                check.Passed = false;
                check.Message = "用户未登录或不是会员";
                check.UserFriendlyMessage = "请先登录后再试";
                check.Actual = "未登录";
                result.UnavailableReasons.Add(check.UserFriendlyMessage);
                
                if (result.Status == TrialStatus.Available)
                    result.Status = TrialStatus.MemberLevelNotMet;
            }
            else if (!coupon.UsageCondition.AllowedMemberLevels.Contains(context.Member.Level))
            {
                check.Passed = false;
                var currentLevel = GetMemberLevelName(context.Member.Level);
                var allowedLevels = string.Join("或", coupon.UsageCondition.AllowedMemberLevels.Select(GetMemberLevelName));
                check.Message = $"需要 {allowedLevels}，当前为 {currentLevel}";
                check.UserFriendlyMessage = $"此券仅限 {allowedLevels} 使用，您的当前等级是{currentLevel}";
                check.Actual = context.Member.Level;
                result.UnavailableReasons.Add(check.UserFriendlyMessage);
                
                if (result.Status == TrialStatus.Available)
                    result.Status = TrialStatus.MemberLevelNotMet;
            }
            else
            {
                check.Passed = true;
                check.Message = "会员等级满足要求";
                check.UserFriendlyMessage = $"您的{currentLevel}可以使用此券";
                check.Actual = context.Member.Level;
            }

            result.CheckResults.Add(check);
        }
    }

    private void AddScopeChecks(Coupon coupon, OrderContext context, EnhancedCouponTrialResult result)
    {
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom || now > coupon.ValidTo)
            return;

        var applicableItems = context.Items
            .Where(item => coupon.IsApplicableToProduct(item.ProductId) && 
                          coupon.IsApplicableToCategory(item.CategoryId))
            .ToList();

        var applicableAmount = applicableItems.Sum(i => i.TotalAmount);
        var applicableProductIds = applicableItems.Select(i => i.ProductId).ToList();
        var applicableCategoryIds = applicableItems.Select(i => i.CategoryId).Distinct().ToList();

        result.ApplicableProductIds = applicableProductIds;
        result.ApplicableCategoryIds = applicableCategoryIds;

        if (coupon.ExcludedScope.ProductIds.Any())
        {
            result.ExcludedProductIds = coupon.ExcludedScope.ProductIds
                .Where(id => context.Items.Any(i => i.ProductId == id))
                .ToList();
        }

        if (!coupon.ApplicableScope.IsAllApplicable && applicableAmount == 0)
        {
            var check = new TrialCheckResult
            {
                CheckName = "适用范围检查",
                Type = CheckType.Scope,
                Passed = false,
                Message = "订单中无可适用的商品",
                UserFriendlyMessage = $"此券仅限{coupon.GetScopeDescription()}，您选购的商品不在范围内",
                Expected = coupon.GetScopeDescription(),
                Actual = "无可适用商品"
            };

            result.CheckResults.Add(check);
            result.UnavailableReasons.Add(check.UserFriendlyMessage);
            
            if (result.Status == TrialStatus.Available)
                result.Status = TrialStatus.ScopeNotMatched;
        }
        else
        {
            var check = new TrialCheckResult
            {
                CheckName = "适用范围检查",
                Type = CheckType.Scope,
                Passed = true,
                Message = $"有 {applicableItems.Count} 个商品适用",
                UserFriendlyMessage = $"您的订单中有 {applicableItems.Count} 个商品符合条件",
                Expected = coupon.GetScopeDescription(),
                Actual = $"{applicableItems.Count} 个商品可用，金额 {applicableAmount:F2} 元"
            };

            result.CheckResults.Add(check);
        }
    }

    private void AddStackingChecks(Coupon coupon, OrderContext context, EnhancedCouponTrialResult result)
    {
        var check = new TrialCheckResult
        {
            CheckName = "叠加规则检查",
            Type = CheckType.StackingConflict
        };

        if (!string.IsNullOrEmpty(coupon.StackingGroup))
        {
            check.Passed = true;
            check.Message = $"属于互斥组: {coupon.StackingGroup}";
            check.UserFriendlyMessage = $"此券为{coupon.StackingGroup}专属，不可与其他同组券叠加";
        }
        else
        {
            check.Passed = true;
            check.Message = "无叠加限制";
            check.UserFriendlyMessage = "可以与其他优惠券叠加使用";
        }

        result.CheckResults.Add(check);
    }

    private UserFriendlyExplanation GenerateUserExplanation(
        Coupon coupon, 
        OrderContext context, 
        EnhancedCouponTrialResult trialResult)
    {
        var explanation = new UserFriendlyExplanation();

        if (trialResult.IsAvailable)
        {
            explanation.Summary = $"可以使用！{coupon.GetDiscountDescription()}，最高可省 {trialResult.DiscountAmount:F2} 元";
            
            if (coupon.Type == CouponType.AmountOff)
            {
                explanation.Highlights.Add($"订单金额 {context.OriginalAmount:F2} 元已满足满 {coupon.MinOrderAmount:F0} 元门槛");
            }
            else if (coupon.Type == CouponType.DiscountRate)
            {
                explanation.Highlights.Add($"指定商品可享受 {coupon.DiscountValue * 100:F0} 折优惠");
            }

            if (trialResult.ApplicableProductIds.Count > 0)
            {
                explanation.Highlights.Add($"共 {trialResult.ApplicableProductIds.Count} 个商品适用此券");
            }

            explanation.Tips.Add("建议在结算页面勾选使用此券");
        }
        else
        {
            explanation.Summary = "暂不可用此券";

            if (trialResult.UnavailableReasons.Any())
            {
                explanation.Highlights.Add(trialResult.UnavailableReasons.First());
            }

            var thresholdReason = trialResult.UnavailableReasons
                .FirstOrDefault(r => r.Contains("还差") || r.Contains("还须"));
            
            if (thresholdReason != null)
            {
                explanation.Tips.Add(thresholdReason);
            }

            if (coupon.ValidTo > DateTime.UtcNow)
            {
                explanation.NextStep = $"优惠券有效期至 {coupon.ValidTo:MM月dd日}，请在有效期内使用";
            }
        }

        return explanation;
    }

    public List<string> GetUnavailableReasons(Coupon coupon, OrderContext context)
    {
        var trialResult = GetDetailedTrialResult(coupon, context);
        return trialResult.UnavailableReasons;
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
}
