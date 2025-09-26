using System.Threading.Channels;

namespace XmppBot.Services;

public class QueueService :IQueueService
{    
    public Channel<string> WorkChannel { get; } = Channel.CreateUnbounded<string>();

    public async Task Queue(string message)
    {
        await WorkChannel.Writer.WriteAsync(message);
    }
}