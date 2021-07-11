namespace Ticket.Services.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Emzi0767.Utilities;
    using Microsoft.Extensions.Hosting;
    using Ticket.Core;
    using Ticket.Core.Entities;
    using Ticket.Services.Services.BotService.Events;
    using Ticket.Services.Services.BotService.LogicModels;

    public class EventService : IHostedService, IDisposable
    {
        private readonly DiscordClient bot;
        private readonly Config config;

        private AsyncEvent<DiscordGuild, TicketCreatedEventArgs> _ticketCreated;
        private AsyncEvent<DiscordGuild, TicketClosedEventArgs> _ticketClosed;
        private AsyncEvent<DiscordGuild, TicketTimeoutEventArgs> _ticketTimeout;
        private AsyncEvent<DiscordGuild, SteamIdEventArgs> _steamIdComplete;
        private AsyncEvent<DiscordGuild, IngameIssueEventArgs> _ingameIssueComplete;
        private AsyncEvent<DiscordGuild, ClusterEventArgs> _clusterComplete;
        private AsyncEvent<DiscordGuild, MapEventArgs> _mapComplete;
        private AsyncEvent<DiscordGuild, CategoryEventArgs> _categoryComplete;
        private AsyncEvent<DiscordGuild, HitlistCordsEventArgs> _hitlistCordsComplete;
        private AsyncEvent<DiscordGuild, HitlistWipeEventArgs> _hitlistWipeComplete;

        private AsyncEventHandler<DiscordGuild, TicketCreatedEventArgs> ticketCreated;
        private AsyncEventHandler<DiscordGuild, TicketClosedEventArgs> ticketClosed;
        private AsyncEventHandler<DiscordGuild, TicketTimeoutEventArgs> ticketTimeout;
        private AsyncEventHandler<DiscordGuild, SteamIdEventArgs> steamIdComplete;
        private AsyncEventHandler<DiscordGuild, IngameIssueEventArgs> ingameIssueComplete;
        private AsyncEventHandler<DiscordGuild, ClusterEventArgs> clusterComplete;
        private AsyncEventHandler<DiscordGuild, MapEventArgs> mapComplete;
        private AsyncEventHandler<DiscordGuild, CategoryEventArgs> categoryComplete;
        private AsyncEventHandler<DiscordGuild, HitlistCordsEventArgs> hitlistCordsComplete;
        private AsyncEventHandler<DiscordGuild, HitlistWipeEventArgs> hitlistWipeComplete;

        public EventService(DiscordClient _bot, IUnitOfWork _unitOfWork, TicketController _control, Config _config)
        {
            bot = _bot;
            config = _config;
            InitialiseEvents();
        }

        private void InitialiseEvents()
        {
            _ticketCreated = new AsyncEvent<DiscordGuild, TicketCreatedEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _ticketClosed = new AsyncEvent<DiscordGuild, TicketClosedEventArgs>("Ticket Closed", TimeSpan.FromMinutes(10), EventErrorHandler);
            _ticketTimeout = new AsyncEvent<DiscordGuild, TicketTimeoutEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _steamIdComplete = new AsyncEvent<DiscordGuild, SteamIdEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _ingameIssueComplete = new AsyncEvent<DiscordGuild, IngameIssueEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _clusterComplete = new AsyncEvent<DiscordGuild, ClusterEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _mapComplete = new AsyncEvent<DiscordGuild, MapEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _categoryComplete = new AsyncEvent<DiscordGuild, CategoryEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _hitlistCordsComplete = new AsyncEvent<DiscordGuild, HitlistCordsEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
            _hitlistWipeComplete= new AsyncEvent<DiscordGuild, HitlistWipeEventArgs>("Ticket Created", TimeSpan.FromMinutes(10), EventErrorHandler);
        }

        private void EventErrorHandler<TSender, TArgs>(
            AsyncEvent<TSender, TArgs> _asyncEvent,
            Exception _ex,
            AsyncEventHandler<TSender, TArgs> _handler,
            TSender _sender,
            TArgs _eventArgs)
            where TArgs : AsyncEventArgs
        {
            Console.WriteLine("Error with new events");
        }

        public async Task StartAsync(CancellationToken _cancellationToken)
        {
            bot.ComponentInteractionCreated += OnComponentInteractionCreated;
            ticketCreated += OnTicketCreated;
            ticketClosed += OnTicketClosed;
            ticketTimeout += OnTicketTimeout;
            steamIdComplete += OnSteamIdComplete;
            ingameIssueComplete += OnIngameIssueComplete;
            clusterComplete += OnClusterComplete;
            mapComplete += OnMapComplete;
            categoryComplete += OnCategoryComplete;
            hitlistCordsComplete += OnHitlistCordsComplete;
            hitlistWipeComplete += OnHitlistWipeComplete;

            await BeginTickets();
        }

        public async Task StopAsync(CancellationToken _cancellationToken)
        {
            bot.ComponentInteractionCreated -= OnComponentInteractionCreated;
            ticketCreated -= OnTicketCreated;
            ticketClosed -= OnTicketClosed;
            ticketTimeout -= OnTicketTimeout;
            steamIdComplete -= OnSteamIdComplete;
            ingameIssueComplete -= OnIngameIssueComplete;
            clusterComplete -= OnClusterComplete;
            mapComplete -= OnMapComplete;
            categoryComplete -= OnCategoryComplete;
            hitlistCordsComplete -= OnHitlistCordsComplete;
            hitlistWipeComplete -= OnHitlistWipeComplete;

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose();
        }

        #region InvokeEvents

        public async Task InvokeTicketCreated(DiscordGuild _source, TicketCreatedEventArgs _args)
        {
            await _ticketCreated.InvokeAsync(_source, _args);
        }

        public async Task InvokeTicketClosed(DiscordGuild _source, TicketClosedEventArgs _args)
        {
            await _ticketClosed.InvokeAsync(_source, _args);
        }

        public async Task InvokeTicketTimeout(DiscordGuild _source, TicketTimeoutEventArgs _args)
        {
            await _ticketTimeout.InvokeAsync(_source, _args);
        }

        public async Task InvokeSteamIdComplete(DiscordGuild _source, SteamIdEventArgs _args)
        {
            await _steamIdComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeIngameIssueComplete(DiscordGuild _source, IngameIssueEventArgs _args)
        {
            await _ingameIssueComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeClusterComplete(DiscordGuild _source, ClusterEventArgs _args)
        {
            await _clusterComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeMapComplete(DiscordGuild _source, MapEventArgs _args)
        {
            await _mapComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeCategoryComplete(DiscordGuild _source, CategoryEventArgs _args)
        {
            await _categoryComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeHitlistCordsComplete(DiscordGuild _source, HitlistCordsEventArgs _args)
        {
            await _hitlistCordsComplete.InvokeAsync(_source, _args);
        }

        public async Task InvokeHitlistWipeComplete(DiscordGuild _source, HitlistWipeEventArgs _args)
        {
            await _hitlistWipeComplete.InvokeAsync(_source, _args);
        }

        #endregion

        #region OnEvents
        private async Task OnComponentInteractionCreated(DiscordClient _sender, ComponentInteractionCreateEventArgs _e)
        {
            if (!_e.Id.Equals("Tile-Create")) return;
        }

        private async Task OnTicketCreated(DiscordGuild _sender, TicketCreatedEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnTicketClosed(DiscordGuild _sender, TicketClosedEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnTicketTimeout(DiscordGuild _sender, TicketTimeoutEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnSteamIdComplete(DiscordGuild _sender, SteamIdEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnIngameIssueComplete(DiscordGuild _sender, IngameIssueEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnClusterComplete(DiscordGuild _sender, ClusterEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnMapComplete(DiscordGuild _sender, MapEventArgs _e)
        {
            throw new NotImplementedException();
        }
        
        private async Task OnCategoryComplete(DiscordGuild _sender, CategoryEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnHitlistCordsComplete(DiscordGuild _sender, HitlistCordsEventArgs _e)
        {
            throw new NotImplementedException();
        }

        private async Task OnHitlistWipeComplete(DiscordGuild _sender, HitlistWipeEventArgs _e)
        {
            throw new NotImplementedException();
        }

        #endregion

        private async Task BeginTickets()
        {

            DiscordChannel tileChannel = await bot.GetChannelAsync(config.TicketConfig.TicketCreateChannel); //gets tile channel from id
            IReadOnlyList<DiscordMessage> previousTile = await tileChannel.GetMessagesAsync(10); //gets 10 messages from channel

            //deletes previous 10 messages in channel
            foreach (DiscordMessage message in previousTile)
            {
                await message.DeleteAsync();
            }

            DiscordMessageBuilder tile = new();

            DiscordEmbedBuilder embed = new()
            {
                Title = "Bloody Support",
                ImageUrl = "https://cdn.discordapp.com/attachments/552290472236941313/638515223786946594/young-carers-resources-copy.png",
                Color = DiscordColor.Red
            };

            embed.WithFooter("bloody-ark.com");

            embed.WithDescription("In order to create a ticket, react with :envelope_with_arrow: to open a ticket and a conversation will be opened " +
                                  "between you and the admin team. Please do not contact members of the admin team directly.\n\n" +
                                  "To make assisting you easier, please follow the bots instructions. Once all the information has been provided," +
                                  "an admin will review the ticket and will reply as soon as possible.\n" +
                                  "if there is an error during this process, please type /error and an admin will be with you when one is available.\n\n" +
                                  "Remember, you can always ask the community for help in #ask-the-community channel.\n\n" +
                                  "If you intentionally avoid providing the bot with the required information, the ticket will be deleted without warning.\n\n" +
                                  "There will be no support for:\n" +
                                  "- Replacing kits, lootboxes or dino boxes because of server rollbacks, misclicks or bugs in any way\n" +
                                  "- Replacing your inventory or cryo'd dinos because of server rollbacks or bug in any way\n" +
                                  "- Helping you or your dino from getting unstuck from any location. If your dino is inside the mesh try to upload it with a " +
                                  "Transmitter, if your player gets stuck use /suicide\n" +
                                  "- Force joining a player into a tribe for any reason\n" +
                                  "- Replacing items or dinos lost to insiding. Everything belongs to the tribe owner. It's your own responsibility to invite people " +
                                  "in to your tribe that you trust\n" +
                                  "(Problems with items bought in our webshop will still be supported!)\n\n" +
                                  "Tickets about these cases will be closed without any reply from the Bloody Ark Team!:\n" +
                                  "- Crashed Servers\n" +
                                  "- Asking when a cluster will wipe\n");

            tile.WithEmbed(embed);

            tile.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "Tile-Create", "Create Ticket",
                emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(bot, ":envelope_with_arrow:"))));

            DiscordMessage tileMessage = await tileChannel.SendMessageAsync(tile);
        }
    }
}
