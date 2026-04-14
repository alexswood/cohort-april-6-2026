using BudgetTracker.Api.Features.Transactions.List;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class TransactionListApiTests
{
    [Fact]
    public void Should_register_transaction_list_endpoint()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();

        var serviceProvider = services.BuildServiceProvider();
        var routes = new FakeEndpointRouteBuilder(serviceProvider);

        var result = TransactionListApi.MapTransactionListEndpoint(routes);

        Assert.Same(routes, result);
        Assert.NotEmpty(routes.DataSources);
    }

    private class FakeEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public FakeEndpointRouteBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            DataSources = new List<EndpointDataSource>();
        }

        public IServiceProvider ServiceProvider { get; }

        public ICollection<EndpointDataSource> DataSources { get; }

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }
}
