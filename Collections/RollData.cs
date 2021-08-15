using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class RollData
    {
        public int Dice { get; set; }
        public int Modifiers { get; set; }
        public int Boosts { get; set; }
        public int Action { get; set; }
        public int Actor { get; set; }
        public int Fortune { get; set; }
        public int Judgement { get; set; }
        public string Skill { get; set; }
        public string Discipline { get; set; }
        public ulong Encounter { get; set; }

        public string Serialize()
        {
            return Dice + "," + Modifiers + "," + Boosts + "," + Action + "," + Actor + "," + Fortune + "," + Judgement + "," + Skill+","+Discipline+","+Encounter;
        }
        public RollData Deserialize(string input)
        {
            string[] vars = input.Split(",");

            return new RollData()
            {
                Dice = int.Parse(vars[0]),
                Modifiers = int.Parse(vars[1]),
                Boosts = int.Parse(vars[2]),
                Action = int.Parse(vars[3]),
                Actor = int.Parse(vars[4]),
                Fortune = int.Parse(vars[5]),
                Judgement = int.Parse(vars[6]),
                Skill = vars[7],
                Discipline = vars[8],
                Encounter = ulong.Parse(vars[9])
            };
        }
    }
}
