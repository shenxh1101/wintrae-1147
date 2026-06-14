namespace CouponCalculator.Models;

public class ItemLevelDiscountBreakdown
{
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalOriginalAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalFinalAmount { get; set; }
    
    public List<ItemDiscountDetail> ItemDetails { get; set; } = new();
    public FreightDiscountDetail FreightDetail { get; set; } = new();
    public MemberDiscountDetail MemberDetail { get; set; } = new();
    
    public List<CouponAllocation> CouponAllocations { get; set; } = new();
    
    public DateTime CalculatedAt { get; set; }
    public string CalculationId { get; set; } = Guid.NewGuid().ToString();
    
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    public string GenerateBreakdownReport()
    {
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add("              商品级优惠明细分摊报告                           ");
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add($"订单号: {OrderId}");
        report.Add($"计算时间: {CalculatedAt:yyyy-MM-dd HH:mm:ss}");
        report.Add($"计算ID: {CalculationId}");
        report.Add("");
        
        report.Add("【商品明细】");
        foreach (var item in ItemDetails)
        {
            report.Add($"  {item.ProductName} ({item.ProductId})");
            report.Add($"    原价: ¥{item.OriginalAmount:F2} x {item.Quantity}件 = ¥{item.TotalOriginal:F2}");
            report.Add($"    优惠分摊:");
            
            if (item.CouponDiscounts.Any())
            {
                foreach (var coupon in item.CouponDiscounts)
                {
                    report.Add($"      · {coupon.CouponName}: ¥{coupon.AllocatedAmount:F2}");
                    report.Add($"        分摊比例: {coupon.AllocationRatio:P2}");
                    report.Add($"        分摊原因: {coupon.AllocationReason}");
                }
            }
            
            if (item.MemberDiscount > 0)
            {
                report.Add($"      · 会员折扣: ¥{item.MemberDiscount:F2}");
            }
            
            report.Add($"    最终金额: ¥{item.FinalAmount:F2}");
            report.Add("");
        }
        
        if (FreightDetail.DiscountAmount > 0)
        {
            report.Add("【运费明细】");
            report.Add($"  原运费: ¥{FreightDetail.OriginalFreight:F2}");
            report.Add($"  优惠: ¥{FreightDetail.DiscountAmount:F2}");
            report.Add($"  最终运费: ¥{FreightDetail.FinalFreight:F2}");
            report.Add($"  优惠来源: {FreightDetail.Source}");
            report.Add("");
        }
        
        if (MemberDetail.DiscountAmount > 0)
        {
            report.Add("【会员折扣明细】");
            report.Add($"  会员等级: {MemberDetail.MemberLevelName}");
            report.Add($"  折扣率: {MemberDetail.DiscountRate:P2}");
            report.Add($"  折扣金额: ¥{MemberDetail.DiscountAmount:F2}");
            report.Add("");
        }
        
        report.Add("【优惠券分摊汇总】");
        foreach (var allocation in CouponAllocations)
        {
            report.Add($"  {allocation.CouponName} ({allocation.CouponCode})");
            report.Add($"    总优惠: ¥{allocation.TotalDiscountAmount:F2}");
            report.Add($"    分摊明细:");
            foreach (var detail in allocation.ItemAllocations)
            {
                report.Add($"      · {detail.ProductName}: ¥{detail.Amount:F2} ({detail.Ratio:P2})");
            }
            report.Add("");
        }
        
        report.Add("【订单汇总】");
        report.Add($"  商品总额: ¥{TotalOriginalAmount:F2}");
        report.Add($"  总优惠: ¥{TotalDiscountAmount:F2}");
        report.Add($"  应付总额: ¥{TotalFinalAmount:F2}");
        report.Add("═══════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class ItemDiscountDetail
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    
    public decimal OriginalAmount { get; set; }
    public int Quantity { get; set; }
    public decimal TotalOriginal { get; set; }
    
    public List<CouponItemAllocation> CouponDiscounts { get; set; } = new();
    public decimal TotalCouponDiscount { get; set; }
    
    public decimal MemberDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    
    public decimal FinalAmount { get; set; }
    
    public List<string> AppliedCouponIds { get; set; } = new();
    
    public Dictionary<string, object> ExtendedData { get; set; } = new();
}

public class CouponItemAllocation
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType CouponType { get; set; }
    
    public decimal AllocatedAmount { get; set; }
    public decimal AllocationRatio { get; set; }
    public string AllocationReason { get; set; } = string.Empty;
    public string AllocationMethod { get; set; } = string.Empty;
    
    public decimal ApplicableAmount { get; set; }
    public decimal DiscountRate { get; set; }
    
    public bool IsProportional { get; set; }
    public bool IsFixed { get; set; }
}

public class CouponAllocation
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public CouponType CouponType { get; set; }
    
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalApplicableAmount { get; set; }
    
