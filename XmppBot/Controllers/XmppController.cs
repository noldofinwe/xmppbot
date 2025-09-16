using Microsoft.AspNetCore.Mvc;
using XmppBot.Services;

namespace XmppBot.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class XmppController : ControllerBase
  {
    private IXmppService _xmppService;
    public XmppController(IXmppService xmppService)
    {
      _xmppService = xmppService;
    }

    [HttpGet(Name = "Connect to XMPP Server")]
    public async  Task<IActionResult> Get()
    {
      await _xmppService.Connect();
      return Ok("Xmpp Started");
    }
  }
}
