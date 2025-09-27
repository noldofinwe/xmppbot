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
  public class XmppService : BackgroundService, IXmppService
  {
    private readonly IPrinterService _printerService;
    private readonly string _ownerJid;
    private readonly string _botJid;
    private readonly string _botPassword;
    private readonly string _printerUrl;
    private readonly HashSet<string> _whitelist;
    
    private XmppClient _xmppClient;

    public XmppService(IPrinterService printerService)
    {
      _printerService = printerService;
      _ownerJid = Environment.GetEnvironmentVariable("OwnerJid");
      _botJid = Environment.GetEnvironmentVariable("BotJid");
      _botPassword = Environment.GetEnvironmentVariable("BotPassword");
      _printerUrl = Environment.GetEnvironmentVariable("PrinterUrl");
      var whitelist = Environment.GetEnvironmentVariable("Whitelist");
      _whitelist = whitelist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase); 

    }
    public async Task Connect()
    {
      CreateXmppClient();

      // subscribe to the Binded session state1
      SubscribeToBindingSession();

      SubscribeToMessages();
      // connect so the server
      await _xmppClient.ConnectAsync();

    }

    private void SubscribeToMessages()
    {
      _xmppClient
          .XmppXElementReceived
          .Where(el => el is XmppDotNet.Xmpp.Base.Message)
          .Subscribe(async el =>
          {
            await ProcessMessage((XmppDotNet.Xmpp.Base.Message)el);
          });
    }

    private void SubscribeToBindingSession()
    {
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
            await _xmppClient.SendChatMessageAsync(_ownerJid, "Printer service ready");
            //var result = await _printerService.GetSupportedFormats(_printerUrl);
            //await _xmppClient.SendChatMessageAsync(_ownerJid, "Supported formats");
            //await _xmppClient.SendChatMessageAsync(_ownerJid, result);

          });
    }

    private void CreateXmppClient()
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
    }

    private async Task ProcessMessage(Message el)
    {
      var oob = el.XOob;

      if (oob != null)
      {
        if (_whitelist.Contains(el.From.Domain))
        {
          await PrintFileInMessage(el, oob);
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

    private async Task PrintFileInMessage(Message el, XmppDotNet.Xmpp.Oob.XOob oob)
    {
      await _xmppClient.SendChatMessageAsync(el.From, "Printing file");
      var bytes = await DownloadFileAsync(oob.Url);
      var result = await _printerService.SendPrintJobAsync(_printerUrl, bytes);
      await _xmppClient.SendChatMessageAsync(el.From, result);
    }

    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
      using var httpClient = new HttpClient();
      return await httpClient.GetByteArrayAsync(fileUrl);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
      await _xmppClient.SendChatMessageAsync(_ownerJid, "Stopping Service");
      await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      await Connect();

    }

    public async Task SendMessageToOwner(string message)
    {
      await _xmppClient.SendChatMessageAsync(_ownerJid, message);
    }
  }
}
