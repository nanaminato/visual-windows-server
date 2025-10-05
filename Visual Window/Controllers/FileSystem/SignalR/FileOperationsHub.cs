using Microsoft.AspNetCore.SignalR;

namespace Visual_Window.Controllers.FileSystem.SignalR;

public class FileOperationsHub : Hub
{
    public async Task SubscribeToOperation(string operationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, operationId);
    }

    public async Task UnsubscribeFromOperation(string operationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
    }
}