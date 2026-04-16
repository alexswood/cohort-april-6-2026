using BudgetTracker.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BudgetTracker.Api.Tests.Infrastructure;

public class AzureAiConfigurationTests
{
    [Fact]
    public void Should_default_to_empty_strings()
    {
        var config = new AzureAiConfiguration();

        Assert.Equal(string.Empty, config.Endpoint);
        Assert.Equal(string.Empty, config.ApiKey);
        Assert.Equal(string.Empty, config.DeploymentName);
    }

    [Fact]
    public void Should_bind_from_configuration()
    {
        var values = new Dictionary<string, string?>
        {
            ["AzureAI:Endpoint"] = "https://my-resource.cognitiveservices.azure.com/",
            ["AzureAI:ApiKey"] = "test-api-key",
            ["AzureAI:DeploymentName"] = "gpt-4.1-mini"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var config = new AzureAiConfiguration();
        configuration.GetSection(AzureAiConfiguration.SectionName).Bind(config);

        Assert.Equal("https://my-resource.cognitiveservices.azure.com/", config.Endpoint);
        Assert.Equal("test-api-key", config.ApiKey);
        Assert.Equal("gpt-4.1-mini", config.DeploymentName);
    }

    [Fact]
    public void Should_have_correct_section_name()
    {
        Assert.Equal("AzureAI", AzureAiConfiguration.SectionName);
    }

    [Fact]
    public void Should_throw_when_endpoint_is_empty()
    {
        var services = new ServiceCollection();
        services.AddOptions<AzureAiConfiguration>()
            .Configure(c =>
            {
                c.Endpoint = string.Empty;
                c.ApiKey = "some-key";
                c.DeploymentName = "gpt-4.1-mini";
            });

        services.AddSingleton<IChatClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureAiConfiguration>>().Value;

            if (string.IsNullOrEmpty(config.Endpoint) || string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException(
                    "Azure AI configuration is missing. Please configure Endpoint and ApiKey in user secrets.");
            }

            return Substitute.For<IChatClient>();
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void Should_throw_when_api_key_is_empty()
    {
        var services = new ServiceCollection();
        services.AddOptions<AzureAiConfiguration>()
            .Configure(c =>
            {
                c.Endpoint = "https://my-resource.cognitiveservices.azure.com/";
                c.ApiKey = string.Empty;
                c.DeploymentName = "gpt-4.1-mini";
            });

        services.AddSingleton<IChatClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureAiConfiguration>>().Value;

            if (string.IsNullOrEmpty(config.Endpoint) || string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException(
                    "Azure AI configuration is missing. Please configure Endpoint and ApiKey in user secrets.");
            }

            return Substitute.For<IChatClient>();
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IChatClient>());
    }
}
