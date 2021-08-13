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
    public class CharacterModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Character","View, Create, Delete or Select a character!")]
        public async Task Character(InteractionContext context, [Option("Action","Action to perform")]CharCommands Action = CharCommands.View, [Option("Name","Name of the Character")]string Name = null)
        {
            var User = Utils.GetUser(context.User.Id);

            if(Action == CharCommands.View)
            {
                if (User.Active == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
                    return;
                }
                else
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        User.Active.BuildSheet(0)
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_main_page","Main Page"),
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_skills_page","Skills")
                        }));
                }
            }
            else if( Action == CharCommands.Select)
            {
                if (Name.NullorEmpty())
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                            new DiscordInteractionResponseBuilder()
                                            .WithContent("Please type the name of a character to select!"));
                    return;
                }
                var col = db.GetCollection<Actor>("Actors");
                
                var query = col.Find(x => x.Name.StartsWith(Name.ToLower()) && x.Owner ==context.User.Id).ToList();

                if(query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Could not find any character you own with that name."));
                    return;
                }
                else
                {
                    var Character = query.FirstOrDefault();

                    User.Active = Character;

                    Utils.UpdateUser(User);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed( new DiscordEmbedBuilder().WithDescription(context.User.Mention+" is now playing as **" + Character.Name + "**!")
                        .WithThumbnail(Character.Image)
                        .WithTitle("Changed active character")));
                }
            }
            else if (Action == CharCommands.Create)
            {
                if (Name.NullorEmpty())
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                            new DiscordInteractionResponseBuilder()
                                            .WithContent("Please type the name of a character to create!"));
                    return;
                }

                var col = db.GetCollection<Actor>("Actors");

                if(col.Exists(x=>x.Owner==context.User.Id && x.Name == Name.ToLower()))
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                            new DiscordInteractionResponseBuilder()
                                            .WithContent("You already have a character with this exact name!"));
                    return;
                }

                Actor actor = new Actor()
                {
                    Owner = context.User.Id,
                    Name = Name
                };
                var id = col.Insert(actor);
                col.EnsureIndex("Name", "LOWER($.Name)");

                User.Active = col.FindById(id);

                Utils.UpdateUser(User);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(context.User.Mention+", Created character **"+Name+"** and assigned it as your active character!"));
            }
            else if(Action == CharCommands.Delete)
            {
                if (Name.NullorEmpty())
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                            new DiscordInteractionResponseBuilder()
                                            .WithContent("Please type the name of a character to select!"));
                    return;
                }
                var col = db.GetCollection<Actor>("Actors");

                var query = col.Find(x => x.Name.StartsWith(Name.ToLower()) && x.Owner == context.User.Id).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Could not find any character you own with that name."));
                    return;
                }
                else 
                {
                    var C = query.FirstOrDefault();
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Are you sure you want to delete **" + C.Name + "**? (This cannot be undone!)")
                    .AddComponents( new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                        new DiscordButtonComponent(ButtonStyle.Danger,"dl"+C.Id,"Delete")
                    }
                    ));
                }
                
            }
        }
        public enum CharCommands
        {
            [ChoiceName("View Character")]
            View = 0,
            [ChoiceName("Select Character")] 
            Select =1,
            [ChoiceName("Create Character")] 
            Create =2,
            [ChoiceName("Delete Character")]
            Delete = 3
        }
        
        [SlashCommand("Set","Sets a variable on your Active Character.")]
        public async Task Set(InteractionContext context,
            [Option("Variable","Variable to Change")]string Variable, 
            [Option("Value","Value to change the variable to.")]string Value)
        {
            var User = Utils.GetUser(context.User.Id);

            if (User.Active == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
                return;
            }

            var actor = User.Active;

            try
            {
                if(Variable.ToLower() == "image")
                {
                    if (Value.IsImageUrl())
                    {
                        actor.Image = Value;

                        Utils.UpdateActor(actor);

                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("Updated **" + actor + "**'s image!")
                            .AddEmbed(new DiscordEmbedBuilder().WithImageUrl(Value).Build()));
                        return;
                    }
                    else
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("This value is not a valid `.png` or `.jpeg` image URL!"));
                        return;
                    }
                }
                else if (Variable.ToLower() == "color")
                {
                    try
                    {
                        var color = new DiscordColor(Value);

                        actor.Color = color.ToString();
                        Utils.UpdateActor(actor);
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .AddEmbed( new DiscordEmbedBuilder().WithColor(color).WithDescription("Changed **"+actor.Name+"**'s Sheet color to "+color.ToString()+"!")));
                    }
                    catch
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("This value is not a valid Hex color code (#AABBCC)."));
                        return;
                    }
                }
                else
                {
                    
                    int v = Math.Abs(int.Parse(Value));
                    int old = actor.Vars[Variable.ToLower()];

                    actor.Vars[Variable] = v;
                    if(Variable == "health")
                    {
                        actor.Health = actor.Vars["health"];
                    }
                    if (Variable == "energy")
                    {
                        actor.Energy = actor.Vars["energy"];
                    }
                    Utils.UpdateActor(actor);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("Updated **" + Variable + "** variable on " + actor.Name + " from " + old + " to " + v));
                }

            }
            catch
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Something went wrong! Maybe the variable name was wrong? Be sure to use all lower-case and replace any spaces with a dash (`-`). If you wish to see all variables, use the `/help variables` command! **Remember**! All variables but Image and Color must be non-decimal numbers!."));
            }
        }
    }
}
