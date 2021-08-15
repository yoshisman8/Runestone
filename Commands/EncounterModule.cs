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
using Dice;
using System.IO;

namespace Runestone.Commands
{
    [SlashCommandGroup("Combat","Combat Commands")]
    public class EncounterModule : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Turn","Ends the current turn and pings the next person in initiative.")]
        public async Task Next(InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);
            if (!encounter.Active || (!encounter.Active && !encounter.Started))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel."));
                return;
            }

            if (encounter.Current.Actor > -1)
            {
                var actor = Utils.GetActor(encounter.Current.Actor);
                if(actor.Owner != context.User.Id && encounter.Narrator != context.User.Id)
                {
                    var u = await context.Guild.GetMemberAsync(actor.Owner);
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent("Only (" + u.DisplayName + ") or the Narrator can end "+actor.Name+"'s turn!"));
                    return;
                }
            }
            else if (encounter.Narrator != context.User.Id)
            {
                var u = await context.Guild.GetMemberAsync(encounter.Narrator);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator (" + u.DisplayName + ") can end Adverary turns!"));
                return;
            }

            int I = encounter.Combatants.FindIndex(x=>x.Name == encounter.Current.Name);

            if(I + 1 >= encounter.Combatants.Count)
            {
                encounter.Current = encounter.Combatants[0];
            }
            else
            {
                encounter.Current = encounter.Combatants[I+1];
            }
            encounter.Combatants[I].Actions = 0;

            Utils.UpdateEncounter(encounter);

            string mention = "";
            if (encounter.Current.player)
            {
                var actor = Utils.GetActor(encounter.Current.Actor);
                var user = await context.Guild.GetMemberAsync(actor.Owner);
                mention = user.Mention + ", It is " + actor.Name + "'s turn!";
            }
            else
            {
                var user = await context.Guild.GetMemberAsync(encounter.Narrator);
                mention = user.Mention + ", It is " + encounter.Current.Name + "'s turn!"; ;
            }
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Loading..."));

            var embed = Utils.EmbedCombat(encounter.Id, true);

            var Map = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", context.Channel.Id.ToString(), $"{context.Channel.Id}-battlemap.png"), FileMode.Open);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(mention)
                .AddFile(Map)
                .AddEmbed(embed));
            Map.Close();
        }
        [SlashCommand("Previous", "Rolls back to the previous turn and pings the previous person in initiative.")]
        public async Task Prev(InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);
            if (!encounter.Active || (!encounter.Active && !encounter.Started))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel."));
                return;
            }

            if (encounter.Narrator != context.User.Id)
            {
                var u = await context.Guild.GetMemberAsync(encounter.Narrator);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator (" + u.DisplayName + ") can rewind turns!"));
                return;
            }

            int I = encounter.Combatants.FindIndex(x => x.Name == encounter.Current.Name);

            if (I - 1 < 0)
            {
                encounter.Current = encounter.Combatants.Last();
            }
            else
            {
                encounter.Current = encounter.Combatants[I - 1];
            }

            encounter.Combatants[I].Actions = 0;

            Utils.UpdateEncounter(encounter);

            string mention = "";
            if (encounter.Current.player)
            {
                var actor = Utils.GetActor(encounter.Current.Actor);
                var user = await context.Guild.GetMemberAsync(actor.Owner);
                mention = user.Mention + ", It is " + actor.Name + "'s turn!";
            }
            else
            {
                var user = await context.Guild.GetMemberAsync(encounter.Narrator);
                mention = user.Mention + ", It is " + encounter.Current.Name + "'s turn!"; ;
            }
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Loading..."));

            var embed = Utils.EmbedCombat(encounter.Id, true);

            var Map = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", context.Channel.Id.ToString(), $"{context.Channel.Id}-battlemap.png"), FileMode.Open);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(mention)
                .AddFile(Map)
                .AddEmbed(embed));
            Map.Close();
        }
        [SlashCommand("View","View the current Encounter.")]
        public async Task View (InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);
            if (!encounter.Active || (!encounter.Active && !encounter.Started))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel."));
                return;
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Loading..."));

            var embed = Utils.EmbedCombat(encounter.Id, true);

            var Map = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", context.Channel.Id.ToString(), $"{context.Channel.Id}-battlemap.png"), FileMode.Open);

            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("")
                .AddFile(Map)
                .AddEmbed(embed));
            Map.Close();
        }

        [SlashCommand("Start", "Starts an encounter in the current channel. Use this again to begin the encounter.")]
        public async Task Start(InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (encounter.Active && context.User.Id != encounter.Narrator)
            {
                var u = await context.Guild.GetMemberAsync(encounter.Narrator);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator (" + u.DisplayName + ") can initiate this encounter."));
                return;
            }
            else if (encounter.Active && context.User.Id == encounter.Narrator && !encounter.Started)
            {

                if (encounter.Combatants.Count == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("There are no combatants in this encounter!"));
                    return;
                }

                encounter.Started = true;
                encounter.Combatants = encounter.Combatants.OrderByDescending(x => x.Initiative).ToList();
                encounter.Current = encounter.Combatants[0];

                Utils.UpdateEncounter(encounter);

                string mention = "";
                if (encounter.Current.player)
                {
                    var actor = Utils.GetActor(encounter.Current.Actor);
                    var user = await context.Guild.GetMemberAsync(actor.Owner);
                    mention = user.Mention + ", It is " + actor.Name + "'s turn!";
                }
                else
                {
                    var user = await context.Guild.GetMemberAsync(encounter.Narrator);
                    mention = user.Mention + ", It is " + encounter.Current.Name + "'s turn!"; ;
                }
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Loading..."));

                var embed = Utils.EmbedCombat(encounter.Id, true);

                var Map = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", context.Channel.Id.ToString(), $"{context.Channel.Id}-battlemap.png"), FileMode.Open);

                await context.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(mention)
                    .AddFile(Map)
                    .AddEmbed(embed));
                Map.Close();
            }
            else if (encounter.Active && encounter.Started)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("The encounter already started!"));
                return;
            }
            else if (!encounter.Active)
            {
                encounter.Narrator = context.Member.Id;
                encounter.Combatants = new List<Combatant>();
                encounter.Current = null;
                encounter.Started = false;
                encounter.Active = true;

                Utils.UpdateEncounter(encounter);
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(context.User.Username + " has started an encounter!")
                    .AddEmbed(Utils.EmbedCombat(encounter.Id,true)));
            }
        }
        [SlashCommand("End","Ends the current encounter.")]
        public async Task End(InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (!encounter.Active)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel!"));
                return;
            }
            else
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Clear the boarda and end the encounter?")
                    .AddComponents(new DiscordComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Primary,"cancel","No"),
                        new DiscordButtonComponent(ButtonStyle.Danger,"endenc","End Encounter")
                    }));
            }
        }
        [SlashCommand("Join","Join the encounter as a player.")]
        public async Task Join(InteractionContext context,
            [Choice("Upper Edge",1)]
            [Choice("North-Western Flank",2)]
            [Choice("North-Eastern Flank",3)]
            [Choice("Inner Edge",4)]
            [Choice("Heat",5)]
            [Choice("Outer-Edge",6)]
            [Choice("South-Western Flank",7)]
            [Choice("South-Eastern Flank",8)]
            [Choice("Lower Edge",9)]
            [Option("Tile","Which Tile are you Joining on?")]long Tile)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (!encounter.Active)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel!"));
                return;
            }

            var User = Utils.GetUser(context.User.Id);

            if (User.Active == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
                return;
            }

            var actor = User.Active;

            if (encounter.Combatants.Any(x=>x.Actor == actor.Id))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You're already participating in this encounter! Use `/Combat Move` if you wish to move to a different tile!"));
                return;
            }

            var dice = Roller.Roll("1d20");

            var data = new Collections.RollData()
            {
                Action = -1,
                Actor = actor.Id,
                Boosts = 0,
                Dice = (int)dice.Value,
                Discipline = "n",
                Fortune = 0,
                Encounter = encounter.Id,
                Judgement = 0,
                Modifiers = Math.Max(actor.Vars["agility"], actor.Vars["insight"]),
                Skill = "initiative"
            };

            var comb = new Combatant()
            {
                Actor = actor.Id,
                Name = actor.Name,
                player = true,
                Tile = (int)Tile,
                Image = actor.Image.NullorEmpty()? "https://media.discordapp.net/attachments/722857470657036299/725046172175171645/defaulttoken.png" : actor.Image,
                Initiative = data.Dice + data.Modifiers + data.Boosts + (data.Modifiers*0.1)
            };

            encounter.Combatants.Add(comb);

            encounter.Combatants = encounter.Combatants.OrderByDescending(x => x.Initiative).ToList();
            encounter.Refresh = true;

            Utils.PrepareToken(comb.Image, comb, encounter.Id);

            Utils.UpdateEncounter(encounter);

            string serial = data.Serialize();

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent(actor.Name + " has joined the encounter in tile "+ Dictionaries.TileNames[(int)Tile] + "!")
                .AddComponents(new DiscordComponent[] 
                {
                    new DiscordButtonComponent(ButtonStyle.Primary,"init"+serial,"Boost",false,new DiscordComponentEmoji(875526327774617671))
                })
                .AddEmbed(Utils.EmbedRoll(data)));
        }
        [SlashCommand("Add", "Add a Combatant (Narrator Only).")]
        public async Task add(InteractionContext context,[Option("Name","Name of the Adversary")]string Name,
            [Choice("Upper Edge",1)]
            [Choice("North-Western Flank",2)]
            [Choice("North-Eastern Flank",3)]
            [Choice("Inner Edge",4)]
            [Choice("Heat",5)]
            [Choice("Outer-Edge",6)]
            [Choice("South-Western Flank",7)]
            [Choice("South-Eastern Flank",8)]
            [Choice("Lower Edge",9)]
            [Option("Tile","Tile this Adversary is joining on.")]long Tile,
            [Option("Initiative","Initiative value of the adversary.")]long Initiative,
            [Option("Image","Image Url for the Adversary Token")]string Image)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (!encounter.Active)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel!"));
                return;
            }

            if(encounter.Narrator != context.User.Id)
            {
                var user = await context.Guild.GetMemberAsync(encounter.Narrator);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator ("+user.Username+") can add Adversaries!"));
                return;
            }

            if (encounter.Combatants.Where(x => x.Tile == (int)Tile).Count() >= 5)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This tile is overcrowded!"));
                return;
            }

            var comb = new Combatant()
            {
                Actor = -1,
                Name = Name,
                player = false,
                Tile = (int)Tile,
                Initiative = Initiative,
                Image = Image
            };

            encounter.Combatants.Add(comb);

            encounter.Combatants = encounter.Combatants.OrderByDescending(x => x.Initiative).ToList();
            encounter.Refresh = true;

            Utils.PrepareToken(comb.Image, comb, encounter.Id);

            Utils.UpdateEncounter(encounter);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Added Adversary "+Name+" to the encounter in tile "+ Dictionaries.TileNames[(int)Tile] + "!"));
        }
        [SlashCommand("Move","Move to a different Tile")]
        public async Task Move(InteractionContext context, [Choice("Upper Edge",1)]
            [Choice("North-Western Flank",2)]
            [Choice("North-Eastern Flank",3)]
            [Choice("Inner-Edge",4)]
            [Choice("Heat",5)]
            [Choice("Outer-Edge",6)]
            [Choice("South-Western Flank",7)]
            [Choice("South-Eastern Flank",8)]
            [Choice("Lower-Edge",9)]
            [Option("Tile","Tile this Adversary is joining on.")]long Tile,[Option("Name","Name of the Combatant (Narrator Only)")]string Name = null)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (!encounter.Active)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel!"));
                return;
            }
            if(!Name.NullorEmpty() && encounter.Narrator != context.User.Id)
            {
                var user = await context.Guild.GetMemberAsync(encounter.Narrator);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator (" + user.Username + ") can move Adversaries!"));
                return;
            }

            var User = Utils.GetUser(context.User.Id);

            if (User.Active == null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
                return;
            }

            var actor = User.Active;

            if(!encounter.Combatants.Any(x=>x.Actor == actor.Id))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(actor.Name+" is not in this Encounter! Use `/Combat Join` to join in!"));
                return;
            }
            if (Name.NullorEmpty())
            {
                int i = encounter.Combatants.FindIndex(x => x.Actor == actor.Id);

                if(encounter.Combatants.Where(x=>x.Tile == (int)Tile).Count() >= 5)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This tile is overcrowded!"));
                    return;
                }

                encounter.Combatants[i].Tile = (int)Tile;
                encounter.Refresh = true;
                Utils.UpdateEncounter(encounter);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent(actor.Name + " Moved to the **"+Dictionaries.TileNames[(int)Tile]+"**!"));
                return;
            }
            else
            {
                if (encounter.Combatants.Where(x => x.Tile == (int)Tile).Count() >= 5)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("This tile is overcrowded!"));
                    return;
                }

                var query = encounter.Combatants.Where(x=>x.Name.ToLower().StartsWith(Name.ToLower()));

                if(query.Count() == 0)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no combatant in this encounter with that name!"));
                    return;
                }
                else
                {
                    var Com = query.FirstOrDefault();

                    var i = encounter.Combatants.FindIndex(x=>x.Name == Com.Name);

                    encounter.Combatants[i].Tile = (int)Tile;
                    encounter.Refresh = true;
                    Utils.UpdateEncounter(encounter);

                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                        .WithContent(Com.Name + " Moved to the **" + Dictionaries.TileNames[(int)Tile] + "**!"));
                    return;
                }
            }
        }
    
        [SlashCommand("Remove","Removes a combatant from the encounter (Narrator Only).")]
        public async Task Remove(InteractionContext context, [Option("Name","Name of the combatant.")]string Name)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            if (!encounter.Active)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("There is no active encounter in this channel!"));
                return;
            }

            if (encounter.Narrator != context.User.Id)
            {
                var user = await context.Guild.GetMemberAsync(encounter.Narrator);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Only the narrator (" + user.Username + ") can add Adversaries!"));
                return;
            }

            var query = encounter.Combatants.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

            if (query.Count() == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("There is no combatant in this encounter with that name!"));
                return;
            }

            var com = query.FirstOrDefault();

            var I = encounter.Combatants.FindIndex(x => x.Name == com.Name);

            if(encounter.Current.Name == com.Name)
            {
                if (I - 1 < 0)
                {
                    encounter.Current = encounter.Combatants[I + 1];
                }
                else
                {
                    encounter.Current = encounter.Combatants[I - 1];
                }
            }
            encounter.Combatants.Remove(com);

            encounter.Combatants = encounter.Combatants.OrderByDescending(x => x.Initiative).ToList();

            encounter.Refresh = true;
            Utils.UpdateEncounter(encounter);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Removed combatant "+com.Name+" from the encounter!"));
            return;
        }

        [SlashCommand("Test","Tests out the Combat rendering (Warning! This overrides the current channel's combat!")]
        public async Task Test(InteractionContext context)
        {
            var encounter = Utils.GetEncounter(context.Channel.Id);

            encounter.Combatants = new List<Combatant>();

            var user = Utils.GetUser(context.User.Id);

            var actor = user.Active;

            if (actor == null) return;
            
            for(int t = 1; t <= 9; t++)
            {
                for(int i = 0; i < 5; i++)
                {
                    encounter.Combatants.Add(new Combatant()
                    {
                        Image = actor.Image,
                        Initiative = t+i,
                        Name = actor.Name+t+i,
                        Tile = t,
                        Actor = actor.Id
                    });
                }
            }
            encounter.Refresh = true;
            encounter.Current = encounter.Combatants[0];
            encounter.Active = true;
            encounter.Started = true;
            encounter.Combatants = encounter.Combatants.OrderByDescending(x => x.Initiative).ToList();

            Utils.UpdateEncounter(encounter);

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                    .WithContent("Loading..."));

            var Start = DateTime.Now;

            var embed = Utils.EmbedCombat(encounter.Id, true);

            var Map = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", context.Channel.Id.ToString(), $"{context.Channel.Id}-battlemap.png"), FileMode.Open);

            var finish = DateTime.Now;

            var diff =  finish- Start;
            await context.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Done! Took: {diff.Minutes}M:{diff.Seconds}S:{diff.Milliseconds}MS")
                .AddFile(Map)
                .AddEmbed(embed));

            Map.Close();
        }
    }
}
