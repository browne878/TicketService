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
            const string sql = @"INSERT INTO example_table (Name, Description) Values (@Name, @Description)";

            await ExecuteAsync(sql, _entity);
        }

        public async Task<IEnumerable<TicketChannel>> GetAllActiveAsync()
        {
            const string sql = "SELECT * FROM example_table";

            return await QueryAsync<TicketChannel>(sql);
        }

        public async Task<TicketChannel> GetAsync(string _id)
        {
            const string sql = "SELECT * FROM example_table WHERE ID = @Id";
            var param = new { Id = _id };

            return await QueryFirstOrDefaultAsync<TicketChannel>(sql, param);
        }

        public async Task<TicketChannel> FindByIdAsync(int _id)
        {
            const string sql = "SELECT * FROM example_table WHERE ID = @Id";
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
            const string sql = "DELETE FROM example_table WHERE id = @Key";
            var param = new { Key = _key };

            await ExecuteAsync(sql, param);
        }

        public async Task UpdateAsync(TicketChannel _entity)
        {
            const string sql = @"UPDATE example_table SET name = @name, description = @Description WHERE id = @Id";

            await ExecuteAsync(sql, _entity);
        }
    }
}
