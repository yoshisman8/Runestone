using System;
using System.Collections.Generic;
using System.Text;

namespace Runestone.Collections
{
    public class RollData
    {
        public int Dice { get; set; }
        public Actionable Action { get; set; }
        public int Fortune { get; set; }
        public int Judgement { get; set; }
        public string Skill { get; set; }
    }
}
