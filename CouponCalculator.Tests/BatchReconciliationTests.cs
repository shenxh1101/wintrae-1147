using CouponCalculator.Engine;
using CouponCalculator.Models;
using CouponCalculator.Services;
using Xunit;

namespace CouponCalculator.Tests;

public class BatchReconciliationTests
{
    private readonly BatchReconciliationService _service;
    
    public BatchReconciliationTests()
    {
        _service = new BatchReconciliationService();
    }
    
    [Fact]
    public async Task ProcessBatchReconciliation_WithValidOrders_ReturnsResult()
    {
        var request = new BatchReconciliationRequest
        {
            Name = "Test Batch",
            ReconciliationType = ReconciliationType.FullFlow,
            ExportOptions = new ExportOptions
            {
                Format = ExportFormat.Csv,
                IncludeSummary = true
            }
        };
        
        var result = await _service.ProcessBatchReconciliationAsync(request);
        
        Assert.NotNull(result);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.True(result.TotalOrders >= 0);
    }
    
    [Fact]
    public async Task DailyReconciliation_ReturnsValidResult()
    {
        var date = DateTime.Today.AddDays(-1);
        
        var result = await _service.DailyReconciliationAsync(date);
        
        Assert.NotNull(result);
        Assert.Contains("每日对账", result.RequestId);
    }
    
    [Fact]
    public async Task CampaignReconciliation_WithValidCampaign_ReturnsResult()
    {
        var campaignId = "CAMPAIGN-001";
        
        var result = await _service.CampaignReconciliationAsync(campaignId);
        
        Assert.NotNull(result);
        Assert.Contains(campaignId, result.RequestId);
    }
    
    [Fact]
    public void BatchReconciliationResult_GenerateSummaryReport_ReturnsNonEmpty()
    {
        var result = new BatchReconciliationResult
        {
            ResultId = "TEST-001",
            ProcessedAt = DateTime.UtcNow,
            TotalOrders = 100,
            ProcessedOrders = 95,
            FailedOrders = 5,
            Summary = new ReconciliationSummary
            {
                OriginalTotal = 10000m,
                DiscountTotal = 1000m,
                FinalTotal = 9000m,
                OrdersWithDifference = 10
            }
        };
        
        var report = result.GenerateSummaryReport();
        
        Assert.NotEmpty(report);
        Assert.Contains("批量对账汇总报告", report);
    }
    
    [Fact]
    public void BatchExportData_GenerateExportContent_ReturnsValidCsv()
    {
        var exportData = new BatchExportData
        {
            Headers = new List<string> { "OrderId", "Amount" },
            Rows = new List<List<string>>
            {
                new List<string> { "ORDER-001", "100.00" },
                new List<string> { "ORDER-002", "200.00" }
            }
        };
        
        var result = new BatchReconciliationResult
        {
            ExportData = exportData
        };
        
        var content = result.GenerateExportContent();
        
        Assert.Contains("OrderId", content);
        Assert.Contains("ORDER-001", content);
    }
    
    [Fact]
    public async Task ProcessBatchReconciliation_WithFilters_AppliesFilters()
    {
        var request = new BatchReconciliationRequest
        {
            Name = "Filtered Batch",
            ReconciliationType = ReconciliationType.FullFlow,
            Filters = new FilterCriteria
            {
                MinAmount = 200m,
                MaxAmount = 500m
            }
        };
        
        var result = await _service.ProcessBatchReconciliationAsync(request);
        
        Assert.NotNull(result);
        foreach (var detail in result.Details)
        {
            Assert.True(detail.OriginalAmount >= 200m);
            Assert.True(detail.OriginalAmount <= 500m);
        }
    }
    
    [Fact]
    public async Task ProcessBatchReconciliation_WithGrouping_GroupsResults()
    {
        var request = new BatchReconciliationRequest
        {
            Name = "Grouped Batch",
            ReconciliationType = ReconciliationType.FullFlow,
            ExportOptions = new ExportOptions
            {
                GroupBy = true,
                GroupByField = "Channel"
            }
        };
        
        var result = await _service.ProcessBatchReconciliationAsync(request);
        
        Assert.NotNull(result.GroupedResult);
        Assert.True(result.GroupedResult.Groups.Any());
    }
    
    [Fact]
    public void GetTopAnomalies_ReturnsOrderedResults()
    {
        var result = new BatchReconciliationResult
        {
            Anomalies = new List<AnomalyOrder>
            {
                new AnomalyOrder { OrderId = "O1", ImpactAmount = 10m },
                new AnomalyOrder { OrderId = "O2", ImpactAmount = 100m },
                new AnomalyOrder { OrderId = "O3", ImpactAmount = 50m }
            }
        };
        
        var topAnomalies = _service.GetTopAnomalies(result, 2);
        
        Assert.Equal(2, topAnomalies.Count);
        Assert.Equal("O2", topAnomalies[0].OrderId);
        Assert.Equal("O3", topAnomalies[1].OrderId);
    }
    
    [Fact]
    public void GetTopDifferenceOrders_ReturnsOrderedResults()
    {
        var result = new BatchReconciliationResult
        {
            Details = new List<OrderReconciliationDetail>
            {
                new OrderReconciliationDetail { OrderId = "O1", Difference = 5m, HasDifference = true },
                new OrderReconciliationDetail { OrderId = "O2", Difference = 50m, HasDifference = true },
                new OrderReconciliationDetail { OrderId = "O3", Difference = 25m, HasDifference = true },
                new OrderReconciliationDetail { OrderId = "O4", Difference = 0m, HasDifference = false }
            }
        };
        
        var topDiffs = _service.GetTopDifferenceOrders(result, 2);
        
        Assert.Equal(2, topDiffs.Count);
        Assert.Equal("O2", topDiffs[0].OrderId);
        Assert.Equal("O3", topDiffs[1].OrderId);
    }
}

