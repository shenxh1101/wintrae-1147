using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class RuleVersionManager
{
    private readonly List<RuleVersion> _versions = new();
    private readonly List<RuleVersionPlan> _plans = new();
    private readonly Dictionary<string, RuleVersion> _activeVersions = new();
    
    public RuleVersion CreateVersion(string versionName, string description, List<Coupon> coupons, List<DiscountRule> rules, string createdBy)
    {
        var version = new RuleVersion
        {
            VersionId = Guid.NewGuid().ToString(),
            VersionName = versionName,
            Description = description,
            MajorVersion = GetNextMajorVersion(),
            MinorVersion = 0,
            CreatedAt = DateTime.UtcNow,
            Status = VersionStatus.Draft,
            CreatedBy = createdBy,
            Coupons = coupons,
            Rules = rules
        };
        
        _versions.Add(version);
        return version;
    }
    
    public RuleVersionPlan CreateSwitchPlan(string fromVersionId, string toVersionId, DateTime scheduledTime, string createdBy)
    {
        var fromVersion = GetVersion(fromVersionId);
        var toVersion = GetVersion(toVersionId);
        
        if (fromVersion == null || toVersion == null)
        {
            throw new ArgumentException("版本不存在");
        }
        
        var plan = new RuleVersionPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanName = $"版本切换计划: {fromVersion.FullVersion} → {toVersion.FullVersion}",
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            ScheduledTime = scheduledTime,
            CreatedAt = DateTime.UtcNow,
            Status = PlanStatus.Created,
            CreatedBy = createdBy,
            Preview = GenerateDiffPreview(fromVersion, toVersion)
        };
        
        _plans.Add(plan);
        return plan;
    }
    
    public VersionDiffPreview GenerateDiffPreview(RuleVersion fromVersion, RuleVersion toVersion)
    {
        var preview = new VersionDiffPreview();
        
        var fromRuleIds = fromVersion.Rules.Select(r => r.RuleId).ToHashSet();
        var toRuleIds = toVersion.Rules.Select(r => r.RuleId).ToHashSet();
        
        preview.AddedRules = toVersion.Rules
            .Where(r => !fromRuleIds.Contains(r.RuleId))
            .Select(r => new RuleChange
            {
                RuleId = r.RuleId,
                RuleName = r.RuleName,
                Type = r.Type,
                DiscountValue = r.DiscountValue,
                ChangeDescription = "新增规则"
            })
            .ToList();
        
        preview.RemovedRules = fromVersion.Rules
            .Where(r => !toRuleIds.Contains(r.RuleId))
            .Select(r => new RuleChange
            {
                RuleId = r.RuleId,
                RuleName = r.RuleName,
                Type = r.Type,
                DiscountValue = r.DiscountValue,
                ChangeDescription = "删除规则"
            })
            .ToList();
        
        preview.ModifiedRules = fromVersion.Rules
            .Where(r => toRuleIds.Contains(r.RuleId))
            .Select(r => 
            {
                var newRule = toVersion.Rules.FirstOrDefault(nr => nr.RuleId == r.RuleId);
                if (newRule == null) return null;
                
                var changes = CompareRules(r, newRule);
                if (!changes.Any()) return null;
                
                return new RuleChange
                {
                    RuleId = r.RuleId,
                    RuleName = r.RuleName,
                    Type = r.Type,
                    DiscountValue = newRule.DiscountValue,
                    ChangeDescription = string.Join(", ", changes),
                    OldValues = GetRuleValues(r),
                    NewValues = GetRuleValues(newRule)
                };
            })
            .Where(r => r != null)
            .ToList()!;
        
        var fromCouponIds = fromVersion.Coupons.Select(c => c.CouponId).ToHashSet();
        var toCouponIds = toVersion.Coupons.Select(c => c.CouponId).ToHashSet();
        
        preview.AddedCoupons = toVersion.Coupons
            .Where(c => !fromCouponIds.Contains(c.CouponId))
            .Select(c => new CouponChange
            {
                CouponId = c.CouponId,
                CouponCode = c.CouponCode,
                CouponName = c.CouponName,
                ChangeDescription = "新增优惠券"
            })
            .ToList();
        
        preview.RemovedCoupons = fromVersion.Coupons
            .Where(c => !toCouponIds.Contains(c.CouponId))
            .Select(c => new CouponChange
            {
                CouponId = c.CouponId,
                CouponCode = c.CouponCode,
                CouponName = c.CouponName,
                ChangeDescription = "删除优惠券"
            })
            .ToList();
        
        preview.ModifiedCoupons = fromVersion.Coupons
            .Where(c => toCouponIds.Contains(c.CouponId))
            .Select(c => 
            {
                var newCoupon = toVersion.Coupons.FirstOrDefault(nc => nc.CouponId == c.CouponId);
                if (newCoupon == null) return null;
                
                var changes = CompareCoupons(c, newCoupon);
                if (!changes.Any()) return null;
                
                return new CouponChange
                {
                    CouponId = c.CouponId,
                    CouponCode = c.CouponCode,
                    CouponName = c.CouponName,
                    ChangeDescription = string.Join(", ", changes),
                    OldValues = GetCouponValues(c),
                    NewValues = GetCouponValues(newCoupon)
                };
            })
            .Where(c => c != null)
            .ToList()!;
        
        preview.ImpactAnalysis = AnalyzeImpact(fromVersion, toVersion, preview);
        
        return preview;
    }
    
    public ImpactAnalysis AnalyzeImpact(RuleVersion fromVersion, RuleVersion toVersion, VersionDiffPreview preview)
    {
        var analysis = new ImpactAnalysis();
        
        var totalChanges = preview.AddedRules.Count + preview.RemovedRules.Count + preview.ModifiedRules.Count +
                          preview.AddedCoupons.Count + preview.RemovedCoupons.Count + preview.ModifiedCoupons.Count;
        
        analysis.EstimatedAffectedOrders = EstimateAffectedOrders(preview);
        analysis.EstimatedDiscountChange = EstimateDiscountChange(preview);
        
        analysis.RiskLevel = DetermineRiskLevel(totalChanges, preview);
        
        if (preview.RemovedCoupons.Any())
        {
            analysis.Warnings.Add($"有 {preview.RemovedCoupons.Count} 张优惠券将被删除，可能影响用户使用体验");
        }
        
        if (preview.ModifiedRules.Any(r => r.Type == DiscountRuleType.AmountThreshold))
        {
            analysis.Warnings.Add("满减规则有变化，可能影响大额订单的优惠力度");
        }
        
        if (preview.AddedCoupons.Any(c => c.ChangeDescription.Contains("会员")))
        {
            analysis.AffectedUserGroups.Add("会员用户");
        }
        
        analysis.Recommendations.Add("建议在切换前进行小范围灰度测试");
        analysis.Recommendations.Add("建议提前通知受影响的用户群体");
        
        return analysis;
    }
    
    public List<SampleImpactCase> GenerateSampleImpactCases(RuleVersion fromVersion, RuleVersion toVersion, List<OrderContext> sampleOrders)
    {
        var cases = new List<SampleImpactCase>();
        
        var fromEngine = new Engine.CouponCalculatorEngine();
        var toEngine = new Engine.CouponCalculatorEngine();
        
        toEngine.LoadRules(new Engine.DefaultRuleProvider());
        
        foreach (var order in sampleOrders.Take(10))
        {
            var fromResult = fromEngine.CalculateOptimal(order, fromVersion.Coupons);
            var toResult = toEngine.CalculateOptimal(order, toVersion.Coupons);
            
            if (fromResult.TotalDiscountAmount != toResult.TotalDiscountAmount)
            {
                cases.Add(new SampleImpactCase
                {
                    OrderId = order.OrderId,
                    OldDiscount = fromResult.TotalDiscountAmount,
                    NewDiscount = toResult.TotalDiscountAmount,
                    Difference = toResult.TotalDiscountAmount - fromResult.TotalDiscountAmount,
                    ChangeReason = IdentifyChangeReason(fromResult, toResult, fromVersion, toVersion)
                });
            }
        }
        
        return cases;
    }
    
    public bool ApprovePlan(string planId, string approvedBy)
    {
        var plan = GetPlan(planId);
        if (plan == null) return false;
        
        if (plan.Status != PlanStatus.Created) return false;
        
        plan.ApprovedBy.Add(approvedBy);
        
        if (!plan.RequireApproval || plan.ApprovedBy.Count >= 2)
        {
            plan.Status = PlanStatus.Approved;
        }
        
        return true;
    }
    
    public bool ExecutePlan(string planId)
    {
        var plan = GetPlan(planId);
        if (plan == null) return false;
        
        if (plan.Status != PlanStatus.Approved) return false;
        
        plan.Status = PlanStatus.Executing;
        
        try
        {
            var fromVersion = GetVersion(plan.FromVersionId);
            var toVersion = GetVersion(plan.ToVersionId);
            
            if (fromVersion != null)
            {
                fromVersion.Status = VersionStatus.Deprecated;
                fromVersion.EffectiveTo = DateTime.UtcNow;
            }
            
            if (toVersion != null)
            {
                toVersion.Status = VersionStatus.Active;
                toVersion.EffectiveFrom = DateTime.UtcNow;
            }
            
            plan.Status = PlanStatus.Completed;
            return true;
        }
        catch
        {
            plan.Status = PlanStatus.Failed;
            return false;
        }
    }
    
    public bool CancelPlan(string planId)
    {
        var plan = GetPlan(planId);
        if (plan == null) return false;
        
        if (plan.Status == PlanStatus.Executing || plan.Status == PlanStatus.Completed) return false;
        
        plan.Status = PlanStatus.Cancelled;
        return true;
    }
    
    public RuleVersion? GetVersion(string versionId)
    {
        return _versions.FirstOrDefault(v => v.VersionId == versionId);
    }
    
    public RuleVersion? GetActiveVersion(string scope = "default")
    {
        return _activeVersions.TryGetValue(scope, out var version) ? version : null;
    }
    
    public List<RuleVersion> GetAllVersions()
    {
        return _versions.OrderByDescending(v => v.CreatedAt).ToList();
    }
    
    public List<RuleVersion> GetVersionsByStatus(VersionStatus status)
    {
        return _versions.Where(v => v.Status == status).ToList();
    }
    
    public RuleVersionPlan? GetPlan(string planId)
    {
        return _plans.FirstOrDefault(p => p.PlanId == planId);
    }
    
    public List<RuleVersionPlan> GetAllPlans()
    {
        return _plans.OrderByDescending(p => p.CreatedAt).ToList();
    }
    
    public List<RuleVersionPlan> GetPendingPlans()
    {
        return _plans.Where(p => p.Status == PlanStatus.Approved && p.ScheduledTime > DateTime.UtcNow).ToList();
    }
    
    public List<RuleVersionPlan> GetScheduledPlansForToday()
    {
        var today = DateTime.UtcNow.Date;
        return _plans.Where(p => 
            p.Status == PlanStatus.Approved && 
            p.ScheduledTime.Date == today).ToList();
    }
    
    public void ProcessScheduledPlans()
    {
        var duePlans = _plans.Where(p => 
            p.Status == PlanStatus.Approved && 
            p.ScheduledTime <= DateTime.UtcNow).ToList();
        
        foreach (var plan in duePlans)
        {
            ExecutePlan(plan.PlanId);
        }
    }
    
    private int GetNextMajorVersion()
    {
        return _versions.Any() ? _versions.Max(v => v.MajorVersion) + 1 : 1;
    }
    
    private List<string> CompareRules(DiscountRule oldRule, DiscountRule newRule)
    {
        var changes = new List<string>();
        
        if (oldRule.DiscountValue != newRule.DiscountValue)
            changes.Add($"优惠值从 {oldRule.DiscountValue} 改为 {newRule.DiscountValue}");
        
        if (oldRule.Threshold != newRule.Threshold)
            changes.Add($"门槛从 {oldRule.Threshold} 改为 {newRule.Threshold}");
        
        if (oldRule.Priority != newRule.Priority)
            changes.Add($"优先级从 {oldRule.Priority} 改为 {newRule.Priority}");
        
        if (oldRule.IsStackable != newRule.IsStackable)
            changes.Add($"叠加性从 {oldRule.IsStackable} 改为 {newRule.IsStackable}");
        
        return changes;
    }
    
    private List<string> CompareCoupons(Coupon oldCoupon, Coupon newCoupon)
    {
        var changes = new List<string>();
        
        if (oldCoupon.DiscountValue != newCoupon.DiscountValue)
            changes.Add($"优惠值从 {oldCoupon.DiscountValue} 改为 {newCoupon.DiscountValue}");
        
        if (oldCoupon.MinOrderAmount != newCoupon.MinOrderAmount)
            changes.Add($"门槛从 {oldCoupon.MinOrderAmount} 改为 {newCoupon.MinOrderAmount}");
        
        if (oldCoupon.ValidTo != newCoupon.ValidTo)
            changes.Add($"有效期从 {oldCoupon.ValidTo:yyyy-MM-dd} 改为 {newCoupon.ValidTo:yyyy-MM-dd}");
        
        if (oldCoupon.Priority != newCoupon.Priority)
            changes.Add($"优先级从 {oldCoupon.Priority} 改为 {newCoupon.Priority}");
        
        return changes;
    }
    
    private Dictionary<string, object> GetRuleValues(DiscountRule rule)
    {
        return new Dictionary<string, object>
        {
            ["DiscountValue"] = rule.DiscountValue,
            ["Threshold"] = rule.Threshold,
            ["Priority"] = rule.Priority,
            ["IsStackable"] = rule.IsStackable
        };
    }
    
    private Dictionary<string, object> GetCouponValues(Coupon coupon)
    {
        return new Dictionary<string, object>
        {
            ["DiscountValue"] = coupon.DiscountValue,
            ["MinOrderAmount"] = coupon.MinOrderAmount,
            ["ValidTo"] = coupon.ValidTo,
            ["Priority"] = coupon.Priority
        };
    }
    
    private int EstimateAffectedOrders(VersionDiffPreview preview)
    {
        var baseEstimate = 1000;
        var multiplier = preview.AddedCoupons.Count + preview.ModifiedCoupons.Count * 2 + preview.RemovedCoupons.Count * 3;
        return baseEstimate * Math.Max(1, multiplier);
    }
    
    private decimal EstimateDiscountChange(VersionDiffPreview preview)
    {
        var addedValue = preview.AddedCoupons.Sum(c => 20m) + preview.AddedRules.Sum(r => 15m);
        var removedValue = preview.RemovedCoupons.Sum(c => 30m) + preview.RemovedRules.Sum(r => 25m);
        var modifiedValue = preview.ModifiedCoupons.Count * 10m + preview.ModifiedRules.Count * 8m;
        
        return addedValue - removedValue + modifiedValue;
    }
    
    private string DetermineRiskLevel(int totalChanges, VersionDiffPreview preview)
    {
        if (preview.RemovedCoupons.Any() || preview.RemovedRules.Any())
            return "高";
        
        if (totalChanges > 10 || preview.ModifiedCoupons.Count > 5)
            return "中";
        
        return "低";
    }
    
    private string IdentifyChangeReason(CalculationResult fromResult, CalculationResult toResult, RuleVersion fromVersion, RuleVersion toVersion)
    {
        var reasons = new List<string>();
        
        var fromCouponIds = fromResult.AppliedCoupons.Select(c => c.CouponId).ToHashSet();
        var toCouponIds = toResult.AppliedCoupons.Select(c => c.CouponId).ToHashSet();
        
        var addedCoupons = toCouponIds.Except(fromCouponIds);
        var removedCoupons = fromCouponIds.Except(toCouponIds);
        
        if (addedCoupons.Any())
            reasons.Add($"新增应用券: {string.Join(",", addedCoupons)}");
        
        if (removedCoupons.Any())
            reasons.Add($"移除应用券: {string.Join(",", removedCoupons)}");
        
        return reasons.Any() ? string.Join("; ", reasons) : "优惠金额变化";
    }
}