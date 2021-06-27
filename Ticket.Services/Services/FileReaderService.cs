namespace Ticket.Services.Services
{
    using System.IO;
    using DSharpPlus;
    using Newtonsoft.Json;
    using Ticket.Core.Entities;

    public static class FileReaderService
    {
        public static Config GetConfig()
        {
            const string file = "./Config/Config.json";
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Config>(data);
        }

        public static DiscordConfiguration GetDiscordConfig()
        {
            const string file = "./Config/DiscordConfiguration.json";
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<DiscordConfiguration>(data);
        }
    }
}
