using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace Runestone.Collections
{
    public class Server
    {
        [BsonId]
        public ulong Id { get; set; }

    }
}
