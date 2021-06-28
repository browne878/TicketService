namespace Ticket.Core.Entities
{
    using System;
    using System.Collections.Generic;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;

    public class TicketChannel
    {
        [JsonProperty("TicketId")] public int TicketId { get; set; }
        [JsonProperty("TicketName")] public List<string> TicketName { get; set; }
        [JsonProperty("TicketChannel")] public ulong ChannelTicket { get; private set; }
        [JsonProperty("Guild")] public ulong Guild { get; }
        [JsonProperty("TicketOwner")] public ulong TicketOwner { get; }
        [JsonProperty("CreatedAt")] public DateTime CreatedAt { get; set; }

        [JsonProperty("AddedUsers")] public List<ulong> AddedUsers { get; private set; }
        [JsonProperty("TicketCategory")] public string TicketCategory { get; set; }

        [JsonProperty("Cluster")] public string Cluster { get; set; }

        [JsonProperty("SteamId")] public ulong SteamId { get; set; }

        [JsonProperty("IngameIssue")] public bool IngameIssue { get; set; }

        [JsonProperty("Map")] public string Map { get; set; }

        [JsonProperty("ClosedBy")] public ulong ClosedBy { get; set; }

        [JsonProperty("ClosedAt")] public DateTime ClosedAt { get; set; }

        [JsonProperty("TranscriptUrl")] public string TranscriptUrl { get; private set; }

        [JsonProperty("TicketComplete")] public bool TicketComplete { get; set; }

        [JsonProperty("HitlistCcc")] public string HitlistCcc { get; set; }

        [JsonProperty("HitlistCords")] public bool HitlistWipe { get; set; }

    }
}