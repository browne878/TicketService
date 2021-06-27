namespace Ticket.Services.Services.BotService.Commands
{
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using Ticket.Services.Services.BotService.Commands.PreCommandChecks;

    public class TicketCommands : SlashCommandModule
    {
        [SlashCommand("close", "Closes the current ticket or a defined ticket")]
        public async Task CloseTicket(InteractionContext _ctx,
                                      [Option("Channel", "The Ticket Channel you would like to close")]
                                      DiscordChannel _channel = null)
        {
            await _ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await _ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("5 second delay complete!"));
        }

        [SlashCommand("assign", "Sets who can deal with the ticket")]
        [RequireRole(new ulong[] { 815355998935842827, 815355998935842827, 815356034415329280 })]
        public async Task AssignTicket(InteractionContext _ctx,
                                       [Option("Role", "Role that can deal with the ticket")]
                                       DiscordRole _role)
        {
            await _ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await _ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("5 second delay complete!"));
        }

        [SlashCommand("add", "Adds a User to the ticket")]
        [RequireRole(new ulong[] { 815355998935842827, 815355998935842827, 815356034415329280 })]
        public async Task AddUser(InteractionContext _ctx,
                                  [Option("User1", "The User you want to add to the ticket")]
                                  DiscordUser _user1,
                                  [Option("User2", "The User you want to add to the ticket")]
                                  DiscordUser _user2 = null,
                                  [Option("User3", "The User you want to add to the ticket")]
                                  DiscordUser _user3 = null,
                                  [Option("User4", "The User you want to add to the ticket")]
                                  DiscordUser _user4 = null,
                                  [Option("User5", "The User you want to add to the ticket")]
                                  DiscordUser _user5 = null,
                                  [Option("User6", "The User you want to add to the ticket")]
                                  DiscordUser _user6 = null,
                                  [Option("User7", "The User you want to add to the ticket")]
                                  DiscordUser _user7 = null)
        {
            await _ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await _ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("5 second delay complete!"));
        }

        [SlashCommand("remove", "Removes a User to the ticket")]
        [RequireRole(new ulong[] { 815355998935842827, 815355998935842827, 815356034415329280 })]
        public async Task RemoveUser(InteractionContext _ctx,
                                     [Option("User1", "The User you want to remove from the ticket")]
                                     DiscordUser _user1,
                                     [Option("User2", "The User you want to remove from the ticket")]
                                     DiscordUser _user2 = null,
                                     [Option("User3", "The User you want to remove from the ticket")]
                                     DiscordUser _user3 = null,
                                     [Option("User4", "The User you want to remove from the ticket")]
                                     DiscordUser _user4 = null,
                                     [Option("User5", "The User you want to remove from the ticket")]
                                     DiscordUser _user5 = null,
                                     [Option("User6", "The User you want to remove from the ticket")]
                                     DiscordUser _user6 = null,
                                     [Option("User7", "The User you want to remove from the ticket")]
                                     DiscordUser _user7 = null)
        {
            await _ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await Task.Delay(5000);
            await _ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("5 second delay complete!"));
        }
    }
}