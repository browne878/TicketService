namespace Ticket.Api
{
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.SlashCommands;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Ticket.Services.Services.BotService.Commands;

    public static class Program
    {
        public static async Task Main(string[] _args)
        {
            IHost host = CreateHostBuilder(_args).Build();

            DiscordClient bot = host.Services.GetRequiredService<DiscordClient>();

            SlashCommandsExtension slash = bot.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = host.Services
            });

            slash.RegisterCommands<TicketCommands>();

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] _args) =>
            Host.CreateDefaultBuilder(_args)
                .ConfigureWebHostDefaults(_webBuilder =>
                {
                    _webBuilder.UseStartup<Startup>();
                });
    }
}
