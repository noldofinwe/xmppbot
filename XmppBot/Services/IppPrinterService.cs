using ceTe.DynamicPDF.Rasterizer;
using SharpIpp;
using SharpIpp.Models;
using SharpIpp.Protocol.Models;
using System.Diagnostics;
using System.Drawing;
namespace XmppBot.Services
{
  public class IppPrinterService : IPrinterService
  {
    private readonly HttpClient _httpClient;

    private const int HeaderSize = 1796;

    public IppPrinterService()
    {
      _httpClient = new HttpClient();
    }

    public async Task<string> SendPrintJobAsync(string printerUri, byte[] fileBytes)
    {

      // 1. Download the file
      var tempFile = Path.GetTempFileName();
      await File.WriteAllBytesAsync(tempFile, fileBytes);

      // 2. Print the file using lp
      var process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = "lp",
          Arguments = tempFile,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        }
      };
      process.Start();
      string output = await process.StandardOutput.ReadToEndAsync();
      string error = await process.StandardError.ReadToEndAsync();
      process.WaitForExit();

      return output;

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


    public byte[][] ConvertPdfToImage(byte[] pdfBytes)
    {
      var input = new InputPdf(pdfBytes);


      var rasterizer = new PdfRasterizer(input);

      // Define output image format and size
      var format = ImageFormat.Bmp;
      var size = new DpiImageSize(300, 300); // 300 DPI

      // Rasterize all pages to image byte arrays
      var imageBytes = rasterizer.Draw(format, size);

      return imageBytes;

    }



    public MemoryStream CombineImagesToStream(byte[][] imagePages)
    {
      var combinedStream = new MemoryStream();

      foreach (var pageBytes in imagePages)
      {
        combinedStream.Write(pageBytes, 0, pageBytes.Length);
      }

      combinedStream.Position = 0; // Reset position for reading
      return combinedStream;
    }

    private async Task<PrintJobResponse> Print(string printerUri, byte[] fileBytes, SharpIppClient client)
    {

      var rastereized = ConvertPdfToImage(fileBytes);
      var test = ConvertBmpToPwg(rastereized);
      test.Position = 0;
      //var stream = CombineImagesToStream(rastereized);

      var printJobRequest = new PrintJobRequest
      {
        Document = test,
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

    private MemoryStream ConvertBmpToPwg(byte[][] rastereized)
    {
      var memoryStream = new MemoryStream();
      var writer = new BinaryWriter(memoryStream);

      WritePwgHeader(writer);

      foreach (var bmpRaster in rastereized)
      {
        var bmpStream = new MemoryStream();

        bmpStream.Write(bmpRaster, 0, bmpRaster.Length);

        bmpStream.Position = 0;

        using (var bmp = new Bitmap(bmpStream))
        {
          int width = bmp.Width;
          int height = bmp.Height;
          int dpi = 300; // Assume 300 DPI

          WritePageHeader(writer, width, height, dpi);
          WriteGrayscaleRaster(writer, bmp);
        }
      }

      memoryStream.Position = 0;
      return memoryStream;
    }

    static void WritePwgHeader(BinaryWriter writer)
    {
      byte[] header = new byte[HeaderSize];
      var signature = System.Text.Encoding.ASCII.GetBytes("PWG_RASTER_HEADER");
      Array.Copy(signature, header, signature.Length);
      writer.Write(header);
    }

    static void WritePageHeader(BinaryWriter writer, int width, int height, int dpi)
    {
      byte[] pageHeader = new byte[HeaderSize];
      using (var ms = new MemoryStream(pageHeader))
      using (var bw = new BinaryWriter(ms))
      {
        bw.Write(width);   // Width
        bw.Write((ushort)height); // Height
        bw.Write(dpi);     // DPI
        bw.Write(1);       // Grayscale flag
      }
      writer.Write(pageHeader);
    }

    static void WriteGrayscaleRaster(BinaryWriter writer, Bitmap bmp)
    {
      for (int y = 0; y < bmp.Height; y++)
      {
        for (int x = 0; x < bmp.Width; x++)
        {
          System.Drawing.Color pixel = bmp.GetPixel(x, y);
          byte gray = (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
          writer.Write(gray);
        }
      }
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