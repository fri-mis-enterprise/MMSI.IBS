using IBS.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class NotificationHub(IHubConnectionRepository hubConnectionRepository): Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("OnConnected");
            await base.OnConnectedAsync();
        }

        public async Task SaveUserConnection(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                await hubConnectionRepository.SaveConnectionAsync(username, Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await hubConnectionRepository.RemoveConnectionAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
