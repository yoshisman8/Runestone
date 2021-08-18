using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Runestone.Services;
using LiteDB;
using Runestone.Collections;
using Dice;
using Newtonsoft.Json;

namespace Runestone.Commands
{
    public class RollingCommands : ApplicationCommandModule
    {
        public Services.Utilities Utils;
        public LiteDatabase db;

        [SlashCommand("Check","Rolls a single skill or discipline check")]
        public async Task Check(InteractionContext context, [Option("Skill","Skill or Discipline being rolled")]string Skill,[Option("Modifier","Additional modifiers, if any.")]long Modifier = 0, [Option("Overwrite","(OPpional)Overwrite the Discipline for this skill.")]Overwrite overwrite = Overwrite.None)
        {
			var User = Utils.GetUser(context.User.Id);

			if (User.Active == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
				return;
			}

			Actor actor = User.Active;
			if(Skill.ToLower().Trim() == "woe" || Skill.ToLower().Trim() == "tested")
            {
				var dice = Roller.Roll("1d20");
				var data = new Runestone.Collections.RollData()
				{
					Action = -1,
					Actor = actor.Id,
					Judgement = 13,
					Fortune = 17,
					Skill = "tested",
					Discipline = "t",
					Dice = (int)dice.Value,
					Boosts = 0,
					Encounter = 0,
					Modifiers = 0
				};

				var embed = Utils.EmbedRoll(data);
				var serial = data.Serialize();

				actor.Woe = 0;
				Utils.UpdateActor(actor);

				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.WithContent($"{actor.Name}'s resolve is being tested... (Woe reset to 0).")
					.AddEmbed(embed)
					.AddComponents(new DiscordComponent[]
					{
						new DiscordButtonComponent(ButtonStyle.Primary,"boost"+serial,"Boost",false, new DiscordComponentEmoji(875526328500232203))
					}));
				return;
			}
			else if(Dictionaries.Skills.TryGetValue(Skill.ToLower().Trim(),out string value))
            {
				if(actor.Conditions.Any(x=>x.Discipline != "none" && x.Skill.ToLower() == Skill.ToLower().Trim()))
                {
					await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
						.WithContent(actor.Name+" cannot use this skill due to a condition!"));
					return;
				}

				int mod = (int)Modifier;

				if(actor.Conditions.Count > 0)
                {
					mod -= Utils.ProcessConditions(Skill.Trim(), actor);
                }

				var dice = Roller.Roll("1d20");

				int judgement = 8;

				if (overwrite != Overwrite.None)
				{
					judgement = actor.Vars[overwrite.ToString()];
				}

				int fortune = actor.Vars[value];
				

                if (Dictionaries.SubSkills.ContainsKey(Skill.ToLower().Trim()))
                {
					judgement = Math.Max(2, 8-actor.Vars[Skill.ToLower().Trim()]);
                }

