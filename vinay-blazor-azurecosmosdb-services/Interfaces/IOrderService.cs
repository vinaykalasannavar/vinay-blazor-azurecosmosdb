using vinay_blazor_azurecosmosdb_core.Sales;

namespace vinay_blazor_azurecosmosdb_services.Interfaces;

public interface IOrderService
{
    Task<List<Order>> FetchAllOrders();
    Task<bool> DeleteOrder(Order? order);
    Task GenerateOrders(Func<string, Task> writeOutputAync, string customerName, int noOfOrders);
    Task<bool> UpdateOrder(Order order);
}