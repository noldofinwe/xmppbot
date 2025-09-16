namespace XmppBot.Services
{
  public interface IPrinterService
  {
    Task<bool> SendPrintJobAsync(string printerUri, byte[] filePath);
  }
}
