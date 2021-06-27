namespace Ticket.Services.Services.BotService.Events
{
    using System;
    using Ticket.Core.Entities;

    public class TicketTimeoutEventArgs : EventArgs
    {
        public TicketChannel TicketChannel { get; set; }
    }
}