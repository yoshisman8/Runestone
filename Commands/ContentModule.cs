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
    [SlashCommandGroup("Corebook", "Manage the Corebook content (Only usable by Vyklade)")]
    public class ContentModule : ApplicationCommandModule
    {
        

        [SlashCommandGroup("Talents","Manage Talents")]
        public class TalentsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create","Creates/Updates a new Corebook Talent")]
            public async Task CorebookNew(InteractionContext context, [Option("Name","Name of the Talent")] string Name, 
                [Option("Type","What kind of Talent is this?")]ActionType Type, 
                [Option("Resource","What resource, if any, does this talent use?")]Costs Resource, 
                [Option("Cost","How much of a resource does it cost? Type 0 If none")]long Cost,
                [Option("Skill", "What skill or discipline check does this Talent need? Use 'None' or 'Any' as needed.")]string Roll,
                [Option("Description", "The Talent's Description. Use the `<line>` symbol to add new lines.")]string Description)
            {
                if (context.User.Id != 165212654388903936) return;

                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Actionable>("Actionables");

                if(col.Exists(x=>x.Name == Name && x.Core && x.Talent))
                {
                    var Talent = col.FindOne(x => x.Name == Name && x.Core && x.Talent);

                    Talent.Action = Type;
                    Talent.Cost = Resource;
                    Talent.Amount = (int)Math.Floor((decimal)Cost);
                    Talent.Skill = Roll;
                    Talent.Description = Description.Replace("<line>","\n");

                    col.Update(Talent);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing core talent **" + Talent.Name + "**.").AddEmbed(Talent.BuildEmbed()));
                    return;
                }
                else
                {
                    Actionable talent = new Actionable()
                    {
                        Author = context.User.Id,
                        Core = true,
                        Talent = true,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Cost = Resource,
                        Amount = (int)Math.Floor((decimal)Cost),
                        Action = Type
                    };

                    if(Roll.ToLower() != "none" && Roll.ToLower() != "any" && !Dictionaries.Skills.TryGetValue(Roll.ToLower(),out string v))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("The value `" + Roll.ToLower() + "` is not a valid skill or discipline."));
                        return;
                    }

                    talent.Skill = Roll.ToLower();

                    col.Insert(talent);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created Corebook talent **"+talent.Name+"**.").AddEmbed(talent.BuildEmbed()));
                }
            }

            [SlashCommand("Delete","Deletes a talent from the corebook. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name","Name of the Talent being deleted.")]string Talent)
            {
                if (context.User.Id != 165212654388903936) return;
                var col = db.GetCollection<Actionable>("Actionables");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Core && x.Talent).ToList();

                if(query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no Corebook talent whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** Corebook Talent?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Tdl"+talent.Id,"Delete Talent")
                        }));
                }
            }
        }

        [SlashCommandGroup("Actions", "Manage Talents")]
        public class ActionsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create", "Creates/Updates a new Corebook Action")]
            public async Task CorebookNew(InteractionContext context, [Option("Name", "Name of the Action")] string Name,
                [Option("Type", "What kind of Action is this?")] ActionType Type,
                [Option("Resource", "What resource, if any, does this action use?")] Costs Resource,
                [Option("Cost", "How much of a resource does it cost? Type 0 If none")] long Cost,
                [Option("Skill", "What skill or discipline check does this action need? Use 'None' or 'Any' as needed.")] string Roll,
                [Option("Description", "The action's Description. Use the `<line>` symbol to add new lines.")] string Description)
            {
                if (context.User.Id != 165212654388903936) return;

                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Actionable>("Actionables");

                if (col.Exists(x => x.Name == Name && x.Core))
                {
                    var Talent = col.FindOne(x => x.Name == Name && x.Core);

                    Talent.Action = Type;
                    Talent.Cost = Resource;
                    Talent.Amount = (int)Math.Floor((decimal)Cost);
                    Talent.Skill = Roll;
                    Talent.Description = Description.Replace("<line>", "\n"); ;

                    col.Update(Talent);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing core action **" + Talent.Name + "**.").AddEmbed(Talent.BuildEmbed()));
                    return;
                }
                else
                {
                    Actionable talent = new Actionable()
                    {
                        Author = context.User.Id,
                        Core = true,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Cost = Resource,
                        Amount = (int)Math.Floor((decimal)Cost),
                        Action = Type
                    };

                    if (Roll.ToLower() != "none" && Roll.ToLower() != "any" && !Dictionaries.Skills.TryGetValue(Roll.ToLower(), out string v))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("The value `" + Roll.ToLower() + "` is not a valid skill or discipline."));
                        return;
                    }

                    talent.Skill = Roll.ToLower();

                    col.Insert(talent);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created Corebook action **" + talent.Name + "**.").AddEmbed(talent.BuildEmbed()));
                }
            }

            [SlashCommand("Delete", "Deletes an action from the corebook. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name", "Name of the action being deleted.")] string Talent)
            {
                if (context.User.Id != 165212654388903936) return;
                var col = db.GetCollection<Actionable>("Actionables");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Core).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no Corebook action whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** Corebook Action?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Tdl"+talent.Id,"Delete Action")
                        }));
                }
            }
        }

        [SlashCommandGroup("Items", "Manage Items")]
        public class ItemsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create", "Creates/Updates a new Corebook Item")]
            public async Task CorebookNew(InteractionContext context, [Option("Name", "Name of the Item")] string Name,
                [Option("Type", "What kind of Item is this?")] ItemType Type,
                [Option("Value1", "For weapons: Damage. For Armor/Shields: Armor value. For Assets: it's ignored.")] long Value1,
                [Option("Value2", "For weapons: Range. For Armor/Shields: Check Penalty. For Assets: it's ignored.")] long Value2,
                [Option("Description", "The Item's Description. Use the `<line>` symbol to add new lines.")] string Description)
            {
                if (context.User.Id != 165212654388903936) return;
                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Item>("Items");

                if (col.Exists(x => x.Name == Name && x.Core))
                {
                    var Item = col.FindOne(x => x.Name == Name && x.Core);

                    Item.Var1 = (int)Math.Floor((double)Value1);
                    Item.Var2 = (int)Math.Floor((double)Value2);
                    Item.Description = Description.Replace("<line>", "\n");

                    col.Update(Item);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing core Item **" + Item.Name + "**.").AddEmbed(Item.BuildEmbed()));
                    return;
                }
                else
                {
                    Item Item = new Item()
                    {
                        Author = context.User.Id,
                        Core = true,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Type = Type,
                        Var1 = (int)Math.Floor((double)Value1),
                        Var2 = (int)Math.Floor((double)Value2)
                    };

                    col.Insert(Item);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created Corebook item **" + Item.Name + "**.").AddEmbed(Item.BuildEmbed()));
                }
            }

            [SlashCommand("Delete", "Deletes an Item from the corebook. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name", "Name of the Item being deleted.")] string Talent)
            {
                if (context.User.Id != 165212654388903936) return;
                var col = db.GetCollection<Item>("Items");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Core).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no Corebook Item whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** Corebook item?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Idl"+talent.Id,"Delete Item")
                        }));
                }
            }
        }
    }

    [SlashCommandGroup("Homebrew", "Manage your Homebrew content.")]
    public class HomebrewContentModule : ApplicationCommandModule
    {


        [SlashCommandGroup("Talents", "Manage Talents")]
        public class TalentsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create", "Creates/Updates a new Talent")]
            public async Task CorebookNew(InteractionContext context, [Option("Name", "Name of the Talent")] string Name,
                [Option("Type", "What kind of Talent is this?")] ActionType Type,
                [Option("Resource", "What resource, if any, does this talent use?")] Costs Resource,
                [Option("Cost", "How much of a resource does it cost? Type 0 If none")] long Cost,
                [Option("Skill", "What skill or discipline check does this Talent need?  Use 'None' or 'Any' as needed.")] string Roll,
                [Option("Description", "The Talent's Description. Use the `<line>` symbol to add new lines.")] string Description)
            {

                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Actionable>("Actionables");

                if (col.Exists(x => x.Name == Name && x.Core && x.Talent))
                {
                    var Talent = col.FindOne(x => x.Name == Name && x.Core && x.Talent);

                    Talent.Action = Type;
                    Talent.Cost = Resource;
                    Talent.Amount = (int)Math.Floor((decimal)Cost);
                    Talent.Skill = Roll;
                    Talent.Description = Description.Replace("<line>", "\n");

                    col.Update(Talent);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing homebrew talent **" + Talent.Name + "**.").AddEmbed(Talent.BuildEmbed()));
                    return;
                }
                else
                {
                    Actionable talent = new Actionable()
                    {
                        Author = context.User.Id,
                        Talent = true,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Cost = Resource,
                        Amount = (int)Math.Floor((decimal)Cost),
                        Action = Type
                    };

                    if (Roll.ToLower() != "none" && Roll.ToLower() != "any" && !Dictionaries.Skills.TryGetValue(Roll.ToLower(), out string v))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("The value `" + Roll.ToLower() + "` is not a valid skill or discipline."));
                        return;
                    }

                    talent.Skill = Roll.ToLower();

                    col.Insert(talent);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created homebrew talent **" + talent.Name + "**.").AddEmbed(talent.BuildEmbed()));
                }
            }

            [SlashCommand("Delete", "Deletes a talent you made. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name", "Name of the Talent being deleted.")] string Talent)
            {
                
                var col = db.GetCollection<Actionable>("Actionables");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Author == context.User.Id && x.Talent).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no homebrew talent whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** homebrew Talent?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Tdl"+talent.Id,"Delete Talent")
                        }));
                }
            }
        }

        [SlashCommandGroup("Actions", "Manage Talents")]
        public class ActionsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create", "Creates/Updates a new Action")]
            public async Task CorebookNew(InteractionContext context, [Option("Name", "Name of the Action")] string Name,
                [Option("Type", "What kind of Action is this?")] ActionType Type,
                [Option("Resource", "What resource, if any, does this action use?")] Costs Resource,
                [Option("Cost", "How much of a resource does it cost? Type 0 If none")] long Cost,
                [Option("Skill", "What skill or discipline check does this action need?  Use 'None' or 'Any' as needed.")] string Roll,
                [Option("Description", "The action's Description. Use the `<line>` symbol to add new lines.")] string Description)
            {
                

                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Actionable>("Actionables");

                if (col.Exists(x => x.Name == Name && x.Author == context.User.Id))
                {
                    var Talent = col.FindOne(x => x.Name == Name && x.Author==context.User.Id);

                    Talent.Action = Type;
                    Talent.Cost = Resource;
                    Talent.Amount = (int)Math.Floor((decimal)Cost);
                    Talent.Skill = Roll;
                    Talent.Description = Description.Replace("<line>", "\n"); ;

                    col.Update(Talent);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing action **" + Talent.Name + "**.").AddEmbed(Talent.BuildEmbed()));
                    return;
                }
                else
                {
                    Actionable talent = new Actionable()
                    {
                        Author = context.User.Id,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Cost = Resource,
                        Amount = (int)Math.Floor((decimal)Cost),
                        Action = Type
                    };

                    if (Roll.ToLower() != "none" && Roll.ToLower() != "any" && !Dictionaries.Skills.TryGetValue(Roll.ToLower(), out string v))
                    {
                        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("The value `" + Roll.ToLower() + "` is not a valid skill or discipline."));
                        return;
                    }

                    talent.Skill = Roll.ToLower();

                    col.Insert(talent);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created homebrew action **" + talent.Name + "**.").AddEmbed(talent.BuildEmbed()));
                }
            }

            [SlashCommand("Delete", "Deletes an action you made. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name", "Name of the action being deleted.")] string Talent)
            {
                
                var col = db.GetCollection<Actionable>("Actionables");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Author == context.User.Id).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no homebrew actions whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** homebrew Action?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Tdl"+talent.Id,"Delete Action")
                        }));
                }
            }
        }

        [SlashCommandGroup("Items", "Manage Items")]
        public class ItemsManage
        {
            public Services.Utilities Utils;
            public LiteDatabase db;

            [SlashCommand("Create", "Creates/Updates a new Item")]
            public async Task CorebookNew(InteractionContext context, [Option("Name", "Name of the Item")] string Name,
                [Option("Type", "What kind of Item is this?")] ItemType Type,
                [Option("Value1", "For weapons: Damage. For Armor/Shields: Armor value. For Assets: it's ignored.")] long Value1,
                [Option("Value2", "For weapons: Range. For Armor/Shields: Check Penalty. For Assets: it's ignored.")] long Value2,
                [Option("Description", "The Item's Description. Use the `<line>` symbol to add new lines.")] string Description)
            {

                var user = Utils.GetUser(context.User.Id);

                var col = db.GetCollection<Item>("Items");

                if (col.Exists(x => x.Name == Name))
                {
                    var Item = col.FindOne(x => x.Name == Name);

                    Item.Var1 = (int)Math.Floor((double)Value1);
                    Item.Var2 = (int)Math.Floor((double)Value2);
                    Item.Description = Description.Replace("<line>", "\n");

                    col.Update(Item);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Updated existing core Item **" + Item.Name + "**.").AddEmbed(Item.BuildEmbed()));
                    return;
                }
                else
                {
                    Item Item = new Item()
                    {
                        Author = context.User.Id,
                        Name = Name,
                        Description = Description.Replace("<line>", "\n"),
                        Type = Type,
                        Var1 = (int)Math.Floor((double)Value1),
                        Var2 = (int)Math.Floor((double)Value2)
                    };

                    col.Insert(Item);
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.EnsureIndex(x => x.Author);
                    col.EnsureIndex(x => x.Core);
                    col.EnsureIndex(x => x.Author);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Created homebrew item **" + Item.Name + "**.").AddEmbed(Item.BuildEmbed()));
                }
            }

            [SlashCommand("Delete", "Deletes an Item you made. Warning! This may cause issues with existing characters!")]
            public async Task DelCoreTalent(InteractionContext context, [Option("Name", "Name of the Talent being deleted.")] string Talent)
            {
                var col = db.GetCollection<Item>("Items");

                var query = col.Find(x => x.Name.StartsWith(Talent.ToLower()) && x.Author == context.User.Id).ToList();

                if (query.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("There are no homebrew Item whose name starts with that."));
                    return;
                }
                else
                {
                    var talent = query.FirstOrDefault();

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Are you sure you want to delete the **" + talent.Name + "** Corebook item?\n**WARNING**: It may take a while for the bot to parse all existing characters to remove this talent.")
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"cancel","Cancel"),
                            new DiscordButtonComponent(ButtonStyle.Danger,"Idl"+talent.Id,"Delete Item")
                        }));
                }
            }
        }
    }
}
