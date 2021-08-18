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
                if (Name.NullorEmpty() && User.Active == null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
                    return;
                }
                else if (!Name.NullorEmpty())
                {
                    var col = db.GetCollection<Actor>("Actors");

                    ulong[] users = context.Guild.GetAllMembersAsync().GetAwaiter().GetResult().Select(x => x.Id).ToArray();

                    var query = col.Find(x => x.Name.StartsWith(Name.ToLower()) && users.Contains(x.Owner));

                    if(query.Count() == 0)
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("Could not find a character in this server with that name."));
                        return;
                    }

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        query.FirstOrDefault().BuildSheet(0));
                }
                else
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        User.Active.BuildSheet(0));
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

                var query = new List<Actor>();
                if (context.User.Id == 165212654388903936)
                {
                    query = col.Find(x => x.Name.StartsWith(Name.ToLower())).ToList();
                }
                else
                {
                    query = col.Find(x => x.Name.StartsWith(Name.ToLower()) && x.Owner == context.User.Id).ToList();
                }
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
                            .WithContent("Updated **" + actor.Name + "**'s image!")
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
                    int old = actor.Vars[Variable.ToLower().Trim()];

                    actor.Vars[Variable.ToLower()] = v;
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
                        new DiscordInteractionResponseBuilder().WithContent("Updated **" + Variable.Trim() + "** variable on " + actor.Name + " from " + old + " to " + v));
                }

            }
            catch
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Something went wrong! Maybe the variable name was wrong? Be sure to use all lower-case and replace any spaces with a dash (`-`). If you wish to see all variables, use the `/help variables` command! **Remember**! All variables but Image and Color must be non-decimal numbers!."));
            }
        }

        [SlashCommand("Health","Change your active character's Health.")]
        public async Task HP(InteractionContext context, 
            [Option("Value","Value the Health is being modified by. Positive numbers increase, Negative numbers decrease.")]long Value, 
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if(num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Health = Math.Abs(num);

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Health to " + Math.Abs(num)));
                return;
            }
            else
            {
                if(actor.Health + num < 0)
                {
                    num = actor.Health;
                }
                else if (actor.Health + num > actor.Vars["health"])
                {
                    num = actor.Vars["health"] - actor.Health;
                }

                actor.Health += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value < 0 ? " took " + Math.Abs(num) + " damage!" : " regained " + Math.Abs(num) + " health!")));
            }
        }
        [SlashCommand("Armor", "Change your active character's Health.")]
        public async Task Armor(InteractionContext context,
            [Option("Value", "Value the Armor is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Armor = Math.Abs(num);

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Armor to " + Math.Abs(num)));
                return;
            }
            else
            {
                if (actor.Armor + num < 0)
                {
                    num = actor.Armor;
                }
                else if (actor.Armor + num > actor.GetTotalArmor())
                {
                    num = actor.GetTotalArmor() - actor.Armor;
                }

                actor.Armor += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "**'s armor" + (Value < 0 ? " took " + Math.Abs(num) + " damage!" : " regained " + Math.Abs(num) + " armor!")));
            }
        }
        [SlashCommand("Energy", "Change your active character's Energy.")]
        public async Task En(InteractionContext context,
            [Option("Value", "Value the Energy is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Energy = num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Energy to " + num));
                return;
            }
            else
            {

                actor.Energy += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value < 0 ? " took " + Math.Abs(num) + " energy damage!" : " regained " + Math.Abs(num) + " energy!")));
            }
        }
        [SlashCommand("Woe", "Change your active character's Woe.")]
        public async Task woe(InteractionContext context,
            [Option("Value", "Value the Woe is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Woe = Math.Abs(num);

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Woe to " + Math.Abs(num)));
                return;
            }
            else
            {
                if (actor.Woe + num < 0)
                {
                    num = actor.Woe;
                }
                else if (actor.Woe + num > 9)
                {
                    num = 9 - actor.Woe;
                }

                actor.Woe += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value > 0 ? " gained " + Math.Abs(num) + " points of Woe!" : " lost " + Math.Abs(num) + " points of Woe!")));
            }
        }
        [SlashCommand("Currency", "Change your active character's Currency.")]
        public async Task Cu(InteractionContext context,
            [Option("Value", "Value the Currency is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Vars["currency"] = num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Currency to " + num));
                return;
            }
            else
            {

                actor.Vars["currency"] += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value < 0 ? " spent " + Math.Abs(num) + " currency!" : " gained " + Math.Abs(num) + " currency!")));
            }
        }
        [SlashCommand("Material", "Change your active character's Material.")]
        public async Task mat(InteractionContext context,
            [Option("Value", "Value the material is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Vars["material"] = num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Material to " + num));
                return;
            }
            else
            {

                actor.Vars["material"] += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value < 0 ? " spent " + Math.Abs(num) + " material!" : " gained " + Math.Abs(num) + " material!")));
            }
        }
        [SlashCommand("Consumable", "Change your active character's Material.")]
        public async Task cons(InteractionContext context,
            [Option("Value", "Value the consumable is being modified by. Positive numbers increase, Negative numbers decrease.")] long Value,
            [Choice("Set",1)]
            [Choice("Modifiy",2)]
            [Option("Command","Choose whether you are Adding/Subtracting, or setting it to a fixed number.")]long Command = 2)
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

            int num = (int)Value;
            if (num == 0)
            {
                return;
            }

            if (Command == 1)
            {
                actor.Vars["consumable"] = num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Set **" + actor.Name + "**'s Consumable to " + num));
                return;
            }
            else
            {

                actor.Vars["consumable"] += num;

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("**" + actor.Name + "** " + (Value < 0 ? " spent " + Math.Abs(num) + " consumables!" : " gained " + Math.Abs(num) + " consumables!")));
            }
        }
        [SlashCommand("Restore","Fully restore Health and Energy. Use this when at sanctuaries.")]
        public async Task FullHeal(InteractionContext context)
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

            actor.Health = actor.Vars["health"];
            actor.Energy = actor.Vars["energy"];

            Utils.UpdateActor(actor);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent($"Fully restored **{actor.Name}**'s Health and Energy!"));
        }
    
    }

    [SlashCommandGroup("Talents","Add, Remove and View talents")]
    public class TalentModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Add","Add a talent to your active character.")]
        public async Task Add(InteractionContext context,[Option("Name", "Name of the Talent")]string Name)
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

            var query = Utils.GetTalent(context, User.Id, Name);

            if (query == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no Talent you got access to with that name."));
                return;
            }
            else
            {
                if (actor.Talents.Contains(query))
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You already have this talent!"));
                    return;
                }
                else
                {
                    actor.Talents.Add(query);

                    Utils.UpdateActor(actor);
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Added talent **"+query.Name+"** to " + actor.Name + "."));
                }
            }
        }
        [SlashCommand("Remove","Removes a talent from your active character.")]
        public async Task Remove(InteractionContext context, [Option("Name", "Name of the Talent")] string Name)
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

            var query = actor.Talents.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

            if (query.Count() == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(""+actor.Name+" has no talent with that name."));
                return;
            }
            else
            {
                var talent = query.FirstOrDefault();

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Are you sure you want to remove the talent " + talent.Name + " from " + actor.Name + "?")
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                        new DiscordButtonComponent(ButtonStyle.Danger,"taldel"+talent.Id,"Remove")
                    }));
            }
        }
        [SlashCommand("View","Views a Talent or Action.")]
        public async Task View (InteractionContext context, [Option("Name", "Name of the Talent or action")] string Name)
        {
            var all = Utils.GetAllActionables(context, context.User.Id);

            var query = all.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).ToList();

            if(query == null || query.Count == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Could not find any Talent or Action you have access to with that name."));
                return;
            }
            else if(query.Count > 1)
            {
                var embeds = query.Take(Math.Min(5,query.Count)).Select(x => x.BuildEmbed());
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbeds(embeds));
            }
            else
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(query.FirstOrDefault().BuildEmbed()));
            }
            
        }
    }

    [SlashCommandGroup("Items", "Add, Remove and View items")]
    public class ItemModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Add", "Add an item to your active character.")]
        public async Task Add(InteractionContext context, [Option("Name", "Name of the Item")] string Name)
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

            var query = Utils.GetItem(context, User.Id, Name);

            if (query == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no item you got access to with that name."));
                return;
            }
            else
            {
                var item = query.FirstOrDefault();
                actor.Inventory.Add(item);

                Utils.UpdateActor(actor);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Added item **" + item.Name + "** to "+actor.Name+"'s Inventory."));
            }
        }
        [SlashCommand("Remove", "Removes an item from your active character.")]
        public async Task Remove(InteractionContext context, [Option("Name", "Name of the item")] string Name)
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

            var query = actor.Inventory.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

            if (query.Count() == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("" + actor.Name + " has no item with that name."));
                return;
            }
            else
            {
                var talent = query.FirstOrDefault();

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Are you sure you want to remove the item " + talent.Name + " from " + actor.Name + "'s Inventory?")
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                        new DiscordButtonComponent(ButtonStyle.Danger,"itemdel"+talent.Id,"Remove")
                    }));
            }
        }
        [SlashCommand("View", "Views an Item.")]
        public async Task View(InteractionContext context, [Option("Name", "Name of the Item.")] string Name)
        {
            var query = Utils.GetItem(context, context.User.Id, Name);

            if (query == null || query.Count == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Could not find any item you have access to with that name."));
                return;
            }
            else if (query.Count > 1)
            {
                var embeds = query.Take(Math.Min(5, query.Count)).Select(x => x.BuildEmbed());
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbeds(embeds));
            }
            else
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(query.FirstOrDefault().BuildEmbed()));
            }

        }
        [SlashCommand("Use","Use or Equip an item")]
        public async Task Use(InteractionContext context, [Option("Name", "Name of the Item.")] string Name)
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

            var query = actor.Inventory.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

            if (query.Count() == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("" + actor.Name + " has no item with that name."));
                return;
            }
            else
            {
                var talent = query.FirstOrDefault();
                var i = actor.Inventory.IndexOf(talent);

                string message = "";
                switch (talent.Type)
                {
                    case ItemType.Asset:
                        talent.Spent = !talent.Spent;
                        switch (talent.Spent) 
                        {
                            case true:
                                message = actor.Name + " used up their **" + talent.Name + "**!";
                                break;
                            case false:
                                message = actor.Name + "'s **" + talent.Name + "** can be used again!";
                                break;
                        }
                        break;
                    default:
                        talent.Equipped = !talent.Equipped;
                        switch (talent.Equipped)
                        {
                            case true:
                                message = actor.Name + " equipped their **" + talent.Name + "**!";
                                break;
                            case false:
                                message = actor.Name + " unequipped their **" + talent.Name + "**!";
                                break;
                        }
                        break;
                }
                if(talent.Type == ItemType.Armor || talent.Type == ItemType.Shield)
                {
                    int increase = 0;

                    if(talent.Equipped && (actor.Armor + talent.Var1)> actor.GetTotalArmor())
                    {
                        increase = talent.Var1 - actor.Armor;
                    }
                    else if (!talent.Equipped && (actor.Armor-talent.Var1) > 0)
                    {
                        increase = actor.Armor;
                    }
                    else
                    {
                        increase = talent.Var1;
                    }

                    switch (talent.Equipped)
                    {
                        case true:
                            actor.Armor += increase;
                            break;
                        case false:
                            actor.Armor -= increase;
                            break;
                    }
                }
                actor.Inventory[i] = talent;

                Utils.UpdateActor(actor);
                
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(message));
            }
        }
    }
    [SlashCommandGroup("Conditions","Manage conditions")]
    public class ConditionModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;
        
        [SlashCommand("Add","Add a condition to your active character.")]
        public async Task add(InteractionContext context, [Option("Name","Name of the condition")]string Name,[Option("Skill","Skill to penalize. Use 'All' to affect all rolls.")]string Skill, [Option("Penalty","Penalty being applied.")]long Penalty, [Option("Discipline","Discipline to Penalize. If set, the Skill becomes locked rather than penalized.")]Overwrite Discipline = Overwrite.None)
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

            if(Dictionaries.SubSkills.TryGetValue(Skill.ToLower(),out string parsed))
            {
                if(actor.Conditions.Exists(x=>x.Name.ToLower() == Name.ToLower()))
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(actor.Name+" already has a condition with that exact name!"));
                    return;
                }

                Condition c = new Condition()
                {
                    Name = Name,
                    Penalty = Math.Abs((int)Penalty),
                    Skill = Skill,
                    Discipline = Discipline.ToString().ToLower()
                };

                actor.Conditions.Add(c);

                Utils.UpdateActor(actor);

                string message = "";

                if (Discipline != Overwrite.None)
                {
                    message = actor.Name + " is now afflected by the **" + Name + "** burden!";
                }
                else
                {
                    message = actor.Name + " is now afflected by the **" + Name + "** condition!";
                }

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(message));
            }
        }

        [SlashCommand("Remove","Remove a condition from your active character.")]
        public async Task remove(InteractionContext context, [Option("Name", "Name of the condition")] string Name)
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

            var query = actor.Conditions.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).ToList();

            if(query.Count == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(actor.Name+" has no conidtion with that name!"));
                return;
            }
            else
            {
                var con = query.FirstOrDefault();

                var i = actor.Conditions.IndexOf(con);

                actor.Conditions.RemoveAt(i);

                Utils.UpdateActor(actor);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Removed condition "+con.Name+" from "+actor.Name + "."));
                return;
            }
        }
    }

}
