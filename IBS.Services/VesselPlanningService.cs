using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;

namespace IBS.Services
{
    public class VesselPlanningService(IUnitOfWork unitOfWork) : IVesselPlanningService
    {
        public async Task<VesselPlanningDashboardDto> GetVesselPlanningDashboardAsync(int? portId, CancellationToken cancellationToken = default)
        {
            var ports = await unitOfWork.Port.GetAllAsync(p => !portId.HasValue || p.PortId == portId, cancellationToken: cancellationToken);
            var tugboats = await unitOfWork.Tugboat.GetTugboatsWithOwnersAsync(cancellationToken);

            // Define a relevant window for "Active" or "Upcoming" context (e.g., last 12h to next 24h)
            var start = DateTime.Now.AddHours(-12);
            var end = DateTime.Now.AddHours(24);

            var jobOrders = await unitOfWork.JobOrder.GetJobOrdersWithDetailsAsync(start, end, cancellationToken);
            var dispatchTickets = await unitOfWork.DispatchTicket.GetDispatchTicketsWithDetailsAsync(start, end, cancellationToken);

            var dashboard = new VesselPlanningDashboardDto();

            // 1. Pending Jobs (Jobs that need more tugs assigned)
            var pending = jobOrders.Where(j => j.Status != "Closed" && (j.PreferredTugboatId == null || j.DispatchTickets.Count < j.RequiredTugCount));
            foreach (var jo in pending)
            {
                var assignedIds = new List<int>();
                if (jo.PreferredTugboatId.HasValue)
                {
                    assignedIds.Add(jo.PreferredTugboatId.Value);
                }
                foreach (var dt in jo.DispatchTickets)
                {
                    assignedIds.Add(dt.TugBoatId);
                }

                dashboard.PendingJobs.Add(new UnassignedJobDto
                {
                    JobOrderId = jo.JobOrderId,
                    VesselName = jo.Vessel?.VesselName ?? "Unknown",
                    TerminalName = jo.Terminal?.TerminalName ?? "Unknown",
                    Start = jo.PlannedStartTime ?? jo.Date.ToDateTime(TimeOnly.MinValue),
                    RequiredTugs = jo.RequiredTugCount,
                    AssignedTugs = jo.DispatchTickets.Count + (jo.PreferredTugboatId.HasValue ? 1 : 0),
                    PortId = jo.PortId,
                    PortName = jo.Port?.PortName ?? "Unknown Port",
                    AssignedTugboatIds = assignedIds.Distinct().ToList()
                });
            }

            // 2. Port Inventories
            foreach (var port in ports.OrderBy(p => p.PortName))
            {
                var portDto = new PortFleetDto
                {
                    PortId = port.PortId,
                    PortName = port.PortName
                };

                var portTugs = tugboats.Where(t => t.PortId == port.PortId);

                foreach (var tug in portTugs)
                {
                    var activeTicket = dispatchTickets.FirstOrDefault(dt => dt.TugBoatId == tug.TugboatId && dt.DateLeft != null && dt.DateArrived == null);
                    var upcomingJO = jobOrders.FirstOrDefault(jo => jo.PreferredTugboatId == tug.TugboatId && jo.PlannedStartTime > DateTime.Now);

                    var card = new TugboatCardDto
                    {
                        TugboatId = tug.TugboatId,
                        TugboatName = tug.TugboatName,
                        IsCompanyOwned = tug.IsCompanyOwned,
                        ProviderName = tug.TugboatOwner?.TugboatOwnerName,
                        Status = activeTicket != null ? "Working" : "Idle",
                        CurrentVessel = activeTicket?.Vessel?.VesselName ?? upcomingJO?.Vessel?.VesselName,
                        Until = activeTicket != null ? null : upcomingJO?.PlannedStartTime,
                        PortId = tug.PortId
                    };

                    if (tug.IsCompanyOwned)
                    {
                        portDto.TotalOwned++;
                        if (card.Status == "Working") portDto.ActiveOwned++;
                        portDto.OwnedTugboats.Add(card);
                    }
                    else
                    {
                        // Outsourced tugboats only show up if they are actively working or planned
                        if (card.Status == "Working" || upcomingJO != null)
                        {
                            portDto.OutsourcedInUse++;
                            portDto.OutsourcedTugboats.Add(card);
                        }
                    }
                }

                dashboard.Ports.Add(portDto);
            }

            return dashboard;
        }
    }
}


