using Stormancer.Authentication;
using System;

namespace Stormancer
{
    public static class ClientExtension
    {
        public static AuthenticatorService Authenticator(this Client client)
        {
            UnityEngine.Debug.Log("Getting authenticatorService");
            try
            {
                return client.DependencyResolver.GetComponent<AuthenticatorService>();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw;
            }
            finally
            {
                UnityEngine.Debug.Log("I'm out of there!");
            }
        }
    }
}
