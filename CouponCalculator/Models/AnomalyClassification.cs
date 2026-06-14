namespace CouponCalculator.Models;

public class AnomalyClassification
{
    public string ClassificationId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    
    public AnomalyCategory PrimaryCategory { get; set; }
    public AnomalyCategory? SecondaryCategory { get; set; }
    
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    
    public decimal ImpactAmount { get; set; }
    public decimal ImpactPercent { get; set; }
    
    public AnomalySeverity Severity { get; set; }
    public AnomalyPriority Priority { get; set; }
    
    public List<AnomalyCause> Causes { get; set; } = new();
    public List<AnomalyEvidence> Evidence { get; set; } = new();
    
    public string Description { get; set; } = string.Empty;
    public string UserFriendlyDescription { get; set; } = string.Empty;
    
    public List<string> Recommendations { get; set; } = new();
    public List<string> RelatedRules { get; set; } = new();
    public List<string> RelatedCoupons { get; set; } = new();
    
    public DateTime DetectedAt { get; set; }
    public string DetectedBy { get; set; } = string.Empty;
    
    public ClassificationStatus Status { get; set; }
    
    public List<ClassificationHistory> History { get; set; } = new();
    
    public string GenerateClassificationReport()
    {
        var report = new List<string>();
        
        report.Add($"【异常分类报告】订单: {OrderId}");
        report.Add($"主类别: {CategoryName} ({CategoryCode})");
        if (SecondaryCategory.HasValue)
            report.Add($"次类别: {SecondaryCategory.Value}");
        report.Add($"严重程度: {Severity}");
        report.Add($"优先级: {Priority}");
        report.Add($"影响金额: ¥{ImpactAmount:F2}");
        report.Add($"影响比例: {ImpactPercent:P2}");
        report.Add("");
        
        if (Causes.Any())
        {
            report.Add("【原因分析】");
            foreach (var cause in Causes)
            {
                report.Add($"  · {cause.CauseType}: {cause.Description}");
                report.Add($"    确信度: {cause.Confidence:P2}");
            }
            report.Add("");
        }
        
        if (Evidence.Any())
        {
            report.Add("【证据链】");
            foreach (var e in Evidence)
            {
                report.Add($"  · {e.Type}: {e.Description}");
                report.Add($"    来源: {e.Source}, 时间: {e.Timestamp:yyyy-MM-dd HH:mm}");
            }
            report.Add("");
        }
        
        if (Recommendations.Any())
        {
            report.Add("【处理建议】");
            foreach (var rec in Recommendations)
            {
                report.Add($"  ✓ {rec}");
            }
            report.Add("");
        }
        
        report.Add($"【用户说明】{UserFriendlyDescription}");
        
        return string.Join(Environment.NewLine, report);
    }
}

public enum AnomalyCategory
{
    RuleChange,
    CouponStatusChange,
    ExternalDataMismatch,
    SystemError,
    DataInconsistency,
    ThresholdViolation,
    StackingIssue,
    MemberDiscountIssue,
    ShippingCalculationIssue,
    TimeRangeIssue,
    VersionMismatch,
    ManualOverride,
    Unknown
}

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum AnomalyPriority
{
    P1,
    P2,
    P3,
    P4
}

public enum ClassificationStatus
{
    New,
    Analyzing,
    Classified,
    Confirmed,
    Resolved,
    Escalated,
    Closed
}

public class AnomalyCause
{
    public string CauseId { get; set; } = Guid.NewGuid().ToString();
    public string CauseType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public bool IsPrimary { get; set; }
    
    public List<string> SupportingData { get; set; } = new();
    public string? RuleReference { get; set; }
    public string? CouponReference { get; set; }
}

public class AnomalyEvidence
{
    public string EvidenceId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    public Dictionary<string, string> Data { get; set; } = new();
    public bool IsReliable { get; set; }
}

