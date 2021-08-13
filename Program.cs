using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using Runestone.Services;
using Runestone.Commands;

namespace Runestone
{
    class Program
    {
        private static string Token = "";
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            // Create the Data folder if it isn't already existing
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data"));
            
            // Attempt to read the Token file, throws an error if it's not there.
            try { 
                Token = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", "token.txt"));
            }catch
            {
                Console.WriteLine("No Token file found! Please create a file called \"token.txt\" in the Data folder.");
            }

            // Configure the discord client entitiy.    
            var client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LogTimestampFormat = "MMM dd yyyy - hh:mm:ss tt",
                MinimumLogLevel = LogLevel.Debug    
            });
            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(new LiteDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database.db")))
                .AddSingleton<Services.Utilities>()
                .AddSingleton<ButtonService>()
                .BuildServiceProvider();
            
            var Slash = client.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = services
            });

            Slash.RegisterCommands<CharacterModule>(875387462552289331);
            Slash.RegisterCommands<HelpModule>(875387462552289331);

            client.ComponentInteractionCreated += services.GetService<ButtonService>().HandleButtonAsync;

            await client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}
