using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models.MMSI;

namespace IBS.Services
{
    public class TugboatMonitoringService(IUnitOfWork unitOfWork) : ITugboatMonitoringService
    {
        private const int TransitBufferMinutes = 30;

        public async Task<List<TugboatTimelineDto>> GetTugboatTimelineDataAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            var tugboats = await unitOfWork.Tugboat.GetAllAsync(cancellationToken: cancellationToken);
            var jobOrders = await unitOfWork.JobOrder.GetJobOrdersWithDetailsAsync(start, end, cancellationToken);
            var dispatchTickets = await unitOfWork.DispatchTicket.GetDispatchTicketsWithDetailsAsync(start, end, cancellationToken);

            var result = new List<TugboatTimelineDto>();

            foreach (var tugboat in tugboats.OrderBy(t => t.TugboatName))
            {
                var tugboatDto = new TugboatTimelineDto
                {
                    TugboatId = tugboat.TugboatId,
                    TugboatName = tugboat.TugboatName,
                    Blocks = new List<TimelineBlockDto>()
                };

                // Add Planned blocks from JobOrders
                var plannedJobs = jobOrders.Where(j => j.PreferredTugboatId == tugboat.TugboatId && !j.DispatchTickets.Any());
                foreach (var job in plannedJobs)
                {
                    if (job.PlannedStartTime.HasValue && job.PlannedEndTime.HasValue)
                    {
                        tugboatDto.Blocks.Add(new TimelineBlockDto
                        {
                            Id = $"JO-{job.JobOrderId}",
                            Title = job.Vessel?.VesselName ?? "Unknown Vessel",
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

                // Add In-Progress and Completed blocks from DispatchTickets
                var tickets = dispatchTickets.Where(dt => dt.TugBoatId == tugboat.TugboatId);
                foreach (var ticket in tickets)
                {
                    DateTime? startTime = null;
                    if (ticket.DateLeft.HasValue && ticket.TimeLeft.HasValue)
                    {
                        startTime = ticket.DateLeft.Value.ToDateTime(ticket.TimeLeft.Value);
                    }

                    DateTime? endTime = null;
                    if (ticket.DateArrived.HasValue && ticket.TimeArrived.HasValue)
                    {
                        endTime = ticket.DateArrived.Value.ToDateTime(ticket.TimeArrived.Value);
                    }

                    if (startTime.HasValue)
                    {
                        var status = endTime.HasValue ? "Completed" : "In-Progress";
                        var actualEnd = endTime ?? DateTime.Now; // Use current time if still in progress

                        tugboatDto.Blocks.Add(new TimelineBlockDto
                        {
                            Id = $"DT-{ticket.DispatchTicketId}",
                            Title = $"{ticket.Vessel?.VesselName} / {ticket.Service?.ServiceName}",
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

                // Conflict Detection
                DetectConflicts(tugboatDto.Blocks);

                result.Add(tugboatDto);
            }

            return result;
        }

        private void DetectConflicts(List<TimelineBlockDto> blocks)
        {
            var sortedBlocks = blocks.OrderBy(b => b.Start).ToList();
            for (int i = 0; i < sortedBlocks.Count; i++)
            {
                for (int j = i + 1; j < sortedBlocks.Count; j++)
                {
                    var b1 = sortedBlocks[i];
                    var b2 = sortedBlocks[j];

                    // Buffer-aware overlap
                    // Overlap = (RequestedStart < ExistingEnd + Buffer) && (RequestedEnd > ExistingStart - Buffer)
                    if (b2.Start < b1.End.AddMinutes(TransitBufferMinutes) && b2.End > b1.Start.AddMinutes(-TransitBufferMinutes))
                    {
                        b1.IsConflict = true;
                        b2.IsConflict = true;
                    }
                }
            }
        }
    }
}
