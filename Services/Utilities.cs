using System;
using DSharpPlus;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;
using Runestone.Collections;
using System.Net;
using System.Globalization;
using DSharpPlus.Entities;

namespace Runestone.Services
{
    public class Utilities
    {
        private LiteDatabase database;
        public Utilities(LiteDatabase _db)
        {
            database = _db;
        }
        public List<Actionable> GetAllActions(ulong User)
        {
            var col = database.GetCollection<Actionable>("Actionables");

            List<Actionable> Actions = new List<Actionable>();

            Actions.AddRange(col.Find(x => x.Author == User).ToList());



            return Actions;
        }
        /// <summary>
        /// Gets or creates the user entry for the current user.
        /// </summary>
        /// <param name="Id">Discord ID of the user</param>
        /// <returns>The user entry.</returns>
        public User GetUser(ulong Id)
        {
            var col = database.GetCollection<User>("Users");

            if(col.Exists(x=>x.Id == Id))
            {
                return col.Include(x=>x.Active).FindOne(x => x.Id == Id);
            }
            else
            {
                var User = new User()
                {
                    Id = Id
                };
                col.Insert(User);
                col.EnsureIndex(x => x.Id);

                return col.Include(x => x.Active).FindOne(x => x.Id == Id);
            }
        }
        public void UpdateUser(User U)
        {
            var col = database.GetCollection<User>("Users");
            col.Update(U);
        }
        public void UpdateActor(Actor A)
        {
            var col = database.GetCollection<Actor>("Actors");

            col.Update(A);
        }

        public DiscordEmbed HelpEmbed(int page)
        {
            var home = new DiscordEmbedBuilder()
                .WithTitle("User Guide: Combat")
                .WithDescription("Runestune comes built in with a full-feature combat tracker for Narrators and players. The following is a short guide explaining the basics of use, followed by two pages specializing in the details of the combat tracker.")
                .AddField("Starting an Encounter","Starting an Encounter is as simple as using the `/Encounter Start` command. Whoever begins the encounter is labeled the Narrator for this encounter.\nOnce all combatants have been added to the encounter, the Narrator must use this command again to start the encounter proper.")
                .Build();


            return home;
        }
    }
    public static class Extentions
    {
        public static bool IsImageUrl(this string URL)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(URL);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                            .StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }
        public static bool NullorEmpty(this string _string)
        {
            if (_string == null) return true;
            if (_string == "") return true;
            else return false;
        }
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            for (int index = 0; index < str.Length; index += maxLength)
            {
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
            }
        }
    }
    public static class Dictionaries
    {
        public static Dictionary<int, string> d20 { get; set; } = new Dictionary<int, string>()
        {
            {20, "<:d20_20:663149799792705557>" },
            {19, "<:d20_19:663149782847586304>" },
            {18, "<:d20_18:663149770621190145>" },
            {17, "<:d20_17:663149758885396502>" },
            {16, "<:d20_16:663149470216749107>" },
            {15, "<:d20_15:663149458963300352>" },
            {14, "<:d20_14:663149447278100500>" },
            {13, "<:d20_13:663149437459234846>" },
            {12, "<:d20_12:663149424909746207>" },
            {11, "<:d20_11:663149398712123415>" },
            {10, "<:d20_10:663149389396574212>" },
            {9, "<:d20_9:663149377954775076>" },
            {8, "<:d20_8:663149293695139840>" },
            {7, "<:d20_7:663149292743032852>" },
            {6, "<:d20_6:663149290532634635>" },
            {5, "<:d20_5:663147362608480276>" },
            {4, "<:d20_4:663147362512011305>" },
            {3, "<:d20_3:663147362067415041>" },
            {2, "<:d20_2:663147361954037825>" },
            {1, "<:d20_1:663146691016523779>" }
        };
        public static Dictionary<string, string> Bars { get; set; } = new Dictionary<string, string>()
        {
            {"Health","<:HP:875522342904803339>" },
            {"Energy","<:EN:875522342955135046>" },
            {"Woe","<:HP:875522342904803339>" },
            {"Empty","<:Empty:875522342988693545>" },
            {"Armor","<:ArmorFull:875522343055790200>" },
            {"ArmorEmpty","<:ArmorEmpty:875522343080976404>" }
        };
        public static Dictionary<string, string> Icons { get; set; } = new Dictionary<string, string>()
        {
            {"Health","<:Health:875526328227614780>" },
            {"Energy","<:Energy:875526327774617671>" },
            {"Woe","<:Woe:875526328500232203>" },
            {"Armor","<:Armor:875526328076615761>" },
            {"Recharge","<:Recharge:875526874783178803>" },
            {"Currency","<:Currency:875526327766245407>" },
            {"Material","<:Material:875526328147935302>" },
            {"Consumable","<:Consumable:875526328043057202>" },
            {"Combat","<:Combat:875801973461487677>" },
            {"Exploration","<:Exploration:875801973134360598>" },
            {"Social","<:Social:875801973507624970>"},
            {"Survival", "<:Survival:875801973520232478>" },
            {"Magic","<:Magic:875801973209837640>" }
        };
    }
}
