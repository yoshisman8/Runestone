using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;

namespace Runestone.Collections
{
    public class User
    {
        [BsonId]
        public ulong Id { get; set; }

        [BsonRef("Actors")]
        public Actor Active { get; set; } = null;

        [BsonRef("Actionables")]
        public List<Actionable> HomebrewActions { get; set; } = new List<Actionable>();
        [BsonRef("Items")]
        public List<Item> HomebrewItems { get; set; } = new List<Item>();
    }
}
