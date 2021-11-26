using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using File = System.IO.File;

namespace TaskToEvent {
    public class DeviceCodeAuthProvider : IAuthenticationProvider {
        private readonly IPublicClientApplication _msalClient;
        private readonly string[] _scopes;
        private IAccount _userAccount;

        public DeviceCodeAuthProvider(string appId, string[] scopes) {
            _scopes = scopes;

            _msalClient = PublicClientApplicationBuilder
                .Create(appId)
                .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true)
                .WithRedirectUri("http://localhost:8383")
                .Build();
            TokenCacheHelper.EnableSerialization(_msalClient.UserTokenCache);
        }

        private async Task<string> GetAccessToken() {
            //First tries to get a token from the cache
            try {
                string previousLogin = await File.ReadAllTextAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                    "\\tasktoevent\\prevUser.txt");

                previousLogin = previousLogin.Split("\r")[0].Split("\n")[0]; //Evil formatting

                var result = await _msalClient
                    .AcquireTokenSilent(_scopes, previousLogin)
                    .ExecuteAsync();

                return result.AccessToken;
            } catch (Exception) {
                
                // If there is no saved user account, the user must sign-in
                try {
                    // Let user sign in
                    var result = await _msalClient.AcquireTokenInteractive(_scopes).ExecuteAsync();
                    _userAccount = result.Account;
                    
                    string[] lines = { _userAccount.Username };
                    File.WriteAllLines(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                        "\\tasktoevent\\prevUser.txt",
                        lines); //Questionable saving of previous user but its just a username and is local so should be fine
                    
                    return result.AccessToken;
                    
                } catch (Exception exception) {
                    Console.WriteLine($"Error getting access token: {exception.Message}");
                    return null;
                }
            }
        }

        // This is the required function to implement IAuthenticationProvider
        // The Graph SDK will call this function each time it makes a Graph
        // call.
        public async Task AuthenticateRequestAsync(HttpRequestMessage requestMessage) {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("bearer", await GetAccessToken());
        }
    }
}