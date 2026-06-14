using CouponCalculator.Engine;
using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class BatchReconciliationService
{
    private readonly CouponCalculatorEngine _engine;
    private readonly ReconciliationService _reconciliationService;
    private readonly TemplateManagementService _templateService;
    private readonly ExportService _exportService;
    
    private readonly Dictionary<string, List<OrderRecord>> _orderRecords = new();
    
    public BatchReconciliationService()
    {
        _engine = new CouponCalculatorEngine();
        _reconciliationService = new ReconciliationService();
        _templateService = new TemplateManagementService();
        _exportService = new ExportService();
    }
    
    public async Task<BatchReconciliationResult> ProcessBatchReconciliationAsync(
        BatchReconciliationRequest request)
    {
        var result = new BatchReconciliationResult
        {
            RequestId = request.RequestId,
            ProcessedAt = DateTime.UtcNow
        };
        
        var orders = await LoadOrdersAsync(request);
        result.TotalOrders = orders.Count;
        
        var orderContexts = orders.Select(o => BuildOrderContext(o)).ToList();
        
        var stage1Results = new Dictionary<string, CalculationResult>();
        var stage2Results = new Dictionary<string, CalculationResult>();
        
        foreach (var order in orders)
        {
            try
            {
                var context = BuildOrderContext(order);
                var coupons = LoadCouponsForOrder(order);
                var result1 = _engine.CalculateOptimalEnhanced(context, coupons);
                stage1Results[order.OrderId] = result1;
                result.ProcessedOrders++;
            }
            catch (Exception ex)
            {
                result.FailedOrders++;
                result.Anomalies.Add(new AnomalyOrder
                {
                    OrderId = order.OrderId,
                    AnomalyType = "CalculationError",
                    Description = ex.Message,
                    Severity = "High"
                });
            }
        }
        
        foreach (var order in orders)
        {
            try
            {
                var context = BuildOrderContextWithLatestRules(order);
                var coupons = LoadCouponsForOrder(order);
                var result2 = _engine.CalculateOptimalEnhanced(context, coupons);
                stage2Results[order.OrderId] = result2;
            }
            catch
            {
                result.FailedOrders++;
            }
        }
        
        foreach (var orderId in stage1Results.Keys)
        {
            if (!stage2Results.ContainsKey(orderId)) continue;
            
            var r1 = stage1Results[orderId];
            var r2 = stage2Results[orderId];
            var order = orders.First(o => o.OrderId == orderId);
            
            var detail = new OrderReconciliationDetail
            {
                OrderId = orderId,
                OriginalAmount = r1.OriginalAmount,
                FinalAmount = r1.RecommendedPlan?.FinalTotalAmount ?? r1.OriginalAmount,
                TotalDiscount = r1.RecommendedPlan?.TotalDiscountAmount ?? 0,
                Stage1Amount = r1.RecommendedPlan?.FinalTotalAmount ?? r1.OriginalAmount,
                Stage2Amount = r2.RecommendedPlan?.FinalTotalAmount ?? r2.OriginalAmount,
                Difference = (r1.RecommendedPlan?.FinalTotalAmount ?? r1.OriginalAmount) - 
                             (r2.RecommendedPlan?.FinalTotalAmount ?? r2.OriginalAmount),
                HasDifference = Math.Abs((r1.RecommendedPlan?.FinalTotalAmount ?? r1.OriginalAmount) - 
                              (r2.RecommendedPlan?.FinalTotalAmount ?? r2.OriginalAmount)) > 0.01m,
                AppliedCoupons = r1.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToList() ?? new List<string>(),
                ChangeReasons = DetectChangeReasons(r1, r2),
                CreatedAt = order.CreatedAt,
                StoreId = order.StoreId,
                UserId = order.UserId,
                Channel = order.Channel,
                MemberLevel = order.MemberLevel,
                CampaignId = order.CampaignId
            };
            
            if (detail.HasDifference)
            {
                result.Summary.OrdersWithDifference++;
                
                if (Math.Abs(detail.Difference) > result.Summary.MaxDifference)
                    result.Summary.MaxDifference = Math.Abs(detail.Difference);
            }
            
            ApplyFilters(detail, request.Filters);
            
            if (detail.IsAnomaly || detail.HasDifference)
            {
                result.Details.Add(detail);
            }
            
            if (detail.IsAnomaly)
            {
                result.Anomalies.Add(new AnomalyOrder
                {
                    OrderId = detail.OrderId,
                    AnomalyType = detail.AnomalyType ?? "Unknown",
                    Description = detail.AnomalyDescription ?? "Anomaly detected",
                    ImpactAmount = Math.Abs(detail.Difference),
                    Severity = ClassifySeverity(Math.Abs(detail.Difference))
                });
            }
        }
        
        ComputeSummary(result, request);
        
        if (request.ExportOptions.GroupBy && !string.IsNullOrEmpty(request.ExportOptions.GroupByField))
        {
            result.GroupedResult = GroupResults(result.Details, request.ExportOptions.GroupByField);
        }
        
        if (request.ExportOptions.IncludeSummary)
        {
            result.ExportData = GenerateExportData(result, request.ExportOptions);
        }
        
        return result;
    }
    
    public async Task<BatchReconciliationResult> DailyReconciliationAsync(
        DateTime date,
        List<string>? storeIds = null,
        ExportOptions? exportOptions = null)
    {
        var request = new BatchReconciliationRequest
        {
            Name = $"每日对账-{date:yyyy-MM-dd}",
            FromDate = date.Date,
            ToDate = date.Date.AddDays(1),
            StoreIds = storeIds,
            ReconciliationType = ReconciliationType.FullFlow,
            ExportOptions = exportOptions ?? new ExportOptions()
        };
        
        return await ProcessBatchReconciliationAsync(request);
    }
    
    public async Task<BatchReconciliationResult> CampaignReconciliationAsync(
        string campaignId,
        ExportOptions? exportOptions = null)
    {
        var request = new BatchReconciliationRequest
        {
            Name = $"活动对账-{campaignId}",
            Campaigns = new List<string> { campaignId },
            ReconciliationType = ReconciliationType.FullFlow,
            ExportOptions = exportOptions ?? new ExportOptions()
        };
        
        return await ProcessBatchReconciliationAsync(request);
    }
    
    public async Task<string> ExportReconciliationResultAsync(
        BatchReconciliationResult result,
        ExportFormat format,
        string filePath)
    {
        return await _exportService.ExportAsync(result, format, filePath);
    }
    
    public List<AnomalyOrder> GetTopAnomalies(BatchReconciliationResult result, int topN = 10)
    {
        return result.Anomalies
            .OrderByDescending(a => a.ImpactAmount)
            .Take(topN)
            .ToList();
    }
    
    public List<OrderReconciliationDetail> GetTopDifferenceOrders(
        BatchReconciliationResult result, 
        int topN = 10)
    {
        return result.Details
            .Where(d => d.HasDifference)
            .OrderByDescending(d => Math.Abs(d.Difference))
            .Take(topN)
            .ToList();
    }
    
    public CampaignReconciliation GenerateCampaignReport(BatchReconciliationResult result, string campaignId)
    {
        var campaignReport = new CampaignReconciliation
        {
            CampaignId = campaignId,
            CampaignName = result.RequestId,
            CampaignStartDate = DateTime.UtcNow.AddDays(-30),
            CampaignEndDate = DateTime.UtcNow,
            TotalOrders = result.Summary.TotalOrders,
            TotalOriginalAmount = result.Summary.OriginalTotal,
            TotalDiscount = result.Summary.DiscountTotal,
            AverageDiscountRate = result.Summary.AverageDiscountRate
        };
        
        foreach (var detail in result.Details)
        {
            if (!string.IsNullOrEmpty(detail.CampaignId) && detail.CampaignId == campaignId)
            {
                campaignReport.RuleEffects.Add(new RuleEffectiveness
                {
                    RuleId = detail.AppliedCoupons.FirstOrDefault() ?? "Unknown",
                    RuleName = $"规则_{detail.AppliedCoupons.FirstOrDefault()}",
                    UsageCount = 1,
                    TotalDiscount = detail.TotalDiscount,
                    AverageDiscount = detail.TotalDiscount
                });
            }
        }
        
        return campaignReport;
    }
    
    private async Task<List<OrderRecord>> LoadOrdersAsync(BatchReconciliationRequest request)
    {
        await Task.Delay(10);
        
        var orders = new List<OrderRecord>();
        
        if (request.OrderIds.Any())
        {
            foreach (var orderId in request.OrderIds)
            {
                orders.Add(new OrderRecord
                {
                    OrderId = orderId,
                    OriginalAmount = 100 + Random.Shared.Next(100, 500),
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    StoreId = $"STORE-{Random.Shared.Next(1, 10)}",
                    UserId = $"USER-{Random.Shared.Next(1000, 9999)}",
                    Channel = Random.Shared.Next(2) == 0 ? "APP" : "WEB",
                    MemberLevel = $"Level-{Random.Shared.Next(1, 5)}"
                });
            }
        }
        else
        {
            var days = (int)(request.ToDate - request.FromDate).TotalDays;
            days = Math.Max(1, Math.Min(days, 30));
            
            for (int i = 0; i < days * 10; i++)
            {
                orders.Add(new OrderRecord
                {
                    OrderId = $"ORDER-{Guid.NewGuid():N}".Substring(0, 20),
                    OriginalAmount = 100 + Random.Shared.Next(100, 500),
                    CreatedAt = request.FromDate.AddDays(Random.Shared.Next(0, days)),
                    StoreId = $"STORE-{Random.Shared.Next(1, 10)}",
                    UserId = $"USER-{Random.Shared.Next(1000, 9999)}",
                    Channel = Random.Shared.Next(2) == 0 ? "APP" : "WEB",
                    MemberLevel = $"Level-{Random.Shared.Next(1, 5)}",
                    CampaignId = request.Campaigns?.FirstOrDefault()
                });
            }
        }
        
        if (request.StoreIds?.Any() == true)
        {
            orders = orders.Where(o => request.StoreIds.Contains(o.StoreId)).ToList();
        }
        
        if (request.UserIds?.Any() == true)
        {
            orders = orders.Where(o => request.UserIds.Contains(o.UserId)).ToList();
        }
        
        return orders;
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
        context.ExtendedData["CampaignId"] = order.CampaignId ?? "";
        
        return context;
    }
    
    private OrderContext BuildOrderContextWithLatestRules(OrderRecord order)
    {
        var context = BuildOrderContext(order);
        context.ExtendedData["UseLatestRules"] = "true";
        return context;
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
    
    private List<string> DetectChangeReasons(CalculationResult r1, CalculationResult r2)
    {
        var reasons = new List<string>();
        
        if (r1.RecommendedPlan?.AppliedCoupons.Count != r2.RecommendedPlan?.AppliedCoupons.Count)
        {
            reasons.Add("优惠券应用数量发生变化");
        }
        
        var coupons1 = r1.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToHashSet() ?? new HashSet<string>();
        var coupons2 = r2.RecommendedPlan?.AppliedCoupons?.Select(c => c.CouponId).ToHashSet() ?? new HashSet<string>();
        
        var removed = coupons1.Except(coupons2);
        var added = coupons2.Except(coupons1);
        
        if (removed.Any())
            reasons.Add($"移除优惠券: {string.Join(", ", removed)}");
        if (added.Any())
            reasons.Add($"新增优惠券: {string.Join(", ", added)}");
        
        return reasons;
    }
    
    private void ApplyFilters(OrderReconciliationDetail detail, FilterCriteria filters)
    {
        if (filters.MinAmount.HasValue && detail.OriginalAmount < filters.MinAmount.Value)
            return;
        if (filters.MaxAmount.HasValue && detail.OriginalAmount > filters.MaxAmount.Value)
            return;
        if (filters.MinDiscount.HasValue && detail.TotalDiscount < filters.MinDiscount.Value)
            return;
        if (filters.MaxDiscount.HasValue && detail.TotalDiscount > filters.MaxDiscount.Value)
            return;
        
        if (filters.HasAnomaly == true && !detail.IsAnomaly)
            return;
        if (filters.HasChange == true && !detail.HasDifference)
            return;
        
        if (filters.TopNByDifference.HasValue && detail.HasDifference)
        {
            var diff = Math.Abs(detail.Difference);
        }
        
        if (filters.MemberLevels?.Any() == true && 
            !filters.MemberLevels.Contains(detail.MemberLevel ?? ""))
            return;
        if (filters.Channels?.Any() == true && 
            !filters.Channels.Contains(detail.Channel ?? ""))
            return;
        if (filters.Stores?.Any() == true && 
            !filters.Stores.Contains(detail.StoreId ?? ""))
            return;
    }
    
    private void ComputeSummary(BatchReconciliationResult result, BatchReconciliationRequest request)
    {
        var filteredDetails = result.Details;
        
        result.Summary.TotalOrders = result.ProcessedOrders;
        result.Summary.OrdersWithDifference = filteredDetails.Count(d => d.HasDifference);
        result.Summary.OrdersWithAnomaly = filteredDetails.Count(d => d.IsAnomaly);
        
        result.Summary.OriginalTotal = filteredDetails.Sum(d => d.OriginalAmount);
        result.Summary.DiscountTotal = filteredDetails.Sum(d => d.TotalDiscount);
        result.Summary.FinalTotal = filteredDetails.Sum(d => d.FinalAmount);
        
        if (result.Summary.OriginalTotal > 0)
        {
            result.Summary.AverageDiscountRate = result.Summary.DiscountTotal / result.Summary.OriginalTotal;
        }
        
        var differences = filteredDetails.Where(d => d.HasDifference).Select(d => Math.Abs(d.Difference)).ToList();
        if (differences.Any())
        {
            result.Summary.TotalDifference = differences.Sum();
            result.Summary.AverageDifference = differences.Average();
            result.Summary.MaxDifference = differences.Max();
            result.Summary.MinDifference = differences.Min();
        }
        
        result.Summary.OrdersWithDifference = filteredDetails.Count(d => d.Difference > 0);
        result.Summary.DecreasedDiscountOrders = filteredDetails.Count(d => d.Difference < 0);
        result.Summary.NoChangeOrders = filteredDetails.Count(d => !d.HasDifference);
        
        result.Summary.TopDifferenceOrders = filteredDetails
            .Where(d => d.HasDifference)
            .OrderByDescending(d => Math.Abs(d.Difference))
            .Take(10)
            .Select(d => new TopDifferenceOrder
            {
                OrderId = d.OrderId,
                Difference = d.Difference,
                DifferencePercent = d.OriginalAmount > 0 ? d.Difference / d.OriginalAmount : 0,
                Reason = string.Join("; ", d.ChangeReasons)
            })
            .ToList();
        
        result.Summary.DifferenceByChannel = filteredDetails
            .GroupBy(d => d.Channel ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Difference)));
        
        result.Summary.DifferenceByStore = filteredDetails
            .GroupBy(d => d.StoreId ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Difference)));
        
        result.Summary.DifferenceByMemberLevel = filteredDetails
            .GroupBy(d => d.MemberLevel ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Abs(x.Difference)));
    }
    
    private GroupedReconciliationResult GroupResults(List<OrderReconciliationDetail> details, string groupByField)
    {
        var grouped = new GroupedReconciliationResult();
        
        IEnumerable<IGrouping<string, OrderReconciliationDetail>> groups = groupByField switch
        {
            "Channel" => details.GroupBy(d => d.Channel ?? "Unknown"),
            "Store" => details.GroupBy(d => d.StoreId ?? "Unknown"),
            "MemberLevel" => details.GroupBy(d => d.MemberLevel ?? "Unknown"),
            "Campaign" => details.GroupBy(d => d.CampaignId ?? "Unknown"),
            _ => details.GroupBy(d => "All")
        };
        
        foreach (var g in groups)
        {
            grouped.Groups.Add(new ReconciliationGroup
            {
                GroupName = g.Key,
                GroupByField = groupByField,
                OrderCount = g.Count(),
                TotalDifference = g.Sum(x => Math.Abs(x.Difference)),
                AverageDifference = g.Average(x => Math.Abs(x.Difference)),
                AnomalyCount = g.Count(x => x.IsAnomaly),
                Orders = g.ToList()
            });
        }
        
        return grouped;
    }
    
    private BatchExportData GenerateExportData(BatchReconciliationResult result, ExportOptions options)
    {
        var exportData = new BatchExportData();
        
        var defaultColumns = new List<string>
        {
            "OrderId", "OriginalAmount", "FinalAmount", "TotalDiscount",
            "Stage1Amount", "Stage2Amount", "Difference", "HasDifference",
            "StoreId", "UserId", "Channel", "MemberLevel"
        };
        
        exportData.Headers = defaultColumns;
        
        foreach (var detail in result.Details)
        {
            exportData.Rows.Add(new List<string>
            {
                detail.OrderId,
                detail.OriginalAmount.ToString("F2"),
                detail.FinalAmount.ToString("F2"),
                detail.TotalDiscount.ToString("F2"),
                detail.Stage1Amount.ToString("F2"),
                detail.Stage2Amount.ToString("F2"),
                detail.Difference.ToString("F2"),
                detail.HasDifference.ToString(),
                detail.StoreId ?? "",
                detail.UserId ?? "",
                detail.Channel ?? "",
                detail.MemberLevel ?? ""
            });
        }
        
        return exportData;
    }
    
    private string ClassifySeverity(decimal impactAmount)
    {
        return impactAmount switch
        {
            > 100 => "Critical",
            > 50 => "High",
            > 10 => "Medium",
            _ => "Low"
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
    public string CampaignId { get; set; } = string.Empty;
}
