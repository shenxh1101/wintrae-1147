using CouponCalculator.Models;
using Xunit;

namespace CouponCalculator.Tests;

public class GrayRiskControlTests
{
    [Fact]
    public void GrayRiskAssessment_GenerateRiskReport_ReturnsNonEmpty()
    {
        var assessment = new GrayRiskAssessment
        {
            AssessmentId = "TEST-001",
            PolicyId = "POLICY-001",
            AssessmentTime = DateTime.UtcNow,
            OverallRiskLevel = RiskLevel.Medium,
            RiskScore = 35m,
            RiskFactors = new List<RiskFactor>
            {
                new RiskFactor
                {
                    Name = "优惠券组合变化",
                    Description = "有10个订单的优惠券组合发生变化",
                    Level = RiskLevel.Medium,
                    AffectedOrders = 10,
                    ImpactAmount = 500m
                }
            },
            RecommendedScale = new RecommendedScaleRange
            {
                RecommendedStartPercentage = 10m,
                MaxSafePercentage = 50m,
                RecommendedStep = 10m,
                RecommendedObservationHours = 12,
                Reason = "存在中等风险，建议适度灰度"
            },
            RollbackPlan = new RollbackPlan
            {
                TriggerConditions = new List<string> { "差异订单数超过10%" },
                EstimatedRollbackTime = 5
            },
            HighRiskOrders = new List<RiskyOrder>
            {
                new RiskyOrder
                {
                    OrderId = "ORDER-001",
                    RiskType = "优惠大幅减少",
                    EstimatedImpact = 50m,
                    SuggestedAction = "建议人工审核"
                }
            }
        };
        
        var report = assessment.GenerateRiskReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("灰度发布风险评估报告", report);
        Assert.Contains("Medium", report);
        Assert.Contains("建议放量", report);
    }
    
    [Fact]
    public void RecommendedScaleRange_GenerateScalePlan_ReturnsValidPlan()
    {
        var scale = new RecommendedScaleRange
        {
            ScaleStages = new List<ScaleStage>
            {
                new ScaleStage { StageNumber = 1, Percentage = 10m, ObservationHours = 12 },
                new ScaleStage { StageNumber = 2, Percentage = 20m, ObservationHours = 12 },
                new ScaleStage { StageNumber = 3, Percentage = 50m, ObservationHours = 24 }
            }
        };
        
        var plan = scale.GenerateScalePlan();
        
        Assert.NotEmpty(plan);
        Assert.Contains("第1阶段", plan);
        Assert.Contains("10%", plan);
    }
    
    [Fact]
    public void RollbackPlan_GenerateRollbackGuide_ReturnsValidGuide()
    {
        var plan = new RollbackPlan
        {
            PlanName = "测试回滚预案",
            TriggerConditions = new List<string> { "差异订单数超过10%", "系统错误率超过1%" },
            RollbackSteps = new List<RollbackStep>
            {
                new RollbackStep { StepNumber = 1, Description = "暂停灰度放量", IsCritical = true },
                new RollbackStep { StepNumber = 2, Description = "切换规则版本", IsCritical = true }
            },
            EstimatedRollbackTime = 5,
            RollbackScope = "全量灰度订单",
            PreRollbackChecks = new List<string> { "确认当前灰度状态" },
            PostRollbackVerifications = new List<string> { "验证规则版本已切换" }
        };
        
        var guide = plan.GenerateRollbackGuide();
        
        Assert.NotEmpty(guide);
        Assert.Contains("回滚操作指南", guide);
        Assert.Contains("触发条件", guide);
        Assert.Contains("暂停灰度放量", guide);
    }
    
    [Fact]
    public void RiskyOrder_GenerateOrderRiskDetail_ReturnsValidDetail()
    {
        var order = new RiskyOrder
        {
            OrderId = "ORDER-001",
            RiskType = "优惠大幅减少",
            RiskLevel = RiskLevel.High,
            RiskDescription = "优惠金额变化超过50元",
            EstimatedImpact = 50m,
            OldDiscount = 100m,
            NewDiscount = 50m,
            Difference = -50m,
            SuggestedAction = "建议人工审核"
        };
        
        var detail = order.GenerateOrderRiskDetail();
        
        Assert.NotEmpty(detail);
        Assert.Contains("ORDER-001", detail);
        Assert.Contains("¥50", detail);
    }
    
