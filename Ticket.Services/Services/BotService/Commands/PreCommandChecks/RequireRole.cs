namespace Ticket.Services.Services.BotService.Commands.PreCommandChecks
{
    using System.Linq;
    using System.Threading.Tasks;
    using DSharpPlus.SlashCommands;

    public class RequireRole : SlashCheckBaseAttribute
    {
        public ulong[] Ids;

        public RequireRole(ulong[] _ids) => Ids = _ids;

        public override Task<bool> ExecuteChecksAsync(InteractionContext _ctx) => Task.FromResult(Ids.Any(_id => _ctx.Member.Roles.Any(_role => _role.Id == _id)));
            
    }
}