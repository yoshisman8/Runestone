using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class Encounter
    {
        [BsonId]
        public ulong Id { get; set; }
        public ulong Narrator { get; set; }
        public List<Combatant> Combatants { get; set; } = new List<Combatant>();
        public Combatant Current { get; set; } = null;
        public bool Started { get; set; } = false;
        public bool Active { get; set; } = false;
    }
    public class Combatant
    {
        public double Initiative { get; set; }
        public bool player { get; set; }
        [BsonRef("Actors")]
        public Actor Actor { get; set; }
        [BsonRef("Adversaries")]
        public Adversary Adversary { get; set; }
        public int Tile { get; set; }
        
    }
}
