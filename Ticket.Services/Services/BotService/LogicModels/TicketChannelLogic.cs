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
    using Newtonsoft.Json;
    using Ticket.Core.Entities;
    using Ticket.Services.Services.BotService.Events;

    public class TicketChannelLogic : TicketChannel
    {

        [JsonIgnore]
        private readonly Config config;

        [JsonIgnore]
        private readonly EventManager eventManager;

        [JsonIgnore]
        private readonly InteractivityExtension interactivity;

        [JsonIgnore]
        private readonly DiscordUser owner;

        [JsonIgnore]
        private readonly DiscordGuild server;

        [JsonIgnore]
        private readonly DiscordChannel channel;

        public TicketChannelLogic(DiscordUser _owner, DiscordClient _bot, DiscordGuild _server,
                             Config _config, EventManager _eventManager, FileReaderService _fileService)
        {
            owner = _owner;
            TicketComplete = false;
            server = _server;
            config = _config;
            eventManager = _eventManager;
            fileService = _fileService;
            interactivity = _bot.GetInteractivity();
            channel = Task.WhenAll(CreateTicket()).Result[0];
        }

        private int SetTicketId() => (int)dbManager.ObjectQuery($"SELECT * FROM discord_tickets ORDER BY ID DESC LIMIT 1 WHERE ID = {owner.Id}");

        private async Task<List<DiscordOverwriteBuilder>> SetChannelPermissions()
        {
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

            DiscordOverwriteBuilder ownerPermissions = new(await server.GetMemberAsync(owner.Id));
            ownerPermissions.Allow(Permissions.AccessChannels);
            permissions.Add(ownerPermissions);

            DiscordOverwriteBuilder everyonePermissions = new(server.EveryoneRole);
            everyonePermissions.Deny(Permissions.AccessChannels);
            permissions.Add(everyonePermissions);

            return permissions;
        }

        private async Task<DiscordChannel> CreateTicket()
        {

            TicketId = SetTicketId();
            List<DiscordOverwriteBuilder> channelPermissions = await SetChannelPermissions();
            TicketName = new List<string> { "ticket", $"{TicketId}" };
            DiscordChannel category = server.GetChannel(config.TicketConfig.TicketCategories.Creating);
            TicketCategory = category.Name;
            CreatedAt = DateTime.Now;

            DiscordChannel discordChannel = await server.CreateChannelAsync($"{TicketName[0]}-{TicketName[1]}", ChannelType.Text, category, overwrites: channelPermissions);

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

            welcomeBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Close", "Close"));

            await discordChannel.SendMessageAsync(welcomeBuilder);

            eventManager.TicketCreated.Invoke(this, new TicketCreatedEventArgs()
            {
                Ticket = this
            });

            return discordChannel;
        }

        private async Task<bool> GetSteamId()
        {
            SteamId = (ulong)dbManager.ObjectQuery($"SELECT steamid FROM discord_vote_rewards WHERE discordid = {owner.Id}");

            if (SteamId != 0)
            {
                DiscordMessageBuilder confirmId = new();
                confirmId.WithEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Is this the correct Steam ID?",
                    Description = $"{SteamId}"
                });
                confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{TicketId}-SteamID-Yes", "YES"));
                confirmId.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, $"{TicketId}-SteamID-No", "No"));

                DiscordMessage confirmIdMessage = await channel.SendMessageAsync(confirmId);

                InteractivityResult<ComponentInteractionCreateEventArgs> btnComplete = await confirmIdMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                if (btnComplete.TimedOut)
                {
                    eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                    {
                        TicketChannel = this
                    });
                    return false;
                }

                if (btnComplete.Result.Id == $"{TicketId}-SteamID-Yes")
                {
                    return true;
                }
            }

            DiscordEmbedBuilder requestSteamId = new()
            {
                Title = "You have not linked your account ingame",
                Color = DiscordColor.Red
            };

            //adds embed description
            requestSteamId.WithDescription("We do not have your SteamID. Please provide your SteamID bellow\n\n" +
                                           "Don't know how to find your SteamID?\n" +
                                           "1. Sign in with steam here: https://steamid.io/ \n" +
                                           "2. Copy SteamID 64bit ID\n" +
                                           "3. Send your SteamID here\n\n" +
                                           "The bot will only accept a SteamID. Please send only your Steam64ID (numbers only) without any spaces at the end.\n\n" +
                                           "***Note: Many people send the example SteamID: 76561197960287930 from the website. THIS IS NOT YOUR STEAMID. The bot will not accept this SteamID." +
                                           "Please be sure to follow the instructions in order to progress with your ticket.***");

            //loop counter
            int loopCount = 0;

            //gets steamid from user
            while (true)
            {
                //checks with user for issues with getting steamid
                if (loopCount >= 5)
                {
                    await channel.SendMessageAsync("Are you having issues?\n\nIf you are - please say `yes`\nIf you are not - please say `no`\n\nPlease copy the reply or " +
                                                   "write it exactly otherwise the bot will not recognize your response.");

                    //gets reply from correct user in correct channel
                    InteractivityResult<DiscordMessage> issueResponse = await interactivity.WaitForMessageAsync(_x => _x.Channel == channel &&
                                                                                                                      _x.Author == owner);
                    //if there is an issue will break the loop and return 0
                    if (issueResponse.Result.Content.ToLower() != "no")
                    {
                        await channel.SendMessageAsync("No problem! We will move onto the next step.");
                        return false;
                    }
                }

                //sends steam id request embed
                await channel.SendMessageAsync(requestSteamId);

                //waits for users response
                InteractivityResult<DiscordMessage> userResponse = await interactivity.WaitForMessageAsync(_x => _x.Channel == channel &&
                                                                                                                 _x.Author == owner);
                //bool for if reply is numeric
                bool responseIsNumeric;

                switch (userResponse.TimedOut)
                {
                    //checks if response is numeric no matter what length
                    case false:
                        responseIsNumeric = Regex.IsMatch(userResponse.Result.Content, @"^\d+$");
                        break;
                    //checks user reply for correct steamid
                    case true:
                        eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                        {
                            TicketChannel = this
                        });
                        return false;
                }

                if (userResponse.Result.Content.Contains("76561197960287930"))
                {
                    //checks if user entered example steamID
                    await channel.SendMessageAsync("You provided the example SteamID. Please follow the instructions");
                }
                else if (!responseIsNumeric)
                {
                    //checks if users response was numeric
                    await channel.SendMessageAsync("You did not provide your SteamID. Please ensure there are no letters or spaces in your message.");
                }
                else if (userResponse.Result.Content.Length != 17)
                {
                    //checks if users response was the length of steamID's
                    await channel.SendMessageAsync("You have not provided a valid SteamID. Please follow the instructions!");
                }
                else if (userResponse.Result.Content[..4] != "7656")
                {
                    //checks if user response contains start numbers of every steam id
                    await channel.SendMessageAsync("You have not provided a valid SteamID. Please follow the instructions!");
                }
                else if (userResponse.Result.Content[..4] == "7656")
                {
                    //successfully retrieved a steamID
                    await channel.SendMessageAsync("Thank You!");
                    SteamId = ulong.Parse(userResponse.Result.Content);
                    return true;
                }

                loopCount++;
            }
        }

        private async Task<bool> GetIngameIssue()
        {
            DiscordMessageBuilder issue = new();

            issue.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Is your issue ingame or not ingame?"
            });

            issue.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Issue-Yes", "Ingame Issue"));
            issue.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Issue-No", "Not Ingame Issue"));

            DiscordMessage issueMessage = await channel.SendMessageAsync(issue);

            InteractivityResult<ComponentInteractionCreateEventArgs> issueResponse = await issueMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (issueResponse.TimedOut)
            {
                eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                {
                    TicketChannel = this
                });
                return false;
            }

            if (issueResponse.Result.Id == $"{TicketId}-Issue-Yes")
            {
                IngameIssue = true;
                return true;
            }

            if (issueResponse.Result.Id != $"{TicketId}-Issue-No") return false;
            IngameIssue = false;
            return true;
        }

        private async Task<bool> GetCluster()
        {
            DiscordMessageBuilder cluster = new();

            cluster.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What cluster is your issue on?"
            });

            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Cluster-PVPVE", "PVPVE"));
            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Cluster-PVP", "PVP"));
            cluster.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Cluster-PVE", "PVE"));

            DiscordMessage clusterMessage = await channel.SendMessageAsync(cluster);

            InteractivityResult<ComponentInteractionCreateEventArgs> clusterResponse = await clusterMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (clusterResponse.TimedOut)
            {
                eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                {
                    TicketChannel = this
                });
                return false;
            }

            if (clusterResponse.Result.Id == $"{TicketId}-Cluster-PVPVE")
            {
                Cluster = "PVPVE";
                return true;
            }

            if (clusterResponse.Result.Id == $"{TicketId}-Cluster-PVP")
            {
                Cluster = "PVP";
                return true;
            }

            if (clusterResponse.Result.Id != $"{TicketId}-Cluster-PVE") return false;
            Cluster = "PVE";
            return true;
        }

        private async Task<bool> GetMap()
        {
            DiscordMessageBuilder map = new();

            map.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What map is your issue on?"
            });

            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Island", "Island"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Scorched_Earth", "Scorched Earth"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Aberration", "Aberration"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-The_Center", "The Center"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Ragnarok", "Ragnarok"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Extinction", "Extinction"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Valguero", "Valguero"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Genesis_1", "Genesis 1"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Crystal_Isles", "Crystal Isles"));
            map.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Map-Genesis_2", "Genesis 2"));

            DiscordMessage mapMessage = await channel.SendMessageAsync(map);

            InteractivityResult<ComponentInteractionCreateEventArgs> mapResponse = await mapMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (mapResponse.TimedOut)
            {
                eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                {
                    TicketChannel = this
                });
                return false;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Island")
            {
                Map = "Island";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Scorched_Earth")
            {
                Map = "Scorched Earth";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Aberration")
            {
                Map = "Aberration";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-The_Center")
            {
                Map = "The Center";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Ragnarok")
            {
                Map = "Ragnarok";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Extinction")
            {
                Map = "Extinction";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Valguero")
            {
                Map = "Valguero";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Genesis_1")
            {
                Map = "Genesis 1";
                return true;
            }

            if (mapResponse.Result.Id == $"{TicketId}-Map-Crystal_Isles")
            {
                Map = "Crystal Isles";
                return true;
            }

            if (mapResponse.Result.Id != $"{TicketId}-Map-Genesis_2") return false;
            Map = "Genesis 2";
            return true;
        }

        private async Task<bool> GetCategory()
        {
            DiscordMessageBuilder category = new();

            category.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "What category of issue do you have?"
            });

            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Hitlist", "Hitlist"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Orp_Pve", "Orp or Pve"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Rule_Breaks", "Rule Breaks"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Account", "Account"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Highlights", "Highlights"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Vote", "Vote"));
            category.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, $"{TicketId}-Category-Other", "Other"));

            DiscordMessage categoryMessage = await channel.SendMessageAsync(category);

            InteractivityResult<ComponentInteractionCreateEventArgs> categoryResponse = await categoryMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (categoryResponse.TimedOut)
            {
                eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                {
                    TicketChannel = this
                });
                return false;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Hitlist")
            {
                TicketCategory = "Hitlist";
                return true;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Orp_Pve")
            {
                TicketCategory = "Orp or Pve";
                return true;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Rule_Breaks")
            {
                TicketCategory = "Rule Breaks";
                return true;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Account")
            {
                TicketCategory = "Account";
                return true;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Highlights")
            {
                TicketCategory = "Highlights";
                return true;
            }

            if (categoryResponse.Result.Id == $"{TicketId}-Category-Vote")
            {
                TicketCategory = "Vote";
                return true;
            }

            if (categoryResponse.Result.Id != $"{TicketId}-Category-Other") return false;
            TicketCategory = "Other";
            return true;
        }

        private async Task<bool> HitlistCords()
        {
            //gets bot commands channel
            DiscordChannel commandsChannel = server.GetChannel(424672319756042240);

            int loopCounter = 0;
            while (true)
            {
                switch (loopCounter)
                {
                    case >= 5:
                        await channel.SendMessageAsync("We were unable to get your base cords");
                        return false;
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

                    timeout.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{TicketId}-CCC-Timeout-Yes", "Yes"));
                    timeout.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{TicketId}-CCC-Timeout-No", "No"));

                    DiscordMessage timeoutConfirmMessage = await channel.SendMessageAsync(timeout);

                    InteractivityResult<ComponentInteractionCreateEventArgs> timeoutConfirmResult = await timeoutConfirmMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

                    //Checks User reaction
                    if (timeoutConfirmResult.TimedOut || timeoutConfirmResult.Result.Id == $"{TicketId}-CCC-Timeout-No")
                    {
                        eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                        {
                            TicketChannel = this
                        });
                        return false;
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
                            HitlistCcc = string.Join(" ", cords);
                            return true;
                        }
                    }
                }

                loopCounter++;
            }
        }

        private async Task<bool> HitlistWipePrevious()
        {
            DiscordMessageBuilder wipe = new();

            wipe.WithEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Did you Wipe the previous Hitlist Tribe?"
            });

            wipe.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{TicketId}-Wipe-Yes", "Yes"));
            wipe.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, $"{TicketId}-Wipe-No", "No"));

            DiscordMessage wipeMessage = await channel.SendMessageAsync(wipe);

            InteractivityResult<ComponentInteractionCreateEventArgs> wipeResult = await wipeMessage.WaitForButtonAsync(owner, TimeSpan.FromMinutes(5));

            if (wipeResult.TimedOut)
            {
                eventManager.TicketTimeout.Invoke(this, new TicketTimeoutEventArgs
                {
                    TicketChannel = this
                });
                return false;
            }

            if (wipeResult.Result.Id == $"{TicketId}-Wipe-Yes")
            {
                HitlistWipe = true;
                return true;
            }

            if (wipeResult.Result.Id != $"{TicketId}-Wipe-No") return false;
            HitlistWipe = false;
            return true;
        }

        private async Task TicketCompleted()
        {
            int ingameIssue = IngameIssue ? 1 : 0;

            dbManager.VoidQuery($"INSERT INTO discord_tickets(ID,ChannelID,UserID,SteamID,CreatedAt,Closed,InGameIssue,Cluster,Issue)" +
                                $"VALUES ('{TicketId}','{channel.Id}','{owner.Id}',{SteamId}','{CreatedAt:yyyy/MM/dd HH:MM:ss}',0,{ingameIssue},'{Cluster}','{TicketCategory}')");

            TicketLog tickets = fileService.GetTicketLog();
            tickets.ActiveTickets.Add(this);
            fileService.SaveTicketLog(tickets);

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

        private async Task<string> GenerateURL()
        {
            Random random = new Random();

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

        private async Task CreateTranscript()
        {
            string fileName = await GenerateURL() + "_" + channel.Name[..] + ".html";
            string ticketUrl = config.TicketConfig.TicketUrl + fileName;
            string filepath = @"C:\xampp\htdocs\logs\" + fileName;
            DiscordChannel transcriptChannel = server.GetChannel(config.TicketConfig.TicketLogChannel);

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
            ticketLogEmbed.AddField("Steam ID", $"{SteamId}", true);
            ticketLogEmbed.AddField("Issue", $"{IngameIssue} - {Cluster} - {TicketCategory}", true);
            ticketLogEmbed.AddField("Created At", $"{CreatedAt}", true);
            ticketLogEmbed.AddField("Closed At", $"{ClosedAt}", true);
            ticketLogEmbed.AddField("Closed By", $"{ClosedBy.Username}", true);

            DiscordMessageBuilder ticketLogMessage = new()
            {
                Embed = ticketLogEmbed,
                Content = $"{ticketUrl}"
            };

            await transcriptChannel.SendMessageAsync(ticketLogMessage);
        }

        public async Task<bool> TicketHandler()
        {
            if (!await GetSteamId())
            {
                //throw new exception
                return false;
            }

            if (!await GetIngameIssue())
            {
                //throw new exception
                return false;
            }

            if (IngameIssue)
            {
                if (!await GetCluster())
                {
                    //throw new exception
                    return false;
                }

                if (!await GetMap())
                {
                    //throw new exception
                    return false;
                }
            }

            if (!await GetCategory())
            {
                //throw new exception
                return false;
            }

            if (TicketCategory == "Hitlist")
            {
                if (!await HitlistCords())
                {
                    //throw new exception
                    return false;
                }

                if (!await HitlistWipePrevious())
                {
                    //throw new exception
                    return false;
                }
            }

            await TicketCompleted();

            return true;
        }

        public async Task<bool> Close(DiscordUser _user)
        {
            DiscordMember member = await server.GetMemberAsync(owner.Id);

            await channel.AddOverwriteAsync(member, Permissions.None, Permissions.SendMessages);

            DiscordButtonComponent yes = new(ButtonStyle.Success, $"{TicketId}-Close-Yes", "Yes");
            DiscordButtonComponent no = new(ButtonStyle.Danger, $"{TicketId}-Close-No", "No");

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
                case var x when x == $"{TicketId}-Close-Yes":

                    await confirmMessage.DeleteAsync("Ticket Closed");

                    TicketLog tickets = fileService.GetTicketLog();
                    tickets.ActiveTickets.Remove(this);
                    fileService.SaveTicketLog(tickets);

                    ClosedBy = _user;
                    ClosedAt = DateTime.Now;

                    DiscordChannel dmChannel = await member.CreateDmChannelAsync();

                    switch (TicketComplete)
                    {
                        case false:
                            {
                                await dmChannel.SendMessageAsync("Sorry, we were unable to provide you with a transcript...\nThis is because you did not complete the ticket" +
                                                                 "setup process.");
                                break;
                            }
                        case true:

                            await CreateTranscript();

                            Dictionary<DiscordUser, string> supportMembers = new();

                            foreach (DiscordMessage discordMessage in await channel.GetMessagesAsync())
                            {
                                if (supportMembers.Keys.Contains(discordMessage.Author)) continue;

                                DiscordMember messageAuthor = await server.GetMemberAsync(discordMessage.Author.Id);

                                if (messageAuthor.Roles.Contains(server.GetRole(config.TicketConfig.AdminRole)) ||
                                    messageAuthor.Roles.Contains(server.GetRole(config.TicketConfig.PveAdminRole)) ||
                                    messageAuthor.Roles.Contains(server.GetRole(config.TicketConfig.SupportRole)))
                                {
                                    supportMembers.Add(discordMessage.Author, $"{TranscriptUrl} - {messageAuthor.DisplayName}#{messageAuthor.Discriminator} - Add Comment Here");
                                }
                            }

                            if (server.Members.ContainsKey(owner.Id))
                            {
                                string supportDonate = supportMembers.Aggregate(string.Empty, (_current, _supportMember) => _current + "\n" + _supportMember.Value);

                                DiscordMessageBuilder dmMessage = new()
                                {
                                    Content = "Thank you for creating a ticket. We hope we were able to solve your issue! Please leave feedback in our server feedback channel. " +
                                                                                $"We have attached the transcript to this message.\n{TranscriptUrl}\n\nIf you would like to thank the support team for their hard work then you can" +
                                                                                $" donate directly to them! To do this please visit {config.TicketConfig.PaypalLink}\nTo select the support member you would like the donation to go to" +
                                                                                $"please add one of the following to the comment of your donation:{supportDonate}\n\nAll donations are anonymous and are monitored."
                                };

                                await dmChannel.SendMessageAsync(dmMessage);
                            }

                            break;
                    }

                    break;

                case var x when x == $"{TicketId}-Close-No":
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

        public async Task<bool> Assign(DiscordChannel _channel)
        {
            if (!_channel.IsCategory) return false;

            await channel.ModifyAsync(_x => _x.Parent = _channel);

            return channel.ParentId == _channel.Id;
        }

        public async Task AddMembers(IEnumerable<DiscordUser> _users)
        {
            foreach (DiscordUser discordUser in _users)
            {
                await channel.AddOverwriteAsync(await server.GetMemberAsync(discordUser.Id), Permissions.AccessChannels);
            }
        }

        public async Task RemoveMembers(IEnumerable<DiscordUser> _users)
        {
            foreach (DiscordUser discordUser in _users)
            {
                await channel.AddOverwriteAsync(await server.GetMemberAsync(discordUser.Id), deny: Permissions.AccessChannels);
            }
        }
    }
}
