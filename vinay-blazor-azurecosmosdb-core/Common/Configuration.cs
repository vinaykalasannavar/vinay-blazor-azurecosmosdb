using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vinay_blazor_azurecosmosdb_core.Common
{
    public class Configuration
    {
        public required SalesCosmosDb SalesCosmosDb { get; init; }
    }


    public class SalesCosmosDb
    {
        public required string DatabaseName { get; init; }

        public required string ContainerName { get; init; }
    }
}