    public List<ItemAllocationDetail> ItemAllocations { get; set; } = new();
    
    public string AllocationStrategy { get; set; } = string.Empty;
    public Dictionary<string, string> AllocationRules { get; set; } = new();
}

public class ItemAllocationDetail
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Ratio { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RefundCalculation
{
    public string OriginalOrderId { get; set; } = string.Empty;
    public string OriginalCalculationId { get; set; } = string.Empty;
    public string RefundOrderId { get; set; } = string.Empty;
    
    public List<RefundItemDetail> RefundItems { get; set; } = new();
    
    public decimal OriginalTotalAmount { get; set; }
    public decimal OriginalTotalDiscount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RefundDiscount { get; set; }
    
    public List<RefundCouponDetail> RefundCoupons { get; set; } = new();
    
    public RefundMethod RefundMethod { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    
    public DateTime CalculatedAt { get; set; }
    public string CalculationId { get; set; } = Guid.NewGuid().ToString();
    
    public string GenerateRefundReport()
    {
        var report = new List<string>();
        
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add("              退款优惠重算报告                                ");
        report.Add("═══════════════════════════════════════════════════════════");
        report.Add($"原订单号: {OriginalOrderId}");
        report.Add($"退款订单号: {RefundOrderId}");
        report.Add($"退款原因: {RefundReason}");
        report.Add($"计算方法: {RefundMethod}");
        report.Add("");
        
        report.Add("【退款商品明细】");
        foreach (var item in RefundItems)
        {
            report.Add($"  {item.ProductName} ({item.ProductId})");
            report.Add($"    退款数量: {item.RefundQuantity}件");
            report.Add($"    原价: ¥{item.OriginalAmount:F2}");
            report.Add($"    原优惠分摊: ¥{item.OriginalDiscount:F2}");
            report.Add($"    退款金额: ¥{item.RefundAmount:F2}");
            report.Add($"    退款优惠: ¥{item.RefundDiscount:F2}");
            
            if (item.CouponRefunds.Any())
            {
                report.Add($"    优惠券退款明细:");
                foreach (var coupon in item.CouponRefunds)
                {
                    report.Add($"      · {coupon.CouponName}: ¥{coupon.RefundAmount:F2}");
                }
            }
            report.Add("");
        }
        
        report.Add("【退款汇总】");
        report.Add($"  原订单总额: ¥{OriginalTotalAmount:F2}");
        report.Add($"  原订单优惠: ¥{OriginalTotalDiscount:F2}");
        report.Add($"  退款金额: ¥{RefundAmount:F2}");
        report.Add($"  退款优惠: ¥{RefundDiscount:F2}");
        report.Add($"  实际退款: ¥{RefundAmount - RefundDiscount:F2}");
        
        if (RefundCoupons.Any())
        {
            report.Add("");
            report.Add("【优惠券处理】");
            foreach (var coupon in RefundCoupons)
            {
                report.Add($"  {coupon.CouponName} ({coupon.CouponCode})");
                report.Add($"    原优惠: ¥{coupon.OriginalDiscount:F2}");
                report.Add($"    退款优惠: ¥{coupon.RefundDiscount:F2}");
                report.Add($"    处理方式: {coupon.HandleMethod}");
            }
        }
        
        report.Add("═══════════════════════════════════════════════════════════");
        
        return string.Join(Environment.NewLine, report);
    }
}

public class RefundItemDetail
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    
    public int OriginalQuantity { get; set; }
    public int RefundQuantity { get; set; }
    
    public decimal OriginalAmount { get; set; }
    public decimal OriginalDiscount { get; set; }
    
    public decimal RefundAmount { get; set; }
    public decimal RefundDiscount { get; set; }
    
    public List<RefundCouponAllocation> CouponRefunds { get; set; } = new();
}

public class RefundCouponAllocation
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    public decimal OriginalAllocation { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RefundRatio { get; set; }
}

public class RefundCouponDetail
{
    public string CouponId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public string CouponName { get; set; } = string.Empty;
    
    public decimal OriginalDiscount { get; set; }
    public decimal RefundDiscount { get; set; }
    
    public string HandleMethod { get; set; } = string.Empty;
    public bool IsReturned { get; set; }
    public bool IsPartiallyUsed { get; set; }
}

public enum RefundMethod
{
    Proportional,
    OriginalRatio,
    Recalculate,
    FixedAmount
}