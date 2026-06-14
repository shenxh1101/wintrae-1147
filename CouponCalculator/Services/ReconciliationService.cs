using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class ReconciliationService
{
    private readonly List<CalculationCheckpoint> _checkpoints = new();
    
    public CalculationCheckpoint CreateCheckpoint(
        OrderContext context,
        CalculationResult result,
        EnhancedCalculationResult enhancedResult,
        ItemLevelDiscountBreakdown itemBreakdown,
        CalculationStage stage,
        string operatorName,
        string operatorRole,
        string ruleVersionId = "")
    {
        var checkpoint = new CalculationCheckpoint
        {
            OrderId = context.OrderId,
            Stage = stage,
            StageDescription = stage.ToString(),
            Timestamp = DateTime.UtcNow,
            Operator = operatorName,
            OperatorRole = operatorRole,
            Result = result,
            EnhancedResult = enhancedResult,
            ItemBreakdown = itemBreakdown,
            ContextSnapshot = CreateContextSnapshot(context),
            RuleVersionId = ruleVersionId,
            AppliedCouponIds = result.AppliedCoupons.Select(c => c.CouponId).ToList()
        };
        
        checkpoint.CouponSnapshots = result.AppliedCoupons
            .Select(c => new CouponSnapshot
            {
                CouponId = c.CouponId,
                CouponCode = c.CouponCode,
                CouponName = c.Description,
                Type = c.Type,
                DiscountValue = c.DiscountAmount,
                Status = "Applied"
            })
            .ToList();
        
        if (enhancedResult.RecommendedPlan != null)
        {
            checkpoint.AppliedRuleIds = enhancedResult.RecommendedPlan.AppliedCoupons
                .Select(c => c.CouponId)
                .ToList();
        }
        
        _checkpoints.Add(checkpoint);
        
        return checkpoint;
    }
    
    public CalculationComparison CompareCheckpoints(string fromCheckpointId, string toCheckpointId)
    {
        var fromCheckpoint = GetCheckpoint(fromCheckpointId);
        var toCheckpoint = GetCheckpoint(toCheckpointId);
        
        if (fromCheckpoint == null || toCheckpoint == null)
        {
            throw new ArgumentException("Checkpoint not found");
        }
        
        var comparison = new CalculationComparison
        {
            OrderId = fromCheckpoint.OrderId,
            FromCheckpointId = fromCheckpointId,
            ToCheckpointId = toCheckpointId,
            FromStage = fromCheckpoint.Stage,
            ToStage = toCheckpoint.Stage,
            ComparisonTime = DateTime.UtcNow
        };
        
        comparison.Differences = FindDifferences(fromCheckpoint, toCheckpoint);
        comparison.HasChanges = comparison.Differences.Any();
        
        comparison.AmountDifference = toCheckpoint.Result.FinalAmount - fromCheckpoint.Result.FinalAmount;
        comparison.DiscountDifference = toCheckpoint.Result.TotalDiscountAmount - fromCheckpoint.Result.TotalDiscountAmount;
        
        comparison.ChangeReasons = IdentifyChangeReasons(fromCheckpoint, toCheckpoint, comparison.Differences);
        comparison.TriggeredRules = IdentifyTriggeredRules(fromCheckpoint, toCheckpoint, comparison.Differences);
        
        comparison.ImpactLevel = DetermineImpactLevel(comparison);
        comparison.ImpactWarnings = GenerateImpactWarnings(comparison);
        
        return comparison;
    }
    
    public ReconciliationReport GenerateReconciliationReport(string orderId)
    {
        var orderCheckpoints = _checkpoints
            .Where(c => c.OrderId == orderId)
            .OrderBy(c => c.Timestamp)
            .ToList();
        
        var report = new ReconciliationReport
        {
            OrderId = orderId,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "System",
            Checkpoints = orderCheckpoints,
            Summary = GenerateSummary(orderCheckpoints)
        };
        
        report.Comparisons = GenerateComparisons(orderCheckpoints);
        report.AuditTrail = GenerateAuditTrail(orderCheckpoints);
        report.Recommendations = GenerateRecommendations(report.Summary);
        
        return report;
    }
    
    public List<CalculationCheckpoint> GetCheckpointsByOrder(string orderId)
    {
        return _checkpoints
            .Where(c => c.OrderId == orderId)
            .OrderBy(c => c.Timestamp)
            .ToList();
    }
    
    public CalculationCheckpoint? GetCheckpoint(string checkpointId)
    {
        return _checkpoints.FirstOrDefault(c => c.CheckpointId == checkpointId);
    }
    
    public CalculationCheckpoint? GetLatestCheckpoint(string orderId)
    {
        return _checkpoints
            .Where(c => c.OrderId == orderId)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefault();
    }
    
    public List<CalculationCheckpoint> GetCheckpointsByStage(CalculationStage stage)
    {
        return _checkpoints
            .Where(c => c.Stage == stage)
            .OrderByDescending(c => c.Timestamp)
            .ToList();
    }
    
    public List<CalculationCheckpoint> GetCheckpointsByTimeRange(DateTime from, DateTime to)
    {
        return _checkpoints
            .Where(c => c.Timestamp >= from && c.Timestamp <= to)
            .OrderBy(c => c.Timestamp)
            .ToList();
    }
    
    public List<CalculationComparison> FindInconsistentCalculations(DateTime from, DateTime to)
    {
        var checkpointsInRange = GetCheckpointsByTimeRange(from, to);
        var comparisons = new List<CalculationComparison>();
        
        var groupedByOrder = checkpointsInRange.GroupBy(c => c.OrderId);
        
        foreach (var group in groupedByOrder)
        {
            var checkpoints = group.ToList();
            for (int i = 0; i < checkpoints.Count - 1; i++)
            {
                var comparison = CompareCheckpoints(
                    checkpoints[i].CheckpointId,
                    checkpoints[i + 1].CheckpointId);
                
                if (comparison.HasChanges && comparison.ImpactLevel != ImpactLevel.None)
                {
                    comparisons.Add(comparison);
                }
            }
        }
        
        return comparisons;
    }
    
    private OrderContextSnapshot CreateContextSnapshot(OrderContext context)
    {
        return new OrderContextSnapshot
        {
            OriginalAmount = context.OriginalAmount,
            FreightAmount = context.FreightAmount,
            ItemCount = context.Items.Count,
            TotalQuantity = context.Items.Sum(i => i.Quantity),
            Items = context.Items.Select(i => new ItemSnapshot
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity,
                CategoryId = i.CategoryId
            }).ToList(),
            Member = context.Member != null ? new MemberSnapshot
            {
                MemberId = context.Member.MemberId,
                Level = context.Member.Level,
                Points = context.Member.Points
            } : null,
            Shipping = context.Shipping != null ? new ShippingSnapshot
            {
                ShippingMethod = context.Shipping.ShippingMethod,
                Weight = context.Shipping.Weight,
                CalculatedFreight = context.FreightAmount
            } : null
        };
    }
    
    private List<ResultDifference> FindDifferences(
        CalculationCheckpoint from,
        CalculationCheckpoint to)
    {
        var differences = new List<ResultDifference>();
        
        if (from.Result.OriginalAmount != to.Result.OriginalAmount)
        {
            differences.Add(new ResultDifference
            {
                FieldName = "原始金额",
                OldValue = $"¥{from.Result.OriginalAmount:F2}",
                NewValue = $"¥{to.Result.OriginalAmount:F2}",
                ChangeDescription = $"变化 ¥{to.Result.OriginalAmount - from.Result.OriginalAmount:F2}",
                ChangeReason = "订单商品或数量发生变化",
                Type = DifferenceType.Amount,
                Severity = DifferenceSeverity.Major
            });
        }
        
        if (from.Result.ProductDiscountAmount != to.Result.ProductDiscountAmount)
        {
            differences.Add(new ResultDifference
            {
                FieldName = "商品优惠",
                OldValue = $"¥{from.Result.ProductDiscountAmount:F2}",
                NewValue = $"¥{to.Result.ProductDiscountAmount:F2}",
                ChangeDescription = $"变化 ¥{to.Result.ProductDiscountAmount - from.Result.ProductDiscountAmount:F2}",
                ChangeReason = "优惠券应用发生变化",
                Type = DifferenceType.Discount,
                Severity = DifferenceSeverity.Moderate
            });
        }
        
        if (from.Result.MemberDiscount != to.Result.MemberDiscount)
        {
            differences.Add(new ResultDifference
            {
                FieldName = "会员折扣",
                OldValue = $"¥{from.Result.MemberDiscount:F2}",
                NewValue = $"¥{to.Result.MemberDiscount:F2}",
                ChangeDescription = $"变化 ¥{to.Result.MemberDiscount - from.Result.MemberDiscount:F2}",
                ChangeReason = "会员等级或订单金额变化",
                Type = DifferenceType.MemberDiscount,
                Severity = DifferenceSeverity.Minor
            });
        }
        
        if (from.Result.FreightDiscount != to.Result.FreightDiscount)
        {
            differences.Add(new ResultDifference
            {
                FieldName = "运费优惠",
                OldValue = $"¥{from.Result.FreightDiscount:F2}",
                NewValue = $"¥{to.Result.FreightDiscount:F2}",
                ChangeDescription = $"变化 ¥{to.Result.FreightDiscount - from.Result.FreightDiscount:F2}",
                ChangeReason = "免运费券应用变化",
                Type = DifferenceType.Freight,
                Severity = DifferenceSeverity.Minor
            });
        }
        
        var fromCouponIds = from.AppliedCouponIds.ToHashSet();
        var toCouponIds = to.AppliedCouponIds.ToHashSet();
        
        var addedCoupons = toCouponIds.Except(fromCouponIds);
        var removedCoupons = fromCouponIds.Except(toCouponIds);
        
        foreach (var couponId in addedCoupons)
        {
            var coupon = to.CouponSnapshots.FirstOrDefault(c => c.CouponId == couponId);
            differences.Add(new ResultDifference
            {
                FieldName = "优惠券应用",
                OldValue = "未应用",
                NewValue = coupon?.CouponName ?? couponId,
                ChangeDescription = "新增应用优惠券",
                ChangeReason = "优惠券满足条件或规则变化",
                Type = DifferenceType.CouponApplied,
                Severity = DifferenceSeverity.Moderate,
                RelatedCoupons = new List<string> { couponId }
            });
        }
        
        foreach (var couponId in removedCoupons)
        {
            var coupon = from.CouponSnapshots.FirstOrDefault(c => c.CouponId == couponId);
            differences.Add(new ResultDifference
            {
                FieldName = "优惠券移除",
                OldValue = coupon?.CouponName ?? couponId,
                NewValue = "未应用",
                ChangeDescription = "移除已应用优惠券",
                ChangeReason = "优惠券不再满足条件或被其他券替代",
                Type = DifferenceType.CouponRemoved,
                Severity = DifferenceSeverity.Moderate,
                RelatedCoupons = new List<string> { couponId }
            });
        }
        
        if (from.RuleVersionId != to.RuleVersionId)
        {
            differences.Add(new ResultDifference
            {
                FieldName = "规则版本",
                OldValue = from.RuleVersionName,
                NewValue = to.RuleVersionName,
                ChangeDescription = "规则版本切换",
                ChangeReason = "定时切换或手动切换规则版本",
                Type = DifferenceType.RuleChange,
                Severity = DifferenceSeverity.Major
            });
        }
        
        return differences;
    }
    
    private List<string> IdentifyChangeReasons(
        CalculationCheckpoint from,
        CalculationCheckpoint to,
        List<ResultDifference> differences)
    {
        var reasons = new List<string>();
        
        if (differences.Any(d => d.Type == DifferenceType.Amount))
        {
            reasons.Add("订单金额发生变化，导致优惠门槛判断改变");
        }
        
        if (differences.Any(d => d.Type == DifferenceType.CouponApplied))
        {
            reasons.Add("新增优惠券应用，可能因满足门槛或规则变化");
        }
        
        if (differences.Any(d => d.Type == DifferenceType.CouponRemoved))
        {
            reasons.Add("移除优惠券应用，可能因不再满足条件或被替代");
        }
        
        if (differences.Any(d => d.Type == DifferenceType.RuleChange))
        {
            reasons.Add($"规则版本从 {from.RuleVersionName} 切换到 {to.RuleVersionName}");
        }
        
        if (from.ContextSnapshot.Member?.Level != to.ContextSnapshot.Member?.Level)
        {
            reasons.Add("会员等级发生变化，影响会员折扣");
        }
        
        if (!reasons.Any() && differences.Any())
        {
            reasons.Add("计算逻辑或参数发生变化");
        }
        
        return reasons;
    }
    
    private List<string> IdentifyTriggeredRules(
        CalculationCheckpoint from,
        CalculationCheckpoint to,
        List<ResultDifference> differences)
    {
        var rules = new List<string>();
        
        foreach (var diff in differences.Where(d => d.Type == DifferenceType.CouponApplied))
        {
            foreach (var couponId in diff.RelatedCoupons)
            {
                var coupon = to.CouponSnapshots.FirstOrDefault(c => c.CouponId == couponId);
                if (coupon != null)
                {
                    rules.Add($"优惠券规则: {coupon.CouponName} ({coupon.Type})");
                }
            }
        }
        
        foreach (var diff in differences.Where(d => d.Type == DifferenceType.RuleChange))
        {
            rules.Add($"规则版本切换: {from.RuleVersionName} → {to.RuleVersionName}");
        }
        
        return rules;
    }
    
    private ImpactLevel DetermineImpactLevel(CalculationComparison comparison)
    {
        if (!comparison.HasChanges) return ImpactLevel.None;
        
        var absAmountDiff = Math.Abs(comparison.AmountDifference);
        var absDiscountDiff = Math.Abs(comparison.DiscountDifference);
        
        if (absAmountDiff > 50m || absDiscountDiff > 30m)
            return ImpactLevel.High;
        
        if (absAmountDiff > 20m || absDiscountDiff > 10m)
            return ImpactLevel.Medium;
        
        if (absAmountDiff > 5m || absDiscountDiff > 3m)
            return ImpactLevel.Low;
        
        return ImpactLevel.None;
    }
    
    private List<string> GenerateImpactWarnings(CalculationComparison comparison)
    {
        var warnings = new List<string>();
        
        if (comparison.ImpactLevel == ImpactLevel.High)
        {
            warnings.Add("金额变化较大，建议人工核查");
        }
        
        if (comparison.Differences.Any(d => d.Type == DifferenceType.RuleChange))
        {
            warnings.Add("规则版本发生变化，可能影响其他订单");
        }
        
        if (comparison.Differences.Any(d => d.Type == DifferenceType.CouponRemoved))
        {
            warnings.Add("优惠券被移除，可能影响用户体验");
        }
        
        return warnings;
    }
    
    private List<CalculationComparison> GenerateComparisons(List<CalculationCheckpoint> checkpoints)
    {
        var comparisons = new List<CalculationComparison>();
        
        for (int i = 0; i < checkpoints.Count - 1; i++)
        {
            comparisons.Add(CompareCheckpoints(
                checkpoints[i].CheckpointId,
                checkpoints[i + 1].CheckpointId));
        }
        
        return comparisons;
    }
    
    private ReconciliationSummary GenerateSummary(List<CalculationCheckpoint> checkpoints)
    {
        var summary = new ReconciliationSummary
        {
            TotalCalculations = checkpoints.Count
        };
        
        if (checkpoints.Count > 1)
        {
            var comparisons = GenerateComparisons(checkpoints);
            summary.ChangedCalculations = comparisons.Count(c => c.HasChanges);
            
            summary.MaxAmountFluctuation = comparisons
                .Max(c => Math.Abs(c.AmountDifference));
            
            summary.MaxDiscountFluctuation = comparisons
                .Max(c => Math.Abs(c.DiscountDifference));
            
            var lastCheckpoint = checkpoints.Last();
            summary.FinalAmount = lastCheckpoint.Result.FinalAmount;
            summary.FinalDiscount = lastCheckpoint.Result.TotalDiscountAmount;
            
            summary.AllAppliedCoupons = checkpoints
                .SelectMany(c => c.AppliedCouponIds)
                .Distinct()
                .ToList();
            
            summary.AllTriggeredRules = checkpoints
                .SelectMany(c => c.AppliedRuleIds)
                .Distinct()
                .ToList();
            
            if (summary.ChangedCalculations > 0)
            {
                summary.ConsistencyStatus = "存在变化";
                summary.InconsistencyReason = $"在 {summary.ChangedCalculations} 个计算节点中存在结果变化";
            }
            else
            {
                summary.ConsistencyStatus = "完全一致";
            }
        }
        else if (checkpoints.Count == 1)
        {
            summary.FinalAmount = checkpoints[0].Result.FinalAmount;
            summary.FinalDiscount = checkpoints[0].Result.TotalDiscountAmount;
            summary.ConsistencyStatus = "单一计算";
        }
        
        return summary;
    }
    
    private List<string> GenerateAuditTrail(List<CalculationCheckpoint> checkpoints)
    {
        var trail = new List<string>();
        
        foreach (var checkpoint in checkpoints)
        {
            trail.Add($"{checkpoint.Timestamp:yyyy-MM-dd HH:mm:ss} - {checkpoint.GetStageDescription()} by {checkpoint.Operator} ({checkpoint.OperatorRole})");
            trail.Add($"  应用优惠券: {string.Join(",", checkpoint.AppliedCouponIds.Take(5))}");
            trail.Add($"  最终金额: ¥{checkpoint.Result.FinalAmount:F2}");
        }
        
        return trail;
    }
    
    private List<string> GenerateRecommendations(ReconciliationSummary summary)
    {
        var recommendations = new List<string>();
        
        if (summary.ChangedCalculations > 0)
        {
            recommendations.Add("建议检查变化原因，确认是否符合预期");
            recommendations.Add("建议记录变化详情，用于后续审计");
        }
        
        if (summary.MaxAmountFluctuation > 50m)
        {
            recommendations.Add("金额波动较大，建议增加人工审核环节");
        }
        
        if (summary.AllAppliedCoupons.Count > 5)
        {
            recommendations.Add("应用优惠券较多，建议简化优惠策略");
        }
        
        if (summary.ConsistencyStatus != "完全一致")
        {
            recommendations.Add("计算结果不一致，建议排查规则配置");
        }
        
        return recommendations;
    }
}