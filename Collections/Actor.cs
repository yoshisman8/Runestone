using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;
using DSharpPlus.Entities;
using Runestone.Services;
using DSharpPlus;

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
            DiscordEmbed Embeds = null;

            if (page == 0)
            {
                var MainPage = new DiscordEmbedBuilder()
                .WithTitle(this.Name + " Lv." + Vars["level"])
                .WithColor(new DiscordColor(Color))
                .WithThumbnail(Image)
                .WithDescription(Dictionaries.Icons["Health"] + " **Health**: [" + Health + "/" + Vars["health"] + "] " + (GetTotalArmor() > 0 ? ("[" + Armor + "/" + GetTotalArmor() + "]") : "") + "\n"
                + BuildBar(1) + "\n"
                + Dictionaries.Icons["Energy"] + " **Energy**: [" + Energy + "/" + Vars["energy"] + "]" + "\n"
                + BuildBar(2) + "\n"
                + Dictionaries.Icons["Woe"] + " **Woe**: [" + Woe + "/" + 9 + "]" + "\n"
                + BuildBar(3))
                .AddField("Attributes", "**Vigor**: " + Vars["vigor"] + "\n" +
                "**Agility**: " + Vars["agility"] + "\n\n" +
                "**Insight**: " + Vars["insight"] + "\n" +
                "**Presence**: " + Vars["presence"], true)
                .AddField("Disciplines", Dictionaries.Icons["Exploration"] + " [8] [9~" + (Vars["exploration"] - 1) + "] [" + Vars["exploration"] + "]\n" +
                Dictionaries.Icons["Survival"] + " [8] [9~" + (Vars["survival"] - 1) + "] [" + Vars["survival"] + "]\n" +
                Dictionaries.Icons["Combat"] + " [8] [9~" + (Vars["combat"] - 1) + "] [" + Vars["combat"] + "]\n" +
                Dictionaries.Icons["Social"] + " [8] [9~" + (Vars["social"] - 1) + "] [" + Vars["social"] + "]\n" +
                Dictionaries.Icons["Magic"] + " [8] [9~" + (Vars["magic"] - 1) + "] [" + Vars["magic"] + "]\n", true);

                var sb = new StringBuilder();


                foreach (var I in Inventory.OrderByDescending(x=>x.Equipped).ThenBy(x=>x.Name))
                {
                    switch (I.Type)
                    {
                        case ItemType.Armor:
                            sb.AppendLine("• " + I.Name + (I.Equipped?" [Worn]":""));
                            break;
                        case ItemType.Asset:
                            sb.AppendLine("• " + I.Name + " " + (I.Spent ? Dictionaries.Icons["Charging"] : Dictionaries.Icons["Usable"]));
                            break;
                        case ItemType.Weapon:
                            sb.AppendLine("• " + I.Name + (I.Equipped ? " [Wielded]" : ""));
                            break;
                        case ItemType.Shield:
                            sb.AppendLine("• " + I.Name + (I.Equipped ? " [Held]" : ""));
                            break;
                    }
                }

                MainPage.AddField("Inventory", Dictionaries.Icons["Currency"] + " " + Vars["currency"] + " | " + Dictionaries.Icons["Material"] + " " + Vars["material"] + " | " + Dictionaries.Icons["Consumable"] + " " + Vars["consumable"] + "\n" +
                    sb.ToString());

                sb.Clear();

                if (Conditions.Count > 0)
                {
                    foreach (var c in Conditions)
                    {
                        sb.AppendLine("[-" + c.Penalty + "]" + " " + c.Name + (c.Discipline.NullorEmpty() ? (" (" + c.Discipline + ")") : "") + (c.Discipline.NullorEmpty() ? (" {" + c.Discipline + "}") : ""));
                    }
                    MainPage.AddField("Conditions", sb.ToString());
                }

                Embeds = MainPage.Build();
            }

        
            if(page == 1)
            {

                var Skills = new DiscordEmbedBuilder()
                    .WithTitle(this.Name+" Lv."+Vars["level"])
                    .WithColor(new DiscordColor(Color))
                    .WithThumbnail(Image)
                    .WithDescription(Dictionaries.Icons["Health"] + " **Health**: [" + Health + "/" + Vars["health"] + "] " + (GetTotalArmor() > 0 ? ("[" + Armor + "/" + GetTotalArmor() + "]") : "") + "\n"
                    + BuildBar(1) + "\n"
                    + Dictionaries.Icons["Energy"] + " **Energy**: [" + Energy + "/" + Vars["energy"] + "]" + "\n"
                    + BuildBar(2) + "\n"
                    + Dictionaries.Icons["Woe"] + " **Woe**: [" + Woe + "/" + 9 + "]" + "\n"
                    + BuildBar(3))
                    .AddField(Dictionaries.Icons["Exploration"] + "Exploration", "•  Awareness: [" + Vars["awareness"] + "]\n" +
                    "• Balance: [" + Vars["balance"] + "]\n" +
                    "• Cartography: [" + Vars["cartography"] + "]\n" +
                    "• Climb: [" + Vars["climb"] + "]\n" +
                    "• Jump: [" + Vars["jump"] + "]\n" +
                    "• Lift: [" + Vars["lift"] + "]\n" +
                    "• Reflex: [" + Vars["reflex"] + "]\n" +
                    "• Track: [" + Vars["track"] + "]", true)
                    .AddField(Dictionaries.Icons["Survival"] + "Survival", "• Cook: [" + Vars["cook"] + "]\n" +
                    "• Craft: [" + Vars["craft"] + "]\n" +
                    "• Forage: [" + Vars["forage"] + "]\n" +
                    "• Fortitude: [" + Vars["fortitude"] + "]\n" +
                    "• Heal: [" + Vars["heal"] + "]\n" +
                    "• Nature: [" + Vars["nature"] + "]\n" +
                    "• Sneak: [" + Vars["sneak"] + "]\n" +
                    "• Swim: [" + Vars["swim"] + "]", true)
                    .AddField(Dictionaries.Icons["Social"] + "Social", "• Empathy: [" + Vars["empathy"] + "]\n" +
                    "• Handle Animal:[" + Vars["handle-animal"] + "]\n" +
                    "• Influence: [" + Vars["influence"] + "]\n" +
                    "• Intimidate: [" + Vars["intimidate"] + "]\n" +
                    "• Lead: [" + Vars["lead"] + "]\n" +
                    "• Negotiate: [" + Vars["negotiate"] + "]\n" +
                    "• Perform: [" + Vars["perform"] + "]\n" +
                    "• Resolve:  [" + Vars["resolve"] + "]", true)
                    .AddField(Dictionaries.Icons["Combat"] + "Combat", "• Aim: [" + Vars["aim"] + "] \n" +
                    "• Defend: [" + Vars["defend"] + "]\n" +
                    "• Fight: [" + Vars["fight"] + "]\n" +
                    "• Maneuver: [" + Vars["maneuver"] + "]", true)
                    .AddField(Dictionaries.Icons["Magic"] + "Magic", "• Control: [" + Vars["control"] + "]\n" +
                    "• Create: [" + Vars["create"] + "]\n" +
                    "• Maim: [" + Vars["maim"] + "]\n" +
                    "• Mend:  [" + Vars["mend"] + "]", true);

                Embeds = Skills.Build();
            }

            if (page >= 2)
            {
                var TalentPage = new DiscordEmbedBuilder()
                    .WithTitle(this.Name + " Lv." + Vars["level"])
                    .WithColor(new DiscordColor(Color))
                    .WithThumbnail(Image)
                    .WithDescription(Dictionaries.Icons["Health"] + " **Health**: [" + Health + "/" + Vars["health"] + "] " + (GetTotalArmor() > 0 ? ("[" + Armor + "/" + GetTotalArmor() + "]") : "") + "\n"
                    + BuildBar(1) + "\n"
                    + Dictionaries.Icons["Energy"] + " **Energy**: [" + Energy + "/" + Vars["energy"] + "]" + "\n"
                    + BuildBar(2) + "\n"
                    + Dictionaries.Icons["Woe"] + " **Woe**: [" + Woe + "/" + 9 + "]" + "\n"
                    + BuildBar(3));
                var Tals = new List<Actionable>();
                int StartPoint = 0 + (4 * (page - 2));

                for(int i = 0; i < 4; i++)
                {
                    if (StartPoint + i >= Talents.Count) break;
                    Tals.Add(Talents[StartPoint + i]);
                }
                foreach(var t in Tals)
                {
                    TalentPage.AddField(t.Name, t.Summary());
                }
                Embeds = TalentPage;
            }

            var buttons = new List<DiscordComponent>()
            {
                new DiscordButtonComponent(ButtonStyle.Primary,"s,0,"+Id,"Main Page"),
                new DiscordButtonComponent(ButtonStyle.Primary,"s,1,"+Id,"Skills")
            };

            if (Talents.Count > 0)
            {
                int TalentPages = (int)Math.Ceiling(((double)Talents.Count / (double)4));
                for (int t = 0; t < TalentPages; t++)
                {
                    buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, "s," + (2 + t)+","+Id, "Talents (" + (t + 1) + ")"));
                }
            }
            
            var builder = new DiscordInteractionResponseBuilder()
                .AddEmbed(Embeds)
                .AddComponents(buttons);

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
                    else
                    {
                        decimal percent = ((decimal)Math.Max(Math.Min(Health, max), 0) / (decimal)max) * 10;
                        var EHealth = 10 - Math.Ceiling(percent);

                        int FArmor = 0;
                        int EArmor = 0;
                        int HPArmorDiff = 0;

                        if (Armor > percent)
                        {
                            FArmor = (int)percent;
                            EArmor = (Armor - (int)percent);
                            EHealth -= EArmor;
                        }
                        else
                        {
                            FArmor = Armor;
                            HPArmorDiff = (int)percent - Armor;
                        }

                        if (Armor > 0)
                        {
                            for (int i = 0; i < FArmor; i++)
                            {
                                sb.Append(Dictionaries.Bars["Armor"]);
                            }
                            if (HPArmorDiff > 0)
                            {
                                for (int i = 0; i < HPArmorDiff; i++)
                                {
                                    sb.Append(Dictionaries.Bars["Health"]);
                                }
                            }
                            if (EArmor > 0)
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
                            for (int i = 0; i < (int)percent; i++)
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
                        int diff = max - Math.Max(Math.Min(Energy, max), 0);
                        for (int i = 0; i < Energy; i++)
                        {
                            sb.Append(Dictionaries.Bars["Energy"]);
                        }
                        for (int i = 0; i < diff; i++)
                        {
                            sb.Append(Dictionaries.Bars["Empty"]);
                        }
                    }
                    else
                    {
                        decimal percent = ((decimal)Math.Max(Math.Min(Energy,max), 0) / (decimal)max) * 10;
                        var diff = 10 - Math.Ceiling(percent);
                        for (int i = 0; i < percent; i++)
                        {
                            sb.Append(Dictionaries.Bars["Energy"]);
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
