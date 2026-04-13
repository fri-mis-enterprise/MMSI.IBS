using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class HubConnectionRepository(ApplicationDbContext dbContext): IHubConnectionRepository
    {
        public async Task SaveConnectionAsync(string username, string connectionId)
        {
            var connection = new HubConnection
            {
                UserName = username,
                ConnectionId = connectionId
            };

            dbContext.HubConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            var connection = await dbContext.HubConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

            if (connection != null)
            {
                dbContext.HubConnections.Remove(connection);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveConnectionsByUsernameAsync(string username)
        {
            await dbContext.HubConnections
                .Where(c => c.UserName == username)
                .ExecuteDeleteAsync(); 

            await dbContext.SaveChangesAsync();
        }
    }
}
