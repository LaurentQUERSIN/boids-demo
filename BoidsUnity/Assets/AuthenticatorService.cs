using Stormancer.Authentication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Authentication
{
    public class AuthenticatorService
    {
        #region Config
        private string _authenticatorSceneName = "authenticator";
        public string AuthenticatorSceneName
        {
            get { return _authenticatorSceneName; }
            set { _authenticatorSceneName = value; }
        }

        private string _createUserRoute = "provider.loginpassword.createAccount";
        public string CreateUserRoute
        {
            get { return _createUserRoute; }
            set { _createUserRoute = value; }
        }

        private string _loginRoute = "login";
        public string LoginRoute
        {
            get { return _loginRoute; }
            set { _loginRoute = value; }
        }
        #endregion

        #region fields
        private readonly Client _client;
        private Task<Scene> _authenticatorScene;
        #endregion

        public AuthenticatorService(Client client)
        {
            _client = client;
        }

        /// <summary>
        /// Logs a user in using the Login/Passowrd authentication provider.
        /// </summary>
        /// <param name="login">The user login.</param>
        /// <param name="password">The user password.</param>
        /// <returns>A task returning the scene to be logged in.</returns>
        /// <remarks>The returned scene is not connected yet. In most cases, you will want to add some routes to listen to before connecting to it.</remarks>
        public Task<Scene> Login(string login, string password)
        {
            return Login(new Dictionary<string, string>
            {
                { "login", login },
                {"password", password },
                { "provider", "loginpassword" }
            });
        }

        public Task<Scene> LoginAsViewer()
        {
            UnityEngine.Debug.Log("login as viewer");
            return Login(new Dictionary<string, string>
            {
                { "provider", "viewer" }
            });
        }


        /// <summary>
        /// Logs a user in using the provided authentication context.
        /// </summary>
        /// <param name="authenticationContext">A key/value dictionary with the values used by the authentication providers on the server.</param>
        /// <returns>A task returning the scene to be logged in.</returns>
        /// <remarks>The returned scene is not connected yet. In most cases, you will want to add some routes to listen to before connecting to it.</remarks>
        public Task<Scene> Login(Dictionary<string, string> authenticationContext)
        {
            UnityEngine.Debug.Log("début de log");
            EnsureAuthenticatorSceneExists();

            UnityEngine.Debug.Log("authScene existe");
            return _authenticatorScene.Then(scene =>
            {
                UnityEngine.Debug.Log("authScene connectée");

                return scene.RpcTask<Dictionary<string, string>, LoginResult>(LoginRoute, authenticationContext, Core.PacketPriority.HIGH_PRIORITY)
                    .Then(loginResult =>
                    {
                        UnityEngine.Debug.Log("loginResult reçu");
                        if (!loginResult.Success)
                        {

                            UnityEngine.Debug.Log("échec!");
                            throw new InvalidCredentialException(loginResult.ErrorMsg);
                        }
                        return _client.GetScene(loginResult.Token);
                    });
            }
            )
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    UnityEngine.Debug.LogException(t.Exception);
                }
                return t;
            }).Unwrap();
        }



        private void EnsureAuthenticatorSceneExists()
        {
            _authenticatorScene = _authenticatorScene ?? GetAuthenticatorScene();
        }

        private Task<Scene> GetAuthenticatorScene()
        {
            UnityEngine.Debug.Log("getting authenticator scene.");

            return _client.GetPublicScene(AuthenticatorSceneName, "")
                .Then(scene =>
                {
                    UnityEngine.Debug.Log("scene authenticator récupérée. Connection...");
                    return scene.Connect()
                    .Then(() => scene)
                    .ContinueWith(t => {
                        if (t.IsFaulted)
                        {
                            UnityEngine.Debug.LogException(t.Exception);
                        }
                        return t;
                    }).Unwrap();
                });
        }

        /// <summary>
        /// Logs the connected user out. 
        /// </summary>
        /// <returns></returns>
        public Task Logout()
        {
            var scene = _authenticatorScene.Result;
            _authenticatorScene = null;

            if (scene != null)
            {
                if (scene.Connected)
                {
                    scene.Disconnect();
                }
            }
            return TaskHelper.FromResult(true);
        }
    }
}
