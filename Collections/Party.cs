using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;

namespace Runestone.Collections
{
    public class Party
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; }
        public ulong Narrator { get; set; }
        public List<Actor> Players { get; set; }
    }
}
