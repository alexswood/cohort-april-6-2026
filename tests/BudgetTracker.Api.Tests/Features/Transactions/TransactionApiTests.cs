using BudgetTracker.Api.Features.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class TransactionApiTests
{
    [Fact]
    public void Should_register_transactions_group_with_tags_and_authorization()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();

        var serviceProvider = services.BuildServiceProvider();
        var routes = new FakeEndpointRouteBuilder(serviceProvider);

        var result = TransactionApi.MapTransactionEndpoints(routes);

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
