using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;
using DSharpPlus.Entities;
using Runestone.Services;

namespace Runestone.Collections
{
    public class Actor
    {
        [BsonId]
        public int Id { get; set; }
        public ulong Owner { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Image { get; set; }
        public string Color { get; set; } = "#696866";
        public int Health { get; set; } = 10;
        public int Energy { get; set; } = 10;
        public int Woe { get; set; }
        public int Armor { get; set; }
        public Dictionary<string, int> Vars { get; set; } = new Dictionary<string, int>()
        {
            {"health",10 },
            {"energy",10 },
            {"level",1 },
            {"vigor",1 },
            {"agility",1 },
            {"insight",1 },
            {"presence",1 },
            {"exploration",18 },
            {"survival",18 },
            {"combat",18 },
            {"social",18 },
            {"magic",18 },
            {"awareness",0 },
            {"balance",0 },
            {"cartography",0 },
            { "climb",0 },
            { "cook",0 },
            { "jump",0 },
            { "lift",0 },
            { "reflex",0 },
            { "craft",0 },
            { "forage",0 },
            { "fortitude",0 },
            { "heal",0 },
            { "nature",0 },
            { "sneak",0 },
            { "swim",0 },
            { "track",0 },
            { "aim",0 },
            { "defend",0 },
            { "fight",0 },
            { "maneuver",0 },
            { "empathy",0 },
            { "handle-animal",0 },
            { "influence",0 },
            { "intimidate",0},
            { "lead",0 },
            { "negotiate",0 },
            { "perform",0 },
            { "resolve",0 },
            { "control",0 },
            { "maim",0 },
            { "mend",0 },
            { "create",0 },
            {"currency",0 },
            {"material", 0 },
            {"consumable",0 }
        };

        public List<Actionable> Talents { get; set; } = new List<Actionable>();
        public List<Item> Inventory { get; set; } = new List<Item>();
        public List<Condition> Conditions { get; set; } = new List<Condition>();

        public int GetTotalArmor()
        {
            var Items = Inventory.Where(x => x.Equipped == true && (x.Type == ItemType.Armor || x.Type == ItemType.Shield));

            return Items.Select(x => x.Var1).Sum();
        }
        public DiscordInteractionResponseBuilder BuildSheet(int page)
        {
            List<DiscordEmbed> Embeds = new List<DiscordEmbed>();

            var MainPage = new DiscordEmbedBuilder()
                .WithTitle(this.Name)
                .WithColor(new DiscordColor(Color))
                .WithThumbnail(Image)
                .WithDescription(Dictionaries.Icons["Health"] + " **Health**: [" + Health + "/" + Vars["health"] + "] " + (GetTotalArmor() > 0 ? ("[" + Armor + "/" + GetTotalArmor() + "]") : "") + "\n"
                + BuildBar(1) + "\n"
                + Dictionaries.Icons["Energy"] + " **Energy**: [" + Energy + "/" + Vars["energy"] + "]" + "\n"
                + BuildBar(2) +"\n"
                + Dictionaries.Icons["Woe"] + " **Woe**: [" + Woe + "/" + 9 + "]" + "\n"
                + BuildBar(3))
                .AddField("Attributes", "**Vigor**: " + Vars["vigor"] + "\n" +
                "**Agility**: " + Vars["agility"] + "\n\n" +
                "**Insight**: " + Vars["insight"] + "\n" +
                "**Presence**: " + Vars["presence"], true)
                .AddField("Disciplines", Dictionaries.Icons["Exploration"]+" [8] [9~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                Dictionaries.Icons["Survival"] + " [8] [9~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                Dictionaries.Icons["Combat"] + " [8] [9~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]\n" +
                Dictionaries.Icons["Social"] + " [8] [9~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                Dictionaries.Icons["Magic"] + " [8] [9~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]\n", true);

            var sb = new StringBuilder();

            
            MainPage.AddField("Inventory", Dictionaries.Icons["Currency"] + " " + Vars["currency"] + " | " + Dictionaries.Icons["Material"] + " " + Vars["material"] + " | " + Dictionaries.Icons["Consumable"] + " " + Vars["consumable"] + "\n" +
                sb.ToString());
            sb.Clear();
            if(Conditions.Count > 0)
            {
                foreach(var c in Conditions)
                {
                    sb.AppendLine("[-" + c.Penalty + "]" + " " + c.Name + (c.Discipline.NullorEmpty() ? (" (" + c.Discipline + ")") : "") + (c.Discipline.NullorEmpty() ? (" {" + c.Discipline + "}") : ""));
                }
                MainPage.AddField("Conditions", sb.ToString());
            }
            Embeds.Add(MainPage);

            var Skills = new DiscordEmbedBuilder()
                .WithTitle(this.Name)
                .WithColor(new DiscordColor(Color))
                .WithThumbnail(Image)
                .WithDescription(Dictionaries.Icons["Health"] + " **Health**: [" + Health + "/" + Vars["health"] + "] " + (GetTotalArmor() > 0 ? ("[" + Armor + "/" + GetTotalArmor() + "]") : "") + "\n"
                + BuildBar(1) + "\n"
                + Dictionaries.Icons["Energy"] + " **Energy**: [" + Energy + "/" + Vars["energy"] + "]" + "\n"
                + BuildBar(2) +"\n"
                + Dictionaries.Icons["Woe"] + " **Woe**: [" + Woe + "/" + 9 + "]" + "\n"
                + BuildBar(3))
                .AddField("Exploration","• ["+Vars["awareness"]+"] Awareness: ["+(8-Vars["awareness"])+"] ["+ (8 - Vars["awareness"]+1)+"~"+ (Vars["exploration"] - 1) + "] ["+Vars["exploration"]+"]\n"+
                "• [" + Vars["balance"] + "] Balance: [" + (8 - Vars["balance"]) + "] [" + (8 - Vars["balance"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["cartography"] + "] Cartography: [" + (8 - Vars["cartography"]) + "] [" + (8 - Vars["cartography"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["climb"] + "] Climb: [" + (8 - Vars["climb"]) + "] [" + (8 - Vars["climb"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["cook"] + "] Cook: [" + (8 - Vars["cook"]) + "] [" + (8 - Vars["cook"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["jump"] + "] Jump: [" + (8 - Vars["jump"]) + "] [" + (8 - Vars["jump"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["lift"] + "] Lift: [" + (8 - Vars["lift"]) + "] [" + (8 - Vars["lift"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                "• [" + Vars["reflex"] + "] Reflex: [" + (8 - Vars["reflex"]) + "] [" + (8 - Vars["reflex"] + 1) + "~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]",true)
                .AddField("Survival", "• [" + Vars["craft"] + "] Craft: [" + (8 - Vars["craft"]) + "] [" + (8 - Vars["craft"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["forage"] + "] Forage: [" + (8 - Vars["forage"]) + "] [" + (8 - Vars["forage"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["fortitude"] + "] Fortitude: [" + (8 - Vars["fortitude"]) + "] [" + (8 - Vars["fortitude"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["heal"] + "] Heal: [" + (8 - Vars["heal"]) + "] [" + (8 - Vars["heal"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["nature"] + "] Nature: [" + (8 - Vars["nature"]) + "] [" + (8 - Vars["nature"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["sneak"] + "] Sneak: [" + (8 - Vars["sneak"]) + "] [" + (8 - Vars["sneak"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["swim"] + "] Swim: [" + (8 - Vars["swim"]) + "] [" + (8 - Vars["swim"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                "• [" + Vars["track"] + "] Track: [" + (8 - Vars["track"]) + "] [" + (8 - Vars["track"] + 1) + "~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]",true)
                .AddField("Social", "• [" + Vars["empathy"] + "] Empathy: [" + (8 - Vars["empathy"]) + "] [" + (8 - Vars["empathy"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["handle-animal"] + "] Handle Animal: [" + (8 - Vars["handle-animal"]) + "] [" + (8 - Vars["handle-animal"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["influence"] + "] Influence: [" + (8 - Vars["influence"]) + "] [" + (8 - Vars["influence"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["intimidate"] + "] Intimidate: [" + (8 - Vars["intimidate"]) + "] [" + (8 - Vars["intimidate"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["lead"] + "] Lead: [" + (8 - Vars["lead"]) + "] [" + (8 - Vars["lead"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["negotiate"] + "] Negotiate: [" + (8 - Vars["negotiate"]) + "] [" + (8 - Vars["negotiate"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["perform"] + "] Perform: [" + (8 - Vars["perform"]) + "] [" + (8 - Vars["perform"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                "• [" + Vars["resolve"] + "] Resolve: [" + (8 - Vars["resolve"]) + "] [" + (8 - Vars["resolve"] + 1) + "~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]")
                .AddField("Combat", "• [" + Vars["aim"] + "] Aim: [" + (8 - Vars["aim"]) + "] [" + (8 - Vars["aim"] + 1) + "~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]\n" +
                "• [" + Vars["defend"] + "] Defend: [" + (8 - Vars["defend"]) + "] [" + (8 - Vars["defend"] + 1) + "~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]\n" +
                "• [" + Vars["fight"] + "] Fight: [" + (8 - Vars["fight"]) + "] [" + (8 - Vars["fight"] + 1) + "~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]\n" +
                "• [" + Vars["maneuver"] + "] Maneuver: [" + (8 - Vars["maneuver"]) + "] [" + (8 - Vars["maneuver"] + 1) + "~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]",true)
                .AddField("Magic", "• [" + Vars["control"] + "] Control: [" + (8 - Vars["control"]) + "] [" + (8 - Vars["control"] + 1) + "~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]\n" +
                "• [" + Vars["create"] + "] Create: [" + (8 - Vars["create"]) + "] [" + (8 - Vars["create"] + 1) + "~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]\n" +
                "• [" + Vars["maim"] + "] Maim: [" + (8 - Vars["maim"]) + "] [" + (8 - Vars["maim"] + 1) + "~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]\n" +
                "• [" + Vars["mend"] + "] Mend: [" + (8 - Vars["mend"]) + "] [" + (8 - Vars["mend"] + 1) + "~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]",true);

            Embeds.Add(Skills);

            var builder = new DiscordInteractionResponseBuilder()
                .AddEmbed(Embeds[page]);

            return builder;
        }

        /// <summary>
        /// Builds Icon bars.
        /// 1 = Health
        /// 2 = Energy
        /// 3 = Woe
        /// </summary>
        /// <param name="Res"></param>
        /// <returns></returns>
        public string BuildBar(int Res)
        {
            int max = 0;
            var sb = new StringBuilder();
            switch (Res)
            {
                case 1:
                    max = Vars["health"];
                    var Marmor = GetTotalArmor();
                    if(max <= 10)
                    {
                        int EHealth= max - Health;
                        int FArmor = 0;
                        int EArmor = 0;
                        int HPArmorDiff = 0;

                        if(Armor > Health)
                        {
                            FArmor = Health;
                            EArmor = (Armor - Health);
                            EHealth -= EArmor;
                        }
                        else
                        {
                            FArmor = Armor;
                            HPArmorDiff = Health - Armor;
                        }

                        if(Armor > 0)
                        {
                            for(int i = 0; i < FArmor; i++)
                            {
                                sb.Append(Dictionaries.Bars["Armor"]);
                            }
                            if(HPArmorDiff > 0)
                            {
                                for (int i = 0; i < HPArmorDiff; i++)
                                {
                                    sb.Append(Dictionaries.Bars["Health"]);
                                }
                            }
                            if(EArmor > 0)
                            {
                                for (int i = 0; i < EArmor; i++)
                                {
                                    sb.Append(Dictionaries.Bars["ArmorEmpty"]);
                                }
                            }
                            if (EHealth > 0)
                            {
                                for (int i = 0; i < EHealth; i++)
                                {
                                    sb.Append(Dictionaries.Bars["Empty"]);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < Health; i++)
                            {
                                sb.Append(Dictionaries.Bars["Health"]);
                            }
                            for (int i = 0; i < EHealth; i++)
                            {
                                sb.Append(Dictionaries.Bars["Empty"]);
                            }
                        } 
                    }
                    break;
                case 2:
                    max = Vars["energy"];
                    if (max <= 10)
                    {
                        int diff = max - Energy;
                        for (int i = 0; i < Energy; i++)
                        {
                            sb.Append(Dictionaries.Bars["EN"]);
                        }
                        for (int i = 0; i < diff; i++)
                        {
                            sb.Append(Dictionaries.Bars["Empty"]);
                        }
                    }
                    else
                    {
                        decimal percent = ((decimal)Energy / (decimal)max) * 10;
                        var diff = 10 - Math.Ceiling(percent);
                        for (int i = 0; i < percent; i++)
                        {
                            sb.Append(Dictionaries.Bars["EN"]);
                        }
                        for (int i = 0; i < diff; i++)
                        {
                            sb.Append(Dictionaries.Bars["Empty"]);
                        }
                    }
                    break;
                case 3:
                    int EWoe = 9 - Woe;
                    for (int i = 0; i < Woe; i++)
                    {
                        sb.Append(Dictionaries.Bars["Woe"]);
                    }
                    for (int i = 0; i < EWoe; i++)
                    {
                        sb.Append(Dictionaries.Bars["Empty"]);
                    }
                    break;


            }

            return sb.ToString();
        }
    }
    
    
     public class Condition
    {
        public string Name { get; set; }
        public int Penalty { get; set; }
        public string Skill { get; set; }
        public string Discipline { get; set; }
    }
    
}
