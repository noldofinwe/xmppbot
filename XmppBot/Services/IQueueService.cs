namespace XmppBot.Services;

public interface IQueueService
{
    Task Queue(string message);
}