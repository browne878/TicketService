namespace Ticket.Core.Entities
{
    using System.Collections.Generic;

    public class Config
    {
        public TicketConfig TicketConfig { get; set; }
        public List<MySql> MySql { get; set; }
    }
}