public class ExportServiceTests
{
    private readonly ExportService _exportService;
    
    public ExportServiceTests()
    {
        _exportService = new ExportService();
    }
    
    [Fact]
    public void ExportToCsv_ReturnsValidCsvFormat()
    {
        var result = CreateTestReconciliationResult();
        
        var csv = _exportService.ExportToCsv(result);
        
        Assert.NotEmpty(csv);
        Assert.Contains("订单号", csv);
        Assert.Contains("ORDER-001", csv);
    }
    
    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        var result = CreateTestReconciliationResult();
        
        var json = _exportService.ExportToJson(result);
        
        Assert.NotEmpty(json);
        Assert.Contains("summary", json);
        Assert.Contains("details", json);
    }
    
    [Fact]
    public void ExportToHtml_ReturnsValidHtml()
    {
        var result = CreateTestReconciliationResult();
        
        var html = _exportService.ExportToHtml(result);
        
        Assert.NotEmpty(html);
        Assert.Contains("<html", html);
        Assert.Contains("<table", html);
        Assert.Contains("批量对账报告", html);
    }
    
    [Fact]
    public void ExportToExcel_ReturnsValidXml()
    {
        var result = CreateTestReconciliationResult();
        
        var xml = _exportService.ExportToExcel(result);
        
        Assert.NotEmpty(xml);
        Assert.Contains("<?xml", xml);
        Assert.Contains("Workbook", xml);
    }
    
    [Fact]
    public void ExportToPdf_ReturnsSummaryReport()
    {
        var result = CreateTestReconciliationResult();
        
        var pdf = _exportService.ExportToPdf(result);
        
        Assert.NotEmpty(pdf);
        Assert.Contains("批量对账汇总报告", pdf);
    }
    
    [Fact]
    public void ExportReconciliationSummary_ReturnsFormattedReport()
    {
        var result = CreateTestReconciliationResult();
        
        var summary = _exportService.ExportReconciliationSummary(result);
        
        Assert.NotEmpty(summary);
        Assert.Contains("批量对账运营汇总报告", summary);
        Assert.Contains("对账概况", summary);
        Assert.Contains("金额汇总", summary);
    }
    
    [Fact]
    public async Task ExportAsync_CreatesFile()
    {
        var result = CreateTestReconciliationResult();
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.csv");
        
        try
        {
            await _exportService.ExportAsync(result, ExportFormat.Csv, tempPath);
            
            Assert.True(File.Exists(tempPath));
            var content = await File.ReadAllTextAsync(tempPath);
            Assert.NotEmpty(content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
    
    private BatchReconciliationResult CreateTestReconciliationResult()
    {
        return new BatchReconciliationResult
        {
            ResultId = "TEST-001",
            ProcessedAt = DateTime.UtcNow,
            TotalOrders = 100,
            ProcessedOrders = 98,
            FailedOrders = 2,
            Summary = new ReconciliationSummary
            {
                TotalOrders = 100,
                OrdersWithDifference = 15,
                OriginalTotal = 50000m,
                DiscountTotal = 5000m,
                FinalTotal = 45000m,
                AverageDiscountRate = 0.1m,
                TotalDifference = 500m,
                AverageDifference = 33.33m,
                MaxDifference = 100m,
                MinDifference = 5m
            },
            Details = new List<OrderReconciliationDetail>
            {
                new OrderReconciliationDetail
                {
                    OrderId = "ORDER-001",
                    OriginalAmount = 200m,
                    FinalAmount = 180m,
                    TotalDiscount = 20m,
                    HasDifference = true,
                    Difference = 5m,
                    Stage1Amount = 185m,
                    Stage2Amount = 180m,
                    StoreId = "STORE-001",
                    Channel = "APP",
                    MemberLevel = "VIP"
                }
            },
            Anomalies = new List<AnomalyOrder>
            {
                new AnomalyOrder
                {
                    OrderId = "ORDER-001",
                    AnomalyType = "PriceMismatch",
                    Description = "价格不一致",
                    ImpactAmount = 50m,
                    Severity = "High"
                }
            }
        };
    }
}

public class ReconciliationSummaryTests
{
    [Fact]
    public void ReconciliationSummary_CalculatesCorrectTotals()
    {
        var summary = new ReconciliationSummary
        {
            TotalOrders = 100,
            OrdersWithDifference = 20,
            OriginalTotal = 50000m,
            DiscountTotal = 5000m,
            FinalTotal = 45000m,
            AverageDiscountRate = 0.1m
        };
        
        Assert.Equal(100, summary.TotalOrders);
        Assert.Equal(20, summary.OrdersWithDifference);
        Assert.Equal(50000m, summary.OriginalTotal);
        Assert.Equal(0.1m, summary.AverageDiscountRate);
    }
    
    [Fact]
    public void TopDifferenceOrder_CalculatesCorrectPercent()
    {
        var order = new TopDifferenceOrder
        {
            OrderId = "TEST-001",
            Difference = 10m,
            DifferencePercent = 0.05m
        };
        
        Assert.Equal("TEST-001", order.OrderId);
        Assert.Equal(10m, order.Difference);
        Assert.Equal(0.05m, order.DifferencePercent);
    }
}
