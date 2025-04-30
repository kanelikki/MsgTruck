using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

//Invite with permission 534723819584

[assembly:InternalsVisibleTo("MsgTruck.Tests")]
namespace MsgTruck
{
    /// <summary>
    /// Main entry of the bot. Use this for starting the bot.
    /// </summary>
    public class Truck
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly InteractionSender _interactionSender;
        public Truck(ILogger logger)
        {

            _logger = logger;
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
            };
            var collection = new ServiceCollection();
            collection.AddSingleton(logger);
            _client = new DiscordSocketClient(config);
            _serviceProvider = collection.BuildServiceProvider();
            _interactionSender = new InteractionSender(_client, _serviceProvider, _logger);
        }

        public async Task StartAsync()
        {
            _client.Connected += async ()
                => await _logger.LogAsync("[!] DO NOT USE any command to this bot yet until INTERACTION POWER is launched [!]", LogSeverity.Info);
            _client.Log += Log;
            _client.Ready += _interactionSender.InitAsync;

            try
            {
                var token = await TokenReader.ReadTokenAsync(_logger);
                if (string.IsNullOrEmpty(token))
                {
                    return;
                }
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message, LogSeverity.Critical);
                throw;
            }

            await Task.Delay(-1);
        }
        private Task Log(LogMessage msg) => _logger.LogAsync(msg.ToString(), msg.Severity);
    }
    }
