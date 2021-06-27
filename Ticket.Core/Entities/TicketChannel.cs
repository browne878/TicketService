namespace Ticket.Core.Entities
{
    using System;
    using System.Collections.Generic;
    using DSharpPlus.Entities;
    using Newtonsoft.Json;

    public class TicketChannel
    {
        [JsonProperty("TicketId")] public int TicketId { get; protected set; }
        [JsonProperty("TicketName")] public List<string> TicketName { get; protected set; }
        [JsonProperty("TicketChannel")] public ulong ChannelTicket { get; private set; }
        [JsonProperty("Guild")] public ulong Guild { get; }
        [JsonProperty("TicketOwner")] public ulong TicketOwner { get; }
        [JsonProperty("CreatedAt")] public DateTime CreatedAt { get; protected set; }

        [JsonProperty("AddedUsers")] public List<ulong> AddedUsers { get; private set; }
        [JsonProperty("TicketCategory")] public string TicketCategory { get; protected set; }

        [JsonProperty("Cluster")] public string Cluster { get; protected set; }

        [JsonProperty("SteamId")] public ulong SteamId { get; protected set; }

        [JsonProperty("IngameIssue")] public bool IngameIssue { get; protected set; }

        [JsonProperty("Map")] public string Map { get; protected set; }

        [JsonProperty("ClosedBy")] public ulong ClosedBy { get; protected set; }

        [JsonProperty("ClosedAt")] public DateTime ClosedAt { get; protected set; }

        [JsonProperty("TranscriptUrl")] public string TranscriptUrl { get; private set; }

        [JsonProperty("TicketComplete")] public bool TicketComplete { get; protected set; }

        [JsonProperty("HitlistCcc")] public string HitlistCcc { get; protected set; }

        [JsonProperty("HitlistCords")] public bool HitlistWipe { get; protected set; }

    }
}