using System.Text;
using System.Text.Json;
using CouponCalculator.Models;

namespace CouponCalculator.Services;

public class ExportService
{
    public async Task<string> ExportAsync(
        BatchReconciliationResult result,
        ExportFormat format,
        string filePath)
    {
        var content = format switch
        {
            ExportFormat.Csv => ExportToCsv(result),
            ExportFormat.Json => ExportToJson(result),
            ExportFormat.Html => ExportToHtml(result),
            ExportFormat.Excel => ExportToExcel(result),
            ExportFormat.Pdf => ExportToPdf(result),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
        
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        return filePath;
    }
    
    public async Task<byte[]> ExportToBytesAsync(
        BatchReconciliationResult result,
        ExportFormat format)
    {
        var content = format switch
        {
            ExportFormat.Csv => ExportToCsv(result),
            ExportFormat.Json => ExportToJson(result),
            ExportFormat.Html => ExportToHtml(result),
            ExportFormat.Excel => ExportToExcel(result),
            ExportFormat.Pdf => ExportToPdf(result),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
        
        return Encoding.UTF8.GetBytes(content);
    }
    
    public string ExportToCsv(BatchReconciliationResult result)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("订单号,原始金额,优惠金额,最终金额,阶段1金额,阶段2金额,差异,是否有差异,店铺ID,用户ID,渠道,会员等级,创建时间");
        
        foreach (var detail in result.Details)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(detail.OrderId),
                detail.OriginalAmount.ToString("F2"),
                detail.TotalDiscount.ToString("F2"),
                detail.FinalAmount.ToString("F2"),
                detail.Stage1Amount.ToString("F2"),
                detail.Stage2Amount.ToString("F2"),
                detail.Difference.ToString("F2"),
                detail.HasDifference.ToString(),
                EscapeCsv(detail.StoreId ?? ""),
                EscapeCsv(detail.UserId ?? ""),
                EscapeCsv(detail.Channel ?? ""),
                EscapeCsv(detail.MemberLevel ?? ""),
                detail.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            ));
        }
        
        return sb.ToString();
    }
    
    public string ExportToJson(BatchReconciliationResult result)
    {
        var exportData = new
        {
            Summary = result.Summary,
            Details = result.Details,
            Anomalies = result.Anomalies,
            Groups = result.GroupedResult.Groups,
            ExportTime = DateTime.UtcNow,
            ResultId = result.ResultId
        };
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Serialize(exportData, options);
    }
    
    public string ExportToHtml(BatchReconciliationResult result)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>批量对账报告</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 20px; background: #f5f5f5; }
            .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            h1 { color: #333; border-bottom: 2px solid #007bff; padding-bottom: 10px; }
            h2 { color: #555; margin-top: 30px; }
            .summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin: 20px 0; }
            .summary-card { background: #f8f9fa; padding: 15px; border-radius: 8px; text-align: center; }
            .summary-card .value { font-size: 24px; font-weight: bold; color: #007bff; }
            .summary-card .label { color: #666; font-size: 14px; margin-top: 5px; }
            table { width: 100%; border-collapse: collapse; margin: 20px 0; }
            th { background: #007bff; color: white; padding: 12px; text-align: left; }
            td { padding: 10px 12px; border-bottom: 1px solid #dee2e6; }
            tr:hover { background: #f8f9fa; }
            .anomaly { color: #dc3545; font-weight: bold; }
            .difference { color: #ffc107; }
            .footer { margin-top: 30px; text-align: center; color: #666; font-size: 12px; }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");
        
        sb.AppendLine($"<h1>批量对账报告 - {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}</h1>");
        
        sb.AppendLine("<h2>汇总统计</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<div class=\"summary-card\"><div class=\"value\">{result.TotalOrders}</div><div class=\"label\">总订单数</div></div>");
        sb.AppendLine($"<div class=\"summary-card\"><div class=\"value\">¥{result.Summary.OriginalTotal:F2}</div><div class=\"label\">原始总额</div></div>");
        sb.AppendLine($"<div class=\"summary-card\"><div class=\"value\">¥{result.Summary.DiscountTotal:F2}</div><div class=\"label\">优惠总额</div></div>");
        sb.AppendLine($"<div class=\"summary-card\"><div class=\"value\">{result.Summary.OrdersWithDifference}</div><div class=\"label\">差异订单</div></div>");
        sb.AppendLine("</div>");
        
        if (result.Anomalies.Any())
        {
            sb.AppendLine("<h2>异常订单 TOP 10</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>订单号</th><th>异常类型</th><th>描述</th><th>影响金额</th><th>严重程度</th></tr>");
            
            foreach (var anomaly in result.Anomalies.Take(10))
            {
                var severityClass = anomaly.Severity switch
                {
                    "Critical" => "anomaly",
                    "High" => "anomaly",
                    _ => ""
                };
                
                sb.AppendLine($"<tr class=\"{severityClass}\">");
                sb.AppendLine($"<td>{anomaly.OrderId}</td>");
                sb.AppendLine($"<td>{anomaly.AnomalyType}</td>");
                sb.AppendLine($"<td>{anomaly.Description}</td>");
                sb.AppendLine($"<td>¥{anomaly.ImpactAmount:F2}</td>");
                sb.AppendLine($"<td>{anomaly.Severity}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }
        
        sb.AppendLine("<h2>差异订单 TOP 20</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>订单号</th><th>原始金额</th><th>阶段1</th><th>阶段2</th><th>差异</th><th>店铺</th><th>渠道</th><th>变化原因</th></tr>");
        
        foreach (var detail in result.Details.Where(d => d.HasDifference).Take(20))
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{detail.OrderId}</td>");
            sb.AppendLine($"<td>¥{detail.OriginalAmount:F2}</td>");
            sb.AppendLine($"<td>¥{detail.Stage1Amount:F2}</td>");
            sb.AppendLine($"<td>¥{detail.Stage2Amount:F2}</td>");
            sb.AppendLine($"<td class=\"difference\">{(detail.Difference >= 0 ? "+" : "")}¥{detail.Difference:F2}</td>");
            sb.AppendLine($"<td>{detail.StoreId}</td>");
            sb.AppendLine($"<td>{detail.Channel}</td>");
            sb.AppendLine($"<td>{string.Join(", ", detail.ChangeReasons)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</table>");
        
        if (result.GroupedResult.Groups.Any())
        {
            sb.AppendLine("<h2>分组统计</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>分组</th><th>订单数</th><th>差异总额</th><th>平均差异</th><th>异常数</th></tr>");
            
            foreach (var group in result.GroupedResult.Groups.Take(10))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{group.GroupName}</td>");
                sb.AppendLine($"<td>{group.OrderCount}</td>");
                sb.AppendLine($"<td>¥{group.TotalDifference:F2}</td>");
                sb.AppendLine($"<td>¥{group.AverageDifference:F2}</td>");
                sb.AppendLine($"<td>{group.AnomalyCount}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }
        
        sb.AppendLine("<div class=\"footer\">");
        sb.AppendLine($"<p>报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"<p>批次ID: {result.ResultId}</p>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
    
    public string ExportToExcel(BatchReconciliationResult result)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine("<Worksheet ss:Name=\"对账汇总\">");
        sb.AppendLine("<Table>");
        
        sb.AppendLine("<Row>");
        sb.AppendLine("<Cell><Data ss:Type=\"String\">指标</Data></Cell>");
        sb.AppendLine("<Cell><Data ss:Type=\"String\">数值</Data></Cell>");
        sb.AppendLine("</Row>");
        
        AddExcelRow(sb, "总订单数", result.TotalOrders.ToString());
        AddExcelRow(sb, "处理成功", result.ProcessedOrders.ToString());
        AddExcelRow(sb, "处理失败", result.FailedOrders.ToString());
        AddExcelRow(sb, "原始总额", $"¥{result.Summary.OriginalTotal:F2}");
        AddExcelRow(sb, "优惠总额", $"¥{result.Summary.DiscountTotal:F2}");
        AddExcelRow(sb, "应付总额", $"¥{result.Summary.FinalTotal:F2}");
        AddExcelRow(sb, "差异订单数", result.Summary.OrdersWithDifference.ToString());
        AddExcelRow(sb, "差异总额", $"¥{result.Summary.TotalDifference:F2}");
        AddExcelRow(sb, "平均差异", $"¥{result.Summary.AverageDifference:F2}");
        AddExcelRow(sb, "最大差异", $"¥{result.Summary.MaxDifference:F2}");
        
        sb.AppendLine("</Table>");
        sb.AppendLine("</Worksheet>");
        
        sb.AppendLine("<Worksheet ss:Name=\"差异订单\">");
        sb.AppendLine("<Table>");
        
        sb.AppendLine("<Row>");
        var headers = new[] { "订单号", "原始金额", "优惠金额", "阶段1", "阶段2", "差异", "店铺", "渠道", "变化原因" };
        foreach (var header in headers)
        {
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{header}</Data></Cell>");
        }
        sb.AppendLine("</Row>");
        
        foreach (var detail in result.Details.Where(d => d.HasDifference).Take(1000))
        {
            sb.AppendLine("<Row>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{detail.OrderId}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{detail.OriginalAmount:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{detail.TotalDiscount:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{detail.Stage1Amount:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{detail.Stage2Amount:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{detail.Difference:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{detail.StoreId}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{detail.Channel}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{string.Join("; ", detail.ChangeReasons)}</Data></Cell>");
            sb.AppendLine("</Row>");
        }
        
        sb.AppendLine("</Table>");
        sb.AppendLine("</Worksheet>");
        
        sb.AppendLine("<Worksheet ss:Name=\"异常订单\">");
        sb.AppendLine("<Table>");
        
        sb.AppendLine("<Row>");
        var anomalyHeaders = new[] { "订单号", "异常类型", "描述", "影响金额", "严重程度" };
        foreach (var header in anomalyHeaders)
        {
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{header}</Data></Cell>");
        }
        sb.AppendLine("</Row>");
        
        foreach (var anomaly in result.Anomalies.Take(1000))
        {
            sb.AppendLine("<Row>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{anomaly.OrderId}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{anomaly.AnomalyType}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{anomaly.Description}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"Number\">{anomaly.ImpactAmount:F2}</Data></Cell>");
            sb.AppendLine($"<Cell><Data ss:Type=\"String\">{anomaly.Severity}</Data></Cell>");
            sb.AppendLine("</Row>");
        }
        
        sb.AppendLine("</Table>");
        sb.AppendLine("</Worksheet>");
        
        sb.AppendLine("</Workbook>");
        
        return sb.ToString();
    }
    
    public string ExportToPdf(BatchReconciliationResult result)
    {
        return result.GenerateSummaryReport();
    }
    
    public string ExportReconciliationSummary(BatchReconciliationResult result)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    批量对账运营汇总报告                        ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"批次ID: {result.ResultId}");
        sb.AppendLine($"处理时间: {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("");
        
        sb.AppendLine("【一、对账概况】");
        sb.AppendLine($"  总订单数: {result.TotalOrders}");
        sb.AppendLine($"  处理成功: {result.ProcessedOrders}");
        sb.AppendLine($"  处理失败: {result.FailedOrders}");
        sb.AppendLine($"  成功率: {(result.TotalOrders > 0 ? result.ProcessedOrders * 100.0 / result.TotalOrders : 0):F2}%");
        sb.AppendLine("");
        
        sb.AppendLine("【二、金额汇总】");
        sb.AppendLine($"  原始总额: ¥{result.Summary.OriginalTotal:N2}");
        sb.AppendLine($"  优惠总额: ¥{result.Summary.DiscountTotal:N2}");
        sb.AppendLine($"  应付总额: ¥{result.Summary.FinalTotal:N2}");
        sb.AppendLine($"  平均优惠率: {result.Summary.AverageDiscountRate:P2}");
        sb.AppendLine("");
        
        sb.AppendLine("【三、差异分析】");
        sb.AppendLine($"  有差异订单: {result.Summary.OrdersWithDifference} ({result.Summary.OrdersWithDifference * 100.0 / Math.Max(result.TotalOrders, 1):F2}%)");
        sb.AppendLine($"  优惠增加订单: {result.Summary.IncreasedDiscountOrders}");
        sb.AppendLine($"  优惠减少订单: {result.Summary.DecreasedDiscountOrders}");
        sb.AppendLine($"  差异总额: ¥{result.Summary.TotalDifference:N2}");
        sb.AppendLine($"  平均差异: ¥{result.Summary.AverageDifference:N2}");
        sb.AppendLine($"  最大差异: ¥{result.Summary.MaxDifference:N2}");
        sb.AppendLine("");
        
        if (result.Summary.DifferenceByChannel.Any())
        {
            sb.AppendLine("【四、按渠道差异】");
            foreach (var kvp in result.Summary.DifferenceByChannel.OrderByDescending(x => x.Value).Take(5))
            {
                sb.AppendLine($"  {kvp.Key}: ¥{kvp.Value:N2}");
            }
            sb.AppendLine("");
        }
        
        if (result.Summary.DifferenceByStore.Any())
        {
            sb.AppendLine("【五、按店铺差异 TOP 10】");
            foreach (var kvp in result.Summary.DifferenceByStore.OrderByDescending(x => x.Value).Take(10))
            {
                sb.AppendLine($"  {kvp.Key}: ¥{kvp.Value:N2}");
            }
            sb.AppendLine("");
        }
        
        if (result.Summary.DifferenceByMemberLevel.Any())
        {
            sb.AppendLine("【六、按会员等级差异】");
            foreach (var kvp in result.Summary.DifferenceByMemberLevel.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  {kvp.Key}: ¥{kvp.Value:N2}");
            }
            sb.AppendLine("");
        }
        
        if (result.Anomalies.Any())
        {
            sb.AppendLine($"【七、异常订单汇总】共 {result.Anomalies.Count} 个异常");
            sb.AppendLine($"  严重(Critical): {result.Anomalies.Count(a => a.Severity == "Critical")}");
            sb.AppendLine($"  高(High): {result.Anomalies.Count(a => a.Severity == "High")}");
            sb.AppendLine($"  中(Medium): {result.Anomalies.Count(a => a.Severity == "Medium")}");
            sb.AppendLine($"  低(Low): {result.Anomalies.Count(a => a.Severity == "Low")}");
            sb.AppendLine("");
        }
        
        sb.AppendLine("【八、TOP 10 差异订单】");
        foreach (var order in result.Summary.TopDifferenceOrders.Take(10))
        {
            sb.AppendLine($"  {order.OrderId}: ¥{order.Difference:N2} ({order.DifferencePercent:P2})");
            if (!string.IsNullOrEmpty(order.Reason))
                sb.AppendLine($"    原因: {order.Reason}");
        }
        sb.AppendLine("");
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                          报告结束                              ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }
    
    private void AddExcelRow(StringBuilder sb, string label, string value)
    {
        sb.AppendLine("<Row>");
        sb.AppendLine($"<Cell><Data ss:Type=\"String\">{label}</Data></Cell>");
        sb.AppendLine($"<Cell><Data ss:Type=\"String\">{value}</Data></Cell>");
        sb.AppendLine("</Row>");
    }
    
    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
