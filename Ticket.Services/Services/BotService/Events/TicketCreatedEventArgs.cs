﻿namespace Ticket.Services.Services.BotService.Events
{
    using DSharpPlus.EventArgs;
    using Ticket.Core.Entities;

    public class TicketCreatedEventArgs : DiscordEventArgs
    {
        public TicketChannel Ticket { get; set; }
    }
}