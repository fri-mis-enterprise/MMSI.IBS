using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Hubs
{
    public class PlanningHub : Hub
    {
        public async Task UpdatePlan(int portId)
        {
            await Clients.Group(portId.ToString()).SendAsync("OnPlanUpdated", portId);
        }

        public async Task JoinPortGroup(int portId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, portId.ToString());
        }

        public async Task LeavePortGroup(int portId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, portId.ToString());
        }
    }
}
