using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models.MMSI;

namespace IBS.Services
{
    public class VesselPlanningService(IUnitOfWork unitOfWork) : IVesselPlanningService
    {
        private const int TransitBufferMinutes = 30;

        public async Task<VesselPlanningDataDto> GetVesselPlanningDataAsync(int? portId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => !portId.HasValue || t.PortId == portId, cancellationToken: cancellationToken);
            var tugboats = await unitOfWork.Tugboat.GetAllAsync(t => !portId.HasValue || t.PortId == portId, cancellationToken: cancellationToken);
            var jobOrders = await unitOfWork.JobOrder.GetJobOrdersWithDetailsAsync(start, end, cancellationToken);
            var dispatchTickets = await unitOfWork.DispatchTicket.GetDispatchTicketsWithDetailsAsync(start, end, cancellationToken);

            // Filter JOs and DTs by Port if specified
            if (portId.HasValue)
            {
                jobOrders = jobOrders.Where(j => j.PortId == portId.Value).ToList();
                dispatchTickets = dispatchTickets.Where(dt => dt.PortId == portId.Value).ToList();
            }

            var totalFleetSize = tugboats.Count();
            var result = new VesselPlanningDataDto();

            // 1. Build Terminal Timeline
            foreach (var terminal in terminals.OrderBy(t => t.TerminalName))
            {
                var terminalDto = new TerminalTimelineDto
                {
                    TerminalId = terminal.TerminalId,
                    TerminalName = terminal.TerminalName ?? "Unknown Terminal"
                };

                // Planned from JobOrders (not yet dispatched)
                var plannedJobs = jobOrders.Where(j => j.TerminalId == terminal.TerminalId && !j.DispatchTickets.Any());
                foreach (var job in plannedJobs)
                {
                    if (job.PlannedStartTime.HasValue && job.PlannedEndTime.HasValue)
                    {
                        terminalDto.Blocks.Add(new VesselBlockDto
                        {
                            Id = $"JO-{job.JobOrderId}",
                            VesselName = job.Vessel?.VesselName ?? "Unknown Vessel",
                            ServiceType = job.ServiceType,
                            RequiredTugs = job.RequiredTugCount,
                            Start = job.PlannedStartTime.Value,
                            End = job.PlannedEndTime.Value,
                            Status = "Planned",
                            CustomerName = job.Customer?.CustomerName,
                            PortTerminal = $"{job.Port?.PortName} - {job.Terminal?.TerminalName}",
                            Remarks = job.Remarks,
                            LinkUrl = $"/User/JobOrder/Details/{job.JobOrderId}"
                        });
                    }
                }

                // In-Progress and Completed from DispatchTickets
                var tickets = dispatchTickets.Where(dt => dt.TerminalId == terminal.TerminalId);
                foreach (var ticket in tickets)
                {
                    DateTime? startTime = GetTicketStart(ticket);
                    DateTime? endTime = GetTicketEnd(ticket);

                    if (startTime.HasValue)
                    {
                        var status = endTime.HasValue ? "Completed" : "In-Progress";
                        var actualEnd = endTime ?? DateTime.Now;

                        terminalDto.Blocks.Add(new VesselBlockDto
                        {
                            Id = $"DT-{ticket.DispatchTicketId}",
                            VesselName = $"{ticket.Vessel?.VesselName} / {ticket.Service?.ServiceName}",
                            ServiceType = ticket.Service?.ServiceName,
                            RequiredTugs = 1, // Dispatch ticket is usually 1-to-1 with a tugboat in current model
                            Start = startTime.Value,
                            End = actualEnd,
                            Status = status,
                            CustomerName = ticket.Customer?.CustomerName,
                            PortTerminal = $"{ticket.Port?.PortName} - {ticket.Terminal?.TerminalName}",
                            Remarks = ticket.Remarks,
                            LinkUrl = $"/User/DispatchTicket/Preview/{ticket.DispatchTicketId}"
                        });
                    }
                }

                result.Terminals.Add(terminalDto);
            }

            // 2. Capacity Heatmap Calculation
            result.CapacityHeatmap = CalculateCapacityHeatmap(start, end, totalFleetSize, result.Terminals);

            // 3. Flag Capacity Conflicts on Blocks
            FlagConflicts(result);

            return result;
        }

        private DateTime? GetTicketStart(DispatchTicket ticket)
        {
            if (ticket.DateLeft.HasValue && ticket.TimeLeft.HasValue)
            {
                return ticket.DateLeft.Value.ToDateTime(ticket.TimeLeft.Value);
            }

            return null;
        }

        private DateTime? GetTicketEnd(DispatchTicket ticket)
        {
            if (ticket.DateArrived.HasValue && ticket.TimeArrived.HasValue)
            {
                return ticket.DateArrived.Value.ToDateTime(ticket.TimeArrived.Value);
            }

            return null;
        }

        private List<FleetCapacityDto> CalculateCapacityHeatmap(DateTime start, DateTime end, int totalFleetSize, List<TerminalTimelineDto> terminals)
        {
            var heatmap = new List<FleetCapacityDto>();
            var allBlocks = terminals.SelectMany(t => t.Blocks).ToList();

            // Sample every 30 minutes
            for (var time = start; time <= end; time = time.AddMinutes(30))
            {
                var busyTugs = allBlocks
                    .Where(b => time >= b.Start && time < b.End.AddMinutes(TransitBufferMinutes))
                    .Sum(b => b.RequiredTugs);

                heatmap.Add(new FleetCapacityDto
                {
                    Time = time,
                    TotalTugs = totalFleetSize,
                    BusyTugs = busyTugs
                });
            }

            return heatmap;
        }

        private void FlagConflicts(VesselPlanningDataDto data)
        {
            var allBlocks = data.Terminals.SelectMany(t => t.Blocks).ToList();

            foreach (var terminal in data.Terminals)
            {
                foreach (var block in terminal.Blocks)
                {
                    // If at any point during this block's duration (plus buffer) the fleet was over capacity, flag it.
                    block.IsCapacityConflict = data.CapacityHeatmap
                        .Any(h => h.IsOverCapacity && h.Time >= block.Start && h.Time < block.End.AddMinutes(TransitBufferMinutes));

                    if (block.IsCapacityConflict)
                    {
                        // Identify which other vessels are active during this block's window
                        block.ConflictingVessels = allBlocks
                            .Where(other => other.Id != block.Id &&
                                           ((other.Start < block.End.AddMinutes(TransitBufferMinutes) && other.End.AddMinutes(TransitBufferMinutes) > block.Start)))
                            .Select(other => other.VesselName)
                            .Distinct()
                            .ToList();
                    }
                }
            }
        }
    }
}
