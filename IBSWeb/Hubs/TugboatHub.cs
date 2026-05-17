using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class TugboatHub : Hub
    {
        public async Task NotifyTimelineChanged()
        {
            await Clients.All.SendAsync("TimelineChanged");
        }
    }
}
