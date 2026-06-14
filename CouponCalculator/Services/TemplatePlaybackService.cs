using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class TemplatePlaybackService
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ItemLevelAllocationService _allocationService;
    private readonly TemplateManagementService _templateService;
    
    public TemplatePlaybackService()
    {
        _engine = new CouponCalculatorEngine();
        _allocationService = new ItemLevelAllocationService();
        _templateService = new TemplateManagementService();
    }
    
    public TemplatePlaybackResult PlaybackWithTemplate(
        TemplatePlaybackRequest request,
        OrderRecord currentOrder)
    {
        var template = _templateService.GetTemplate(request.TemplateId);
        if (template == null)
        {
            return new TemplatePlaybackResult
            {
                TemplateId = request.TemplateId,
                IsMatch = false,
                Warnings = new List<string> { "模板不存在" }
            };
        }
        
        var result = new TemplatePlaybackResult
        {
            TemplateId = template.TemplateId,
            TemplateName = template.TemplateName,
            OriginalSnapshot = template.Snapshot,
            PlayedAt = DateTime.UtcNow
        };
        
        var context = BuildContextFromSnapshot(template.Snapshot, currentOrder);
        var coupons = BuildCouponsFromSnapshot(template.Snapshot);
        
        foreach (var (key, value) in request.ParameterOverrides)
        {
            ApplyParameterOverride(context, key, value);
        }
        
        try
        {
            result.CurrentResult = _engine.CalculateOptimalEnhanced(context, coupons);
            result.CurrentEnhancedResult = result.CurrentResult;
            
            var differences = CompareSnapshots(template.Snapshot, result.CurrentResult);
            result.Differences = differences;
            
            result.IsMatch = !differences.Any(d => d.IsSignificant);
            result.MatchScore = CalculateMatchScore(template.Snapshot, result.CurrentResult);
            
            if (request.GenerateExplanation)
            {
                result.Explanation = GenerateExplanation(template, result);
            }
            
            result.Warnings = ValidatePlayback(template, result);
            
            _templateService.RecordUsage(request.TemplateId);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"计算失败: {ex.Message}");
        }
        
        return result;
    }
    
    public List<TemplatePlaybackResult> BatchPlaybackWithTemplate(
        string templateId,
        List<OrderRecord> orders)
    {
        var request = new TemplatePlaybackRequest { TemplateId = templateId };
        return orders.Select(o => PlaybackWithTemplate(request, o)).ToList();
    }
    
    public TemplateComparison CompareTemplates(string templateId1, string templateId2)
    {
        var template1 = _templateService.GetTemplate(templateId1);
        var template2 = _templateService.GetTemplate(templateId2);
        
        if (template1 == null || template2 == null)
        {
            return new TemplateComparison
            {
                TemplateId1 = templateId1,
                TemplateId2 = templateId2,
                ComparedAt = DateTime.UtcNow
            };
        }
        
        var comparison = new TemplateComparison
        {
            TemplateId1 = templateId1,
            TemplateId2 = templateId2,
            ComparedAt = DateTime.UtcNow
        };
        
        comparison.Differences = CompareTwoSnapshots(template1.Snapshot, template2.Snapshot);
        comparison.SignificantDifferences = comparison.Differences.Where(d => d.IsSignificant).ToList();
        
        var diff1 = template1.Snapshot.Result?.RecommendedPlan?.TotalDiscountAmount ?? 0;
        var diff2 = template2.Snapshot.Result?.RecommendedPlan?.TotalDiscountAmount ?? 0;
        comparison.TotalDifference = diff2 - diff1;
        comparison.PercentageDifference = diff1 > 0 ? comparison.TotalDifference / diff1 : 0;
        
        return comparison;
    }
    
    public UnifiedExplanation GenerateUnifiedExplanation(
        string templateId,
        OrderRecord order,
        AudienceType audience)
    {
        var template = _templateService.GetTemplate(templateId);
        if (template == null)
        {
            return new UnifiedExplanation
            {
                OrderId = order.OrderId,
                Audience = audience,
                GeneratedAt = DateTime.UtcNow
            };
        }
        
        var playbackResult = PlaybackWithTemplate(
            new TemplatePlaybackRequest { TemplateId = templateId },
            order);
        
        var explanation = new UnifiedExplanation
        {
            OrderId = order.OrderId,
            TemplateId = templateId,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "System",
            Audience = audience,
            RawData = new Dictionary<string, object>
            {
                ["OriginalSnapshot"] = template.Snapshot,
                ["CurrentResult"] = playbackResult.CurrentResult,
                ["Differences"] = playbackResult.Differences
            }
        };
        
        PopulateExplanationContent(explanation, template, playbackResult);
        
        return explanation;
    }
    
    public List<UnifiedExplanation> GenerateAllAudienceExplanations(
        string templateId,
        OrderRecord order)
    {
        var explanations = new List<UnifiedExplanation>();
        
        foreach (AudienceType audience in Enum.GetValues<AudienceType>())
        {
            explanations.Add(GenerateUnifiedExplanation(templateId, order, audience));
        }
        
        return explanations;
    }
    
    public TemplateSnapshot CreateSnapshot(
        OrderContext context,
        List<Coupon> coupons,
        CalculationResult result,
        string orderId)
    {
        var snapshot = new TemplateSnapshot
        {
            OrderId = orderId,
            SnapshotTime = DateTime.UtcNow
        };
        
        snapshot.OrderSnapshot = new OrderContextSnapshot
        {
            OrderId = context.OrderId,
            OriginalAmount = context.OriginalAmount,
            ItemCount = context.Items.Count,
            Member = context.Member != null ? new MemberSnapshot
            {
                MemberId = context.Member.MemberId,
                Level = context.Member.Level.ToString()
            } : null,
            FreightAmount = context.FreightAmount,
            ExtendedData = new Dictionary<string, string>(
                context.ExtendedData.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? "")))
        };
        
        snapshot.CouponsSnapshot = coupons.Select(c => new CouponSnapshot
        {
            CouponId = c.CouponId,
            CouponCode = c.CouponCode,
            Type = c.Type.ToString(),
            DiscountValue = c.DiscountValue,
            MinOrderAmount = c.MinOrderAmount,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo
        }).ToList();
        
        snapshot.Result = result;
        snapshot.AppliedCouponIds = result.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        
        return snapshot;
    }
    
    public CalculationTemplate SaveAsTemplate(
        TemplateSnapshot snapshot,
        string name,
        string category,
        TemplateType type,
        string createdBy,
        string description = "")
    {
        return _templateService.CreateTemplate(
            name, category, type, snapshot, createdBy, description);
    }
    
    public List<CalculationTemplate> FindSimilarTemplates(
        TemplateSnapshot snapshot,
        int limit = 10)
    {
        return _templateService.GetSimilarTemplates(snapshot, limit);
    }
    
    public TemplateSearchResult SearchTemplates(TemplateSearchCriteria criteria)
    {
        var templates = _templateService.SearchTemplates(criteria);
        
        return new TemplateSearchResult
        {
            TotalCount = templates.Count,
            Templates = templates,
            SearchCriteria = criteria
        };
    }
    
    public BatchTemplateComparison CompareMultipleTemplates(List<string> templateIds)
    {
        var result = new BatchTemplateComparison
        {
            TemplateIds = templateIds,
            ComparedAt = DateTime.UtcNow
        };
        
        for (int i = 0; i < templateIds.Count; i++)
        {
            for (int j = i + 1; j < templateIds.Count; j++)
            {
                var comparison = CompareTemplates(templateIds[i], templateIds[j]);
                result.Comparisons.Add(comparison);
            }
        }
        
        if (result.Comparisons.Any())
        {
            result.AverageDifference = result.Comparisons.Average(c => Math.Abs(c.TotalDifference));
            result.MaxDifference = result.Comparisons.Max(c => Math.Abs(c.TotalDifference));
            result.TotalSignificantDifferences = result.Comparisons.Sum(c => c.SignificantDifferences.Count);
        }
        
        return result;
    }
    
    private OrderContext BuildContextFromSnapshot(TemplateSnapshot snapshot, OrderRecord order)
    {
        var context = _engine.CreateOrderContext(order.OrderId);
        
        if (snapshot.OrderSnapshot != null)
        {
            if (order.OriginalAmount > 0)
            {
                context.AddItem(new OrderItem
                {
                    ProductId = $"SKU-{order.OrderId}",
                    Price = order.OriginalAmount,
                    Quantity = 1
                });
            }
            
            if (snapshot.OrderSnapshot.Member != null)
            {
                context.Member = new MemberInfo
                {
                    MemberId = snapshot.OrderSnapshot.Member.MemberId,
                    Level = Enum.TryParse<MemberLevel>(snapshot.OrderSnapshot.Member.Level, out var level) 
                        ? level : MemberLevel.Normal
                };
            }
            
            foreach (var kvp in snapshot.OrderSnapshot.ExtendedData)
            {
                context.ExtendedData[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            context.AddItem(new OrderItem
            {
                ProductId = $"SKU-{order.OrderId}",
                Price = order.OriginalAmount > 0 ? order.OriginalAmount : snapshot.OriginalAmount,
                Quantity = 1
            });
        }
        
        context.ExtendedData["StoreId"] = order.StoreId;
        context.ExtendedData["ChannelId"] = order.Channel;
        
        return context;
    }
    
    private List<Coupon> BuildCouponsFromSnapshot(TemplateSnapshot snapshot)
    {
        return snapshot.CouponsSnapshot.Select(c => new Coupon
        {
            CouponId = c.CouponId,
            CouponCode = c.CouponCode,
            Type = Enum.TryParse<CouponType>(c.Type, out var type) ? type : CouponType.AmountOff,
            DiscountValue = c.DiscountValue,
            MinOrderAmount = c.MinOrderAmount,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo
        }).ToList();
    }
    
    private void ApplyParameterOverride(OrderContext context, string key, object value)
    {
        switch (key.ToLower())
        {
            case "amount" when value is decimal d:
                if (context.Items.Any())
                {
                    context.Items.First().Price = d;
                }
                break;
            case "quantity" when value is int q:
                if (context.Items.Any())
                {
                    context.Items.First().Quantity = q;
                }
                break;
            case "memberlevel" when value is string ml:
                context.Member = new MemberInfo
                {
                    MemberId = context.Member?.MemberId ?? "Unknown",
                    Level = Enum.TryParse<MemberLevel>(ml, out var level) ? level : MemberLevel.Normal
                };
                break;
            case "storeid":
                context.ExtendedData["StoreId"] = value.ToString();
                break;
            case "channelid":
                context.ExtendedData["ChannelId"] = value.ToString();
                break;
        }
    }
    
    private List<TemplateDifference> CompareSnapshots(
        TemplateSnapshot original,
        CalculationResult current)
    {
        var differences = new List<TemplateDifference>();
        
        var originalAmount = original.Result?.RecommendedPlan?.FinalTotalAmount 
                           ?? original.OrderSnapshot?.OriginalAmount ?? 0;
        var currentAmount = current.RecommendedPlan?.FinalTotalAmount ?? current.OriginalAmount;
        
        if (Math.Abs(originalAmount - currentAmount) > 0.01m)
        {
            differences.Add(new TemplateDifference
            {
                Field = "FinalAmount",
                Category = "Amount",
                OldValue = originalAmount.ToString("F2"),
                NewValue = currentAmount.ToString("F2"),
                Description = $"最终金额变化: ¥{currentAmount - originalAmount:F2}",
                IsSignificant = Math.Abs(currentAmount - originalAmount) / Math.Max(originalAmount, 1) > 0.05m
            });
        }
        
        var originalDiscount = original.Result?.RecommendedPlan?.TotalDiscountAmount ?? 0;
        var currentDiscount = current.RecommendedPlan?.TotalDiscountAmount ?? 0;
        
        if (Math.Abs(originalDiscount - currentDiscount) > 0.01m)
        {
            differences.Add(new TemplateDifference
            {
                Field = "TotalDiscount",
                Category = "Discount",
                OldValue = originalDiscount.ToString("F2"),
                NewValue = currentDiscount.ToString("F2"),
                Description = $"优惠金额变化: ¥{currentDiscount - originalDiscount:F2}",
                IsSignificant = true
            });
        }
        
        var originalCoupons = original.AppliedCouponIds ?? new List<string>();
        var currentCoupons = current.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        
        var removed = originalCoupons.Except(currentCoupons).ToList();
        var added = currentCoupons.Except(originalCoupons).ToList();
        
        if (removed.Any())
        {
            differences.Add(new TemplateDifference
            {
                Field = "RemovedCoupons",
                Category = "Coupons",
                OldValue = string.Join(", ", removed),
                NewValue = "",
                Description = $"移除优惠券: {string.Join(", ", removed)}",
                IsSignificant = true
            });
        }
        
        if (added.Any())
        {
            differences.Add(new TemplateDifference
            {
                Field = "AddedCoupons",
                Category = "Coupons",
                OldValue = "",
                NewValue = string.Join(", ", added),
                Description = $"新增优惠券: {string.Join(", ", added)}",
                IsSignificant = true
            });
        }
        
        return differences;
    }
    
    private List<TemplateDifference> CompareTwoSnapshots(
        TemplateSnapshot snapshot1,
        TemplateSnapshot snapshot2)
    {
        return CompareSnapshots(snapshot1, snapshot2.Result ?? new CalculationResult());
    }
    
    private decimal CalculateMatchScore(TemplateSnapshot original, CalculationResult current)
    {
        var score = 100m;
        
        var amountDiff = Math.Abs(
            (original.Result?.RecommendedPlan?.FinalTotalAmount ?? 0) -
            (current.RecommendedPlan?.FinalTotalAmount ?? current.OriginalAmount));
        
        if (amountDiff > 100)
            score -= 30;
        else if (amountDiff > 10)
            score -= 10;
        else if (amountDiff > 0.01m)
            score -= 5;
        
        var originalCoupons = original.AppliedCouponIds ?? new List<string>();
        var currentCoupons = current.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>();
        
        var common = originalCoupons.Intersect(currentCoupons).Count();
        var total = Math.Max(originalCoupons.Count, currentCoupons.Count);
        
        if (total > 0)
        {
            score -= (1 - (decimal)common / total) * 20;
        }
        
        return Math.Max(0, Math.Min(100, score));
    }
    
    private string GenerateExplanation(CalculationTemplate template, TemplatePlaybackResult result)
    {
        var lines = new List<string>();
        
        lines.Add($"模板: {template.TemplateName}");
        lines.Add($"匹配度: {result.MatchScore:F1}%");
        
        if (result.IsMatch)
        {
            lines.Add("状态: ✓ 与模板口径一致");
        }
        else
        {
            lines.Add("状态: ⚠ 与模板口径存在差异");
            if (result.Differences.Any(d => d.IsSignificant))
            {
                lines.Add("显著差异:");
                foreach (var diff in result.Differences.Where(d => d.IsSignificant))
                {
                    lines.Add($"  - {diff.Description}");
                }
            }
        }
        
        return string.Join(Environment.NewLine, lines);
    }
    
    private List<string> ValidatePlayback(CalculationTemplate template, TemplatePlaybackResult result)
    {
        var warnings = new List<string>();
        
        if (result.Differences.Any(d => d.IsSignificant && d.Category == "Amount"))
        {
            warnings.Add("最终金额与模板存在显著差异");
        }
        
        if (result.Differences.Any(d => d.IsSignificant && d.Category == "Coupons"))
        {
            warnings.Add("优惠券应用与模板不一致");
        }
        
        if (result.MatchScore < 80)
        {
            warnings.Add($"匹配度过低({result.MatchScore:F1}%)，建议检查参数");
        }
        
        return warnings;
    }
    
    private void PopulateExplanationContent(
        UnifiedExplanation explanation,
        CalculationTemplate template,
        TemplatePlaybackResult result)
    {
        var content = new UnifiedExplanationContent
        {
            OriginalAmount = result.OriginalSnapshot?.OrderSnapshot?.OriginalAmount ?? 0,
            TotalDiscount = result.CurrentResult?.RecommendedPlan?.TotalDiscountAmount ?? 0,
            FinalAmount = result.CurrentResult?.RecommendedPlan?.FinalTotalAmount ?? 0,
            DiscountRate = result.OriginalSnapshot?.OrderSnapshot?.OriginalAmount > 0
                ? (result.CurrentResult?.RecommendedPlan?.TotalDiscountAmount ?? 0) / 
                  result.OriginalSnapshot.OrderSnapshot.OriginalAmount
                : 0
        };
        
        if (result.CurrentResult?.RecommendedPlan?.AppliedCoupons != null)
        {
            content.AppliedDiscounts = result.CurrentResult.RecommendedPlan.AppliedCoupons
                .Select(c => new AppliedDiscountInfo
                {
                    Name = c.CouponCode,
                    Amount = c.DiscountAmount
                }).ToList();
            
            content.DiscountDetails = result.CurrentResult.RecommendedPlan.AppliedCoupons
                .Select(c => new DiscountDetailInfo
                {
                    Type = c.Type.ToString(),
                    Amount = c.DiscountAmount,
                    Reason = c.Reason ?? "无"
                }).ToList();
        }
        
        if (result.Differences.Any())
        {
            content.OperationsNote = $"存在{result.Differences.Count(d => d.IsSignificant)}项显著差异";
        }
        
        explanation.Content = content;
    }
}

public class TemplateSearchResult
{
    public int TotalCount { get; set; }
    public List<CalculationTemplate> Templates { get; set; } = new();
    public TemplateSearchCriteria SearchCriteria { get; set; } = new();
}

public class BatchTemplateComparison
{
    public List<string> TemplateIds { get; set; } = new();
    public DateTime ComparedAt { get; set; }
    public List<TemplateComparison> Comparisons { get; set; } = new();
    public decimal AverageDifference { get; set; }
    public decimal MaxDifference { get; set; }
    public int TotalSignificantDifferences { get; set; }
}
