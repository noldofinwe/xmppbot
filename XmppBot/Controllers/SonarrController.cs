using Microsoft.AspNetCore.Mvc;
using XmppBot.Services;

namespace XmppBot.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class SonarrController : ControllerBase
  {
    private IQueueService _queueService;
    public SonarrController(IQueueService queueService)
    {
      _queueService = queueService;
    }

    [HttpPost(Name = "Sonar update")]
    public async  Task<IActionResult> Post([FromBody]string message)
    {
     // await _queueService.Queue(message);
 
      return Ok("Xmpp Started");
    }
  }
}
