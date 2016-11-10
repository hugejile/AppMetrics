using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using App.Metrics;
using AspNet.Metrics.Integration.Facts.Startup;
using FluentAssertions;
using Xunit;

namespace AspNet.Metrics.Integration.Facts.Middleware
{
    public class MetricsEndpointDisabledMiddlewareTests : IClassFixture<MetricsHostTestFixture<DisabledMetricsEndpointStartup>>
    {
        public MetricsEndpointDisabledMiddlewareTests(MetricsHostTestFixture<DisabledMetricsEndpointStartup> fixture)
        {
            Client = fixture.Client;
            Context = fixture.Context;
        }

        public HttpClient Client { get; }

        public IMetrics Context { get; }

        [Fact]
        public async Task can_disable_metrics_endpoint_when_metrics_enabled()
        {
            var result = await Client.GetAsync("/metrics");

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}