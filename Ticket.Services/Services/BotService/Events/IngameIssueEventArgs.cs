namespace Ticket.Services.Services.BotService.Events
{
    using DSharpPlus.EventArgs;
    using Ticket.Core.Entities;

    public class IngameIssueEventArgs : DiscordEventArgs
    {
        public TicketChannel TicketChannel { get; set; }
    }
}