public class ClassificationHistory
{
    public string HistoryId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PreviousCategory { get; set; } = string.Empty;
    public string NewCategory { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class AnomalyLayerSummary
{
    public string LayerId { get; set; } = Guid.NewGuid().ToString();
    public string LayerName { get; set; } = string.Empty;
    public AnomalyCategory Category { get; set; }
    
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    
    public decimal TotalImpact { get; set; }
    public decimal AverageImpact { get; set; }
    public decimal MaxImpact { get; set; }
    
    public List<string> CommonCauses { get; set; } = new();
    public List<string> AffectedRules { get; set; } = new();
    public List<string> AffectedCoupons { get; set; } = new();
    
    public List<string> Recommendations { get; set; } = new();
    
    public string LayerDescription { get; set; } = string.Empty;
    public string LayerExplanation { get; set; } = string.Empty;
    
    public string GenerateLayerReport()
    {
        var report = new List<string>();
        
        report.Add($"【{LayerName}】异常分层汇总");
        report.Add($"类别: {Category}");
        report.Add($"总数: {TotalCount}");
        report.Add($"  Critical: {CriticalCount}");
        report.Add($"  High: {HighCount}");
        report.Add($"  Medium: {MediumCount}");
        report.Add($"  Low: {LowCount}");
        report.Add($"总影响金额: ¥{TotalImpact:F2}");
        report.Add($"平均影响: ¥{AverageImpact:F2}");
        report.Add($"最大影响: ¥{MaxImpact:F2}");
        report.Add("");
        
        if (CommonCauses.Any())
        {
            report.Add("常见原因:");
            foreach (var cause in CommonCauses.Take(5))
            {
                report.Add($"  · {cause}");
            }
        }
        
        if (Recommendations.Any())
        {
            report.Add("处理建议:");
            foreach (var rec in Recommendations)
            {
                report.Add($"  ✓ {rec}");
            }
        }
        
        report.Add("");
        report.Add($"说明: {LayerExplanation}");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class AnomalyClassificationResult
{
    public string ResultId { get; set; } = Guid.NewGuid().ToString();
    public string BatchId { get; set; } = string.Empty;
    
    public DateTime ClassifiedAt { get; set; }
    public int TotalAnomalies { get; set; }
    public int ClassifiedCount { get; set; }
    public int UnclassifiedCount { get; set; }
    
    public List<AnomalyClassification> Classifications { get; set; } = new();
    public List<AnomalyLayerSummary> Layers { get; set; } = new();
    
    public AnomalyClassificationSummary Summary { get; set; } = new();
    
    public AnomalyExportData ExportData { get; set; } = new();
    
    public string GenerateClassificationSummaryReport()
    {
        var report = new List<string>();
        
        report.Add("╔══════════════════════════════════════════════════════════════════════╗");
        report.Add("║              异常订单分层分类报告                                    ║");
        report.Add("╚══════════════════════════════════════════════════════════════════════╝");
        report.Add($"批次ID: {BatchId}");
        report.Add($"分类时间: {ClassifiedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add($"总异常数: {TotalAnomalies}");
        report.Add($"已分类: {ClassifiedCount}");
        report.Add($"未分类: {UnclassifiedCount}");
        report.Add("");
        
        report.Add("【分层汇总】");
        foreach (var layer in Layers.OrderByDescending(l => l.TotalImpact))
        {
            report.Add($"  {layer.LayerName}:");
            report.Add($"    数量: {layer.TotalCount} (Critical:{layer.CriticalCount}, High:{layer.HighCount})");
            report.Add($"    影响金额: ¥{layer.TotalImpact:F2}");
            report.Add($"    说明: {layer.LayerExplanation}");
        }
        report.Add("");
        
        report.Add("【按严重程度分布】");
        report.Add($"  Critical: {Summary.CriticalCount} ({Summary.CriticalImpact:F2})");
        report.Add($"  High: {Summary.HighCount} ({Summary.HighImpact:F2})");
        report.Add($"  Medium: {Summary.MediumCount} ({Summary.MediumImpact:F2})");
        report.Add($"  Low: {Summary.LowCount} ({Summary.LowImpact:F2})");
        report.Add("");
        
        report.Add("【TOP 10 影响订单】");
        foreach (var classification in Classifications.OrderByDescending(c => c.ImpactAmount).Take(10))
        {
            report.Add($"  {classification.OrderId}: {classification.CategoryName}");
            report.Add($"    ¥{classification.ImpactAmount:F2} ({classification.Severity})");
            report.Add($"    {classification.UserFriendlyDescription}");
        }
        report.Add("");
        
        report.Add("【运营复盘建议】");
        foreach (var rec in Summary.OverallRecommendations)
        {
            report.Add($"  ✓ {rec}");
        }
        
        report.Add("╚══════════════════════════════════════════════════════════════════════╝");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class AnomalyClassificationSummary
{
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    
    public decimal CriticalImpact { get; set; }
    public decimal HighImpact { get; set; }
    public decimal MediumImpact { get; set; }
    public decimal LowImpact { get; set; }
    
    public Dictionary<AnomalyCategory, int> CategoryDistribution { get; set; } = new();
    public Dictionary<AnomalyCategory, decimal> CategoryImpact { get; set; } = new();
    
    public List<string> TopAffectedRules { get; set; } = new();
    public List<string> TopAffectedCoupons { get; set; } = new();
    
    public List<string> OverallRecommendations { get; set; } = new();
    
    public string ReviewSummary { get; set; } = string.Empty;
}

public class AnomalyExportData
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public List<AnomalyExportRow> DetailedRows { get; set; } = new();
}

public class AnomalyExportRow
{
    public string OrderId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ImpactAmount { get; set; }
    public decimal ImpactPercent { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UserDescription { get; set; } = string.Empty;
    public string Causes { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string RelatedRules { get; set; } = string.Empty;
    public string RelatedCoupons { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class AnomalyClassifier
{
    private readonly Dictionary<string, ClassificationRule> _rules = new();
    
    public AnomalyClassifier()
    {
        InitializeDefaultRules();
    }
    
    private void InitializeDefaultRules()
    {
        _rules["RULE_CHANGE"] = new ClassificationRule
        {
            Category = AnomalyCategory.RuleChange,
            Name = "规则变动导致",
            DetectionConditions = new List<DetectionCondition>
            {
                new DetectionCondition { Field = "RuleVersionDiff", Operator = "exists", Weight = 0.8m },
                new DetectionCondition { Field = "AppliedRuleIdsDiff", Operator = "changed", Weight = 0.6m }
            },
            DefaultExplanation = "规则版本切换导致优惠计算结果变化",
            DefaultRecommendations = new List<string>
            {
                "核实规则版本切换时间点",
                "确认是否在预期范围内",
                "检查规则配置是否正确"
            }
        };
        
        _rules["COUPON_STATUS"] = new ClassificationRule
        {
            Category = AnomalyCategory.CouponStatusChange,
            Name = "券状态变动导致",
            DetectionConditions = new List<DetectionCondition>
            {
                new DetectionCondition { Field = "CouponStatusDiff", Operator = "exists", Weight = 0.9m },
                new DetectionCondition { Field = "CouponValidityDiff", Operator = "changed", Weight = 0.7m }
            },
            DefaultExplanation = "优惠券状态发生变化（过期、失效、使用等）导致计算差异",
            DefaultRecommendations = new List<string>
            {
                "检查券的有效期设置",
                "核实券的使用状态",
                "确认券的适用范围"
            }
        };
        
        _rules["EXTERNAL_MISMATCH"] = new ClassificationRule
        {
            Category = AnomalyCategory.ExternalDataMismatch,
            Name = "外部回传口径不一致",
            DetectionConditions = new List<DetectionCondition>
            {
                new DetectionCondition { Field = "ExternalSystemDiff", Operator = "exists", Weight = 0.85m },
                new DetectionCondition { Field = "PaymentSystemDiff", Operator = "exists", Weight = 0.8m }
            },
            DefaultExplanation = "外部系统（支付、订单等）回传数据与本地计算不一致",
            DefaultRecommendations = new List<string>
            {
                "核对各系统数据同步时间",
                "检查接口字段映射是否一致",
                "确认是否存在数据延迟"
            }
        };
        
        _rules["SYSTEM_ERROR"] = new ClassificationRule
        {
            Category = AnomalyCategory.SystemError,
            Name = "系统错误导致",
            DetectionConditions = new List<DetectionCondition>
            {
                new DetectionCondition { Field = "CalculationError", Operator = "exists", Weight = 0.95m },
                new DetectionCondition { Field = "ExceptionLog", Operator = "exists", Weight = 0.9m }
            },
            DefaultExplanation = "系统计算过程中出现错误导致结果异常",
            DefaultRecommendations = new List<string>
            {
                "检查系统日志定位错误",
                "确认计算引擎版本",
                "必要时人工复核"
            }
        };
        
        _rules["DATA_INCONSISTENCY"] = new ClassificationRule
        {
            Category = AnomalyCategory.DataInconsistency,
            Name = "数据不一致",
            DetectionConditions = new List<DetectionCondition>
            {
                new DetectionCondition { Field = "OrderDataDiff", Operator = "exists", Weight = 0.75m },
                new DetectionCondition { Field = "ItemDataDiff", Operator = "changed", Weight = 0.6m }
            },
            DefaultExplanation = "订单数据在不同时点存在不一致",
            DefaultRecommendations = new List<string>
            {
                "核对订单数据变更记录",
                "确认是否存在并发修改",
                "检查数据同步机制"
            }
        };
    }
    
    public AnomalyClassification ClassifyAnomaly(OrderReconciliationDetail detail)
    {
        var classification = new AnomalyClassification
        {
            ClassificationId = Guid.NewGuid().ToString(),
            OrderId = detail.OrderId,
            DetectedAt = DateTime.UtcNow,
            DetectedBy = "AutoClassifier",
            Status = ClassificationStatus.New,
            ImpactAmount = Math.Abs(detail.Difference),
            ImpactPercent = detail.OriginalAmount > 0 ? Math.Abs(detail.Difference) / detail.OriginalAmount : 0
        };
        
        classification.Severity = DetermineSeverity(classification.ImpactAmount, classification.ImpactPercent);
        classification.Priority = DeterminePriority(classification.Severity);
        
        var matchedRules = new List<(ClassificationRule Rule, decimal Score)>();
        
        foreach (var rule in _rules.Values)
        {
            var score = EvaluateRule(rule, detail);
            if (score > 0.5m)
            {
                matchedRules.Add((rule, score));
            }
        }
        
        if (matchedRules.Any())
        {
            var bestMatch = matchedRules.OrderByDescending(x => x.Score).First();
            classification.PrimaryCategory = bestMatch.Rule.Category;
            classification.CategoryCode = bestMatch.Rule.Category.ToString();
            classification.CategoryName = bestMatch.Rule.Name;
            classification.Description = bestMatch.Rule.DefaultExplanation;
            classification.UserFriendlyDescription = GenerateUserFriendlyDescription(bestMatch.Rule, detail);
            classification.Recommendations = new List<string>(bestMatch.Rule.DefaultRecommendations);
            
            if (matchedRules.Count > 1)
            {
                classification.SecondaryCategory = matchedRules[1].Rule.Category;
            }
            
            classification.Causes.Add(new AnomalyCause
            {
                CauseType = bestMatch.Rule.Name,
                Description = bestMatch.Rule.DefaultExplanation,
                Confidence = bestMatch.Score,
                IsPrimary = true
            });
        }
        else
        {
            classification.PrimaryCategory = AnomalyCategory.Unknown;
            classification.CategoryCode = "UNKNOWN";
            classification.CategoryName = "未分类异常";
            classification.Description = "无法自动分类的异常订单";
            classification.UserFriendlyDescription = $"订单{detail.OrderId}存在¥{classification.ImpactAmount:F2}差异，需人工分析";
            classification.Recommendations.Add("建议人工复核此订单");
        }
        
        classification.RelatedRules = detail.ChangeReasons?.Where(r => r.Contains("规则")).ToList() ?? new List<string>();
        classification.RelatedCoupons = detail.AppliedCoupons ?? new List<string>();
        
        classification.Evidence.Add(new AnomalyEvidence
        {
            Type = "差异金额",
            Description = $"差异: ¥{detail.Difference:F2}",
            Source = "对账系统",
            Timestamp = DateTime.UtcNow,
            IsReliable = true,
            Data = new Dictionary<string, string>
            {
                ["OriginalAmount"] = detail.OriginalAmount.ToString(),
                ["Stage1Amount"] = detail.Stage1Amount.ToString(),
                ["Stage2Amount"] = detail.Stage2Amount.ToString()
            }
        });
        
        classification.Status = ClassificationStatus.Classified;
        
        return classification;
    }
    
    public AnomalyClassificationResult BatchClassify(List<OrderReconciliationDetail> details)
    {
        var result = new AnomalyClassificationResult
        {
            ResultId = Guid.NewGuid().ToString(),
            ClassifiedAt = DateTime.UtcNow,
            TotalAnomalies = details.Count(d => d.HasDifference || d.IsAnomaly)
        };
        
        foreach (var detail in details.Where(d => d.HasDifference || d.IsAnomaly))
        {
            var classification = ClassifyAnomaly(detail);
            result.Classifications.Add(classification);
        }
        
        result.ClassifiedCount = result.Classifications.Count(c => c.PrimaryCategory != AnomalyCategory.Unknown);
        result.UnclassifiedCount = result.Classifications.Count(c => c.PrimaryCategory == AnomalyCategory.Unknown);
        
        BuildLayerSummaries(result);
        BuildSummary(result);
        BuildExportData(result);
        
        return result;
    }
    
    private decimal EvaluateRule(ClassificationRule rule, OrderReconciliationDetail detail)
    {
        var totalScore = 0m;
        var totalWeight = 0m;
        
        foreach (var condition in rule.DetectionConditions)
        {
            var matchScore = EvaluateCondition(condition, detail);
            totalScore += matchScore * condition.Weight;
            totalWeight += condition.Weight;
        }
        
        return totalWeight > 0 ? totalScore / totalWeight : 0;
    }
    
    private decimal EvaluateCondition(DetectionCondition condition, OrderReconciliationDetail detail)
    {
        return condition.Field switch
        {
            "RuleVersionDiff" => detail.ChangeReasons?.Any(r => r.Contains("规则") || r.Contains("版本")) ? 1m : 0m,
            "AppliedRuleIdsDiff" => detail.ChangeReasons?.Any(r => r.Contains("规则")) ? 1m : 0m,
            "CouponStatusDiff" => detail.ChangeReasons?.Any(r => r.Contains("券状态") || r.Contains("过期") || r.Contains("失效")) ? 1m : 0m,
            "CouponValidityDiff" => detail.ChangeReasons?.Any(r => r.Contains("券")) ? 0.5m : 0m,
            "ExternalSystemDiff" => detail.ChangeReasons?.Any(r => r.Contains("外部") || r.Contains("支付") || r.Contains("回传")) ? 1m : 0m,
            "PaymentSystemDiff" => detail.ChangeReasons?.Any(r => r.Contains("支付")) ? 1m : 0m,
            "CalculationError" => detail.IsAnomaly ? 1m : 0m,
            "ExceptionLog" => detail.IsAnomaly ? 0.5m : 0m,
            "OrderDataDiff" => detail.HasDifference ? 1m : 0m,
            "ItemDataDiff" => detail.HasDifference ? 0.5m : 0m,
            _ => 0m
        };
    }
    
    private AnomalySeverity DetermineSeverity(decimal impactAmount, decimal impactPercent)
    {
        if (impactAmount > 100 || impactPercent > 0.5m)
            return AnomalySeverity.Critical;
        if (impactAmount > 50 || impactPercent > 0.3m)
            return AnomalySeverity.High;
        if (impactAmount > 10 || impactPercent > 0.1m)
            return AnomalySeverity.Medium;
        return AnomalySeverity.Low;
    }
    
    private AnomalyPriority DeterminePriority(AnomalySeverity severity)
    {
        return severity switch
        {
            AnomalySeverity.Critical => AnomalyPriority.P1,
            AnomalySeverity.High => AnomalyPriority.P2,
            AnomalySeverity.Medium => AnomalyPriority.P3,
            AnomalySeverity.Low => AnomalyPriority.P4
        };
    }
    
    private string GenerateUserFriendlyDescription(ClassificationRule rule, OrderReconciliationDetail detail)
    {
        var templates = new Dictionary<AnomalyCategory, string>
        {
            [AnomalyCategory.RuleChange] = "规则调整导致优惠变化{amount}元，属于正常版本切换影响",
            [AnomalyCategory.CouponStatusChange] = "优惠券状态变化导致优惠减少{amount}元，请核实券的有效性",
            [AnomalyCategory.ExternalDataMismatch] = "系统数据不一致，差异{amount}元，需核对各系统数据",
            [AnomalyCategory.SystemError] = "计算异常导致差异{amount}元，建议人工复核",
            [AnomalyCategory.DataInconsistency] = "订单数据变更导致差异{amount}元，请核实变更记录"
        };
        
        var template = templates.GetValueOrDefault(rule.Category, "订单存在{amount}元差异，需人工分析");
        return template.Replace("{amount}", Math.Abs(detail.Difference).ToString("F2"));
    }
    
    private void BuildLayerSummaries(AnomalyClassificationResult result)
    {
        var categoryGroups = result.Classifications.GroupBy(c => c.PrimaryCategory);
        
        foreach (var group in categoryGroups)
        {
            var layer = new AnomalyLayerSummary
            {
                LayerId = Guid.NewGuid().ToString(),
                Category = group.Key,
                LayerName = GetLayerName(group.Key),
                TotalCount = group.Count(),
                CriticalCount = group.Count(c => c.Severity == AnomalySeverity.Critical),
                HighCount = group.Count(c => c.Severity == AnomalySeverity.High),
                MediumCount = group.Count(c => c.Severity == AnomalySeverity.Medium),
                LowCount = group.Count(c => c.Severity == AnomalySeverity.Low),
                TotalImpact = group.Sum(c => c.ImpactAmount),
                AverageImpact = group.Average(c => c.ImpactAmount),
                MaxImpact = group.Max(c => c.ImpactAmount)
            };
            
            layer.CommonCauses = group
                .SelectMany(c => c.Causes.Select(ca => ca.CauseType))
                .Distinct()
                .Take(5)
                .ToList();
            
            layer.AffectedRules = group
                .SelectMany(c => c.RelatedRules)
                .Distinct()
                .ToList();
            
            layer.AffectedCoupons = group
                .SelectMany(c => c.RelatedCoupons)
                .Distinct()
                .ToList();
            
            layer.Recommendations = GetLayerRecommendations(group.Key);
            layer.LayerExplanation = GetLayerExplanation(group.Key);
            
            result.Layers.Add(layer);
        }
    }
    
    private void BuildSummary(AnomalyClassificationResult result)
    {
        result.Summary = new AnomalyClassificationSummary
        {
            CriticalCount = result.Classifications.Count(c => c.Severity == AnomalySeverity.Critical),
            HighCount = result.Classifications.Count(c => c.Severity == AnomalySeverity.High),
            MediumCount = result.Classifications.Count(c => c.Severity == AnomalySeverity.Medium),
            LowCount = result.Classifications.Count(c => c.Severity == AnomalySeverity.Low),
            CriticalImpact = result.Classifications.Where(c => c.Severity == AnomalySeverity.Critical).Sum(c => c.ImpactAmount),
            HighImpact = result.Classifications.Where(c => c.Severity == AnomalySeverity.High).Sum(c => c.ImpactAmount),
            MediumImpact = result.Classifications.Where(c => c.Severity == AnomalySeverity.Medium).Sum(c => c.ImpactAmount),
            LowImpact = result.Classifications.Where(c => c.Severity == AnomalySeverity.Low).Sum(c => c.ImpactAmount)
        };
        
        result.Summary.CategoryDistribution = result.Classifications
            .GroupBy(c => c.PrimaryCategory)
            .ToDictionary(g => g.Key, g => g.Count());
        
        result.Summary.CategoryImpact = result.Classifications
            .GroupBy(c => c.PrimaryCategory)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.ImpactAmount));
        
        result.Summary.TopAffectedRules = result.Classifications
            .SelectMany(c => c.RelatedRules)
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(10)
            .ToList();
        
        result.Summary.TopAffectedCoupons = result.Classifications
            .SelectMany(c => c.RelatedCoupons)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(10)
            .ToList();
        
        result.Summary.OverallRecommendations = GenerateOverallRecommendations(result);
        
        result.Summary.ReviewSummary = GenerateReviewSummary(result);
    }
    
    private void BuildExportData(AnomalyClassificationResult result)
    {
        result.ExportData.Headers = new List<string>
        {
            "订单ID", "分类", "分类代码", "严重程度", "优先级",
            "影响金额", "影响比例", "描述", "用户说明",
            "原因", "建议", "相关规则", "相关优惠券", "状态", "检测时间"
        };
        
        result.ExportData.DetailedRows = result.Classifications
            .OrderByDescending(c => c.ImpactAmount)
            .Select(c => new AnomalyExportRow
            {
                OrderId = c.OrderId,
                Category = c.CategoryName,
                CategoryCode = c.CategoryCode,
                Severity = c.Severity.ToString(),
                Priority = c.Priority.ToString(),
                ImpactAmount = c.ImpactAmount,
                ImpactPercent = c.ImpactPercent,
                Description = c.Description,
                UserDescription = c.UserFriendlyDescription,
                Causes = string.Join("; ", c.Causes.Select(ca => ca.Description)),
                Recommendations = string.Join("; ", c.Recommendations),
                RelatedRules = string.Join(", ", c.RelatedRules),
                RelatedCoupons = string.Join(", ", c.RelatedCoupons),
                Status = c.Status.ToString(),
                DetectedAt = c.DetectedAt
            })
            .ToList();
        
        result.ExportData.Rows = result.ExportData.DetailedRows
            .Select(r => new List<string>
            {
                r.OrderId, r.Category, r.CategoryCode, r.Severity, r.Priority,
                r.ImpactAmount.ToString("F2"), r.ImpactPercent.ToString("P2"),
                r.Description, r.UserDescription, r.Causes, r.Recommendations,
                r.RelatedRules, r.RelatedCoupons, r.Status, r.DetectedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }
    
    private string GetLayerName(AnomalyCategory category)
    {
        return category switch
        {
            AnomalyCategory.RuleChange => "规则变动层",
            AnomalyCategory.CouponStatusChange => "券状态变动层",
            AnomalyCategory.ExternalDataMismatch => "外部口径不一致层",
            AnomalyCategory.SystemError => "系统错误层",
            AnomalyCategory.DataInconsistency => "数据不一致层",
            _ => "其他异常层"
        };
    }
    
    private List<string> GetLayerRecommendations(AnomalyCategory category)
    {
        return category switch
        {
            AnomalyCategory.RuleChange => new List<string> { "核实规则版本切换时间", "确认切换范围", "检查规则配置" },
            AnomalyCategory.CouponStatusChange => new List<string> { "检查券有效期", "核实券状态", "确认券适用范围" },
            AnomalyCategory.ExternalDataMismatch => new List<string> { "核对系统数据同步", "检查接口映射", "确认数据延迟" },
            AnomalyCategory.SystemError => new List<string> { "检查系统日志", "确认引擎版本", "人工复核" },
            AnomalyCategory.DataInconsistency => new List<string> { "核对变更记录", "检查并发修改", "确认同步机制" },
            _ => new List<string> { "人工复核" }
        };
    }
    
    private string GetLayerExplanation(AnomalyCategory category)
    {
        return category switch
        {
            AnomalyCategory.RuleChange => "规则版本切换导致的正常差异，需确认是否在预期范围",
            AnomalyCategory.CouponStatusChange => "优惠券状态变化导致的差异，需核实券的有效性",
            AnomalyCategory.ExternalDataMismatch => "外部系统数据不一致，需核对各系统数据同步",
            AnomalyCategory.SystemError => "系统计算异常，需定位错误并人工复核",
            AnomalyCategory.DataInconsistency => "订单数据变更导致的差异，需核实变更记录",
            _ => "未分类异常，需人工分析"
        };
    }
    
    private List<string> GenerateOverallRecommendations(AnomalyClassificationResult result)
    {
        var recommendations = new List<string>();
        
        if (result.Summary.CriticalCount > 0)
            recommendations.Add($"优先处理{result.Summary.CriticalCount}个Critical级别异常");
        
        if (result.Layers.Any(l => l.Category == AnomalyCategory.RuleChange && l.TotalCount > 5))
            recommendations.Add("规则变动影响较大，建议核实版本切换范围");
        
        if (result.Layers.Any(l => l.Category == AnomalyCategory.ExternalDataMismatch && l.TotalCount > 3))
            recommendations.Add("外部数据不一致较多，建议检查系统同步机制");
        
        recommendations.Add("建议导出详细报告进行人工复核");
        recommendations.Add("建议建立异常处理流程并定期复盘");
        
        return recommendations;
    }
    
    private string GenerateReviewSummary(AnomalyClassificationResult result)
    {
        var summary = $"本次对账发现{result.TotalAnomalies}个异常订单，";
        summary += $"其中{result.Summary.CriticalCount}个Critical、{result.Summary.HighCount}个High级别。";
        
        var topCategory = result.Summary.CategoryDistribution.OrderByDescending(x => x.Value).FirstOrDefault();
        if (topCategory.Value > 0)
        {
            summary += $"主要原因是{GetLayerName(topCategory.Key)}({topCategory.Value}个)。";
        }
        
        summary += $"总影响金额¥{result.Classifications.Sum(c => c.ImpactAmount):F2}。";
        
        return summary;
    }
}

public class ClassificationRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public AnomalyCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<DetectionCondition> DetectionConditions { get; set; } = new();
    public string DefaultExplanation { get; set; } = string.Empty;
    public List<string> DefaultRecommendations { get; set; } = new();
}

public class DetectionCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1m;
}