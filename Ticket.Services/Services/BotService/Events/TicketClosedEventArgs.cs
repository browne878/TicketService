namespace Ticket.Services.Services.BotService.Events
{
    using DSharpPlus.EventArgs;
    using Ticket.Core.Entities;

    public class TicketClosedEventArgs : DiscordEventArgs
    {
        public TicketChannel Ticket { get; set; }
    }
}