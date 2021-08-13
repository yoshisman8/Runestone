using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class Adversary
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        public int AdversityLevel { get; set; }
        public string Image { get; set; }
    }
}
