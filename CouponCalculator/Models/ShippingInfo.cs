namespace CouponCalculator.Models;

public class ShippingInfo
{
    public string ShippingMethod { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal BaseFreight { get; set; }
    public decimal Weight { get; set; }
    public decimal FirstWeight { get; set; } = 1.0m;
    public decimal FirstWeightPrice { get; set; }
    public decimal ContinueWeight { get; set; } = 1.0m;
    public decimal ContinueWeightPrice { get; set; }
    public Dictionary<string, object> ExtendedData { get; set; } = new();

    public decimal CalculateFreight()
    {
        if (Weight <= FirstWeight)
        {
            return FirstWeightPrice;
        }

        var continueUnits = Math.Ceiling((Weight - FirstWeight) / ContinueWeight);
        return FirstWeightPrice + (continueUnits * ContinueWeightPrice);
    }
}
