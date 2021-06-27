namespace Ticket.Services.Services.BotService.Events
{
    using System;
    using Ticket.Core.Entities;

    public class TicketClosedEventArgs : EventArgs
    {
        public TicketChannel Ticket { get; set; }
    }
}