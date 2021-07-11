namespace Ticket.Api
{
    using DSharpPlus;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Ticket.Core;
    using Ticket.Data;
    using Ticket.Services.Services;
    using Ticket.Services.Services.BotService;
    using Ticket.Services.Services.BotService.LogicModels;

    public class Startup
    {
        public Startup(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection _services)
        {

            _services.AddHostedService<BotService>();
            _services.AddHostedService<EventService>();

            _services.AddControllers();

            _services.AddSingleton(FileReaderService.GetConfig());
            _services.AddSingleton(FileReaderService.GetDiscordConfig());
            _services.AddSingleton<DiscordClient>();

            _services.AddTransient<TicketController>();

            _services.AddScoped<IUnitOfWork, UnitOfWork>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder _app, IWebHostEnvironment _env)
        {
            if (_env.IsDevelopment())
            {
                _app.UseDeveloperExceptionPage();
            }

            _app.UseRouting();

            _app.UseAuthorization();

            _app.UseEndpoints(_endpoints =>
            {
                _endpoints.MapControllers();
            });
        }
    }
}
