using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;

namespace Cortside.RestSharpClient.Tests.Clients {
    public class HttpStatusClient : IDisposable {
        readonly RestApiClient client;

        public HttpStatusClient(ILogger<HttpStatusClient> logger, string hostUrl) {
            var options = new RestApiClientOptions(hostUrl) {
                Serializer = new JsonNetSerializer(),
                Policy = PolicyBuilderExtensions
                            .HandleTransientHttpError()
                            .Or<TimeoutException>()
                            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                            .WaitAndRetryAsync(PolicyBuilderExtensions.Jitter(1, 5))
            };

            client = new RestApiClient(options, logger);
        }

        public async Task<string> Get200Async() {
            var request = new RestApiRequest("200retry", Method.Get);
            request.Timeout = 1000;
            request.Policy = PolicyBuilderExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutException>()
                    .OrResult(x => ((int)x.StatusCode) == 0 || x.StatusCode == System.Net.HttpStatusCode.Unauthorized || x.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    .WaitAndRetryAsync(PolicyBuilderExtensions.Jitter(1, 5));

            var response = await client.GetAsync(request).ConfigureAwait(false);
            return response?.Content;
        }

        public void Dispose() {
            client?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
