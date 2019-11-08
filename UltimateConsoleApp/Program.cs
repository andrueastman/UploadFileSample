using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
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
            var driveItems = await graphClient.Drive.Root.Children.Request().GetAsync();
            string id = "";
            foreach (var item in driveItems)
            {
                Console.WriteLine(item.Id);
                if (item.File == null)
                {
                    Console.WriteLine("Item is a file");
                    id = item.Id;
                    break;
                }
            }

            await UploadLargeFileWithCallBacks(graphClient,id);

        }

        public static async Task UploadLargeFileInChunks(GraphServiceClient graphClient, string itemId)
        {
            try
            {
                using (Stream stream = getFileStream())
                {
                    // Get the provider. 
                    // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/_hamiltion.png:/microsoft.graph.createUploadSession
                    // The CreateUploadSesssion action doesn't seem to support the options stated in the metadata.
                    var uploadSession = await graphClient.Drive.Items[itemId].ItemWithPath("_hamilton.png").CreateUploadSession().Request().PostAsync();

                    var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                    var largeFileUpload = new LargeFileUpload(uploadSession, graphClient, stream, maxChunkSize);

                    // Setup the chunk request necessities
                    var slicesRequests = largeFileUpload.GetUploadSlicesRequests();
                    var trackedExceptions = new List<Exception>();
                    DriveItem itemResult = null;

                    //upload the chunks
                    foreach (var request in slicesRequests)
                    {
                        // Do your updates here: update progress bar, etc.
                        // ...
                        // Send chunk request
                        var result = await largeFileUpload.UploadSliceAsync(request, trackedExceptions);

                        if (result.UploadSucceeded)
                        {
                            itemResult = result.ItemResponse;
                        }
                    }

                    // Check that upload succeeded
                    if (itemResult == null)
                    {
                        // Retry the upload
                        // ...
                    }
                }
            }
            catch (Microsoft.Graph.ServiceException e)
            {
            }
        }

        private static Stream getFileStream()
        {
            string startupPath = Environment.CurrentDirectory;
            FileStream fileStream = new FileStream(startupPath+ "\\SWEBOKv3.pdf",FileMode.Open);
            return fileStream;
        }

        public static async Task UploadLargeFileWithCallBacks(GraphServiceClient graphClient, String itemId)
        {
            try
            {
                using (Stream stream = getFileStream())
                {
                    // Get the provider. 
                    // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/_hamiltion.png:/microsoft.graph.createUploadSession
                    // The CreateUploadSesssion action doesn't seem to support the options stated in the metadata.
                    var uploadSession = await graphClient.Drive.Items[itemId].ItemWithPath("_hamilton.png").CreateUploadSession().Request().PostAsync();
                    Console.WriteLine("Upload Session Created");

                    var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                    var largeFileUpload = new LargeFileUpload(uploadSession, graphClient, stream, maxChunkSize);

                    // Setup the chunk request necessities
                    DriveItem uploadedFile = null;
                    try
                    {
                        uploadedFile = await largeFileUpload.UploadAsync(new MyProgressKiller());
                    }
                    catch (TaskCanceledException)
                    {
                        //try to refresh the upload info and resume the upload from where we left off.
                        Console.WriteLine("Resuming Download");
                        uploadedFile = await largeFileUpload.ResumeAsync(new MyProgress());
                    }

                    //Sucessful Upload
                    Console.WriteLine(uploadedFile.Id);
                }
            }
            catch (Microsoft.Graph.ServiceException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public class MyProgress : IProgressCallback
        {
            public void OnFailure(ClientException clientException)
            {
                Console.WriteLine(clientException.Message);
            }

            public void OnSuccess(DriveItem result)
            {
                Console.WriteLine("Download completed with id below");
                Console.WriteLine(result.Id);
            }

            public void UpdateProgress(long current, long max)
            {
                Console.WriteLine("Upload in progress. "+ current+ " bytes of "+ max );
            }
        }
    }

    public class MyProgressKiller : IProgressCallback
    {
        public void OnFailure(ClientException clientException)
        {
            Console.WriteLine(clientException.Message);
        }

        public void OnSuccess(DriveItem result)
        {
            Console.WriteLine("Download completed with id below");
            Console.WriteLine(result.Id);
        }

        public void UpdateProgress(long current, long max)
        {
            Console.WriteLine("Upload in progress. " + current + " bytes of " + max);
            throw new TaskCanceledException();
        }
    }
}
