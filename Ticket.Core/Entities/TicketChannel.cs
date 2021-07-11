namespace Ticket.Core.Entities
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class TicketChannel
    {
        [JsonProperty("TicketId")] public int Id { get; set; }
        [JsonProperty("TicketOwner")] public ulong Owner { get; }
        [JsonProperty("TicketName")] public List<string> Name { get; set; }
        [JsonProperty("TicketChannel")] public ulong Channel { get; set; }
        [JsonProperty("CreatedAt")] public DateTime CreatedAt { get; set; }
        [JsonProperty("TicketCategory")] public string Category { get; set; }
        [JsonProperty("Cluster")] public string Cluster { get; set; }
        [JsonProperty("SteamId")] public ulong SteamId { get; set; }
        [JsonProperty("IngameIssue")] public bool IngameIssue { get; set; }
        [JsonProperty("Map")] public string Map { get; set; }
        [JsonProperty("HitlistCcc")] public string HitlistCcc { get; set; }
        [JsonProperty("HitlistCords")] public bool HitlistWipe { get; set; }
        [JsonProperty("ClosedBy")] public ulong ClosedBy { get; set; }
        [JsonProperty("ClosedAt")] public DateTime ClosedAt { get; set; }
        [JsonProperty("TranscriptUrl")] public string TranscriptUrl { get; set; }
        [JsonProperty("TicketComplete")] public bool Complete { get; set; }
    }
}