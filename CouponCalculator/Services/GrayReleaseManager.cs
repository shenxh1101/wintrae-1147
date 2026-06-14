using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class GrayReleaseManager
{
    private readonly CouponCalculatorEngine _engine;
    private readonly Dictionary<string, GrayReleasePolicy> _policies = new();
    private readonly Dictionary<string, List<GrayPlaybackResult>> _playbackResults = new();
    private readonly Dictionary<string, GrayRiskAssessment> _riskAssessments = new();
    private readonly Dictionary<string, GrayReleaseMonitoring> _monitorings = new();
    
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
            StartMonitoring(policyId);
        }
        else if (newStatus == GrayReleaseStatus.Paused || newStatus == GrayReleaseStatus.Terminated)
        {
            policy.DeactivatedAt = DateTime.UtcNow;
        }
        
        return policy;
    }
    
    public GrayRiskAssessment AssessRisk(string policyId, List<OrderRecord> sampleOrders)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        var assessment = new GrayRiskAssessment
        {
            AssessmentId = Guid.NewGuid().ToString(),
            PolicyId = policyId,
            AssessmentTime = DateTime.UtcNow
        };
        
        var playbackResults = new List<GrayPlaybackResult>();
        foreach (var order in sampleOrders)
        {
            var result = PlaybackOrderSync(policyId, order, LoadCouponsForOrder(order));
            playbackResults.Add(result);
        }
        
        AnalyzeRiskFactors(assessment, playbackResults, policy);
        IdentifyRiskyOrders(assessment, playbackResults);
        CalculateRiskByScope(assessment, playbackResults);
        GenerateRecommendedScale(assessment, playbackResults, policy);
        GenerateRollbackPlan(assessment, policy);
        GenerateWarningsAndRecommendations(assessment);
        
        _riskAssessments[policyId] = assessment;
        return assessment;
    }
    
    private void AnalyzeRiskFactors(
        GrayRiskAssessment assessment,
        List<GrayPlaybackResult> playbackResults,
        GrayReleasePolicy policy)
    {
        var eligibleResults = playbackResults.Where(r => r.IsEligible).ToList();
        
        var largeDiffOrders = eligibleResults.Where(r => Math.Abs(r.DifferencePercent) > 0.5m).ToList();
        if (largeDiffOrders.Any())
        {
            assessment.RiskFactors.Add(new RiskFactor
            {
                Name = "大幅优惠差异",
                Description = $"有{largeDiffOrders.Count}个订单优惠变化超过50%",
                Level = largeDiffOrders.Count > eligibleResults.Count * 0.1 ? RiskLevel.High : RiskLevel.Medium,
                AffectedOrders = largeDiffOrders.Count,
                ImpactAmount = largeDiffOrders.Sum(r => Math.Abs(r.Difference)),
                ImpactLevel = "高"
            });
        }
        
        var couponChangeOrders = eligibleResults
            .Where(r => r.OldVersionResult?.RecommendedPlan?.AppliedCoupons?.Count != 
                        r.NewVersionResult?.RecommendedPlan?.AppliedCoupons?.Count)
            .ToList();
        if (couponChangeOrders.Any())
        {
            assessment.RiskFactors.Add(new RiskFactor
            {
                Name = "优惠券组合变化",
                Description = $"有{couponChangeOrders.Count}个订单的优惠券组合发生变化",
                Level = couponChangeOrders.Count > eligibleResults.Count * 0.05 ? RiskLevel.High : RiskLevel.Medium,
                AffectedOrders = couponChangeOrders.Count,
                ImpactAmount = couponChangeOrders.Sum(r => Math.Abs(r.Difference)),
                ImpactLevel = "中"
            });
        }
        
        var thresholdOrders = eligibleResults.Where(r => 
            Math.Abs(r.Difference) > policy.ScaleConfig.MaxPercentage * r.OldVersionResult?.OriginalAmount * 0.01m).ToList();
        if (thresholdOrders.Any())
        {
            assessment.RiskFactors.Add(new RiskFactor
            {
                Name = "超出阈值订单",
                Description = $"有{thresholdOrders.Count}个订单差异超出预设阈值",
                Level = RiskLevel.High,
                AffectedOrders = thresholdOrders.Count,
                ImpactAmount = thresholdOrders.Sum(r => Math.Abs(r.Difference)),
                ImpactLevel = "高"
            });
        }
        
        var negativeDiffOrders = eligibleResults.Where(r => r.Difference < -10m).ToList();
        if (negativeDiffOrders.Any())
        {
            assessment.RiskFactors.Add(new RiskFactor
            {
                Name = "优惠大幅减少",
                Description = $"有{negativeDiffOrders.Count}个订单优惠减少超过10元",
                Level = negativeDiffOrders.Count > 5 ? RiskLevel.Critical : RiskLevel.High,
                AffectedOrders = negativeDiffOrders.Count,
                ImpactAmount = Math.Abs(negativeDiffOrders.Sum(r => r.Difference)),
                ImpactLevel = "极高"
            });
        }
        
        var totalRiskScore = 0m;
        foreach (var factor in assessment.RiskFactors)
        {
            totalRiskScore += factor.Level switch
            {
                RiskLevel.Critical => 40,
                RiskLevel.High => 25,
                RiskLevel.Medium => 10,
                RiskLevel.Low => 5,
                _ => 0
            };
        }
        
        assessment.RiskScore = Math.Min(100, totalRiskScore);
        assessment.OverallRiskLevel = assessment.RiskScore switch
        {
            >= 80 => RiskLevel.Critical,
            >= 50 => RiskLevel.High,
            >= 25 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }
    
    private void IdentifyRiskyOrders(
        GrayRiskAssessment assessment,
        List<GrayPlaybackResult> playbackResults)
    {
        var eligibleResults = playbackResults.Where(r => r.IsEligible).ToList();
        
        foreach (var result in eligibleResults)
        {
            var riskLevel = CalculateOrderRiskLevel(result);
            
            if (riskLevel >= RiskLevel.High)
            {
                var riskyOrder = new RiskyOrder
                {
                    OrderId = result.OrderId,
                    RiskLevel = riskLevel,
                    RiskType = DetermineRiskType(result),
                    RiskDescription = GenerateRiskDescription(result),
                    EstimatedImpact = Math.Abs(result.Difference),
                    SuggestedAction = GenerateSuggestedAction(result, riskLevel),
                    StoreId = result.StoreId ?? "",
                    Channel = result.Channel ?? "",
                    MemberLevel = result.MemberLevel ?? "",
                    OldDiscount = result.OldVersionResult?.RecommendedPlan?.TotalDiscountAmount ?? 0,
                    NewDiscount = result.NewVersionResult?.RecommendedPlan?.TotalDiscountAmount ?? 0,
                    Difference = result.Difference
                };
                
                riskyOrder.RiskReasons = AnalyzeRiskReasons(result);
                riskyOrder.AffectedCoupons = GetAffectedCoupons(result);
                
                if (riskLevel == RiskLevel.Critical)
                    assessment.HighRiskOrders.Add(riskyOrder);
                else
                    assessment.HighRiskOrders.Add(riskyOrder);
            }
            else if (riskLevel == RiskLevel.Medium)
            {
                assessment.MediumRiskOrders.Add(new RiskyOrder
                {
                    OrderId = result.OrderId,
                    RiskLevel = riskLevel,
                    RiskType = DetermineRiskType(result),
                    RiskDescription = GenerateRiskDescription(result),
                    EstimatedImpact = Math.Abs(result.Difference),
                    SuggestedAction = "建议观察",
                    Difference = result.Difference
                });
            }
        }
        
        assessment.HighRiskOrders = assessment.HighRiskOrders
            .OrderByDescending(o => o.EstimatedImpact)
            .ToList();
        
        assessment.MediumRiskOrders = assessment.MediumRiskOrders
            .OrderByDescending(o => o.EstimatedImpact)
            .ToList();
    }
    
    private RiskLevel CalculateOrderRiskLevel(GrayPlaybackResult result)
    {
        var absDiff = Math.Abs(result.Difference);
        var absPercent = Math.Abs(result.DifferencePercent);
        
        if (absDiff > 100 || absPercent > 0.8m)
            return RiskLevel.Critical;
        if (absDiff > 50 || absPercent > 0.5m)
            return RiskLevel.High;
        if (absDiff > 10 || absPercent > 0.2m)
            return RiskLevel.Medium;
        return RiskLevel.Low;
    }
    
    private string DetermineRiskType(GrayPlaybackResult result)
    {
        if (result.Difference < 0 && Math.Abs(result.Difference) > 20)
            return "优惠大幅减少";
        
        if (Math.Abs(result.DifferencePercent) > 0.5m)
            return "比例差异过大";
        
        var oldCoupons = result.OldVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        var newCoupons = result.NewVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        
        if (oldCoupons.Count != newCoupons.Count)
            return "优惠券数量变化";
        
        if (!oldCoupons.SequenceEqual(newCoupons))
            return "优惠券组合变化";
        
        return "金额差异";
    }
    
    private string GenerateRiskDescription(GrayPlaybackResult result)
    {
        var desc = new List<string>();
        
        if (result.Difference != 0)
        {
            desc.Add($"优惠金额变化: ¥{result.Difference:F2}");
        }
        
        if (Math.Abs(result.DifferencePercent) > 0.1m)
        {
            desc.Add($"变化比例: {result.DifferencePercent:P2}");
        }
        
        var oldCoupons = result.OldVersionResult?.RecommendedPlan?.AppliedCoupons?.Count ?? 0;
        var newCoupons = result.NewVersionResult?.RecommendedPlan?.AppliedCoupons?.Count ?? 0;
        
        if (oldCoupons != newCoupons)
        {
            desc.Add($"优惠券数量: {oldCoupons} → {newCoupons}");
        }
        
        return string.Join("; ", desc);
    }
    
    private string GenerateSuggestedAction(GrayPlaybackResult result, RiskLevel level)
    {
        if (level == RiskLevel.Critical)
        {
            return "建议在正式切换前人工审核，或调整规则配置";
        }
        
        if (result.Difference < -30)
        {
            return "建议检查是否遗漏了关键优惠券规则";
        }
        
        if (Math.Abs(result.DifferencePercent) > 0.5m)
        {
            return "建议核实规则适用范围是否正确";
        }
        
        return "建议在灰度期间重点观察此订单类型";
    }
    
    private List<string> AnalyzeRiskReasons(GrayPlaybackResult result)
    {
        var reasons = new List<string>();
        
        if (result.Difference < 0)
            reasons.Add("新版本优惠低于旧版本");
        
        if (Math.Abs(result.DifferencePercent) > 0.3m)
            reasons.Add("差异比例超过30%");
        
        var oldCoupons = result.OldVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        var newCoupons = result.NewVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        
        var removed = oldCoupons.Except(newCoupons).ToList();
        if (removed.Any())
            reasons.Add($"移除优惠券: {string.Join(", ", removed)}");
        
        var added = newCoupons.Except(oldCoupons).ToList();
        if (added.Any())
            reasons.Add($"新增优惠券: {string.Join(", ", added)}");
        
        return reasons;
    }
    
    private List<string> GetAffectedCoupons(GrayPlaybackResult result)
    {
        var coupons = new List<string>();
        
        coupons.AddRange(result.OldVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId) ?? new List<string>());
        coupons.AddRange(result.NewVersionResult?.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId) ?? new List<string>());
        
        return coupons.Distinct().ToList();
    }
    
    private void CalculateRiskByScope(
        GrayRiskAssessment assessment,
        List<GrayPlaybackResult> playbackResults)
    {
        var eligibleResults = playbackResults.Where(r => r.IsEligible).ToList();
        
        var byStore = eligibleResults.GroupBy(r => r.StoreId ?? "Unknown");
        foreach (var g in byStore)
        {
            var scopeRisk = new RiskByScope
            {
                ScopeId = g.Key,
                ScopeName = g.Key,
                OrderCount = g.Count(),
                TotalDifference = g.Sum(r => Math.Abs(r.Difference)),
                AverageDifference = g.Average(r => Math.Abs(r.Difference)),
                MaxDifference = g.Max(r => Math.Abs(r.Difference)),
                HighRiskCount = g.Count(r => CalculateOrderRiskLevel(r) >= RiskLevel.High),
                MediumRiskCount = g.Count(r => CalculateOrderRiskLevel(r) == RiskLevel.Medium)
            };
            
            scopeRisk.RiskScore = CalculateScopeRiskScore(scopeRisk);
            scopeRisk.RiskLevel = scopeRisk.RiskScore >= 50 ? RiskLevel.High :
                                  scopeRisk.RiskScore >= 25 ? RiskLevel.Medium : RiskLevel.Low;
            
            assessment.RiskByStore[g.Key] = scopeRisk;
        }
        
        var byChannel = eligibleResults.GroupBy(r => r.Channel ?? "Unknown");
        foreach (var g in byChannel)
        {
            var scopeRisk = new RiskByScope
            {
                ScopeId = g.Key,
                ScopeName = g.Key,
                OrderCount = g.Count(),
                TotalDifference = g.Sum(r => Math.Abs(r.Difference)),
                AverageDifference = g.Average(r => Math.Abs(r.Difference)),
                MaxDifference = g.Max(r => Math.Abs(r.Difference)),
                HighRiskCount = g.Count(r => CalculateOrderRiskLevel(r) >= RiskLevel.High),
                MediumRiskCount = g.Count(r => CalculateOrderRiskLevel(r) == RiskLevel.Medium)
            };
            
            scopeRisk.RiskScore = CalculateScopeRiskScore(scopeRisk);
            scopeRisk.RiskLevel = scopeRisk.RiskScore >= 50 ? RiskLevel.High :
                                  scopeRisk.RiskScore >= 25 ? RiskLevel.Medium : RiskLevel.Low;
            
            assessment.RiskByChannel[g.Key] = scopeRisk;
        }
        
        var byMemberLevel = eligibleResults.GroupBy(r => r.MemberLevel ?? "Unknown");
        foreach (var g in byMemberLevel)
        {
            var scopeRisk = new RiskByScope
            {
                ScopeId = g.Key,
                ScopeName = g.Key,
                OrderCount = g.Count(),
                TotalDifference = g.Sum(r => Math.Abs(r.Difference)),
                AverageDifference = g.Average(r => Math.Abs(r.Difference)),
                MaxDifference = g.Max(r => Math.Abs(r.Difference)),
                HighRiskCount = g.Count(r => CalculateOrderRiskLevel(r) >= RiskLevel.High),
                MediumRiskCount = g.Count(r => CalculateOrderRiskLevel(r) == RiskLevel.Medium)
            };
            
            scopeRisk.RiskScore = CalculateScopeRiskScore(scopeRisk);
            scopeRisk.RiskLevel = scopeRisk.RiskScore >= 50 ? RiskLevel.High :
                                  scopeRisk.RiskScore >= 25 ? RiskLevel.Medium : RiskLevel.Low;
            
            assessment.RiskByMemberLevel[g.Key] = scopeRisk;
        }
    }
    
    private decimal CalculateScopeRiskScore(RiskByScope scope)
    {
        var score = 0m;
        
        if (scope.HighRiskCount > 0)
            score += scope.HighRiskCount * 15;
        
        if (scope.MediumRiskCount > 0)
            score += scope.MediumRiskCount * 5;
        
        if (scope.AverageDifference > 30)
            score += 20;
        else if (scope.AverageDifference > 10)
            score += 10;
        
        if (scope.MaxDifference > 100)
            score += 15;
        
        return Math.Min(100, score);
    }
    
    private void GenerateRecommendedScale(
        GrayRiskAssessment assessment,
        List<GrayPlaybackResult> playbackResults,
        GrayReleasePolicy policy)
    {
        var eligibleResults = playbackResults.Where(r => r.IsEligible).ToList();
        var highRiskRatio = (decimal)assessment.HighRiskOrders.Count / Math.Max(eligibleResults.Count, 1);
        
        var recommended = new RecommendedScaleRange();
        
        if (assessment.OverallRiskLevel == RiskLevel.Critical)
        {
            recommended.RecommendedStartPercentage = 1m;
            recommended.MaxSafePercentage = 10m;
            recommended.RecommendedStep = 1m;
            recommended.RecommendedObservationHours = 48;
            recommended.Reason = "存在高风险订单，建议极小范围灰度并延长观察期";
        }
        else if (assessment.OverallRiskLevel == RiskLevel.High)
        {
            recommended.RecommendedStartPercentage = 5m;
            recommended.MaxSafePercentage = 30m;
            recommended.RecommendedStep = 5m;
            recommended.RecommendedObservationHours = 24;
            recommended.Reason = "风险较高，建议小范围灰度并密切监控";
        }
        else if (assessment.OverallRiskLevel == RiskLevel.Medium)
        {
            recommended.RecommendedStartPercentage = 10m;
            recommended.MaxSafePercentage = 50m;
            recommended.RecommendedStep = 10m;
            recommended.RecommendedObservationHours = 12;
            recommended.Reason = "存在中等风险，建议适度灰度";
        }
        else
        {
            recommended.RecommendedStartPercentage = 20m;
            recommended.MaxSafePercentage = 100m;
            recommended.RecommendedStep = 20m;
            recommended.RecommendedObservationHours = 6;
            recommended.Reason = "风险较低，可较快放量";
        }
        
        var stages = new List<ScaleStage>();
        var currentPercentage = recommended.RecommendedStartPercentage;
        var stageNum = 1;
        
        while (currentPercentage <= recommended.MaxSafePercentage)
        {
            stages.Add(new ScaleStage
            {
                StageNumber = stageNum,
                Percentage = currentPercentage,
                ObservationHours = recommended.RecommendedObservationHours,
                Checkpoint = $"检查差异订单数是否超过{Math.Max(1, (int)(eligibleResults.Count * 0.05))}个",
                SuccessCriteria = new List<string>
                {
                    "差异订单数 < 5%",
                    "平均差异 < 10元",
                    "无Critical级别异常"
                },
                RollbackTriggers = new List<string>
                {
                    $"差异订单数 > {Math.Max(1, (int)(eligibleResults.Count * 0.1))}",
                    "出现Critical级别异常",
                    "用户投诉超过阈值"
                }
            });
            
            currentPercentage += recommended.RecommendedStep;
            stageNum++;
        }
        
        recommended.ScaleStages = stages;
        assessment.RecommendedScale = recommended;
    }
    
    private void GenerateRollbackPlan(GrayRiskAssessment assessment, GrayReleasePolicy policy)
    {
        var plan = new RollbackPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanName = $"{policy.PolicyName} 回滚预案",
            EstimatedRollbackTime = 5,
            RollbackScope = "全量灰度订单",
            EmergencyContact = "运营负责人"
        };
        
        plan.TriggerConditions = new List<string>
        {
            "差异订单数超过10%",
            "出现Critical级别异常订单",
            "用户投诉量超过阈值",
            "系统错误率超过1%",
            "人工触发回滚"
        };
        
        plan.RollbackSteps = new List<RollbackStep>
        {
            new RollbackStep
            {
                StepNumber = 1,
                Description = "暂停灰度放量",
                Command = "UpdatePolicyStatus(policyId, GrayReleaseStatus.Paused)",
                ExpectedResult = "状态变为Paused",
                EstimatedSeconds = 10,
                IsCritical = true
            },
            new RollbackStep
            {
                StepNumber = 2,
                Description = "切换规则版本到旧版本",
                Command = "SwitchRuleVersion(oldVersionId)",
                ExpectedResult = "规则版本切换成功",
                EstimatedSeconds = 30,
                IsCritical = true
            },
            new RollbackStep
            {
                StepNumber = 3,
                Description = "通知相关系统",
                Command = "NotifySystems('rollback')",
                ExpectedResult = "各系统收到回滚通知",
                EstimatedSeconds = 60,
                IsCritical = false
            },
            new RollbackStep
            {
                StepNumber = 4,
                Description = "验证回滚效果",
                Command = "VerifyRollback()",
                ExpectedResult = "验证通过",
                EstimatedSeconds = 120,
                IsCritical = true
            },
            new RollbackStep
            {
                StepNumber = 5,
                Description = "记录回滚日志",
                Command = "LogRollback()",
                ExpectedResult = "日志记录完成",
                EstimatedSeconds = 30,
                IsCritical = false
            }
        };
        
        plan.PreRollbackChecks = new List<string>
        {
            "确认当前灰度状态",
            "确认旧版本规则可用",
            "确认回滚影响范围",
            "通知相关人员"
        };
        
        plan.PostRollbackVerifications = new List<string>
        {
            "验证规则版本已切换",
            "验证订单计算结果正确",
            "验证系统状态正常",
            "验证用户无异常反馈"
        };
        
        plan.AffectedSystems = new List<string>
        {
            "订单系统",
            "结算系统",
            "优惠券系统",
            "会员系统"
        };
        
        assessment.RollbackPlan = plan;
    }
    
    private void GenerateWarningsAndRecommendations(GrayRiskAssessment assessment)
    {
        assessment.Warnings = new List<string>();
        assessment.Recommendations = new List<string>();
        
        if (assessment.OverallRiskLevel >= RiskLevel.High)
        {
            assessment.Warnings.Add("整体风险等级较高，建议谨慎发布");
        }
        
        if (assessment.HighRiskOrders.Any())
        {
            assessment.Warnings.Add($"存在{assessment.HighRiskOrders.Count}个高风险订单，建议人工审核");
        }
        
        var highRiskStores = assessment.RiskByStore.Where(x => x.Value.RiskLevel >= RiskLevel.High).ToList();
        if (highRiskStores.Any())
        {
            assessment.Warnings.Add($"店铺 {string.Join(", ", highRiskStores.Select(x => x.Key))} 风险较高");
        }
        
        var negativeDiffCount = assessment.HighRiskOrders.Count(o => o.Difference < 0);
        if (negativeDiffCount > assessment.HighRiskOrders.Count * 0.5)
        {
            assessment.Warnings.Add("多数高风险订单优惠减少，可能影响用户体验");
        }
        
        assessment.Recommendations.Add($"建议起始放量: {assessment.RecommendedScale.RecommendedStartPercentage}%");
        assessment.Recommendations.Add($"建议最大放量: {assessment.RecommendedScale.MaxSafePercentage}%");
        assessment.Recommendations.Add($"建议观察周期: {assessment.RecommendedScale.RecommendedObservationHours}小时");
        
        if (assessment.HighRiskOrders.Any())
        {
            assessment.Recommendations.Add("建议对高风险订单进行人工复核");
        }
        
        assessment.Recommendations.Add("建议准备好回滚预案后再发布");
        assessment.Recommendations.Add("建议设置监控告警阈值");
    }
    
    public GrayRiskAssessment? GetRiskAssessment(string policyId)
    {
        return _riskAssessments.TryGetValue(policyId, out var assessment) ? assessment : null;
    }
    
    public GrayReleaseDecision MakeDecision(string policyId, DecisionType type, string approvedBy, string reason)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        var assessment = GetRiskAssessment(policyId);
        
        var decision = new GrayReleaseDecision
        {
            DecisionId = Guid.NewGuid().ToString(),
            PolicyId = policyId,
            DecisionTime = DateTime.UtcNow,
            Type = type,
            ApprovedBy = approvedBy,
            Reason = reason
        };
        
        if (assessment != null)
        {
            decision.RiskSummary = new RiskAssessmentSummary
            {
                OverallLevel = assessment.OverallRiskLevel,
                RiskScore = assessment.RiskScore,
                HighRiskOrderCount = assessment.HighRiskOrders.Count,
                MaxPotentialImpact = assessment.HighRiskOrders.Any() ? 
                    assessment.HighRiskOrders.Max(o => o.EstimatedImpact) : 0
            };
        }
        
        if (type == DecisionType.Proceed)
        {
            if (assessment != null && assessment.OverallRiskLevel >= RiskLevel.Critical)
            {
                decision.Result = DecisionResult.Rejected;
                decision.Conditions.Add("风险等级过高，不允许发布");
            }
            else if (assessment != null && assessment.OverallRiskLevel >= RiskLevel.High)
            {
                decision.Result = DecisionResult.Escalated;
                decision.Conditions.Add("风险等级较高，需要上级审批");
            }
            else
            {
                decision.Result = DecisionResult.Approved;
                decision.ApprovedPercentage = assessment?.RecommendedScale.RecommendedStartPercentage ?? 10m;
            }
        }
        else if (type == DecisionType.Rollback)
        {
            decision.Result = DecisionResult.Approved;
        }
        
        return decision;
    }
    
    public GrayReleaseMonitoring StartMonitoring(string policyId)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            throw new ArgumentException($"Policy not found: {policyId}");
        
        var monitoring = new GrayReleaseMonitoring
        {
            MonitoringId = Guid.NewGuid().ToString(),
            PolicyId = policyId,
            StartTime = DateTime.UtcNow,
            CurrentPercentage = policy.ScaleConfig.CurrentPercentage,
            Status = MonitoringStatus.Normal
        };
        
        monitoring.Metrics = new List<MonitoringMetric>
        {
            new MonitoringMetric { Name = "差异订单比例", ThresholdValue = 0.05m, Threshold = "<5%", IsNormal = true },
            new MonitoringMetric { Name = "平均差异金额", ThresholdValue = 10m, Threshold = "<10元", IsNormal = true },
            new MonitoringMetric { Name = "系统错误率", ThresholdValue = 0.01m, Threshold = "<1%", IsNormal = true },
            new MonitoringMetric { Name = "用户投诉数", ThresholdValue = 5m, Threshold = "<5", IsNormal = true }
        };
        
        _monitorings[policyId] = monitoring;
        return monitoring;
    }
    
    public GrayReleaseMonitoring UpdateMonitoring(string policyId, decimal ordersProcessed, decimal avgDifference)
    {
        var monitoring = _monitorings.TryGetValue(policyId, out var m) ? m : StartMonitoring(policyId);
        
        monitoring.TotalOrdersProcessed = ordersProcessed;
        monitoring.AverageDifference = avgDifference;
        monitoring.TotalDiscountDifference = ordersProcessed * avgDifference;
        
        foreach (var metric in monitoring.Metrics)
        {
            if (metric.Name == "差异订单比例")
            {
                metric.Value = avgDifference > 0 ? 0.1m : 0.02m;
                metric.IsNormal = metric.Value < metric.ThresholdValue;
            }
            else if (metric.Name == "平均差异金额")
            {
                metric.Value = Math.Abs(avgDifference);
                metric.IsNormal = metric.Value < metric.ThresholdValue;
            }
        }
        
        var abnormalMetrics = monitoring.Metrics.Where(m => !m.IsNormal).ToList();
        if (abnormalMetrics.Any())
        {
            monitoring.Status = abnormalMetrics.Any(m => m.Value > m.ThresholdValue * 2) ? 
                MonitoringStatus.Critical : MonitoringStatus.Warning;
            
            foreach (var metric in abnormalMetrics)
            {
                monitoring.Alerts.Add(new MonitoringAlert
                {
                    Timestamp = DateTime.UtcNow,
                    Severity = metric.Value > metric.ThresholdValue * 2 ? AlertSeverity.High : AlertSeverity.Medium,
                    Message = $"{metric.Name} 超出阈值: {metric.Value:F2} > {metric.ThresholdValue}",
                    MetricName = metric.Name,
                    ActualValue = metric.Value,
                    ThresholdValue = metric.ThresholdValue,
                    SuggestedAction = metric.Value > metric.ThresholdValue * 2 ? "建议暂停灰度" : "建议密切观察"
                });
            }
        }
        
        return monitoring;
    }
    
    public GrayReleaseMonitoring? GetMonitoring(string policyId)
    {
        return _monitorings.TryGetValue(policyId, out var monitoring) ? monitoring : null;
    }
    
    public GrayPlaybackResult PlaybackOrderSync(
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
            IsEligible = CheckEligibility(order, policy),
            StoreId = order.StoreId,
            Channel = order.Channel,
            MemberLevel = order.MemberLevel
        };
        
        if (!result.IsEligible)
        {
            result.IneligibleReason = "Order does not match gray release targets";
            return result;
        }
        
        result.OldVersionResult = ExecuteWithOldRules(context, coupons);
        result.NewVersionResult = ExecuteWithNewRules(context, coupons, policy);
        
        result.Difference = (result.NewVersionResult.RecommendedPlan?.FinalTotalAmount ?? result.NewVersionResult.OriginalAmount) -
                            (result.OldVersionResult.RecommendedPlan?.FinalTotalAmount ?? result.OldVersionResult.OriginalAmount);
        
        result.DifferencePercent = result.OldVersionResult.OriginalAmount > 0 ?
            result.Difference / result.OldVersionResult.OriginalAmount : 0;
        
        if (!_playbackResults.ContainsKey(policyId))
        {
            _playbackResults[policyId] = new List<GrayPlaybackResult>();
        }
        _playbackResults[policyId].Add(result);
        
        return result;
    }
    
    public async Task<GrayPlaybackResult> PlaybackOrderWithPolicy(
        string policyId,
        OrderRecord order,
        List<Coupon> coupons)
    {
        return PlaybackOrderSync(policyId, order, coupons);
    }
    
    public async Task<List<GrayPlaybackResult>> BatchPlaybackWithPolicy(
        string policyId,
        List<OrderRecord> orders)
    {
        return orders.Select(o => PlaybackOrderSync(policyId, o, LoadCouponsForOrder(o))).ToList();
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
        var assessment = GetRiskAssessment(policyId);
        
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════════");
        report.Add($"              灰度发布分析报告: {policy.PolicyName}");
        report.Add("═══════════════════════════════════════════════════════════════");
        report.Add($"策略ID: {policy.PolicyId}");
        report.Add($"规则版本: {policy.RuleVersionId}");
        report.Add($"状态: {policy.Status}");
        report.Add($"创建时间: {policy.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add("");
        
        if (assessment != null)
        {
            report.Add("【风险评估】");
            report.Add($"  整体风险等级: {assessment.OverallRiskLevel}");
            report.Add($"  风险评分: {assessment.RiskScore:F1}/100");
            report.Add($"  高风险订单: {assessment.HighRiskOrders.Count}");
            report.Add($"  中风险订单: {assessment.MediumRiskOrders.Count}");
            report.Add("");
            
            report.Add("【建议放量】");
            report.Add($"  推荐起始: {assessment.RecommendedScale.RecommendedStartPercentage}%");
            report.Add($"  最大安全: {assessment.RecommendedScale.MaxSafePercentage}%");
            report.Add($"  原因: {assessment.RecommendedScale.Reason}");
            report.Add("");
        }
        
        report.Add("【回放分析】");
        report.Add($"  总回放次数: {analysis.TotalPlaybackCount}");
        report.Add($"  符合条件: {analysis.EligibleCount}");
        report.Add($"  有差异影响: {analysis.AffectedCount}");
        report.Add($"  平均差异: ¥{analysis.AverageDifference:F2}");
        report.Add("");
        
        report.Add("═══════════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
    
    public List<GrayReleasePolicy> GetPoliciesByTarget(string targetType, string targetId)
    {
        return _policies.Values
            .Where(p => p.Targets.Any(t => t.TargetIds.Contains(targetId)))
            .ToList();
    }
    
    public List<GrayReleasePolicy> GetPoliciesByStatus(GrayReleaseStatus status)
    {
        return _policies.Values.Where(p => p.Status == status).ToList();
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
        _riskAssessments.Remove(policyId);
        _monitorings.Remove(policyId);
        
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
        
        if (!validTransitions.ContainsKey(from) || !validTransitions[from].Contains(to))
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
                GrayTargetType.Percentage => CheckPercentage(order, policy),
                _ => true
            };
            
            if (!matches && target.MatchMode == GrayMatchMode.Include)
                return false;
            if (matches && target.MatchMode == GrayMatchMode.Exclude)
                return false;
        }
        
        return true;
    }
    
    private bool CheckPercentage(OrderRecord order, GrayReleasePolicy policy)
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

public class GrayPlaybackResult
{
    public string PlaybackId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; }
    
    public bool IsEligible { get; set; }
    public string IneligibleReason { get; set; } = string.Empty;
    
    public string StoreId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string MemberLevel { get; set; } = string.Empty;
    
    public CalculationResult OldVersionResult { get; set; }
    public CalculationResult NewVersionResult { get; set; }
    
    public decimal Difference { get; set; }
    public decimal DifferencePercent { get; set; }
}

public class GrayReleaseAnalysis
{
    public string PolicyId { get; set; } = string.Empty;
    public DateTime AnalysisTime { get; set; }
    public int TotalPlaybackCount { get; set; }
    public int EligibleCount { get; set; }
    public int AffectedCount { get; set; }
    
    public decimal AverageDifference { get; set; }
    public decimal MaxPositiveDifference { get; set; }
    public decimal MaxNegativeDifference { get; set; }
    
    public Dictionary<string, int> DifferenceDistribution { get; set; } = new();
    public List<TopDifferenceOrder> TopDifferenceOrders { get; set; } = new();
}

public class TopDifferenceOrder
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Difference { get; set; }
    public decimal DifferencePercent { get; set; }
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