namespace Ticket.Data.Repositories
{
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Ticket.Core.Entities;
    using Ticket.Core.Repositories;

    internal class TicketRepository : RepositoryBase, ITicketRepository
    {
        public TicketRepository(IDbTransaction _transaction) : base(_transaction)
        {

        }

        public async Task AddAsync(TicketChannel _entity)
        {
            const string sql = @"INSERT INTO discord_tickets (ID, ChannelID, UserID, CreatedAt, Closed) Values (@ID, @ChannelID, @UserID, @CreatedAt, Closed)";

            await ExecuteAsync(sql, _entity);
        }

        public async Task<IEnumerable<TicketChannel>> GetAllActiveAsync()
        {
            const string sql = "SELECT * FROM discord_tickets WHERE Closed = 0";

            return await QueryAsync<TicketChannel>(sql);
        }

        public async Task<TicketChannel> GetAsync(string _id)
        {
            const string sql = "SELECT * FROM discord_tickets WHERE ID = @Id";
            var param = new { Id = _id };

            return await QueryFirstOrDefaultAsync<TicketChannel>(sql, param);
        }

        public async Task<TicketChannel> FindByIdAsync(int _id)
        {
            const string sql = "SELECT * FROM discord_tickets WHERE ID = @Id";
            var param = new { Id = _id };

            return await QueryFirstOrDefaultAsync<TicketChannel>(sql, param);
        }

        public async Task<TicketChannel> GetTicketIdAsync(int _limit = 1)
        {
            const string sql = "SELECT ID FROM discord_tickets ORDER BY ID DESC LIMIT @Limit";
            var param = new { Limit = _limit };

            return await QueryFirstOrDefaultAsync<TicketChannel>(sql, param);
        }

        public async Task<string> GetSteamID(ulong _discordId)
        {
            const string sql = "SELECT steamid FROM discord_vote_rewards WHERE discordid = @DiscordId";
            var param = new { DiscordId = _discordId };

            return await QueryFirstOrDefaultAsync<string>(sql, param);
        }

        public async Task RemoveAsync(string _key)
        {
            const string sql = "DELETE FROM discord_tickets WHERE id = @Key";
            var param = new { Key = _key };

            await ExecuteAsync(sql, param);
        }

        public async Task UpdateAsync(TicketChannel _entity)
        {
            const string sql = @"UPDATE discord_tickets SET name = @name, description = @Description WHERE id = @Id";

            await ExecuteAsync(sql, _entity);
        }
    }
}
