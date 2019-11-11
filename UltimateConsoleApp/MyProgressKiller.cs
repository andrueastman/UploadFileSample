using System;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace FileUploadTest
{
    /// <summary>
    /// This kills the progrss of of download by throwing a <see cref="TaskCanceledException"/>
    /// </summary>
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
            long percentage = (current * 100) / max;
            Console.WriteLine("Upload in progress. " + current + " bytes of " + max + "bytes. " + percentage + " percent complete");
            throw new TaskCanceledException(); // This simulates something going wrong with upload.
        }
    }
}
