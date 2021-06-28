namespace Ticket.Core.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Ticket.Core.Entities.IEntities;

    public class TicketChannel
    {
        [JsonProperty("TicketId")] public int TicketId { get; set; }
        [JsonProperty("TicketOwner")] public ulong TicketOwner { get; }
        [JsonProperty("TicketName")] public List<string> TicketName { get; set; }
        [JsonProperty("TicketChannel")] public ulong ChannelTicket { get; set; }
        [JsonProperty("CreatedAt")] public DateTime CreatedAt { get; set; }
        [JsonProperty("TicketCategory")] public string TicketCategory { get; set; }
        [JsonProperty("Cluster")] public string Cluster { get; set; }
        [JsonProperty("SteamId")] public ulong SteamId { get; set; }
        [JsonProperty("IngameIssue")] public bool IngameIssue { get; set; }
        [JsonProperty("Map")] public string Map { get; set; }
        [JsonProperty("HitlistCcc")] public string HitlistCcc { get; set; }
        [JsonProperty("HitlistCords")] public bool HitlistWipe { get; set; }
        [JsonProperty("ClosedBy")] public ulong ClosedBy { get; set; }
        [JsonProperty("ClosedAt")] public DateTime ClosedAt { get; set; }
        [JsonProperty("TranscriptUrl")] public string TranscriptUrl { get; set; }
        [JsonProperty("TicketComplete")] public bool TicketComplete { get; set; }
        
        [JsonIgnore] public ITicketChannelLogic TicketChannelLogic { get; set; }

        public TicketChannel(ulong _ticketOwner)
        {
            TicketOwner = _ticketOwner;
            Task.WhenAll(CreateTicket());
        }

        private async Task CreateTicket()
        {

        }
    }
}