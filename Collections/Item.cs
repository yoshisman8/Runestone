using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class Item
    {
        public string Name { get; set; }
        public int Quantity { get; set; } = 1;
        public string Description { get; set; }
        public bool Spent { get; set; } = false;
        public bool Equipped { get; set; } = false;
        public ItemType Type { get; set; }
        public int Var1 { get; set; }
        public int Var2 { get; set; }
    }
    public enum ItemType { Asset, Weapon, Armor, Shield }
}
