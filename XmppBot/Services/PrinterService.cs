using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;
using System.Collections;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;

namespace XmppBot.Services
{
    public class PrinterService : IPrinterService
    {
        private readonly HttpClient _httpClient;

        public PrinterService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> SendPrintJobAsync(string printerUri, byte[] fileBytes)
        {
            var client = new SharpIppClient();
            var stream = new MemoryStream(fileBytes);
            stream.Position = 0;
            var printJobRequest = new PrintJobRequest
            {
                Document = stream,
                OperationAttributes = new()
                {
                    PrinterUri = new Uri(printerUri),
                    DocumentName = "Document Name",
                    DocumentFormat = "application/octet-stream",
                    Compression = Compression.None,
                    DocumentNaturalLanguage = "en",
                    JobName = "Test Job",
                    IppAttributeFidelity = false
                },
                JobTemplateAttributes = new()
                {
                    Copies = 1,
                    Sides = Sides.OneSided,
                    PrintQuality = PrintQuality.Normal
                }
            };
            var printJobresponse = await client.PrintJobAsync(printJobRequest);


            var getJobRequest = new GetJobAttributesRequest
            {
                OperationAttributes = new()
                {
                    JobId = printJobresponse.JobId,
                    PrinterUri = new Uri(printerUri),
                    JobUri = new Uri(printJobresponse.JobUri),
                }
            };
            GetJobAttributesResponse jobReponse;
            JobState jobState;
            do
            {
                var jobResponse = await client.GetJobAttributesAsync(getJobRequest);
                jobReponse = jobResponse;
                jobState = jobResponse.JobAttributes.JobState.Value;
                Console.WriteLine($"Job state: {jobState}");

                await Task.Delay(1000);
            } while (jobState != JobState.Completed &&
                     jobState != JobState.Aborted &&
                     jobState != JobState.Canceled && 
                     jobState != JobState.ProcessingStopped);

            return jobReponse.JobAttributes.JobState.ToString();
        }
    }
}

// var jobId = printJobresponse.JobId;
// var jobUri = printJobresponse.JobUri;
//
//
// var request = new GetJobAttributesRequest();
//
// request.J
// {
//   JobId = jobId,
//   PrinterUri = printerUri
// };
//
// bool isCompleted = false;
// while (!isCompleted)
// {
//   var response = await client.GetJobAttributesAsync(request);
//   var jobState = response.JobAttributes.JobState;
//
//   Console.WriteLine($"Current job state: {jobState}");
//
//   if (jobState == JobState.Completed || jobState == JobState.Canceled || jobState == JobState.Aborted)
//   {
//     isCompleted = true;
//   }
//   else
//   {
//     await Task.Delay(1000); // Wait 1 second before polling again
//   }
//}