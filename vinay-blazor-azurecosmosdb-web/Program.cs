using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using vinay_blazor_azurecosmosdb_services.Interfaces;
using vinay_blazor_azurecosmosdb_web.Components;
using Common = vinay_blazor_azurecosmosdb_core.Common;
using vinay_blazor_azurecosmosdb_services.Implementations ;
using static System.Environment;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddOptions<Common.Configuration>().Bind(builder.Configuration.GetSection(nameof(Common.Configuration)));


string? azureCosmosDbConnectionString = GetEnvironmentVariable("AZURE_COSMOSDB_CONNECTION_STRING");
builder.Services.AddSingleton<CosmosClient>((serviceProvider) =>
{
    IOptions<Common.Configuration> configurationOptions = serviceProvider.GetRequiredService<IOptions<Common.Configuration>>();
    Common.Configuration configuration = configurationOptions.Value;

    CosmosClient client = new(
        connectionString: azureCosmosDbConnectionString
    );
    return client;
});

builder.Services.AddTransient<IOrderService, OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
