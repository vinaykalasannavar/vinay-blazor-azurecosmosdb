namespace vinay_blazor_azurecosmosdb_core.Sales;

public record OrderLine(
    string id,
    string productId,
    int quantity,
    decimal price);