namespace CouponCalculator.Models;

public class MemberInfo
{
    public string MemberId { get; set; } = string.Empty;
    public MemberLevel Level { get; set; } = MemberLevel.Normal;
    public int Points { get; set; }
    public decimal PointsBalance { get; set; }
    public Dictionary<string, object> ExtendedData { get; set; } = new();

    public decimal GetDiscountRate()
    {
        return Level switch
        {
            MemberLevel.Normal => 1.0m,
            MemberLevel.Silver => 0.95m,
            MemberLevel.Gold => 0.90m,
            MemberLevel.Platinum => 0.85m,
            MemberLevel.Diamond => 0.80m,
            _ => 1.0m
        };
    }
}