				Collections.RollData data = new Collections.RollData()
				{
					Action = -1,
					Skill = Skill.Trim(),
					Dice = (int)dice.Value,
					Fortune = fortune,
					Judgement = judgement,
					Modifiers = mod,
					Discipline = overwrite.ToString(),
					Actor = actor.Id
				};
				var embed = Utils.EmbedRoll(data);
				var serial = data.Serialize();

				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.AddEmbed(embed)
					.AddComponents(new DiscordComponent[]
					{
						new DiscordButtonComponent(ButtonStyle.Primary,"boost"+serial,"Boost",false, new DiscordComponentEmoji(875526327774617671))
					}));

			}
            else
            {
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.WithContent("Could not find a skill named `"+Skill.Trim() + "`. Be sure to use the full skill name, replacing any spaces with dashes."));
				return;
			}
			
			
		}

		[SlashCommand("Act", "Perform an action. Resources will be automatically spent.")]
		public async Task Act(InteractionContext context, [Option("Action", "Action or Talent being performed")] string Act, [Option("Modifier", "Additional modifiers, if any.")] long Modifier = 0, [Option("Overwrite", "(OPpional)Overwrite the Discipline for this skill.")] Overwrite overwrite = Overwrite.None)
		{
			var User = Utils.GetUser(context.User.Id);

			if (User.Active == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.WithContent("You currently have no Active user. Use the `/Character Select` command to select a character."));
				return;
			}

			Actor actor = User.Active;

			var action = Utils.Act(context, context.User.Id, Act.Trim());

			if (action == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder()
					.WithContent("There is no Action or Talent you have access to with that name."));
				return;
			}
			else if (action.Skill == "none")
			{
				switch (action.Cost)
				{
					case Costs.Consumable:
						if (actor.Vars["consumable"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough consumables for this action!"));
							return;
						}
						else
						{
							actor.Vars["consumable"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Currency:
						if (actor.Vars["currency"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough currency for this action!"));
							return;
						}
						else
						{
							actor.Vars["currency"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Material:
						if (actor.Vars["material"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough material for this action!"));
							return;
						}
						else
						{
							actor.Vars["material"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Health:
						if (actor.Health - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough health for this action!"));
							return;
						}
						else
						{
							actor.Health -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Energy:
						actor.Energy -= action.Amount;
						Utils.UpdateActor(actor);
						break;
					case Costs.Woe:
						actor.Woe += action.Amount;
						if (actor.Woe >= 9)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " has maxed out their Woe wheel!"));
							actor.Woe = 9;
						}
						Utils.UpdateActor(actor);
						break;
					default:
						break;
				}
                if (action.Action == ActionType.Action)
                {
					int extracost = Utils.ExtraCost(context.Channel.Id, actor);
                    if (extracost == 0)
                    {
						actor.Energy -= extracost;
						Utils.UpdateActor(actor);
                    }
                }
				var embed = action.BuildEmbed(actor.Color, actor.Name + " does \"" + action.Name + "\"!");

				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
					new DiscordInteractionResponseBuilder().AddEmbed(embed));
			}
			else
			{

				switch (action.Cost)
				{
					case Costs.Consumable:
						if (actor.Vars["consumable"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough consumables for this action!"));
							return;
						}
						else
						{
							actor.Vars["consumable"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Currency:
						if (actor.Vars["currency"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough currency for this action!"));
							return;
						}
						else
						{
							actor.Vars["currency"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Material:
						if (actor.Vars["material"] - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough material for this action!"));
							return;
						}
						else
						{
							actor.Vars["material"] -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Health:
						if (actor.Health - action.Amount < 0)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " doesn't have enough health for this action!"));
							return;
						}
						else
						{
							actor.Health -= action.Amount;
							Utils.UpdateActor(actor);
						}
						break;
					case Costs.Energy:
						actor.Energy -= action.Amount;
						Utils.UpdateActor(actor);
						break;
					case Costs.Woe:
						actor.Woe += action.Amount;
						if (actor.Woe >= 9)
						{
							await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
								new DiscordInteractionResponseBuilder()
								.WithContent(actor.Name + " has maxed out their Woe wheel!"));
							actor.Woe = 9;
						}
						Utils.UpdateActor(actor);
						break;
					default:
						break;
				}
				if (action.Action == ActionType.Action)
				{
					int extracost = Utils.ExtraCost(context.Channel.Id, actor);
					if (extracost > 0)
					{
						actor.Energy -= extracost;
						Utils.UpdateActor(actor);
					}
				}
				int mod = (int)Modifier;

				var dice = Roller.Roll("1d20");

				int judgement = 8;
				int fortune = 18;

				Collections.RollData data = new Collections.RollData()
				{
					Action = action.Id,
					Dice = (int)dice.Value,
					Fortune = fortune,
					Judgement = judgement,
					Modifiers = mod,
					Discipline = overwrite.ToString(),
					Actor = actor.Id
				};


				if (Dictionaries.SubSkills.TryGetValue(action.Skill.ToLower(), out string discipline))
				{
					data.Skill = action.Skill.ToLower();

					data.Judgement = Math.Max(2, 8 - actor.Vars[action.Skill.ToLower()]);

					if (actor.Conditions.Any(x => x.Discipline != "none" && x.Skill.ToLower() == action.Skill.ToLower() ))
					{
						await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
						new DiscordInteractionResponseBuilder()
							.WithContent(actor.Name + " cannot use this skill due to a condition!"));
						return;
					}

					if (actor.Conditions.Count > 0)
					{
						data.Modifiers -= Utils.ProcessConditions(action.Skill.ToLower(), actor);
					}

					if (overwrite != Overwrite.None)
					{
						fortune = actor.Vars[overwrite.ToString()];
					}
                    else
                    {
						fortune = actor.Vars[discipline];
                    }

					var embed = Utils.EmbedRoll(data);
					var serial = data.Serialize();

					await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
						new DiscordInteractionResponseBuilder()
						.AddEmbed(embed)
						.AddComponents(new DiscordComponent[]
						{
						new DiscordButtonComponent(ButtonStyle.Primary,"boost"+serial,"Boost",false, new DiscordComponentEmoji(875526327774617671))
						}));
				}
				else if(action.Skill == "any")
                {
					data.Skill = action.Skill;

					var serial = data.Serialize();

					var Response = new DiscordInteractionResponseBuilder().WithContent("Choose which Discipline to use (You will choose the skill afterwards).");

					var buttons = new DiscordButtonComponent[]
					{
						new DiscordButtonComponent(ButtonStyle.Secondary,"Q."+0+"."+serial,"Exploration"),
						new DiscordButtonComponent(ButtonStyle.Secondary,"Q."+1+"."+serial,"Survival"),
						new DiscordButtonComponent(ButtonStyle.Secondary,"Q."+2+"."+serial,"Combat"),
						new DiscordButtonComponent(ButtonStyle.Secondary,"Q."+3+"."+serial,"Social"),
						new DiscordButtonComponent(ButtonStyle.Secondary,"Q."+4+"."+serial,"Magic"),
					};

					Response.AddComponents(buttons);

					await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
						Response);
				}
				else
				{
					data.Skill = action.Skill;

					var serial = data.Serialize();

					var skills = Dictionaries.SubSkills.Where(x => x.Value == action.Skill.ToLower()).OrderBy(x => x.Key).Select(x => x.Key).ToList();
					var Buttons = new List<DiscordButtonComponent>();
					var Response = new DiscordInteractionResponseBuilder().WithContent("This action or talent can use any skill of the " + action.Skill + " discipline. Please choose which skill to use.");


					if (skills.Count > 5)
					{
						int index = 0;
						int loops = (int)Math.Ceiling((double)skills.Count / (double)5);
						for (int l = 0; l < loops; l++)
						{
							for (int i = 0; i < 5; i++)
							{
								if (index >= skills.Count) break;
								Buttons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, "Q." + index + "." + serial, skills[index].FirstCharToUpper()));
								index++;
							}
							Response.AddComponents(Buttons);
							Buttons.Clear();
						}
					}
                    else
                    {
						for(int i = 0; i < skills.Count; i++)
                        {
							Buttons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, "Q." + i + "." + serial, skills[i].FirstCharToUpper()));
						}
						Response.AddComponents(Buttons);
                    }

					await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
						Response);
				}
			}
		}
	}
    public enum Overwrite
    {
        [ChoiceName("No Overwrite")]
        None = 0,
        [ChoiceName("Exploration")]
        exploration =1,
        [ChoiceName("Survival")]
        survival = 2,
        [ChoiceName("Combat")]
        combat = 3,
        [ChoiceName("Social")]
        social = 4,
        [ChoiceName("Magic")]
        magic = 5
    }
}
