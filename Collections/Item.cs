using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class Item
    {
        [BsonId]
        public int Id { get; set; }
        public ulong Author { get; set; }
        public bool Core { get; set; } = false;
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Spent { get; set; } = false;
        public bool Equipped { get; set; } = false;
        public ItemType Type { get; set; }
        public int Var1 { get; set; }
        public int Var2 { get; set; }

        public DiscordEmbed BuildEmbed(string Color = "")
        {
            var builder = new DiscordEmbedBuilder()
                .WithTitle(Name + " (" + Type + ")");

            var sb = new StringBuilder();

            switch (Type)
            {
                case ItemType.Armor:
                    sb.AppendLine("**Armor**: " + Var1 + " | **Check Penalty**: " + Var2);
                    break;
                case ItemType.Weapon:
                    sb.AppendLine("**Damage**: " + Var1 + " | **Range**: " + Var2);
                    break;
                case ItemType.Shield:
                    sb.AppendLine("**Armor**: " + Var1 + " | **Check Penalty**: " + Var2);
                    break;
                case ItemType.Asset:
                    sb.AppendLine("**Status**: " + (Spent ? "Recharging" : "Usable"));
                    break;
            }
            sb.AppendLine(Description);
            builder.WithDescription(sb.ToString());

            return builder.Build();
        }
    }
    public enum ItemType { [ChoiceName("This item is an Asset (Val1 and 2 do nothing).")]Asset,
        [ChoiceName("This item is a Weapon (Val1 = Damage, Val2 = Range).")] Weapon,
        [ChoiceName("This item is armor (Val 1 = Armor, Val 2 = Penalty).")] Armor,
        [ChoiceName("This item is a shield (Val 1 = Armor, Val 2 = Penalty).")] Shield }
}
