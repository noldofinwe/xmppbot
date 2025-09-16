using System.Diagnostics;
using System.Net.Mail;
using System.Reactive.Linq;
using XmppDotNet;
using XmppDotNet.Extensions.Client.Message;
using XmppDotNet.Extensions.Client.Presence;
using XmppDotNet.Extensions.Client.Roster;
using XmppDotNet.Transport.Socket;
using XmppDotNet.Xmpp;
using XmppDotNet.Xmpp.Base;

namespace XmppBot.Services
{
  public class XmppService(IPrinterService printerService) : IXmppService
  {
    private XmppClient _xmppClient;

    public async Task Connect()
    {
      _xmppClient = new XmppClient(
        conf =>
        {
          conf.UseSocketTransport();
          //conf.UseWebSocketTransport();
          conf.AutoReconnect = true;

          // when your server dow not support SRV records or
          // XEP-0156 Discovering Alternative XMPP Connection Methods
          // then you need to supply host and port for the connection as well.
          // See docs => Host disconvery
        }
    )
      {
        Jid = "test@server",
        Password = ""
      };

      // subscribe to the Binded session state
      _xmppClient
          .StateChanged
          .Where(s => s == SessionState.Binded)
          .Subscribe(async v =>
          {
            // request roster (contact list).
            // This is optional, but most chat clients do this on startup
            var roster = await _xmppClient.RequestRosterAsync();

            // send our online presence to the server
            await _xmppClient.SendPresenceAsync(Show.Chat, "free for chat");

            // send a chat message to user2
            await _xmppClient.SendChatMessageAsync("test@server", "This is a test");
          });

      _xmppClient
          .XmppXElementReceived
          .Where(el => el is XmppDotNet.Xmpp.Base.Message)
          .Subscribe(async el =>
          {
            await ProcessMessage((XmppDotNet.Xmpp.Base.Message)el);
          });
      // connect so the server
      await _xmppClient.ConnectAsync();

    }

    private async Task ProcessMessage(Message el)
    {
      var oob = el.XOob;

      if (oob != null)
      {
        Console.WriteLine($"Received OOB URL: {oob.Url}");
        var bytes = await DownloadFileAsync(oob.Url);
        await Print(bytes); 
      }
      else
      {
        Console.WriteLine($"Received message from {el.From}: {el.Body}");
      }
    }



    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
      using var httpClient = new HttpClient();
      return await httpClient.GetByteArrayAsync(fileUrl);
    }


    private async Task Print(byte[] file)
    {

      bool success = await printerService.SendPrintJobAsync(
          "https://192.168.1.92/ipp/", // IPP URI of the printer
          file
      );

      Console.WriteLine($"Print job success: {success}");

    }
  }
}
