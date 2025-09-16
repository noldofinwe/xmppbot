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

    public async Task<bool> SendPrintJobAsync(string printerUri, byte[] fileBytes)
    {

      var client = new SharpIppClient();

      var printJobRequest = new PrintJobRequest
      {
        Document = new MemoryStream(fileBytes),
        OperationAttributes = new()
        {
          PrinterUri = new Uri("ipp://192.168.1.92:631/"),
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
          MultipleDocumentHandling = MultipleDocumentHandling.SeparateDocumentsCollatedCopies,
          Finishings = Finishings.None,
          PageRanges = [new SharpIpp.Protocol.Models.Range(1, 1)],
          Sides = Sides.OneSided,
          NumberUp = 1,
          OrientationRequested = Orientation.Portrait,
          PrinterResolution = new Resolution(600, 600, ResolutionUnit.DotsPerInch),
          PrintQuality = PrintQuality.Normal
        }
      };
      var printJobresponse = await client.PrintJobAsync(printJobRequest);
      return true;
    }
  }
}
