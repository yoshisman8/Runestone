using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LiteDB;
using Runestone.Services;
using Runestone.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class Actionable
    {
        [BsonId]
        public int Id { get; set; }
        public ulong Author { get; set; }
        public bool Core { get; set; } = false;
        public string Name { get; set; }
        public Costs Cost { get; set; }
        public string Skill { get; set; }
        public bool Talent { get; set; } = false;
        public int Amount { get; set; }
        public string Description { get; set; }
        public ActionType Action { get; set; }


        public DiscordEmbed BuildEmbed(string color = "",string Overwrite = null)
        {
            var builder = new DiscordEmbedBuilder()
                .WithTitle(Overwrite.NullorEmpty()?Name:Overwrite);
            var sb = new StringBuilder();

            sb.Append("**(");
            if (!Skill.NullorEmpty() && Skill != "none")
            {
                sb.Append(Skill.FirstCharToUpper() + " | ");
            }
            if(Cost != Costs.None)
            {
                sb.Append(Dictionaries.Icons[Cost.ToString()] + Amount + " | ");
            }
            sb.Append(Action.ToString() + ")**\n\n");

            sb.AppendLine(Description);
            builder.WithFooter(Core ? "Corebook Talent" : "Homebrew Talent");
            builder.WithDescription(sb.ToString());
            if (!color.NullorEmpty())
            {
                builder.WithColor(new DiscordColor(color));
            }
            return builder.Build();
        }
        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            if (!Skill.NullorEmpty())
            {
                sb.Append(Skill.FirstCharToUpper() + " | ");
            }
            if (Cost != Costs.None)
            {
                sb.Append(Dictionaries.Icons[Cost.ToString()] + Amount + " | ");
            }
            sb.Append(Action.ToString() + ")\n");

            sb.AppendLine(Description);
            return sb.ToString();
        }
    }

    public enum Costs { [ChoiceName("This Talent/Action uses Energy")]Energy,
        [ChoiceName("This Talent/Action uses Health")] Health,
        [ChoiceName("This Talent/Action uses Woe")] Woe,
        [ChoiceName("This Talent/Action uses Material")] Material,
        [ChoiceName("This Talent/Action uses Consumable")] Consumable,
        [ChoiceName("This Talent/Action uses Currency")] Currency,
        [ChoiceName("This Talent/Action uses Downtime Slots")]Downtime,
        [ChoiceName("This Talent/Action uses no resource")] None }
    public enum ActionType { [ChoiceName("This is an Action")] Action,
        [ChoiceName("This is an Reaction")] Reaction,
        [ChoiceName("This is an Passive Talent or Action")] Passive,
        [ChoiceName("This is an Free Action")] Free,
        [ChoiceName("This is an Downtime/Exploration action")] Traversal }
}
