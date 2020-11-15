extern alias GraphBeta;

using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

using Beta = GraphBeta.Microsoft.Graph;

namespace Schlauchboot.Teams.Monitoring.Scripts
{
    class GraphServiceClientManager
    {
        private readonly IConfiguration _config;

        public GraphServiceClientManager(IConfiguration config)
        {
            _config = config;
        }

        private List<string> GetGraphServiceClientCredentials()
        {
            var graphServiceClientCredentialCollection = new List<string>();
            var clientId = _config.GetSection("GraphApplicationCredentials")
                .GetChildren().Where(x => x.Key == "ClientId").First().Value;
            graphServiceClientCredentialCollection.Add(clientId);
            var tenantId = _config.GetSection("GraphApplicationCredentials")
                .GetChildren().Where(x => x.Key == "TenantId").First().Value;
            graphServiceClientCredentialCollection.Add(tenantId);
            var clientSecret = _config.GetSection("GraphApplicationCredentials")
                .GetChildren().Where(x => x.Key == "ClientSecret").First().Value;
            graphServiceClientCredentialCollection.Add(clientSecret);
            return graphServiceClientCredentialCollection;
        }

        public GraphServiceClient GenerateGraphServiceClient()
        {
            var graphServiceClientCredentialCollection = GetGraphServiceClientCredentials();
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(graphServiceClientCredentialCollection[0])
                .WithTenantId(graphServiceClientCredentialCollection[1])
                .WithClientSecret(graphServiceClientCredentialCollection[2])
                .Build();
            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);
            return new GraphServiceClient(authenticationProvider);
        }

        public Beta.GraphServiceClient GenerateBetaGraphServiceClient()
        {
            var graphServiceClientCredentialCollection = GetGraphServiceClientCredentials();
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(graphServiceClientCredentialCollection[0])
                .WithTenantId(graphServiceClientCredentialCollection[1])
                .WithClientSecret(graphServiceClientCredentialCollection[2])
                .Build();
            ClientCredentialProvider authenticationProvider = new ClientCredentialProvider(confidentialClientApplication);
            return new Beta.GraphServiceClient(authenticationProvider);
        }
    }
}
