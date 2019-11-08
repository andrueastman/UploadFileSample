using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace UltimateConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            /* Do the auth stuff first */
            IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder
                .Create("d662ac70-7482-45af-9dc3-c3cde8eeede4")
                .WithRedirectUri("http://localhost:1234")
                .Build();

            var scopes = new[] { "User.Read", "Sites.ReadWrite.All" };
            var authResult = await publicClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync();

            /* Create a DelegateAuthenticationProvider to use */
            var delegatingAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);
                return Task.FromResult(0);
            });

            GraphServiceClient graphClient = new GraphServiceClient(delegatingAuthProvider);
            Stream content = await graphClient.Sites.Root.Drive.Items["01WICLWWAFK4PWP2STYNCZOBWQE7PSNSQU"].Content.Request().GetAsync();

            Console.WriteLine(content.Length);
        }
    }
}
