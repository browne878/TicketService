namespace Ticket.Core.Repositories
{
    using System.Threading.Tasks;
    using Ticket.Core.Entities;

    public interface ITicketRepository : IBaseRepository<TicketChannel, string>
    {
        Task<TicketChannel> FindByIdAsync(int _ticketId);

        Task<TicketChannel> GetTicketIdAsync(int _limit = 1);

        Task<string> GetSteamID(ulong _discordId);
    }
}
