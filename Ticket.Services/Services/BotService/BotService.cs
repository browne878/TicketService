namespace Ticket.Services.Services.BotService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DSharpPlus;
    using Microsoft.Extensions.Hosting;

    public class BotService : IHostedService, IDisposable
    {
        private DiscordClient bot;

        public BotService(DiscordClient _bot)
        {
            bot = _bot;
        }

        public void Dispose()
        {
            bot.Dispose();
        }

        public async Task StartAsync(CancellationToken _cancellationToken)
        {
            await bot.ConnectAsync();
        }

        public async Task StopAsync(CancellationToken _cancellationToken)
        {
            await bot.DisconnectAsync();
        }
    }
}
