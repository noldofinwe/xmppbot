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
  public class XmppService : BackgroundService
  {
    private readonly IPrinterService _printerService;
    private readonly string _ownerJid;
    private readonly string _botJid;
    private readonly string _botPassword;
    private readonly string _printerUrl;
    
    private XmppClient _xmppClient;

    public XmppService(IPrinterService printerService)
    {
      _printerService = printerService;
      _ownerJid = Environment.GetEnvironmentVariable("OwnerJid");
      _botJid = Environment.GetEnvironmentVariable("BotJid");
      _botPassword = Environment.GetEnvironmentVariable("BotPassword");
      _printerUrl = Environment.GetEnvironmentVariable("PrinterUrl");
    }
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
        Jid = _botJid,
        Password = _botPassword
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
            await _xmppClient.SendChatMessageAsync(_ownerJid, "Printing service started");
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
        if (el.From.Domain == _xmppClient.Jid.Domain)
        {
          await _xmppClient.SendChatMessageAsync(el.From, "Printing PDF file");
          var bytes = await DownloadFileAsync(oob.Url);
          var result = await Print(bytes);
          await _xmppClient.SendChatMessageAsync(el.From, result);
        }
        else
        {
          await _xmppClient.SendChatMessageAsync(el.From, "Cannot print from different domain");

        }
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


    private async Task<string> Print(byte[] file)
    {

      var result = await _printerService.SendPrintJobAsync(
          _printerUrl, // IPP URI of the printer
          file
      );

      return result;

    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      await _xmppClient.SendChatMessageAsync("michel@bobbin.synology.me", "Stopping Service");

      await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      await Connect();
    }
  }
}
