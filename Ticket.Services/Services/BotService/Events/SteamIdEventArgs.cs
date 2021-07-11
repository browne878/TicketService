namespace Ticket.Services.Services.BotService.Events
{
    using DSharpPlus.EventArgs;
    using Ticket.Core.Entities;

    public class SteamIdEventArgs : DiscordEventArgs
    {
        public TicketChannel TicketChannel { get; set; }
        public bool Success { get; set; }
    }
}