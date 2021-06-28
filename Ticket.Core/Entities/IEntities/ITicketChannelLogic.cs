using System;
using System.Collections.Generic;
using System.Text;

namespace Ticket.Core.Entities.IEntities
{
    using System.Threading.Tasks;
    using DSharpPlus.Entities;

    public interface ITicketChannelLogic
    {
        Task SetTicketId();
        Task<List<DiscordOverwriteBuilder>> SetChannelPermissions();
        Task CreateTicket();
        Task<bool> GetSteamId();
        Task<bool> GetIngameIssue();
        Task<bool> GetCluster();
        Task<bool> GetMap();
        Task<bool> GetCategory();
        Task<bool> HitlistCords();
        Task<bool> HitlistWipePrevious();
        Task TicketCompleted();
        Task<string> GenerateUrl();
        Task CreateTranscript();
        Task<bool> TicketHandler();
        Task<bool> Close(DiscordUser _user);
        Task<bool> Assign(DiscordChannel _channel);
        Task AddMembers(IEnumerable<DiscordUser> _users);
        Task RemoveMembers(IEnumerable<DiscordUser> _users);
    }
}
