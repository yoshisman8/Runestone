using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Runestone.Collections;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Runestone.Services
{
    public class ButtonService
    {
        private LiteDatabase database;
        private Utilities utils;
        public ButtonService(DiscordClient client, LiteDatabase _db, Utilities _utils)
        {
            database = _db;
            utils = _utils;
        }


        public async Task HandleButtonAsync(DiscordClient c, ComponentInteractionCreateEventArgs e)
        {
            var u = utils.GetUser(e.User.Id);
            if (e.Id == "cancel")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().WithContent("Operation Cancelled!"));
            }
            else if (e.Id.ToLower() == "endenc")
            {
                var encounter = utils.GetEncounter(e.Channel.Id);

                encounter.Active = false;
                encounter.Started = false;
                encounter.Current = null;
                encounter.Combatants = new List<Combatant>();
                encounter.Narrator = 0;
                encounter.Refresh = true;

                utils.UpdateEncounter(encounter);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Encounter over!"));

                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.Id.ToString()));

                foreach (var file in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Data", e.Channel.Id.ToString())))
                {
                    File.Delete(file);
                }
            }
            else if (e.Id.StartsWith("s"))
            {
                string[] args = e.Id.Split(",");
                int page = int.Parse(args[1]);

                int id = int.Parse(args[2]);

                var col = database.GetCollection<Actor>("Actors");

                var C = col.FindById(id);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, C.BuildSheet(page));
            }
            
            else if (e.Id.StartsWith("dl"))
            {
                int id = int.Parse(e.Id.Substring(2));

                var col = database.GetCollection<Actor>("Actors");

                var C = col.FindById(id);

                var User = utils.GetUser(e.User.Id);

                col.Delete(id);

                if (User.Active.Id == id)
                {
                    User.Active = null;
                    utils.UpdateUser(User);
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .WithContent("Character **" + C.Name + "** has been deleted. If this was your active character, you no longer have an active character."));
                return;
            }
            else if (e.Id.StartsWith("Tdl"))
            {
                int id = int.Parse(e.Id.Substring(3));

                var col = database.GetCollection<Actionable>("Actionables");

                var actors = database.GetCollection<Actor>("Actors");

                var C = col.FindById(id);



                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Deleted Talent **" + C.Name+"**. The bot will now attempt to remove this talent from all registered characters. This may take a moment."));

                col.Delete(id);

                var All = actors.Include(x=>x.Talents).FindAll().ToList();

                for (int i = 0; i < All.Count; i++)
                {
                    var a = All[i];
                    if (a.Talents.Any(x=>x.Id == C.Id))
                    {
                        var index = a.Talents.FindIndex(x => x.Id == C.Id);
                        a.Talents.RemoveAt(index);
                        utils.UpdateActor(a);
                    }
                }   

            }
            else if (e.Id.StartsWith("Idl"))
            {
                int id = int.Parse(e.Id.Substring(3));

                var col = database.GetCollection<Item>("Items");

                var actors = database.GetCollection<Actor>("Actors");

                var C = col.FindById(id);



                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Deleted Item **" + C.Name + "**. The bot will now attempt to remove this Item from all registered characters. This may take a moment."));

                col.Delete(id);

                var All = actors.Include(x => x.Talents).FindAll().ToList();

                for (int i = 0; i < All.Count; i++)
                {
                    var a = All[i];
                    if (a.Inventory.Any(x => x.Id == C.Id))
                    {
                        var index = a.Inventory.FindIndex(x => x.Id == C.Id);
                        a.Inventory.RemoveAt(index);
                        utils.UpdateActor(a);
                    }
                }

            }
            else if (e.Id.StartsWith("taldel"))
            {
                int id = int.Parse(e.Id.Substring(6));

                var col = database.GetCollection<Actionable>("Actionables");

                var actors = database.GetCollection<Actor>("Actors");

                var a = u.Active;

                var i = a.Talents.FindIndex(x => x.Id == id);

                var t = a.Talents[i];

                a.Talents.RemoveAt(i);

                utils.UpdateActor(a);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Removed talent/action **" + t.Name + "** from " + a.Name + "."));
            }
            else if (e.Id.StartsWith("itemdel"))
            {
                int id = int.Parse(e.Id.Substring(7));

                var col = database.GetCollection<Item>("Items");

                var actors = database.GetCollection<Actor>("Actors");

                var a = u.Active;

                var i = a.Inventory.FindIndex(x => x.Id == id);

                var t = a.Inventory[i];

                a.Inventory.RemoveAt(i);

                utils.UpdateActor(a);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Removed item **" + t.Name + "** from " + a.Name + "."));
            }
            else if (e.Id.StartsWith("boost"))
            {
                RollData data = new RollData().Deserialize(e.Id.Substring(5));

                var actors = database.GetCollection<Actor>("Actors");

                var a = actors.FindById(data.Actor);

                a.Energy -= 1;

                a.Woe++;

                utils.UpdateActor(a);

                data.Boosts++;

                var Embed = utils.EmbedRoll(data);

                string serial = data.Serialize();

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent($"{a.Name}'s Resolve is being tested...\nSpent {data.Boosts} Energy and Woe boosting!")
                    .AddEmbed(Embed)
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"boost"+serial,"Boost",false, new DiscordComponentEmoji(875526328500232203))
                    }));
            }
            else if (e.Id.StartsWith("init"))
            {
                RollData data = new RollData().Deserialize(e.Id.Substring(4));

                var actors = database.GetCollection<Actor>("Actors");

                var a = actors.FindById(data.Actor);

                a.Energy -= 1;

                utils.UpdateActor(a);

                data.Boosts++;

                var enc = utils.GetEncounter(data.Encounter);

                var I = enc.Combatants.FindIndex(x => x.Actor == data.Actor);

                if(enc.Combatants[I] == enc.Current)
                {
                    enc.Current.Initiative++;
                    enc.Combatants[I].Initiative++;
                }

                utils.UpdateEncounter(enc);

                var Embed = utils.EmbedRoll(data);

                string serial = data.Serialize();

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Spent " + data.Boosts + " energy boosting!")
                    .AddEmbed(Embed)
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"init"+serial,"Boost",false, new DiscordComponentEmoji(875526327774617671))
                    }));
            }
            else if (e.Id.StartsWith("Q"))
            {
                var args = e.Id.Split(".");

                var col = database.GetCollection<Actor>("Actors");

                int choice = int.Parse(args[1]);

                RollData data = new RollData().Deserialize(args[2]);

                var actor = col.FindById(data.Actor);

                if (data.Skill == "any")
                {
                    switch (choice)
                    {
                        case 0:
                            data.Skill = "exploration";
                            break;
                        case 1:
                            data.Skill = "survival";
                            break;
                        case 2:
                            data.Skill = "combat";
                            break;
                        case 3:
                            data.Skill = "social";
                            break;
                        case 4:
                            data.Skill = "magic";
                            break;
                    }
                    var skills = Dictionaries.SubSkills.Where(x => x.Value == data.Skill.ToLower()).OrderBy(x => x.Key).Select(x => x.Key).ToList();

                    var Buttons = new List<DiscordButtonComponent>();
                    var Response = new DiscordInteractionResponseBuilder().WithContent("This action or talent can use any skill of the " + data.Skill + " discipline. Please choose which skill to use.");
                    
                    string serial = data.Serialize();

                    if (skills.Count > 5)
                    {

                        int index = 0;
                        int loops = (int)Math.Ceiling((double)skills.Count / (double)5);

                        for (int l = 0; l < loops; l++)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                if (index >= skills.Count) continue;
                                Buttons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, "Q." + index + "." + serial, skills[index].FirstCharToUpper()));
                                index++;
                            }
                            Response.AddComponents(Buttons);
                            Buttons.Clear();
                        }

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                            Response);
                        return;
                    }
                    else
                    {
                        for (int i = 0; i < skills.Count; i++)
                        {
                            Buttons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, "Q." + i + "." + serial, skills[i].FirstCharToUpper()));
                        }
                        Response.AddComponents(Buttons);

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                            Response);
                        return;
                    }
                    
                }
                else
                {
                    var skills = Dictionaries.SubSkills.Where(x => x.Value == data.Skill.ToLower()).OrderBy(x => x.Key).Select(x => x.Key).ToList();

                    data.Skill = skills[choice];

                    data.Judgement -= actor.Vars[data.Skill];

                    if (actor.Conditions.Any(x => x.Discipline != "none" && x.Skill.ToLower() == data.Skill.ToLower()))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(actor.Name + " cannot use this skill due to a condition!"));
                        return;
                    }

                    if (actor.Conditions.Count > 0)
                    {
                        data.Modifiers -= utils.ProcessConditions(data.Skill, actor);
                    }

                    var embed = utils.EmbedRoll(data);

                    string serial = data.Serialize();

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed)
                        .AddComponents(new DiscordComponent[]
                        {
                        new DiscordButtonComponent(ButtonStyle.Primary,"boost"+serial,"Boost",false, new DiscordComponentEmoji(875526327774617671))
                        }));
                } 
            }
            
        }
    }
}