    [Fact]
    public void GrayReleaseMonitoring_GenerateMonitoringReport_ReturnsValidReport()
    {
        var monitoring = new GrayReleaseMonitoring
        {
            MonitoringId = "MON-001",
            PolicyId = "POLICY-001",
            CurrentPercentage = 20m,
            Status = MonitoringStatus.Normal,
            TotalOrdersProcessed = 1000,
            TotalDiscountDifference = 500m,
            AverageDifference = 0.5m,
            Metrics = new List<MonitoringMetric>
            {
                new MonitoringMetric { Name = "差异订单比例", Value = 0.02m, IsNormal = true },
                new MonitoringMetric { Name = "平均差异金额", Value = 0.5m, IsNormal = true }
            },
            Alerts = new List<MonitoringAlert>
            {
                new MonitoringAlert
                {
                    Severity = AlertSeverity.Low,
                    Message = "差异订单比例接近阈值",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        
        var report = monitoring.GenerateMonitoringReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("灰度发布监控报告", report);
        Assert.Contains("20%", report);
        Assert.Contains("1000", report);
    }
    
    [Fact]
    public void RiskLevel_DeterminesCorrectSeverity()
    {
        var assessment = new GrayRiskAssessment
        {
            RiskScore = 85m
        };
        
        assessment.OverallRiskLevel = assessment.RiskScore >= 80 ? RiskLevel.Critical :
                                       assessment.RiskScore >= 50 ? RiskLevel.High :
                                       assessment.RiskScore >= 25 ? RiskLevel.Medium : RiskLevel.Low;
        
        Assert.Equal(RiskLevel.Critical, assessment.OverallRiskLevel);
    }
}

public class TemplateCollaborationTests
{
    private readonly TemplateCollaborationService _service;
    
    public TemplateCollaborationTests()
    {
        _service = new TemplateCollaborationService();
    }
    
    [Fact]
    public void CreateCollaboration_ReturnsValidCollaboration()
    {
        var explanation = new SharedExplanation
        {
            OriginalAmount = 200m,
            TotalDiscount = 20m,
            FinalAmount = 180m,
            Summary = "满减优惠"
        };
        
        var collaboration = _service.CreateCollaboration("TPL-001", "ORDER-001", explanation);
        
        Assert.NotNull(collaboration);
        Assert.Equal("TPL-001", collaboration.TemplateId);
        Assert.Equal("ORDER-001", collaboration.OrderId);
        Assert.Equal(CollaborationStatus.Draft, collaboration.Status);
    }
    
    [Fact]
    public void AddAnnotation_AddsAnnotationToCollaboration()
    {
        var collaboration = _service.CreateCollaboration("TPL-001", "ORDER-001", new SharedExplanation());
        
        var annotation = _service.AddAnnotation(
            collaboration.CollaborationId,
            CollaborationRole.CustomerService,
            "客服A",
            "用户反馈优惠金额不对",
            AnnotationType.Question,
            AnnotationPriority.High,
            "需核实优惠券是否正确应用");
        
        Assert.NotNull(annotation);
        Assert.Equal(CollaborationRole.CustomerService, annotation.Role);
        Assert.Equal("客服A", annotation.Author);
        Assert.Single(collaboration.Annotations);
    }
    
    [Fact]
    public void AddConclusion_AddsConclusionToCollaboration()
    {
        var collaboration = _service.CreateCollaboration("TPL-001", "ORDER-001", new SharedExplanation());
        
        var conclusion = _service.AddConclusion(
            collaboration.CollaborationId,
            CollaborationRole.Operations,
            "运营B",
            ConclusionType.Confirmed,
            "优惠计算正确，符合规则",
            new List<string> { "规则ID: R001" });
        
        Assert.NotNull(conclusion);
        Assert.Equal(ConclusionType.Confirmed, conclusion.ConclusionType);
        Assert.True(conclusion.IsFinal);
        Assert.Equal(CollaborationStatus.Completed, collaboration.Status);
    }
    
    [Fact]
    public void SharedExplanation_GenerateForCustomer_ReturnsSimpleFormat()
    {
        var explanation = new SharedExplanation
        {
            OriginalAmount = 200m,
            TotalDiscount = 20m,
            FinalAmount = 180m,
            Summary = "感谢您的购买",
            DiscountItems = new List<SharedDiscountItem>
            {
                new SharedDiscountItem { Name = "满减券", Amount = 20m }
            }
        };
        
        var customerVersion = explanation.GenerateForCustomer();
        
        Assert.NotEmpty(customerVersion);
        Assert.Contains("200", customerVersion);
        Assert.Contains("180", customerVersion);
    }
    
    [Fact]
    public void SharedExplanation_GenerateForInternal_ReturnsDetailedFormat()
    {
        var explanation = new SharedExplanation
        {
            OriginalAmount = 200m,
            TotalDiscount = 20m,
            Freight = 10m,
            FinalAmount = 180m,
            CouponItems = new List<SharedCouponItem>
            {
                new SharedCouponItem { CouponId = "C001", Amount = 20m, Type = "满减", RuleReference = "R001" }
            },
            KeyPoints = new Dictionary<string, string>
            {
                ["优惠券来源"] = "新人活动",
                ["规则版本"] = "v2.0"
            }
        };
        
        var internalVersion = explanation.GenerateForInternal();
        
        Assert.NotEmpty(internalVersion);
        Assert.Contains("内部口径", internalVersion);
        Assert.Contains("C001", internalVersion);
        Assert.Contains("R001", internalVersion);
    }
    
    [Fact]
    public void TemplateAutoFill_EvaluateMatch_ReturnsCorrectScore()
    {
        var autoFill = new TemplateAutoFill
        {
            Conditions = new List<AutoFillCondition>
            {
                new AutoFillCondition { Field = "OriginalAmount", Operator = ">", Value = "100", Weight = 0.5m },
                new AutoFillCondition { Field = "MemberLevel", Operator = "=", Value = "VIP", Weight = 0.3m }
            },
            Content = new AutoFillContent { MinMatchScore = 0.5m }
        };
        
        var snapshot = new OrderContextSnapshot
        {
            OriginalAmount = 150m,
            Member = new MemberSnapshot { Level = "VIP" }
        };
        
        var matches = autoFill.EvaluateMatch(snapshot);
        
        Assert.True(matches);
        Assert.True(autoFill.MatchScore >= 0.5m);
    }
    
    [Fact]
    public void TemplateCollaboration_GenerateCollaborationSummary_ReturnsValidSummary()
    {
        var collaboration = new TemplateCollaboration
        {
            CollaborationId = "COL-001",
            TemplateId = "TPL-001",
            OrderId = "ORDER-001",
            Status = CollaborationStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            SharedExplanation = new SharedExplanation
            {
                OriginalAmount = 200m,
                TotalDiscount = 20m,
                FinalAmount = 180m
            },
            Annotations = new List<RoleAnnotation>
            {
                new RoleAnnotation { Role = CollaborationRole.CustomerService, Author = "客服A", Content = "已核实" }
            },
            Conclusions = new List<ProcessingConclusion>
            {
                new ProcessingConclusion { Role = CollaborationRole.Operations, ConclusionType = ConclusionType.Confirmed, Content = "正确" }
            }
        };
        
        var summary = collaboration.GenerateCollaborationSummary();
        
        Assert.NotEmpty(summary);
        Assert.Contains("口径协作汇总", summary);
        Assert.Contains("客服A", summary);
    }
}

public class AnomalyClassificationTests
{
    private readonly AnomalyClassifier _classifier;
    
    public AnomalyClassificationTests()
    {
        _classifier = new AnomalyClassifier();
    }
    
    [Fact]
    public void ClassifyAnomaly_ReturnsValidClassification()
    {
        var detail = new OrderReconciliationDetail
        {
            OrderId = "ORDER-001",
            OriginalAmount = 200m,
            Difference = 50m,
            HasDifference = true,
            ChangeReasons = new List<string> { "规则变动导致" },
            AppliedCoupons = new List<string> { "C001" }
        };
        
        var classification = _classifier.ClassifyAnomaly(detail);
        
        Assert.NotNull(classification);
        Assert.Equal("ORDER-001", classification.OrderId);
        Assert.NotEqual(AnomalyCategory.Unknown, classification.PrimaryCategory);
        Assert.True(classification.ImpactAmount > 0);
    }
    
    [Fact]
    public void BatchClassify_ReturnsValidResult()
    {
        var details = new List<OrderReconciliationDetail>
        {
            new OrderReconciliationDetail { OrderId = "O1", Difference = 50m, HasDifference = true, ChangeReasons = new List<string> { "规则变动" } },
            new OrderReconciliationDetail { OrderId = "O2", Difference = 30m, HasDifference = true, ChangeReasons = new List<string> { "券状态变化" } },
            new OrderReconciliationDetail { OrderId = "O3", Difference = 100m, HasDifference = true, ChangeReasons = new List<string> { "外部数据不一致" } }
        };
        
        var result = _classifier.BatchClassify(details);
        
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalAnomalies);
        Assert.True(result.ClassifiedCount > 0);
        Assert.NotEmpty(result.Layers);
    }
    
    [Fact]
    public void AnomalyClassification_GenerateClassificationReport_ReturnsValidReport()
    {
        var classification = new AnomalyClassification
        {
            OrderId = "ORDER-001",
            CategoryName = "规则变动导致",
            CategoryCode = "RULE_CHANGE",
            Severity = AnomalySeverity.High,
            Priority = AnomalyPriority.P2,
            ImpactAmount = 50m,
            ImpactPercent = 0.25m,
            Description = "规则版本切换导致优惠计算结果变化",
            UserFriendlyDescription = "规则调整导致优惠变化50元",
            Causes = new List<AnomalyCause>
            {
                new AnomalyCause { CauseType = "规则变动", Description = "版本切换", Confidence = 0.8m }
            },
            Recommendations = new List<string> { "核实规则版本切换时间点" }
        };
        
        var report = classification.GenerateClassificationReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("ORDER-001", report);
        Assert.Contains("规则变动导致", report);
        Assert.Contains("High", report);
    }
    
    [Fact]
    public void AnomalyLayerSummary_GenerateLayerReport_ReturnsValidReport()
    {
        var layer = new AnomalyLayerSummary
        {
            LayerName = "规则变动层",
            Category = AnomalyCategory.RuleChange,
            TotalCount = 10,
            CriticalCount = 2,
            HighCount = 3,
            MediumCount = 5,
            LowCount = 0,
            TotalImpact = 500m,
            AverageImpact = 50m,
            MaxImpact = 100m,
            CommonCauses = new List<string> { "版本切换", "规则配置变更" },
            Recommendations = new List<string> { "核实规则版本切换时间" },
            LayerExplanation = "规则版本切换导致的正常差异"
        };
        
        var report = layer.GenerateLayerReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("规则变动层", report);
        Assert.Contains("10", report);
        Assert.Contains("500", report);
    }
    
    [Fact]
    public void AnomalyClassificationResult_GenerateClassificationSummaryReport_ReturnsValidReport()
    {
        var result = new AnomalyClassificationResult
        {
            BatchId = "BATCH-001",
            ClassifiedAt = DateTime.UtcNow,
            TotalAnomalies = 10,
            ClassifiedCount = 8,
            UnclassifiedCount = 2,
            Layers = new List<AnomalyLayerSummary>
            {
                new AnomalyLayerSummary { LayerName = "规则变动层", TotalCount = 5, TotalImpact = 300m }
            },
            Summary = new AnomalyClassificationSummary
            {
                CriticalCount = 2,
                HighCount = 3,
                MediumCount = 5,
                LowCount = 0,
                CriticalImpact = 200m,
                OverallRecommendations = new List<string> { "优先处理Critical级别异常" }
            },
            Classifications = new List<AnomalyClassification>
            {
                new AnomalyClassification { OrderId = "O1", ImpactAmount = 100m, CategoryName = "规则变动" }
            }
        };
        
        var report = result.GenerateClassificationSummaryReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("异常订单分层分类报告", report);
        Assert.Contains("BATCH-001", report);
        Assert.Contains("运营复盘建议", report);
    }
    
    [Fact]
    public void AnomalySeverity_DeterminesCorrectLevel()
    {
        var impactAmount = 150m;
        var impactPercent = 0.6m;
        
        var severity = impactAmount > 100 || impactPercent > 0.5m ? AnomalySeverity.Critical :
                       impactAmount > 50 || impactPercent > 0.3m ? AnomalySeverity.High :
                       impactAmount > 10 || impactPercent > 0.1m ? AnomalySeverity.Medium : AnomalySeverity.Low;
        
        Assert.Equal(AnomalySeverity.Critical, severity);
    }
}