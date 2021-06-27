namespace Ticket.Core.Entities
{
    using Newtonsoft.Json;

    public class TicketConfig
    {
        [JsonProperty("AdminRole")] public ulong AdminRole { get; set; }
        
        [JsonProperty("PveAdminRole")] public ulong PveAdminRole { get; set; }
        
        [JsonProperty("SupportRole")] public ulong SupportRole { get; set; }
        
        [JsonProperty("PaypalLink")] public string PaypalLink { get; set; }
        
        [JsonProperty("TicketCreateChannel")] public ulong TicketCreateChannel { get; set; }
        
        [JsonProperty("TicketLimitPerUser")] public int TicketLimitPerUser { get; set; }
        
        [JsonProperty("TicketLogChannel")] public ulong TicketLogChannel { get; set; }
        
        [JsonProperty("TicketUrl")] public string TicketUrl { get; set; }
        
        [JsonProperty("TicketCategories")] public TicketCategories TicketCategories { get; set; }
    }

    public class TicketCategories
    {
        [JsonProperty("Creating")] public ulong Creating { get; set; }
        
        [JsonProperty("Hitlist")] public ulong Hitlist { get; set; }
        
        [JsonProperty("Pve")] public ulong Pve { get; set; }

        [JsonProperty("Admin")] public ulong Admin { get; set; }

        [JsonProperty("Restarter")] public ulong Restarter { get; set; }

        [JsonProperty("OrpPve")] public ulong OrpPve { get; set; }

        [JsonProperty("RuleBreak")] public ulong RuleBreak { get; set; }

        [JsonProperty("Account")] public ulong Account { get; set; }

        [JsonProperty("Highlights")] public ulong Highlights { get; set; }

        [JsonProperty("Vote")] public ulong Vote { get; set; }

        [JsonProperty("Other")] public ulong Other { get; set; }
    }
}