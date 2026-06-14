using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class GrayReleaseManager
{
    private readonly CouponCalculatorEngine _engine;
    private readonly Dictionary<string, GrayReleasePolicy> _policies = new();
    private readonly Dictionary<string, List<GrayPlaybackResult>> _playbackResults = new();
    
    public GrayReleaseManager()
    {
        _engine = new CouponCalculatorEngine();
    }
    
    public GrayReleasePolicy CreatePolicy(
        string name,
        string ruleVersionId,
        GrayScaleConfig scaleConfig,
        List<GrayTarget> targets)
    {
        var policy = new GrayReleasePolicy
        {
            PolicyId = Guid.NewGuid().ToString(),
            PolicyName = name,
            RuleVersionId = ruleVersionId,
            ScaleConfig = scaleConfig,
            Targets = targets,
            Status = GrayReleaseStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
        
        _policies[policy.PolicyId] = policy;
        return policy;
    }
    
    public GrayReleasePolicy? GetPolicy(string policyId)
    {
        return _policies.TryGetValue(policyId, out var policy) ? policy : null;
    }
    
    public List<GrayReleasePolicy> GetAllPolicies()
    {
        return _policies.Values.ToList();
    }
    
    public List<GrayReleasePolicy> GetActivePolicies()
    {
        return _policies.Values
            .Where(p => p.Status == GrayReleaseStatus.Active)
            .ToList();
    }
    
    public GrayReleasePolicy UpdatePolicyStatus(string policyId, GrayReleaseStatus newStatus)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        ValidateStatusTransition(policy.Status, newStatus);
        
        policy.Status = newStatus;
        policy.UpdatedAt = DateTime.UtcNow;
        
        if (newStatus == GrayReleaseStatus.Active)
        {
            policy.ActivatedAt = DateTime.UtcNow;
        }
        else if (newStatus == GrayReleaseStatus.Paused || newStatus == GrayReleaseStatus.Terminated)
        {
            policy.DeactivatedAt = DateTime.UtcNow;
        }
        
        return policy;
    }
    
    public GrayScaleConfig UpdateScaleConfig(string policyId, GrayScaleConfig newConfig)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        if (policy.Status == GrayReleaseStatus.Active)
        {
            throw new InvalidOperationException("Cannot update scale config while policy is active");
        }
        
        policy.ScaleConfig = newConfig;
        policy.UpdatedAt = DateTime.UtcNow;
        
        return newConfig;
    }
    
    public async Task<GrayPlaybackResult> PlaybackOrderWithPolicy(
        string policyId,
        OrderRecord order,
        List<Coupon> coupons)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        var context = BuildOrderContext(order);
        
        var result = new GrayPlaybackResult
        {
            PlaybackId = Guid.NewGuid().ToString(),
            PolicyId = policyId,
            OrderId = order.OrderId,
            PlayedAt = DateTime.UtcNow,
            IsEligible = CheckEligibility(order, policy)
        };
        
        if (!result.IsEligible)
        {
            result.IneligibleReason = "Order does not match gray release targets";
            return result;
        }
        
        result.OldVersionResult = ExecuteWithOldRules(context, coupons);
        result.NewVersionResult = ExecuteWithNewRules(context, coupons, policy);
        
        result.Difference = result.NewVersionResult.RecommendedPlan?.FinalTotalAmount 
                          ?? result.NewVersionResult.OriginalAmount 
                          - (result.OldVersionResult.RecommendedPlan?.FinalTotalAmount 
                             ?? result.OldVersionResult.OriginalAmount);
        
        result.DifferencePercent = result.OldVersionResult.OriginalAmount > 0 
            ? result.Difference / result.OldVersionResult.OriginalAmount 
            : 0;
        
        if (!_playbackResults.ContainsKey(policyId))
        {
            _playbackResults[policyId] = new List<GrayPlaybackResult>();
        }
        _playbackResults[policyId].Add(result);
        
        return result;
    }
    
    public async Task<List<GrayPlaybackResult>> BatchPlaybackWithPolicy(
        string policyId,
        List<OrderRecord> orders)
    {
        var results = new List<GrayPlaybackResult>();
        
        foreach (var order in orders)
        {
            var result = await PlaybackOrderWithPolicy(policyId, order, LoadCouponsForOrder(order));
            results.Add(result);
        }
        
        return results;
    }
    
    public async Task<BatchPlaybackResult> CompareGrayScales(
        string policyId,
        List<OrderRecord> orders,
        List<decimal> scalePercentages)
    {
        var result = new BatchPlaybackResult
        {
            PolicyId = policyId,
            GeneratedAt = DateTime.UtcNow,
            OrderCount = orders.Count
        };
        
        foreach (var percentage in scalePercentages)
        {
            var scaleComparison = new ScaleComparison
            {
                ScalePercentage = percentage,
                OrderCount = orders.Count
            };
            
            var random = new Random(42);
            var eligibleOrders = orders
                .Select(o => new { Order = o, Rand = random.NextDouble() })
                .Where(x => x.Rand < (double)percentage / 100)
                .Select(x => x.Order)
                .ToList();
            
            var totalDifference = 0m;
            var oldTotalDiscount = 0m;
            var newTotalDiscount = 0m;
            
            foreach (var order in eligibleOrders)
            {
                var playback = await PlaybackOrderWithPolicy(policyId, order, LoadCouponsForOrder(order));
                
                if (playback.OldVersionResult.RecommendedPlan != null)
                    oldTotalDiscount += playback.OldVersionResult.RecommendedPlan.TotalDiscountAmount;
                if (playback.NewVersionResult.RecommendedPlan != null)
                    newTotalDiscount += playback.NewVersionResult.RecommendedPlan.TotalDiscountAmount;
                
                totalDifference += playback.Difference;
            }
            
            scaleComparison.AffectedOrderCount = eligibleOrders.Count;
            scaleComparison.TotalDifference = totalDifference;
            scaleComparison.AverageDifference = eligibleOrders.Any() ? totalDifference / eligibleOrders.Count : 0;
            scaleComparison.OldVersionTotalDiscount = oldTotalDiscount;
            scaleComparison.NewVersionTotalDiscount = newTotalDiscount;
            scaleComparison.DiscountChange = newTotalDiscount - oldTotalDiscount;
            
            result.ScaleComparisons.Add(scaleComparison);
        }
        
        return result;
    }
    
    public List<GrayPlaybackResult> GetPlaybackResults(string policyId)
    {
        return _playbackResults.TryGetValue(policyId, out var results) ? results : new List<GrayPlaybackResult>();
    }
    
    public GrayReleaseAnalysis AnalyzePlaybackResults(string policyId)
    {
        var results = GetPlaybackResults(policyId);
        
        var analysis = new GrayReleaseAnalysis
        {
            PolicyId = policyId,
            AnalysisTime = DateTime.UtcNow,
            TotalPlaybackCount = results.Count,
            EligibleCount = results.Count(r => r.IsEligible),
            AffectedCount = results.Count(r => r.IsEligible && Math.Abs(r.Difference) > 0.01m)
        };
        
        if (results.Any())
        {
            analysis.AverageDifference = results.Where(r => r.IsEligible).Average(r => r.Difference);
            analysis.MaxPositiveDifference = results.Where(r => r.IsEligible && r.Difference > 0).Max(r => r.Difference);
            analysis.MaxNegativeDifference = results.Where(r => r.IsEligible && r.Difference < 0).Min(r => r.Difference);
            
            var eligible = results.Where(r => r.IsEligible).ToList();
            analysis.DifferenceDistribution = new Dictionary<string, int>
            {
                ["无变化"] = eligible.Count(r => Math.Abs(r.Difference) <= 0.01m),
                ["小幅差异(±10%)"] = eligible.Count(r => Math.Abs(r.DifferencePercent) <= 0.1m),
                ["中幅差异(10%-50%)"] = eligible.Count(r => Math.Abs(r.DifferencePercent) > 0.1m && Math.Abs(r.DifferencePercent) <= 0.5m),
                ["大幅差异(>50%)"] = eligible.Count(r => Math.Abs(r.DifferencePercent) > 0.5m)
            };
            
            analysis.TopDifferenceOrders = results
                .Where(r => r.IsEligible && Math.Abs(r.Difference) > 0.01m)
                .OrderByDescending(r => Math.Abs(r.Difference))
                .Take(10)
                .Select(r => new TopDifferenceOrder
                {
                    OrderId = r.OrderId,
                    Difference = r.Difference,
                    DifferencePercent = r.DifferencePercent
                })
                .ToList();
        }
        
        return analysis;
    }
    
    public string GenerateGrayReleaseReport(string policyId)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            return $"Policy not found: {policyId}";
        
        var analysis = AnalyzePlaybackResults(policyId);
        
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════════");
        report.Add($"              灰度发布分析报告: {policy.PolicyName}");
        report.Add("═══════════════════════════════════════════════════════════════");
        report.Add($"策略ID: {policy.PolicyId}");
        report.Add($"规则版本: {policy.RuleVersionId}");
        report.Add($"状态: {policy.Status}");
        report.Add($"创建时间: {policy.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (policy.ActivatedAt.HasValue)
            report.Add($"激活时间: {policy.ActivatedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add("");
        
        report.Add("【灰度配置】");
        report.Add($"  灰度类型: {policy.ScaleConfig.ScaleType}");
        report.Add($"  当前放量: {policy.ScaleConfig.CurrentPercentage}%");
        report.Add($"  目标放量: {policy.ScaleConfig.TargetPercentage}%");
        report.Add($"  开始时间: {policy.ScaleConfig.StartDate:yyyy-MM-dd}");
        report.Add($"  结束时间: {policy.ScaleConfig.EndDate:yyyy-MM-dd}");
        report.Add("");
        
        report.Add("【目标规则】");
        foreach (var target in policy.Targets)
        {
            report.Add($"  {target.TargetType}: {string.Join(", ", target.TargetIds)}");
            if (target.Conditions.Any())
            {
                foreach (var cond in target.Conditions)
                {
                    report.Add($"    条件: {cond.Key} {cond.Operator} {cond.Value}");
                }
            }
        }
        report.Add("");
        
        report.Add("【回放分析】");
        report.Add($"  总回放次数: {analysis.TotalPlaybackCount}");
        report.Add($"  符合条件: {analysis.EligibleCount}");
        report.Add($"  有差异影响: {analysis.AffectedCount}");
        report.Add($"  平均差异: ¥{analysis.AverageDifference:F2}");
        report.Add($"  最大正向差异: ¥{analysis.MaxPositiveDifference:F2}");
        report.Add($"  最大负向差异: ¥{analysis.MaxNegativeDifference:F2}");
        report.Add("");
        
        if (analysis.DifferenceDistribution.Any())
        {
            report.Add("【差异分布】");
            foreach (var kvp in analysis.DifferenceDistribution)
            {
                report.Add($"  {kvp.Key}: {kvp.Value} ({kvp.Value * 100.0 / Math.Max(analysis.EligibleCount, 1):F1}%)");
            }
            report.Add("");
        }
        
        if (analysis.TopDifferenceOrders.Any())
        {
            report.Add("【TOP 10 差异订单】");
            foreach (var order in analysis.TopDifferenceOrders)
            {
                report.Add($"  {order.OrderId}: ¥{order.Difference:F2} ({order.DifferencePercent:P2})");
            }
            report.Add("");
        }
        
        report.Add("═══════════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
    
    public List<GrayReleasePolicy> GetPoliciesByTarget(string targetType, string targetId)
    {
        return _policies.Values
            .Where(p => p.Targets.Any(t => 
                t.TargetType == targetType && t.TargetIds.Contains(targetId)))
            .ToList();
    }
    
    public List<GrayReleasePolicy> GetPoliciesByStatus(GrayReleaseStatus status)
    {
        return _policies.Values
            .Where(p => p.Status == status)
            .ToList();
    }
    
    public bool DeletePolicy(string policyId)
    {
        var policy = GetPolicy(policyId);
        if (policy == null) return false;
        
        if (policy.Status == GrayReleaseStatus.Active)
        {
            throw new InvalidOperationException("Cannot delete an active policy");
        }
        
        _policies.Remove(policyId);
        _playbackResults.Remove(policyId);
        
        return true;
    }
    
    private void ValidateStatusTransition(GrayReleaseStatus from, GrayReleaseStatus to)
    {
        var validTransitions = new Dictionary<GrayReleaseStatus, List<GrayReleaseStatus>>
        {
            [GrayReleaseStatus.Draft] = new List<GrayReleaseStatus> 
                { GrayReleaseStatus.Ready, GrayReleaseStatus.Paused, GrayReleaseStatus.Terminated },
            [GrayReleaseStatus.Ready] = new List<GrayReleaseStatus> 
                { GrayReleaseStatus.Active, GrayReleaseStatus.Draft, GrayReleaseStatus.Terminated },
            [GrayReleaseStatus.Active] = new List<GrayReleaseStatus> 
                { GrayReleaseStatus.Paused, GrayReleaseStatus.Terminated },
            [GrayReleaseStatus.Paused] = new List<GrayReleaseStatus> 
                { GrayReleaseStatus.Active, GrayReleaseStatus.Terminated },
            [GrayReleaseStatus.Terminated] = new List<GrayReleaseStatus>()
        };
        
        if (!validTransitions[from].Contains(to))
        {
            throw new InvalidOperationException($"Invalid status transition from {from} to {to}");
        }
    }
    
    private bool CheckEligibility(OrderRecord order, GrayReleasePolicy policy)
    {
        foreach (var target in policy.Targets)
        {
            var matches = target.TargetType switch
            {
                GrayTargetType.Store => target.TargetIds.Contains(order.StoreId),
                GrayTargetType.User => target.TargetIds.Contains(order.UserId),
                GrayTargetType.Channel => target.TargetIds.Contains(order.Channel),
                GrayTargetType.MemberLevel => target.TargetIds.Contains(order.MemberLevel),
                GrayTargetType.Region => CheckRegion(order, target),
                GrayTargetType.Tag => CheckTags(order, target),
                GrayTargetType.Percentage => CheckPercentage(order, policy, target),
                _ => false
            };
            
            if (!matches && target.MatchMode == GrayMatchMode.Include)
                return false;
            if (matches && target.MatchMode == GrayMatchMode.Exclude)
                return false;
        }
        
        return true;
    }
    
    private bool CheckRegion(OrderRecord order, GrayTarget target)
    {
        return true;
    }
    
    private bool CheckTags(OrderRecord order, GrayTarget target)
    {
        return true;
    }
    
    private bool CheckPercentage(OrderRecord order, GrayReleasePolicy policy, GrayTarget target)
    {
        var hash = order.OrderId.GetHashCode();
        var percentage = Math.Abs(hash % 100);
        return percentage < policy.ScaleConfig.CurrentPercentage;
    }
    
    private OrderContext BuildOrderContext(OrderRecord order)
    {
        var context = _engine.CreateOrderContext(order.OrderId);
        
        context.AddItem(new OrderItem
        {
            ProductId = $"SKU-{order.OrderId}",
            Price = order.OriginalAmount,
            Quantity = 1
        });
        
        if (!string.IsNullOrEmpty(order.MemberLevel))
        {
            context.Member = new MemberInfo
            {
                MemberId = order.UserId,
                Level = Enum.TryParse<MemberLevel>(order.MemberLevel, out var level) ? level : MemberLevel.Normal
            };
        }
        
        context.ExtendedData["StoreId"] = order.StoreId;
        context.ExtendedData["ChannelId"] = order.Channel;
        
        return context;
    }
    
    private CalculationResult ExecuteWithOldRules(OrderContext context, List<Coupon> coupons)
    {
        context.ExtendedData["RuleVersion"] = "Old";
        return _engine.CalculateOptimalEnhanced(context, coupons);
    }
    
    private CalculationResult ExecuteWithNewRules(OrderContext context, List<Coupon> coupons, GrayReleasePolicy policy)
    {
        context.ExtendedData["RuleVersion"] = policy.RuleVersionId;
        return _engine.CalculateOptimalEnhanced(context, coupons);
    }
    
    private List<Coupon> LoadCouponsForOrder(OrderRecord order)
    {
        return new List<Coupon>
        {
            new Coupon
            {
                CouponId = $"COUPON-{order.OrderId}",
                Type = CouponType.PercentOff,
                DiscountValue = 10,
                MinOrderAmount = 50
            }
        };
    }
}

public class OrderRecord
{
    public string OrderId { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string MemberLevel { get; set; } = string.Empty;
}
