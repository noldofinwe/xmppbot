using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;
using System.Net.Http;

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
      var httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMinutes(2);
      var client = new SharpIppClient(httpClient);
      var printJobresponse = await Print(printerUri, fileBytes, client);


      var jobReponse = await WaitForCompletion(printerUri, client, printJobresponse);
      return jobReponse.JobAttributes.JobState.ToString();
    }

    public async Task<string> GetSupportedFormats(string printerUri)
    {
      var client = new SharpIppClient();

      var printerAttributes = await client.GetPrinterAttributesAsync(new GetPrinterAttributesRequest
      {
        OperationAttributes = new()
        {
          PrinterUri = new Uri(printerUri)
        }
      });

      var supportedFormats = printerAttributes.DocumentFormatSupported;
     
      return string.Join("\n\r", supportedFormats);

    }

    private static async Task<GetJobAttributesResponse> WaitForCompletion(string printerUri, SharpIppClient client, PrintJobResponse printJobresponse)
    {
      GetJobAttributesResponse jobReponse;

      var getJobRequest = new GetJobAttributesRequest
      {
        OperationAttributes = new()
        {
          JobId = printJobresponse.JobId,
          PrinterUri = new Uri(printerUri),
          JobUri = new Uri(printJobresponse.JobUri),
        }
      };

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
      return jobReponse;
    }

    private static async Task<PrintJobResponse> Print(string printerUri, byte[] fileBytes, SharpIppClient client)
    {
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
      return printJobresponse;
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