using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MsgTruck
{
    internal class InteractionSender
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly InteractionService _interactionService;
        private readonly ILogger _logger;
        private bool _ready;
        private const string _internalErrorMessage
            = "> *Oh no! An internal error occurred. Check the log, admins!*";

        internal InteractionSender(DiscordSocketClient client, IServiceProvider serviceProvider, ILogger logger)
        {
            _client = client;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _interactionService = new InteractionService(_client.Rest);
        }
        internal async Task InitAsync()
        {
            if (_ready) return;
            await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            //GUILD to register command
            await _interactionService.RegisterCommandsGloballyAsync();
            _client.InteractionCreated += SendInteraction;
            _interactionService.InteractionExecuted += _interactionService_InteractionExecuted;
            await _logger.LogAsync("*** INTERACTION POWER LAUNCHED! *** :: Now you can use your command :)", LogSeverity.Info);
            _ready = true;
        }
        private async Task _interactionService_InteractionExecuted(ICommandInfo commandInfo, IInteractionContext interactionContext, IResult result)
        {
            if (!result.IsSuccess)
            {
                if (interactionContext.Interaction.HasResponded)
                {
                    await interactionContext.Interaction.FollowupAsync(_internalErrorMessage, ephemeral: true);
                }
                else
                {
                    await interactionContext.Interaction.RespondAsync(_internalErrorMessage, ephemeral: true);
                }
                //do not even try to  use RespondAsync never ever here
                await _logger.LogAsync($"({result.Error}) : {result.ErrorReason}", LogSeverity.Error);

                if (result is ExecuteResult exResult)
                {
                    var exception = exResult.Exception;
                    await _logger.LogAsync($"{exception.Message} - {exception.InnerException}", LogSeverity.Error);
#if DEBUG
                    if(exception.StackTrace != null) await _logger.LogAsync(exception.StackTrace, LogSeverity.Error);
#endif
                }
            }
        }

        private async Task SendInteraction(SocketInteraction interaction)
        {
            try
            {
                var scope = _serviceProvider.CreateScope();
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);

            }
            catch (Exception ex)
            {
                await _logger.LogAsync("Interaction failed: " + ex.Message, LogSeverity.Error);
            }
        }
    }
}