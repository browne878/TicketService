namespace Ticket.Services.Services.BotService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Extensions;
    using DSharpPlus.SlashCommands;
    using Microsoft.Extensions.Hosting;
    using Ticket.Services.Services.BotService.Commands;
    using Ticket.Services.Services.BotService.Events;
    using Ticket.Services.Services.BotService.LogicModels;

    public class BotService : IHostedService, IDisposable
    {
        private DiscordClient bot;

        public BotService()
        {
            InitialiseBot();
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

        private void InitialiseBot()
        {

            //Slash Commands Setup
            SlashCommandsExtension slash = bot.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = services
            });
            //Set up interactivity
            bot.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(5)
            });

            //register commands
            //commands.RegisterCommands<TicketCommands>();

            //Register Slash Commands
            slash.RegisterCommands<TicketCommands>(764600703624282153);
        }
    }
}
