using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vinay_blazor_azurecosmosdb_core.Sales
{
    public record Order(
        string id,
        string customer,
        DateTime orderDate,
        decimal totalAmount,
        List<OrderLine> orderLines);
}
