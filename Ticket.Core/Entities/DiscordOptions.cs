namespace Ticket.Core.Entities
{
    using Newtonsoft.Json;

    public class DiscordOptions
    {
        [JsonProperty("Token")] public string Token { get; set; }
        [JsonProperty("Prefix")]public string Prefix { get; set; }
    }
}