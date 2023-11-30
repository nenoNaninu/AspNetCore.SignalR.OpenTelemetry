using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;
using TypedSignalR.Client;

namespace Server.Hubs;

public sealed record Message(
    Guid UserId,
    string UserName,
    string Text,
    DateTime DateTime
);

[Receiver]
public interface IChatHubReceiver
{
    Task OnEnter(string userName);
    Task OnMessage(Message message);
}

[Hub]
public interface IChatHub
{
    Task EnterRoom(Guid roomId, string userName);
    Task PostMessage(string text);
    Task<Message[]> GetMessages();
    Task Error();
}

public class ChatHub : Hub<IChatHubReceiver>, IChatHub
{
    private readonly IMessageRepository _messageRepository;

    public ChatHub(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task EnterRoom(Guid roomId, string userName)
    {
        var groupName = roomId.ToString();

        this.ConnectionState = new ChatHubConnectionState(roomId, groupName, Guid.NewGuid(), userName);

        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, groupName, this.Context.ConnectionAborted);

        await this.Clients.Group(groupName).OnEnter(userName);
    }

    public async Task PostMessage(string text)
    {
        ThrowIfStateIsNull();

        var state = this.ConnectionState;

        var message = new Message(state.UserId, state.UserName, text, DateTime.UtcNow);

        await _messageRepository.AddMessageAsync(state.RoomId, message);

        await this.Clients.Group(state.GroupName).OnMessage(message);
    }

    public async Task<Message[]> GetMessages()
    {
        ThrowIfStateIsNull();

        var state = this.ConnectionState;

        var messages = await _messageRepository.GetMessagesAsync(state.RoomId);

        return messages;
    }

    [MemberNotNull(nameof(ConnectionState))]
    private void ThrowIfStateIsNull()
    {
        if (this.ConnectionState is null)
        {
            throw new HubException(":cry:");
        }
    }

    public Task Error()
    {
        //throw new InvalidOperationException("ChatHub error");
        throw new HubException("ChatHub error");
    }

    private ChatHubConnectionState? ConnectionState
    {
        get => this.Context.Items["ConnectionState"] as ChatHubConnectionState;
        set => this.Context.Items["ConnectionState"] = value;
    }

    private record ChatHubConnectionState(Guid RoomId, string GroupName, Guid UserId, string UserName);
}

public interface IMessageRepository
{
    ValueTask AddMessageAsync(Guid roomId, Message message);
    ValueTask<Message[]> GetMessagesAsync(Guid roomId);
}

public class MessageRepository : IMessageRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Message>> _dictionary = new();

    public ValueTask AddMessageAsync(Guid roomId, Message message)
    {
        var bag = _dictionary.GetOrAdd(roomId, static _ => new ConcurrentBag<Message>());
        bag.Add(message);

        return ValueTask.CompletedTask;
    }

    public ValueTask<Message[]> GetMessagesAsync(Guid roomId)
    {
        if (_dictionary.TryGetValue(roomId, out var bag))
        {
            return ValueTask.FromResult(bag.ToArray());
        }

        return ValueTask.FromResult(Array.Empty<Message>());
    }
}
