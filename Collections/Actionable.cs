using LiteDB;
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
    }

    public enum Costs { Energy, Health, Woe, Material, Consumable, Currency, None }
    public enum ActionType { Action, Reaction, Passive, Free, Traversal }
}
