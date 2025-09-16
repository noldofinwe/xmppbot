using System.Diagnostics;
using System.Net.Http.Headers;

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
      var request = new HttpRequestMessage(HttpMethod.Post, printerUri);
      request.Content = new ByteArrayContent(fileBytes);
      request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); // or application/postscript

      // IPP requires specific headers; some printers may need more
      request.Headers.Add("Expect", "100-continue");

      var response = await _httpClient.SendAsync(request);
      return response.IsSuccessStatusCode;
    }
  }
}
