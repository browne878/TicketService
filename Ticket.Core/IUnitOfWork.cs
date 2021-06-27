namespace Ticket.Core
{
    using System;
    using Ticket.Core.Repositories;

    public interface IUnitOfWork : IDisposable
    {
        ITicketRepository TicketRepository { get; }
        void Commit();
    }
}
