using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Authentication
{
    public class AuthenticationPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.ClientCreated += RegisterAuthenticationService;
        }

        private void RegisterAuthenticationService(Client client)
        {
            var authService = new AuthenticatorService(client);

            client.DependencyResolver.RegisterComponent(authService);
        }
    }
}
