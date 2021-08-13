using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Runestone.Services;
using LiteDB;
using Runestone.Collections;


namespace Runestone.Commands
{
    public class HelpModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Help", "View more info on specific commands.")]
        public async Task Help(InteractionContext context, [Option("Topic","Help Topic to show.")]HelpTopics Topic = HelpTopics.List)
        {
            switch (Topic)
            {
                case HelpTopics.Variables:
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Variable List")
                        .WithDescription("These are the exact names of all variables you can change using the `/set` command.")
                        .AddField("Main Attributes","`health`, `energy`, `vigor`, `agility`, `insight`, `presence`.")
                        .AddField("Disciplines","`exploration`, `survival`, `combat`, `social`, `magic`.")
                        .AddField("Skills", "`Awareness`, `Balance`, `Cartography`, `Climb`, `Cook`, `Jump`, `Lift`, `Reflex`, `Craft`, `Forage`, `Fortitude`, `Heal`, `Nature`, `Sneak`, `Swim`, `Track`, `Aim`, `Defend`, `Fight`, `Maneuver`, `Empathy`, `Handle-Animal`, `Influence`, `Intimidate`, `Lead`, `Negotiate`, `Perform`, `Resolve`, `Control`, `Create`, `Maim`, `Mend`.")
                        .AddField("Sheet details","• `image`: This is an image URL used as the sheet's character image.\n• `Color`: This is a Hex color value that is displayed when you open your sheet.")
                        .Build()
                        ));
                    break;
                case HelpTopics.Comabt:
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()

                        .Build()
                        ));
                    break;
                case HelpTopics.Homebrew:
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()

                        .Build()
                        ));
                    break;
                case HelpTopics.List:

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()

                        .Build()
                        ));


                    break;
            }
        }

        [SlashCommand("Invite","Invite Runestone to your server!")]
        public async Task Invite(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(" ")
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordLinkButtonComponent("https://discord.com/api/oauth2/authorize?client_id=875437273183817779&permissions=259846044736&scope=applications.commands%20bot","Invite")
                }));
        }
    }

    public enum HelpTopics
    {
        [ChoiceName("List all Commands")] List,
        [ChoiceName("List all character Variables")] Variables,
        [ChoiceName("Show how to manage Homebrew content")] Homebrew, 
        [ChoiceName("Show how to use the Combat tracker")]Comabt
    }
}
