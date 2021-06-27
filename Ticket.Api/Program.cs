namespace Ticket.Api
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] _args)
        {
            CreateHostBuilder(_args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] _args) =>
            Host.CreateDefaultBuilder(_args)
                .ConfigureWebHostDefaults(_webBuilder =>
                {
                    _webBuilder.UseStartup<Startup>();
                });
    }
}
