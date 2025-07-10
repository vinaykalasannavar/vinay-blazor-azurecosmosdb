using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common = vinay_blazor_azurecosmosdb_core.Common;
using vinay_blazor_azurecosmosdb_core.Sales;
using vinay_blazor_azurecosmosdb_services.Interfaces;

namespace vinay_blazor_azurecosmosdb_services.Implementations
{
    public class OrderService(CosmosClient client, IOptions<Common.Configuration> configurationOptions) : IOrderService
    {
        private readonly Common.Configuration _configuration = configurationOptions.Value;

        private async Task<Container> GetSalesContainer()
        {
            Database database = client.GetDatabase(_configuration.SalesCosmosDb.DatabaseName);
            database = await database.ReadAsync();

            Container container = database.GetContainer(_configuration.SalesCosmosDb.ContainerName);
            container = await container.ReadContainerAsync();
            return container;
        }

        public async Task<List<Order>> FetchAllOrders()
        {
            Container container = await GetSalesContainer();
            //Console.WriteLine($"Get container:\t{container.Id}");
            var query = new QueryDefinition(query: "SELECT * FROM orders p");
            using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>(queryDefinition: query);
            //Console.WriteLine($"Ran query:\t{query.QueryText}");

            List<Order> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<Order> response = await feed.ReadNextAsync();
                foreach (Order item in response)
                {
                    items.Add(item);
                }
                requestCharge += response.RequestCharge;
            }

            return items;
        }

        public async Task<bool> DeleteOrder(string id, string customerName)
        {
            var deleteStatus = false;
            Container container = await GetSalesContainer();
            //customerName = customerName.Replace("\'", "\\\'");

            var query = new QueryDefinition(query: "SELECT * FROM orders p WHERE p.id = @id and p.customer = @customerName") // 
                .WithParameter("@id", id)
                .WithParameter("@customerName", customerName)
                ;

            using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>(queryDefinition: query);

            if (feed.HasMoreResults)
            {
                FeedResponse<Order> response = await feed.ReadNextAsync();

                foreach (Order item in response)
                {
                    ItemResponse<Order>? resultOfDelete = await container.DeleteItemAsync<Order>(item.id, new PartitionKey(item.customer));
                    deleteStatus = true;
                }
            }

            return deleteStatus;
        }

        public async Task GenerateOrders(Func<string, Task> writeOutputAync, string customerName, int noOfOrders)
        {

            Database database = client.GetDatabase(_configuration.SalesCosmosDb.DatabaseName);

            database = await database.ReadAsync();
            await writeOutputAync($"Get database:\t{database.Id}");

            Container container = database.GetContainer(_configuration.SalesCosmosDb.ContainerName);

            container = await container.ReadContainerAsync();
            await writeOutputAync($"Get container:\t{container.Id}");

            string customer = $"{customerName}";

            for (int index = 0; index < noOfOrders; index++)
                await CreateOrder(writeOutputAync, customer, container, orderId: index + 1000);

            var query = new QueryDefinition(query: "SELECT * FROM orders p WHERE p.customer = @customer")
                .WithParameter("@customer", customer);

            using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>(queryDefinition: query);

            await writeOutputAync($"Ran query:\t{query.QueryText}");

            List<Order> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<Order> response = await feed.ReadNextAsync();
                foreach (Order item in response)
                {
                    items.Add(item);
                }
                requestCharge += response.RequestCharge;
            }

            foreach (var item in items)
            {
                await writeOutputAync($"Found order:\t{item.customer}\t[{item.id}], orderDate:\t{item.orderDate}, totalAmount:\t{item.totalAmount}");
            }
            await writeOutputAync($"Request charge:\t{requestCharge:0.00}");

        }

        private static async Task CreateOrder(Func<string, Task> writeOutputAync, string customer, Container container, int orderId)
        {
            int noOfProducts = new Random().Next(1, 15);

            var orderLines = GenerateOrderLines(noOfProducts);

            var totalAmount = orderLines.Sum(o => o.price * o.quantity);

            Order item = new(
                id: $"{orderId}",
                customer: customer,
                orderDate: DateTime.UtcNow,
                totalAmount: totalAmount,
                orderLines: orderLines
            );
            ItemResponse<Order> response = await container.UpsertItemAsync<Order>(
                item: item,
                partitionKey: new PartitionKey(customer)
            );
            await writeOutputAync($"Upserted item:\t{response.Resource}");
            await writeOutputAync($"Status code:\t{response.StatusCode}");
            await writeOutputAync($"Request charge:\t{response.RequestCharge:0.00}");
        }

        private static List<OrderLine> GenerateOrderLines(int noOfProducts)
        {
            var list = new List<OrderLine>();

            for (int index = 0; index < noOfProducts; index++)
            {
                decimal price = new Random().Next(50, 200);
                var quantity = new Random().Next(1, 10);

                list.Add(new($"{index + 1}", $"product-{index}", quantity, price));
            }
            return list;
        }

        public async Task<bool> UpdateOrder(Order order)
        {
            var deleteStatus = false;
            Container container = await GetSalesContainer();

            var query = new QueryDefinition(query: "SELECT * FROM orders p WHERE p.id = @id")
                .WithParameter("@id", order.id);

            using FeedIterator<Order> feed = container.GetItemQueryIterator<Order>(queryDefinition: query);

            if (feed.HasMoreResults)
            {
                FeedResponse<Order> response = await feed.ReadNextAsync();

                foreach (Order item in response)
                {
                    ItemResponse<Order>? resultOfUpdate = await container.UpsertItemAsync(order, new PartitionKey(item.customer));
                    var status = resultOfUpdate.StatusCode == System.Net.HttpStatusCode.OK;
                    if (status)
                        deleteStatus = true;
                }
            }

            return deleteStatus;
        }
    }
}
