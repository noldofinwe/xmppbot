namespace XmppBot.Services
{
  public interface IPrinterService
  {
    Task<string> SendPrintJobAsync(string printerUri, byte[] filePath);
  }
}
