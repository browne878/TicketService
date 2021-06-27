namespace Ticket.Services.Services.BotService.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using Ticket.Core;
    using Ticket.Core.Entities;

    public class EventManager
    {
        private readonly DiscordClient bot;
        private readonly Config config;
        private readonly IUnitOfWork unitOfWork;

        public EventManager(DiscordClient _bot, Config _config, IUnitOfWork _unitOfWork)
        {
            bot = _bot;
            config = _config;
            unitOfWork = _unitOfWork;
        }

        public EventHandler<TicketCreatedEventArgs> TicketCreated;
        public EventHandler<TicketClosedEventArgs> TicketClosed;
        public EventHandler<TicketTimeoutEventArgs> TicketTimeout;

        public async Task InitialiseAsync()
        {
            
            await BeginTickets();
            
            bot.ComponentInteractionCreated += async (_, _args) =>
            {
                if (_args.Interaction.Type != InteractionType.Component) return;

                if (Regex.IsMatch(_args.Id, @"^\d+$-Close"))
                {
                    TicketLog tickets = fileService.GetTicketLog();
                    TicketChannel ticket = tickets.ActiveTickets.SingleOrDefault(_x => _x.ChannelTicket == _args.Channel.Id);

                    if (ticket != null) await ticket.Close(_args.User);
                }

                if (_args.Id == "Tile-Create")
                {
                    if (await dbManager.UsersOpenTicketsNum(_args.User.Id) >= config.TicketConfig.TicketLimitPerUser)
                    {
                        DiscordMember member = await _args.Guild.GetMemberAsync(_args.User.Id);
                        DiscordChannel dm = await member.CreateDmChannelAsync();

                        await dm.SendMessageAsync("You have already reached the maximum number of tickets. Please close a ticket to continue!");
                    }
                    else
                    {
                        TicketChannel newTicket = new(_args.User, bot, _args.Guild, dbManager, config, this, fileService);
                    }
                }
            };

            TicketCreated += async (_source, _args) => { await _args.Ticket.TicketHandler(); };
        }

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

            tile.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "Tile-Create", "",
                emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(bot, ":envelope_with_arrow:"))));
            
            DiscordMessage tileMessage = await tileChannel.SendMessageAsync(tile);
        }
    }
}