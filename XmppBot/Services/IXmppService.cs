namespace XmppBot.Services
{
  public interface IXmppService
  {
    Task Connect();
    Task SendMessageToOwner(string message);
  }
}
