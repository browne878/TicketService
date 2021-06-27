namespace Ticket.Data
{
    using System;
    using System.Data;
    using Npgsql;
    using Ticket.Core;
    using Ticket.Core.Entities;
    using Ticket.Core.Repositories;
    using Ticket.Data.Repositories;

    public class UnitOfWork : IUnitOfWork
    {
        private IDbConnection connection;
        private IDbTransaction transaction;
        private ITicketRepository ticketRepository;
        private bool disposed;
          
        public UnitOfWork(Config _config, int _dbIndex)
        {
            connection = new NpgsqlConnection(_config.MySql[_dbIndex].GetConnectionString());
            connection.Open();
            transaction = connection.BeginTransaction();
        }

        public ITicketRepository TicketRepository
        {
            get
            {
                return ticketRepository ??= new TicketRepository(transaction);
            }
        }

        public void Commit()
        {
            try
            {
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }
            finally
            {
                transaction.Dispose();
                ResetRepositories();
                transaction = connection.BeginTransaction();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Private Methods
        private void ResetRepositories()
        {
            ticketRepository = null;
        }

        private void Dispose(bool _disposing)
        {
            if (disposed) return;
            if (_disposing)
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                    transaction = null;
                }
                if (connection != null)
                {
                    connection.Dispose();
                    connection = null;
                }
            }
            disposed = true;
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
        #endregion
    }
}
