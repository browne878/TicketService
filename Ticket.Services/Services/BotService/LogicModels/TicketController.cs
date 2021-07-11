namespace Ticket.Services.Services.BotService.LogicModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Extensions;
    using Ticket.Core;
    using Ticket.Core.Entities;
    using Ticket.Services.Services.BotService.Events;

    public class TicketController
    {

        private readonly Config config;
        private readonly IUnitOfWork unitOfWork;
        private readonly DiscordClient bot;
        private readonly EventService eventManager;

        public TicketController(IUnitOfWork _unitOfWork, Config _config, DiscordClient _bot, EventService _eventManager)
        {
            unitOfWork = _unitOfWork;
            config = _config;
            bot = _bot;
            eventManager = _eventManager;
        }

        private async Task SetTicketId(TicketChannel _ticket)
        {
            TicketChannel previous = await unitOfWork.TicketRepository.GetTicketIdAsync();

            _ticket.Id = previous.Id + 1;
        }

        private async Task<List<DiscordOverwriteBuilder>> SetChannelPermissions(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordGuild server = channel.Guild;

            List<DiscordOverwriteBuilder> permissions = new();

            DiscordOverwriteBuilder pvePermissions = new(server.GetRole(config.TicketConfig.PveAdminRole));
            pvePermissions.Allow(Permissions.AccessChannels);
            pvePermissions.Allow(Permissions.ManageChannels);
            permissions.Add(pvePermissions);

            DiscordOverwriteBuilder supportPermissions = new(server.GetRole(config.TicketConfig.SupportRole));
            supportPermissions.Allow(Permissions.AccessChannels);
            supportPermissions.Allow(Permissions.ManageChannels);
            permissions.Add(supportPermissions);

            DiscordOverwriteBuilder adminPermissions = new(server.GetRole(config.TicketConfig.AdminRole));
            adminPermissions.Allow(Permissions.AccessChannels);
            adminPermissions.Allow(Permissions.ManageChannels);
            permissions.Add(adminPermissions);

            DiscordOverwriteBuilder ownerPermissions = new(await server.GetMemberAsync(_ticket.Owner));
            ownerPermissions.Allow(Permissions.AccessChannels);
            permissions.Add(ownerPermissions);

            DiscordOverwriteBuilder everyonePermissions = new(server.EveryoneRole);
            everyonePermissions.Deny(Permissions.AccessChannels);
            permissions.Add(everyonePermissions);

            return permissions;
        }

        public async Task CreateTicket(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordGuild server = channel.Guild;
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            await SetTicketId(_ticket);
            List<DiscordOverwriteBuilder> channelPermissions = await SetChannelPermissions(_ticket);
            _ticket.Name = new List<string> { "ticket", $"{_ticket.Id}" };
            DiscordChannel category = server.GetChannel(config.TicketConfig.TicketCategories.Creating);
            _ticket.Category = category.Name;
            _ticket.CreatedAt = DateTime.Now;

            DiscordChannel discordChannel = await server.CreateChannelAsync($"{_ticket.Name[0]}-{_ticket.Name[1]}", ChannelType.Text, category, overwrites: channelPermissions);

            DiscordEmbedBuilder welcomeEmbed = new()
            {
                Title = $"{owner.Username}'s Ticket",
                Color = DiscordColor.Red
            };

            welcomeEmbed.WithFooter($"Bloody-Ark.com - {DateTime.Now}"); //sets embed footer

            //sets embed description
            welcomeEmbed.WithDescription("Please provide all information requested by the bot! This will help us deal with your issue quicker.\n\n" +
                                         "There will be no support for:\n" +
                                         "- Replacing kits, lootboxes or dino boxes because of server rollbacks, misclicks or bugs in any way\n" +
                                         "- Replacing your inventory or cryo'd dinos because of server rollbacks or bug in any way\n" +
                                         "- Helping you or your dino from getting unstuck from any location. If your dino is inside the mesh try to upload it with a " +
                                         "Transmitter, if your player gets stuck user /suicide\n" +
                                         "- Force joining a player into a tribe for any reason\n" +
                                         "- Enabling engrams from Road to Alpha in any Situation. If you did not get the engrams you did not meet the requirements for it\n" +
                                         "- Replacing items or dinos lost to insiding. Everything belongs to the tribe owner. It's your own responsibility to invite people " +
                                         "in to your tribe that you trust\n" +
                                         "(Problems with items bought in our webshop will still be supported!)\n\n" +
                                         "Tickets about these cases will be closed without any reply from the Bloody Ark Team!:\n" +
                                         "- Crashed Servers\n" +
                                         "- Asking when a cluster will wipe\n\n" +
                                         "Please react with :lock: to close your ticket, or use /close.");

            DiscordMessageBuilder welcomeBuilder = new()
            {
                Content = $"Welcome {owner.Mention}!",
                Embed = welcomeEmbed
            };

            welcomeBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Close", "Close"));

            await discordChannel.SendMessageAsync(welcomeBuilder);

            await eventManager.InvokeTicketCreated(server, new TicketCreatedEventArgs()
            {
                Ticket = _ticket
            });
        }

        public async Task GetSteamId(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            _ticket.SteamId = ulong.Parse(await unitOfWork.TicketRepository.GetSteamID(_ticket.Owner));

            if (_ticket.SteamId != 0)
            {
                DiscordMessageBuilder confirmId = new();
                confirmId.WithEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Is this the correct Steam ID?",
                    Description = $"{_ticket.SteamId}"
                });

                confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-SteamID-Yes", "YES"));
                confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, $"{_ticket.Id}-SteamID-No", "No"));

                DiscordMessage confirmIdMessage = await channel.SendMessageAsync(confirmId);

                InteractivityResult<ComponentInteractionCreateEventArgs> btnComplete = await confirmIdMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                if (btnComplete.TimedOut)
                {
                    await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                    {
                        TicketChannel = _ticket
                    });
                    return;
                }

                if (btnComplete.Result.Id == $"{_ticket.Id}-SteamID-Yes")
                {
                    await eventManager.InvokeSteamIdComplete(channel.Guild, new SteamIdEventArgs
                    {
                        Success = true,
                        TicketChannel = _ticket
                    });
                    return;
                }
            }

            DiscordMessageBuilder link = new();

            DiscordEmbedBuilder requestSteamId = new()
            {
                Title = "You have not linked your account ingame",
                Color = DiscordColor.Red
            };

            //adds embed description
            requestSteamId.WithDescription("We do not have your SteamID.\n\n" +
                                           "Please type `/link` to link your steam account\n\n" +
                                           "You will receive a dm from the bot that will give you a link to retrieve your steam ID\n" +
                                           "Please press the button below once you have done this.");

            link.WithEmbed(requestSteamId);

            link.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"Ticket{_ticket.Id}-SteamId", "Account Linked"));

            DiscordMessage linkMessage = await channel.SendMessageAsync(link);

            InteractivityResult<ComponentInteractionCreateEventArgs> linkResult = await linkMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            linkResult.Result.Handled = true;

            await linkResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (linkResult.TimedOut)
            {
                _ticket.SteamId = ulong.Parse(await unitOfWork.TicketRepository.GetSteamID(_ticket.Owner));

                if (_ticket.SteamId != 0)
                {
                    DiscordMessageBuilder confirmId = new();
                    confirmId.WithEmbed(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Red,
                        Title = "Is this the correct Steam ID?",
                        Description = $"{_ticket.SteamId}"
                    });

                    confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-SteamID-Yes", "YES"));
                    confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, $"{_ticket.Id}-SteamID-No", "No"));

                    DiscordMessage confirmIdMessage = await channel.SendMessageAsync(confirmId);

                    InteractivityResult<ComponentInteractionCreateEventArgs> btnComplete = await confirmIdMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                    btnComplete.Result.Handled = true;

                    //trying to create response

                    await btnComplete.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    if (btnComplete.TimedOut)
                    {
                        await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                        {
                            TicketChannel = _ticket
                        });
                        return;
                    }

                    if (btnComplete.Result.Id == $"{_ticket.Id}-SteamID-Yes")
                    {
                        await eventManager.InvokeSteamIdComplete(channel.Guild, new SteamIdEventArgs
                        {
                            Success = true,
                            TicketChannel = _ticket
                        });
                        return;
                    }
                }
                else
                {
                    DiscordMessageBuilder timeout = new()
                    {
                        Content = "You have not Linked your account yet. Please do `/link` and link your account.",
                        Mentions = { new UserMention(owner) }
                    };

                    timeout.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-SteamID", "Account Linked"));

                    DiscordMessage timeoutMessage = await channel.SendMessageAsync(timeout);

                    InteractivityResult<ComponentInteractionCreateEventArgs> timeoutResult = await timeoutMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                    timeoutResult.Result.Handled = true;

                    await timeoutResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                }
            }
        }

        public async Task GetIngameIssue(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            DiscordMessageBuilder issue = new();

            issue.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Is your issue ingame or not ingame?"
            });

            issue.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Issue-Yes", "Ingame Issue"));
            issue.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Issue-No", "Not Ingame Issue"));

            DiscordMessage issueMessage = await channel.SendMessageAsync(issue);

            InteractivityResult<ComponentInteractionCreateEventArgs> issueResponse = await issueMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (issueResponse.TimedOut)
            {
                await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                {
                    TicketChannel = _ticket
                });
                return;
            }

            if (issueResponse.Result.Id == $"{_ticket.Id}-Issue-Yes")
            {
                _ticket.IngameIssue = true;

                await eventManager.InvokeIngameIssueComplete(channel.Guild, new IngameIssueEventArgs()
                {
                    TicketChannel = _ticket
                });
                return;
            }

            if (issueResponse.Result.Id == $"{_ticket.Id}-Issue-No")
            {
                _ticket.IngameIssue = false;

                await eventManager.InvokeIngameIssueComplete(channel.Guild, new IngameIssueEventArgs()
                {
                    TicketChannel = _ticket
                });
            }
        }

        public async Task GetCluster(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            DiscordMessageBuilder cluster = new();

            cluster.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What cluster is your issue on?"
            });

            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Cluster-PVPVE", "PVPVE"));
            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Cluster-PVP", "PVP"));
            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Cluster-PVE", "PVE"));

            DiscordMessage clusterMessage = await channel.SendMessageAsync(cluster);

            InteractivityResult<ComponentInteractionCreateEventArgs> clusterResponse = await clusterMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (clusterResponse.TimedOut)
            {
                await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                {
                    TicketChannel = _ticket
                });

                return;
            }

            switch (clusterResponse.Result.Id)
            {
                case var x when x == $"{_ticket.Id}-Cluster-PVPVE":
                    _ticket.Cluster = "PVPVE";

                    await eventManager.InvokeClusterComplete(channel.Guild, new ClusterEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    break;

                case var x when x == $"{_ticket.Id}-Cluster-PVP":
                    _ticket.Cluster = "PVP";

                    await eventManager.InvokeClusterComplete(channel.Guild, new ClusterEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    break;

                case var x when x == $"{_ticket.Id}-Cluster-PVE":
                    _ticket.Cluster = "PVE";

                    await eventManager.InvokeClusterComplete(channel.Guild, new ClusterEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    break;
            }
        }

        public async Task GetMap(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            DiscordMessageBuilder map = new();

            map.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What map is your issue on?"
            });

            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Island", "Island"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Scorched_Earth", "Scorched Earth"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Aberration", "Aberration"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-The_Center", "The Center"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Ragnarok", "Ragnarok"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Extinction", "Extinction"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Valguero", "Valguero"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Genesis_1", "Genesis 1"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Crystal_Isles", "Crystal Isles"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Map-Genesis_2", "Genesis 2"));

            DiscordMessage mapMessage = await channel.SendMessageAsync(map);

            InteractivityResult<ComponentInteractionCreateEventArgs> mapResponse = await mapMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (mapResponse.TimedOut)
            {
                await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                {
                    TicketChannel = _ticket
                });
                return;
            }

            switch (mapResponse.Result.Id)
            {
                case var x when x == $"{_ticket.Id}-Map-Island":
                    _ticket.Map = "Island";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Scorched_Earth":
                    _ticket.Map = "Scorched Earth";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Aberration":
                    _ticket.Map = "Aberration";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-The_Center":
                    _ticket.Map = "The Center";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Ragnarok":
                    _ticket.Map = "Ragnarok";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Extinction":
                    _ticket.Map = "Extinction";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Valguero":
                    _ticket.Map = "Valguero";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Genesis_1":
                    _ticket.Map = "Genesis 1";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Crystal_Isles":
                    _ticket.Map = "Crystal Isles";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Map-Genesis_2":
                    _ticket.Map = "Genesis 2";

                    await eventManager.InvokeMapComplete(channel.Guild, new MapEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;
            }
        }

        public async Task GetCategory(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            DiscordMessageBuilder category = new();

            category.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What category of issue do you have?"
            });

            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Hitlist", "Hitlist"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Orp_Pve", "Orp or Pve"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Rule_Breaks", "Rule Breaks"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Account", "Account"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Highlights", "Highlights"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Vote", "Vote"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{_ticket.Id}-Category-Other", "Other"));

            DiscordMessage categoryMessage = await channel.SendMessageAsync(category);

            InteractivityResult<ComponentInteractionCreateEventArgs> categoryResponse = await categoryMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (categoryResponse.TimedOut)
            {
                await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                {
                    TicketChannel = _ticket
                });
                return;
            }

            switch (categoryResponse.Result.Id)
            {
                case var x when x == $"{_ticket.Id}-Category-Hitlist":
                    _ticket.Category = "Hitlist";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Orp_Pve":
                    _ticket.Category = "Orp or Pve";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Rule_Breaks":
                    _ticket.Category = "Rule Breaks";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Account":
                    _ticket.Category = "Account";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Highlights":
                    _ticket.Category = "Highlights";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Vote":
                    _ticket.Category = "Vote";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Category-Other":
                    _ticket.Category = "Other";

                    await eventManager.InvokeCategoryComplete(channel.Guild, new CategoryEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;
            }
        }

        public async Task HitlistCords(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);
            InteractivityExtension interactivity = bot.GetInteractivity();

            //gets bot commands channel
            DiscordChannel commandsChannel = channel.Guild.GetChannel(424672319756042240);

            int loopCounter = 0;
            while (true)
            {
                switch (loopCounter)
                {
                    case >= 5:
                        await channel.SendMessageAsync("We were unable to get your base cords");

                        await eventManager.InvokeHitlistCordsComplete(channel.Guild, new HitlistCordsEventArgs
                        {
                            Success = false,
                            TicketChannel = _ticket
                        });

                        return;

                    case >= 2:
                        await channel.SendMessageAsync($"Please read the instructions carefully. Please use the /ccc command in {commandsChannel.Mention}");
                        break;
                }

                await channel.SendMessageAsync("Please stand in the crafting area of your base and provide use the ccc cords of that location.\n\nTo get the ccc cords please open the console " +
                                               "(Where you type gamma) and type ccc and press enter. Then paste (CTRL + V) in the channel.\n\nInvalid ccc cords will result in your ticket being deleted.\n" +
                                               "If you are still unsure on how to use ccc, type /ccc and you will be shown how.");

                InteractivityResult<DiscordMessage> cordsResult = await interactivity.WaitForMessageAsync(_x => _x.Channel == channel && _x.Author == owner &&
                                                                                                                _x.Content != "/ccc");

                if (cordsResult.TimedOut)
                {
                    DiscordMessageBuilder timeout = new();

                    timeout.WithEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Your Hitlist request has timed out!",
                        Description = "Would you like to continue with this request?"
                    });

                    timeout.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-CCC-Timeout-Yes", "Yes"));
                    timeout.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-CCC-Timeout-No", "No"));

                    DiscordMessage timeoutConfirmMessage = await channel.SendMessageAsync(timeout);

                    InteractivityResult<ComponentInteractionCreateEventArgs> timeoutConfirmResult = await timeoutConfirmMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                    //Checks User reaction
                    if (timeoutConfirmResult.TimedOut || timeoutConfirmResult.Result.Id == $"{_ticket.Id}-CCC-Timeout-No")
                    {
                        await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                        {
                            TicketChannel = _ticket
                        });

                        return;
                    }
                }
                else
                {
                    if (cordsResult.Result.Content.Trim().Split(' ').Length == 5)
                    {
                        List<string> cords = cordsResult.Result.Content.Split(' ').ToList();

                        cords.RemoveAt(4);
                        cords.RemoveAt(3);

                        List<bool> cordsNumericCheck = cords.Select(_cord => Regex.IsMatch(_cord, @"^\d+$") || (!Regex.IsMatch(_cord, @"^\d+$") && _cord.Substring(0, 1) == "-")).ToList();

                        if (cordsNumericCheck.Any(_x => _x == false))
                        {
                            await channel.SendMessageAsync("invalid cords");
                        }
                        else
                        {
                            _ticket.HitlistCcc = string.Join(" ", cords);

                            await eventManager.InvokeHitlistCordsComplete(channel.Guild, new HitlistCordsEventArgs
                            {
                                Success = true,
                                TicketChannel = _ticket
                            });

                            return;
                        }
                    }
                }

                loopCounter++;
            }
        }

        public async Task HitlistWipePrevious(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            DiscordMessageBuilder wipe = new();

            wipe.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Did you Wipe the previous Hitlist Tribe?"
            });

            wipe.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-Wipe-Yes", "Yes"));
            wipe.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{_ticket.Id}-Wipe-No", "No"));

            DiscordMessage wipeMessage = await channel.SendMessageAsync(wipe);

            InteractivityResult<ComponentInteractionCreateEventArgs> wipeResult = await wipeMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (wipeResult.TimedOut)
            {
                await eventManager.InvokeTicketTimeout(channel.Guild, new TicketTimeoutEventArgs
                {
                    TicketChannel = _ticket
                });

                return;
            }

            switch (wipeResult.Result.Id)
            {
                case var x when x == $"{_ticket.Id}-Wipe-Yes":
                    _ticket.HitlistWipe = true;

                    await eventManager.InvokeHitlistWipeComplete(channel.Guild, new HitlistWipeEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;

                case var x when x == $"{_ticket.Id}-Wipe-No":
                    _ticket.HitlistWipe = false;

                    await eventManager.InvokeHitlistWipeComplete(channel.Guild, new HitlistWipeEventArgs
                    {
                        TicketChannel = _ticket
                    });

                    return;
            }
        }

        private async Task TicketCompleted(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            int ingameIssue = _ticket.IngameIssue ? 1 : 0;

            dbManager.VoidQuery($"INSERT INTO discord_tickets(ID,ChannelID,UserID,SteamID,CreatedAt,Closed,InGameIssue,Cluster,Issue)" +
                                $"VALUES ('{TicketId}','{channel.Id}','{owner.Id}',{SteamId}','{CreatedAt:yyyy/MM/dd HH:MM:ss}',0,{ingameIssue},'{Cluster}','{TicketCategory}')");


            IEnumerable<DiscordMessage> messages = await channel.GetMessagesAsync();
            messages.ToList().RemoveAt(messages.Count());

            await channel.DeleteMessagesAsync(messages);

            DiscordEmbedBuilder userTicket = new()
            {
                Title = $"{owner.Username}'s Ticket",
                Color = DiscordColor.Red
            };

            userTicket.WithThumbnail(owner.AvatarUrl);
            userTicket.WithFooter($"BloodySupport - {DateTime.Now}"); //sets footer

            //creates fields with issue information
            userTicket.AddField("Discord Name", $"{owner.Mention}", true);

            userTicket.AddField("Steam ID", $"{SteamId}", true);
            userTicket.AddField("Cluster", $"{Cluster}", true);
            userTicket.AddField("Map", $"{Map}", true);
            userTicket.AddField("Ingame Issue", $"{IngameIssue}", true);
            userTicket.AddField("Category", TicketCategory, true);
            userTicket.AddField("Created At", $"{CreatedAt}", true);
            userTicket.AddField("\u200B", "To close the ticket, you can use /close at any time.");

            if (TicketCategory == "Hitlist")
            {
                userTicket.AddField("Base Cords", HitlistCcc, true);
                userTicket.AddField("Wiped Previous Hitlist", $"{HitlistWipe}", true);
            }
        }

        async Task<string> GenerateUrl()
        {
            Random random = new();

            List<char> letters = new()
            {
                'a',
                'a',
                'b',
                'c',
                'd',
                'e',
                'f',
                'g',
                'h',
                'i',
                'j',
                'k',
                'l',
                'm',
                'n',
                'o',
                'p',
                'q',
                'r',
                's',
                't',
                'u',
                'v',
                'w',
                'x',
                'y',
                'z',
                'z'
            };

            string url = "";

            for (int i = 0; i < 16; i++)
            {
                int letterOrNumber = random.Next(1, 3);
                switch (letterOrNumber)
                {
                    case 1:
                        int letterPos = random.Next(27);
                        int letterCase = random.Next(1, 3);
                        switch (letterCase)
                        {
                            case 1:
                                url += letters[letterPos];
                                break;
                            case 2:
                                string letterCap = letters[letterPos].ToString();
                                letterCap = letterCap.ToUpper();
                                url += letterCap;
                                break;
                        }

                        break;
                    case 2:
                        string number = random.Next(9).ToString();
                        url += number;
                        break;
                }
            }

            await Task.CompletedTask;
            return url;
        }

        async Task CreateTranscript(TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordUser owner = await bot.GetUserAsync(_ticket.Owner);

            string fileName = await GenerateUrl() + "_" + channel.Name[..] + ".html";
            string ticketUrl = config.TicketConfig.TicketUrl + fileName;
            string filepath = @"C:\xampp\htdocs\logs\" + fileName;
            DiscordChannel transcriptChannel = channel.Guild.GetChannel(config.TicketConfig.TicketLogChannel);

            IReadOnlyList<DiscordMessage> allMessages = await channel.GetMessagesAsync();

            List<DiscordMessage> channelMessages = allMessages.ToList();
            List<DiscordUser> ticketUsers = new();

            channelMessages.Reverse();

            int loopCount = 0;

            if (!File.Exists(filepath))
            {
                await using StreamWriter sw = File.CreateText(filepath);
                {
                    await sw.WriteLineAsync("<html>");
                    await sw.WriteLineAsync($"<title> {channel.Name[..]} Transcript </title>");
                    await sw.WriteLineAsync("<body>");

                    foreach (DiscordMessage unused in channelMessages)
                    {
                        if (channelMessages[loopCount].Attachments.Count > 0)
                        {
                            if (channelMessages[loopCount].Content == "")
                            {
                                foreach (DiscordAttachment attachment in channelMessages[loopCount].Attachments)
                                    await sw.WriteLineAsync($"<p>{channelMessages[loopCount].Author.Username} - {attachment.ProxyUrl} - {channelMessages[loopCount].Timestamp}</p>");
                            }
                            else
                            {
                                await sw.WriteLineAsync($"<p>{channelMessages[loopCount].Author.Username} - {channelMessages[loopCount].Content} - {channelMessages[loopCount].Timestamp}</p>");
                                foreach (DiscordAttachment attachment in channelMessages[loopCount].Attachments)
                                    await sw.WriteLineAsync($"<p>{channelMessages[loopCount].Author.Username} - {attachment.ProxyUrl} - {channelMessages[loopCount].Timestamp}</p>");
                            }
                        }
                        else
                        {
                            if (!(channelMessages[loopCount].Embeds.Count > 0)) await sw.WriteLineAsync($"<p>{channelMessages[loopCount].Author.Username} - {channelMessages[loopCount].Content} - {channelMessages[loopCount].Timestamp}</p>");

                            if (!ticketUsers.Contains(channelMessages[loopCount].Author))
                                if (!channelMessages[loopCount].Author.IsBot)
                                    ticketUsers.Add(channelMessages[loopCount].Author);
                        }

                        if (loopCount % 100 == 0 && loopCount != 0)
                        {
                            DiscordMessage finalMessage = channelMessages[loopCount];
                            allMessages = await channel.GetMessagesBeforeAsync(finalMessage.Id);
                            channelMessages = allMessages.ToList();
                        }

                        loopCount++;
                    }

                    await sw.WriteLineAsync("</body>");
                    await sw.WriteLineAsync("</html>");

                    sw.Close();
                    await sw.DisposeAsync();
                }
            }

            DiscordEmbedBuilder ticketLogEmbed = new()
            {
                Title = $"{channel.Name}",
                Color = DiscordColor.Red
            };

            ticketLogEmbed.AddField("Ticket Owner", $"{owner.Username}#{owner.Discriminator}", true);
            ticketLogEmbed.AddField("Steam ID", $"{_ticket.SteamId}", true);
            ticketLogEmbed.AddField("Issue", $"{_ticket.IngameIssue} - {_ticket.Cluster} - {_ticket.Category}", true);
            ticketLogEmbed.AddField("Created At", $"{_ticket.CreatedAt}", true);
            ticketLogEmbed.AddField("Closed At", $"{_ticket.ClosedAt}", true);
            ticketLogEmbed.AddField("Closed By", $"{owner.Username}", true);

            DiscordMessageBuilder ticketLogMessage = new()
            {
                Embed = ticketLogEmbed,
                Content = $"{ticketUrl}"
            };

            await transcriptChannel.SendMessageAsync(ticketLogMessage);
        }

        public async Task<bool> Close(DiscordUser _user, TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);
            DiscordMember member = await channel.Guild.GetMemberAsync(_ticket.Owner);

            await channel.AddOverwriteAsync(member, Permissions.None, Permissions.SendMessages);

            DiscordButtonComponent yes = new(ButtonStyle.Success, $"{_ticket.Id}-Close-Yes", "Yes");
            DiscordButtonComponent no = new(ButtonStyle.Danger, $"{_ticket.Id}-Close-No", "No");

            DiscordMessageBuilder confirm = new()
            {
                Embed = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "Are you sure you want to close the ticket?"
                }
            };

            confirm.AddComponents(yes, no);

            DiscordMessage confirmMessage = await channel.SendMessageAsync(confirm);

            InteractivityResult<ComponentInteractionCreateEventArgs> result =
                await confirmMessage.WaitForButtonAsync(_user, TimeSpan.FromSeconds(30));

            if (result.TimedOut)
            {
                await confirmMessage.DeleteAsync("Ticket Close has timed out");
                return true;
            }

            switch (result.Result.Id)
            {
                case var x when x == $"{_ticket.Id}-Close-Yes":

                    await confirmMessage.DeleteAsync("Ticket Closed");

                    _ticket.ClosedBy = _user.Id;
                    _ticket.ClosedAt = DateTime.Now;

                    DiscordChannel dmChannel = await member.CreateDmChannelAsync();

                    switch (_ticket.Complete)
                    {
                        case false:
                            {
                                await dmChannel.SendMessageAsync("Sorry, we were unable to provide you with a transcript...\nThis is because you did not complete the ticket" +
                                                                 "setup process.");
                                break;
                            }
                        case true:

                            await CreateTranscript(_ticket);

                            Dictionary<DiscordUser, string> supportMembers = new();

                            foreach (DiscordMessage discordMessage in await channel.GetMessagesAsync())
                            {
                                if (supportMembers.Keys.Contains(discordMessage.Author)) continue;

                                DiscordMember messageAuthor = await channel.Guild.GetMemberAsync(discordMessage.Author.Id);

                                if (messageAuthor.Roles.Contains(channel.Guild.GetRole(config.TicketConfig.AdminRole)) ||
                                    messageAuthor.Roles.Contains(channel.Guild.GetRole(config.TicketConfig.PveAdminRole)) ||
                                    messageAuthor.Roles.Contains(channel.Guild.GetRole(config.TicketConfig.SupportRole)))
                                {
                                    supportMembers.Add(discordMessage.Author, $"{_ticket.TranscriptUrl} - {messageAuthor.DisplayName}#{messageAuthor.Discriminator} - Add Comment Here");
                                }
                            }

                            if (channel.Guild.Members.ContainsKey(_ticket.Owner))
                            {
                                string supportDonate = supportMembers.Aggregate(string.Empty, (_current, _supportMember) => _current + "\n" + _supportMember.Value);

                                DiscordMessageBuilder dmMessage = new()
                                {
                                    Content = "Thank you for creating a ticket. We hope we were able to solve your issue! Please leave feedback in our server feedback channel. " +
                                                                                $"We have attached the transcript to this message.\n{_ticket.TranscriptUrl}\n\nIf you would like to thank the support team for their hard work then you can" +
                                                                                $" donate directly to them! To do this please visit {config.TicketConfig.PaypalLink}\nTo select the support member you would like the donation to go to" +
                                                                                $"please add one of the following to the comment of your donation:{supportDonate}\n\nAll donations are anonymous and are monitored."
                                };

                                await dmChannel.SendMessageAsync(dmMessage);
                            }

                            break;
                    }

                    break;

                case var x when x == $"{_ticket.Id}-Close-No":
                    await confirmMessage.DeleteAsync("Ticket Close Denied");
                    await channel.AddOverwriteAsync(member, Permissions.SendMessages);
                    break;

                default:
                    //Insert Error Here
                    await channel.SendMessageAsync("Close Failed");
                    return false;
            }

            return true;
        }

        public async Task<bool> Assign(DiscordChannel _channel, TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);

            if (!_channel.IsCategory) return false;

            await channel.ModifyAsync(_x => _x.Parent = _channel);

            return channel.ParentId == _channel.Id;
        }

        public async Task AddMembers(IEnumerable<DiscordUser> _users, TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);

            foreach (DiscordUser discordUser in _users)
            {
                await channel.AddOverwriteAsync(await channel.Guild.GetMemberAsync(discordUser.Id), Permissions.AccessChannels);
            }
        }

        public async Task RemoveMembers(IEnumerable<DiscordUser> _users, TicketChannel _ticket)
        {
            DiscordChannel channel = await bot.GetChannelAsync(_ticket.Channel);

            foreach (DiscordUser discordUser in _users)
            {
                await channel.AddOverwriteAsync(await channel.Guild.GetMemberAsync(discordUser.Id), deny: Permissions.AccessChannels);
            }
        }
    }
}
