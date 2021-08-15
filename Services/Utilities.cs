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
using DSharpPlus.SlashCommands;
using Runestone.Commands;
using System.Drawing;
using System.IO;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace Runestone.Services
{
    public class Utilities
    {
        private LiteDatabase database;
        public Utilities(LiteDatabase _db)
        {
            database = _db;
        }
        #region Getters
        public List<Actionable> GetAllActionables(InteractionContext context, ulong User)
        {
            var u = GetUser(User);

            var col = database.GetCollection<Actionable>("Actionables");

            List<Actionable> Actions = new List<Actionable>();

            var Authors = context.Guild.GetAllMembersAsync().GetAwaiter().GetResult().Select(x => x.Id);

            Actions.AddRange(col.Find(x => x.Author == User || x.Core || Authors.Contains(x.Author)).ToList());

            return Actions;
        }
        public List<Item> GetItem(InteractionContext context, ulong User, string name)
        {
            var u = GetUser(User);

            var col = database.GetCollection<Item>("Items");

            var Authors = context.Guild.GetAllMembersAsync().GetAwaiter().GetResult().Select(x => x.Id);

            var items = col.Find(x => (x.Author == User || x.Core || Authors.Contains(x.Author)) && x.Name.StartsWith(name.ToLower())).ToList();

            return items.Count > 0 ? items : null;
        }
        public Actionable GetTalent(InteractionContext context, ulong user, string Action)
        {
            var u = GetUser(user);

            var col = database.GetCollection<Actionable>("Actionables");

            var actor = u.Active;

            List<Actionable> Actions = new List<Actionable>();

            var Authors = context.Guild.GetAllMembersAsync().GetAwaiter().GetResult().Select(x => x.Id);

            Actions.AddRange(col.Find(x => (x.Author == user || x.Core || Authors.Contains(x.Author)) && x.Talent).ToList());

            var query = Actions.Where(x => x.Name.ToLower().StartsWith(Action.ToLower()));

            if (query.Count() == 0) return null;
            else return query.FirstOrDefault();
        }
        public Actionable Act(InteractionContext context, ulong user, string Action)
        {
            var u = GetUser(user);

            var col = database.GetCollection<Actionable>("Actionables");

            var actor = u.Active;

            List<Actionable> Actions = new List<Actionable>();

            var Authors = context.Guild.GetAllMembersAsync().GetAwaiter().GetResult().Select(x => x.Id);

            Actions.AddRange(col.Find(x => (x.Author == user || x.Core || Authors.Contains(x.Author)) && !x.Talent).ToList());

            Actions.AddRange(actor.Talents);

            var query = Actions.Where(x => x.Name.ToLower().StartsWith(Action.ToLower()));

            if (query.Count() == 0) return null;
            else return query.FirstOrDefault();
        }
        /// <summary>
        /// Gets or creates the user entry for the current user.
        /// </summary>
        /// <param name="Id">Discord ID of the user</param>
        /// <returns>The user entry.</returns>
        public User GetUser(ulong Id)
        {
            var col = database.GetCollection<User>("Users");

            if (col.Exists(x => x.Id == Id))
            {
                return col.Include(x => x.Active).Include(x => x.Active.Talents).Include(x => x.Active.Inventory).FindOne(x => x.Id == Id);
            }
            else
            {
                var User = new User()
                {
                    Id = Id
                };
                col.Insert(User);
                col.EnsureIndex(x => x.Id);

                return col.Include(x => x.Active).Include(x => x.Active.Talents).Include(x => x.Active.Inventory).FindOne(x => x.Id == Id);
            }
        }
        public Encounter GetEncounter(ulong channel)
        {
            var col = database.GetCollection<Encounter>("Encounters");

            if (col.Exists(x => x.Id == channel))
            {
                return col.Include(x => x.Active).FindOne(x => x.Id == channel);
            }
            else
            {
                var Enc = new Encounter()
                {
                    Id = channel
                };
                col.Insert(Enc);
                col.EnsureIndex(x => x.Id);

                return col.Include(x => x.Active).FindOne(x => x.Id == channel);
            }
        }

        public Actor GetActor(int id)
        {
            var actors = database.GetCollection<Actor>("Actors");

            return actors.FindById(id);
        }
        #endregion

        #region Update Commands
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
        public void UpdateEncounter(Encounter e)
        {
            var col = database.GetCollection<Encounter>("Encounters");

            col.Update(e);
        }
        #endregion

        #region Embeders
        public DiscordEmbed HelpEmbed(int page)
        {
            var home = new DiscordEmbedBuilder()
                .WithTitle("User Guide: Combat")
                .WithDescription("Runestune comes built in with a full-feature combat tracker for Narrators and players. The following is a short guide explaining the basics of use, followed by two pages specializing in the details of the combat tracker.")
                .AddField("Starting an Encounter", "Starting an Encounter is as simple as using the `/Encounter Start` command. Whoever begins the encounter is labeled the Narrator for this encounter.\nOnce all combatants have been added to the encounter, the Narrator must use this command again to start the encounter proper.")
                .Build();


            return home;
        }

        public DiscordEmbed EmbedRoll(RollData data)
        {
            var builder = new DiscordEmbedBuilder();

            int total = data.Dice + data.Modifiers + data.Boosts;
            var sb = new StringBuilder();
            Actor A = null;
            Actionable Ac = null;

            if(data.Skill == "tested")
            {
                if (data.Actor > -1)
                {
                    var col = database.GetCollection<Actor>("Actors");

                    A = col.FindById(data.Actor);

                    builder.WithTitle(A.Name + " makes a " + data.Skill + " check!");

                    if (!A.Image.NullorEmpty()) builder.WithThumbnail(A.Image);
                }
                builder.WithTitle(A.Name + "'s resolve is being tested...");
                
                if (total <= data.Judgement)
                {
                    sb.AppendLine("[**" + data.Judgement + "**] " + "[" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "] " + "[" + data.Fortune + "] ");
                    builder.WithColor(DiscordColor.Red);
                }
                else if (total >= data.Fortune)
                {
                    sb.AppendLine("[" + data.Judgement + "] " + "[" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "] " + "[**" + data.Fortune + "**] ");
                    builder.WithColor(DiscordColor.Green);
                }
                else
                {
                    sb.AppendLine("[" + data.Judgement + "] " + "[**" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "**] " + "[" + data.Fortune + "] ");
                    builder.WithColor(DiscordColor.Yellow);
                }

                sb.AppendLine(Dictionaries.d20[data.Dice] + (data.Modifiers != 0 ? (data.Modifiers > 0 ? " +" + data.Modifiers + " (Modifier)" : " " + data.Modifiers + " (Modifier)") : "") + (data.Boosts > 0 ? " +" + data.Boosts + " (Boost)" : "") + " = `" + total + "`");

                builder.WithDescription(sb.ToString());
            }
            else if(data.Skill == "initiative")
            {
                A = GetActor(data.Actor);

                builder.WithTitle(A.Name + " rolls Initiative!");

                if (!A.Image.NullorEmpty())
                {
                    builder.WithThumbnail(A.Image);
                }

                builder.WithColor(new DiscordColor(A.Color));

                sb.AppendLine(Dictionaries.d20[data.Dice] + " + " + data.Modifiers + " + " + (data.Boosts > 0 ? " +" + data.Boosts + " (Boost)" : "") + " = `" + total + "`");

                builder.WithDescription(sb.ToString());
            }
            else
            {
                if (data.Actor > -1)
                {
                    var col = database.GetCollection<Actor>("Actors");

                    A = col.FindById(data.Actor);

                    builder.WithTitle(A.Name + " makes a " + data.Skill + " check!");

                    if (!A.Image.NullorEmpty()) builder.WithThumbnail(A.Image);
                }
                if (data.Action > -1)
                {
                    var col = database.GetCollection<Actionable>("Actionables");

                    Ac = col.FindById(data.Action);

                    builder.WithTitle(A.Name + " performs the " + Ac.Name + " action!");

                    builder.AddField(Ac.Name, A != null ? ReplaceSymbols(Ac.Summary(), A) : Ac.Summary());
                }
                else
                {
                    builder.WithTitle("Someone made a " + data.Skill + " check!");
                }

                if (data.Discipline != "None")
                {
                    sb.Append(Dictionaries.Icons[data.Discipline]);
                }

                if (total <= data.Judgement)
                {
                    sb.AppendLine("[**" + data.Judgement + "**] " + "[" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "] " + "[" + data.Fortune + "] ");
                    builder.WithColor(DiscordColor.Red);
                }
                else if (total >= data.Fortune)
                {
                    sb.AppendLine("[" + data.Judgement + "] " + "[" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "] " + "[**" + data.Fortune + "**] ");
                    builder.WithColor(DiscordColor.Green);
                }
                else
                {
                    sb.AppendLine("[" + data.Judgement + "] " + "[**" + (data.Judgement + 1) + "~" + (data.Fortune - 1) + "**] " + "[" + data.Fortune + "] ");
                    builder.WithColor(DiscordColor.Yellow);
                }

                sb.AppendLine(Dictionaries.d20[data.Dice] + (data.Modifiers != 0 ? (data.Modifiers > 0 ? " +" + data.Modifiers + " (Modifier)" : " " + data.Modifiers + " (Modifier)") : "") + (data.Boosts > 0 ? " +" + data.Boosts + " (Boost)" : "") + " = `" + total + "`");

                builder.WithDescription(sb.ToString());
            }
            return builder.Build();
        }
        public DiscordEmbed EmbedCombat(ulong channel,bool render)
        {
            var actors = database.GetCollection<Actor>("Actors");


            var enc = GetEncounter(channel);
            var builder = new DiscordEmbedBuilder().WithTitle("Encounter!");
            var sb = new StringBuilder();

            if(enc.Combatants.Count == 0)
            {
                builder.WithImageUrl($"attachment://{enc.Id}.png");
                builder.WithDescription("An encounter has been started! Use `/Combat Join` to join in the encounter as a player!.\nNarrators can use the `/Combat Add` command to add adversaries.\n\nOnce all combatants have been added, use the `/Combat Start` command again to start the encounter!");
                return builder.Build();
            }
            else
            {
                foreach (var c in enc.Combatants)
                {
                    if (enc.Current!= null && enc.Current.Name == c.Name)
                    {
                        sb.AppendLine("`" + c.Initiative.ToString("0.0") + "` - **" + c.Name + "**.");

                        if (c.Actor > -1)
                        {
                            Actor a = actors.FindById(c.Actor);

                            sb.AppendLine("> [" + a.Health + "/" + a.Vars["health"] + "]" + a.BuildBar(1));
                            sb.AppendLine("> [" + a.Energy + "/" + a.Vars["energy"] + "]" + a.BuildBar(2));
                            sb.AppendLine("> [" + a.Woe + "/9]" + a.BuildBar(3));
                        }
                    }
                    else
                    {
                        sb.AppendLine("`" + c.Initiative.ToString("0.0") + "` - " + c.Name + ".");
                    }
                }

                builder.WithDescription(sb.ToString());
                if (render)
                {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data", enc.Id.ToString()));
                    if (enc.Refresh)
                    {
                        RenderMap(enc);
                        enc.Refresh = false;
                        UpdateEncounter(enc);
                    }
                    builder.WithImageUrl($"attachment://{enc.Id}-battlemap.png");
                }
            }
            
            return builder.Build();
        }
        
        #endregion

        #region Image Processing

        private int horizontalDistance = 200;
        private int verticalDistance = 115;
        private int horizontalTileDistance = 280;
        private int verticalTileDistance = 175;
        public System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                System.IO.Stream stream = webResponse.GetResponseStream();

                image = System.Drawing.Image.FromStream(stream);

                webResponse.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            return image;
        }

        public void RenderMap(Encounter enc)
        {
            byte[] Battlemap = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "data", "battlemap.png"));

            using (MemoryStream inStream = new MemoryStream(Battlemap))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                    {
                        imageFactory.Load(inStream);

                        for (int i = 0; i < 9; i++)
                        {
                            var InTile = enc.Combatants.Where(x => x.Tile-1 == i).ToList();
                            for (int j = 0; j < InTile.Count; j++)
                            {
                                var img = PrepareToken(InTile[j].Image, InTile[j], enc.Id);
                                imageFactory.Overlay(new ImageLayer()
                                {
                                    Image = img,
                                    Opacity = 100,
                                    Position = GetPoint(i, j)
                                });
                                img.Dispose();
                            }
                        }
                        imageFactory.Format(new PngFormat { Quality = 100 });
                        imageFactory.Save(Path.Combine(Directory.GetCurrentDirectory(), "data", enc.Id.ToString(), enc.Id+"-battlemap.png"));
                        imageFactory.Dispose();
                    }
                }
            }
        }
        public System.Drawing.Image PrepareToken(string imageurl, Combatant combatant,ulong encounter)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.ToString()));

            if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.ToString(), combatant.Name + ".png")))
            {
                return Image.FromFile(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.ToString(), combatant.Name + ".png"));
            }
            using (FileStream FileStream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.ToString(), combatant.Name + ".png"), FileMode.OpenOrCreate))
            {

                using(MemoryStream OutStream = new MemoryStream())
                {

                    var Mask = new MemoryStream(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "data", "mask.png")));
                    MemoryStream Border;

                    if(combatant.Actor>-1) Border = new MemoryStream(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "data", "player.png")));
                    else Border = new MemoryStream(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "data", "enemy.png")));


                    var img = DownloadImageFromUrl(imageurl);

                    var imageFactory = new ImageFactory(true);

                    imageFactory.Load(img);

                    imageFactory.Resize(new ResizeLayer(new Size(125, 150), ResizeMode.Pad));
                    
                    imageFactory.Mask(new ImageLayer() { Image = Image.FromStream(Mask) });

                    imageFactory.Overlay(new ImageLayer() { Image = Image.FromStream(Border), Position = new Point(0, 0) });


                    imageFactory.Save(OutStream);

                    OutStream.WriteTo(FileStream);

                    FileStream.Close();
                    OutStream.Close();
                    imageFactory.Dispose();

                }

                return Image.FromFile(Path.Combine(Directory.GetCurrentDirectory(), "Data", encounter.ToString(), combatant.Name + ".png"));
            }

        }

        private Point GetPoint(int tile, int subtile)
        {
            Point O = new Point(857, 12);

            switch (tile)
            {
                case 0:
                    break;
                case 1:
                    O = new Point(O.X - horizontalTileDistance, O.Y + verticalTileDistance);
                    break;
                case 2:
                    O = new Point(O.X + horizontalTileDistance, O.Y + verticalTileDistance);
                    break;
                case 3:
                    O = new Point(O.X - (horizontalTileDistance * 2), O.Y + (verticalTileDistance * 2));
                    break;
                case 4:
                    O = new Point(O.X, O.Y + verticalTileDistance * 2);
                    break;
                case 5:
                    O = new Point(O.X + (horizontalTileDistance * 2), O.Y + (verticalTileDistance * 2));
                    break;
                case 6:
                    O = new Point(O.X - horizontalTileDistance, O.Y + (verticalTileDistance * 3));
                    break;
                case 7:
                    O = new Point(O.X + horizontalTileDistance, O.Y + (verticalTileDistance * 3));
                    break;
                case 8:
                    O = new Point(O.X, O.Y + (verticalTileDistance * 4));
                    break;

            }


            switch (subtile)
            {
                case 0:
                    return O;
                case 1:
                    return new Point(O.X - horizontalDistance, O.Y + verticalDistance);
                case 2:
                    return new Point(O.X, O.Y + verticalDistance);
                case 3:
                    return new Point(O.X + horizontalDistance, O.Y + verticalDistance);
                case 4:
                    return new Point(O.X, O.Y + (verticalDistance * 2));
            }
            return O;
        }


        #endregion


        #region Processors
        public string ReplaceSymbols(string Input, Actor actor)
        {
            string Output = Input;
            foreach (var v in actor.Vars)
            {
                Output = Output.Replace("[" + v.Key +"]", "["+v.Value.ToString()+"]").Replace("[" + v.Key.FirstCharToUpper() + "]", "[" + v.Value.ToString() + "]");
            }
            if(Output.Contains(":crossed_swords:") && actor.Inventory.Any(x=>x.Type == ItemType.Weapon && x.Equipped))
            {
                var item = actor.Inventory.Where(x => x.Equipped && x.Type == ItemType.Weapon).OrderByDescending(x => x.Var1).FirstOrDefault();

                Output.Replace(":crossed_swords:", "<" + item.Var1 + ">");
            }

            return Output;
        }

        public int ProcessConditions(string skill, Actor actor)
        {

            var Matches = actor.Conditions.Where(x => x.Skill.ToLower() == skill.ToLower() || x.Skill.ToLower() == "any").ToList();

            foreach (var x in actor.Conditions.Where(x=> x.Discipline != "none"))
            {
                if (Dictionaries.Skills.TryGetValue(skill,out string a))
                {
                    if (a.ToLower() == x.Discipline)
                    {
                        Matches.Add(x);
                    }
                }
            }

            if (Matches.Count() > 0) return Matches.Select(x => x.Penalty).Sum();
            else return 0;
        }

        public int ExtraCost(ulong c, Actor a)
        {
            var e = GetEncounter(c);
            if (!e.Active) return 0;
            if (e.Current.Actor == a.Id)
            {
                int curr = e.Current.Actions;

                int i = e.Combatants.FindIndex(x=>x.Name == e.Current.Name);

                e.Combatants[i].Actions++;
                e.Current.Actions++;

                UpdateEncounter(e);

                return curr;
            }
            else
            {
                return 0;
            }
        }
        #endregion
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
        public static string FirstCharToUpper(this string input) =>
                input switch
                {
                    null => throw new ArgumentNullException(nameof(input)),
                    "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                    _ => input.First().ToString().ToUpper() + input.Substring(1)
                };
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
            {"Currency","<:Currency:875526327766245407>" },
            {"Material","<:Material:875526328147935302>" },
            {"Consumable","<:Consumable:875526328043057202>" },
            {"Combat","<:Combat:875801973461487677>" },
            {"Exploration","<:Exploration:875801973134360598>" },
            {"Social","<:Social:875801973507624970>"},
            {"Survival", "<:Survival:875801973520232478>" },
            {"Magic","<:Magic:875801973209837640>" },
            {"Usable","<:Usable:875811131913011282>" },
            {"Recharging","<:Recharging:875811132315676702>" },
            {"Downtime","<:Downtime:876087722782117899>" }
        };
        public static Dictionary<string, string> Skills { get; set; } = new Dictionary<string, string>()
        {
            {"exploration","exploration" },
            {"survival","survival" },
            {"combat","combat" },
            {"social","social" },
            {"magic","magic" },
            {"awareness","exploration" },
            {"balance","exploration" },
            {"cartography","exploration" },
            { "climb","exploration" },
            { "jump","exploration" },
            { "lift","exploration" },
            { "reflex","exploration" },
            { "swim","exploration" },
            { "track","exploration" },
            { "cook","survival" },
            { "craft","survival" },
            { "forage","survival" },
            { "fortitude","survival" },
            { "heal","survival" },
            { "nature","survival" },
            { "sneak","survival" },
            { "aim","combat" },
            { "defend","combat" },
            { "fight","combat" },
            { "maneuver","combat" },
            { "empathy","social" },
            { "handle-animal","social" },
            { "influence","social" },
            { "intimidate","social"},
            { "lead","social" },
            { "negotiate","social" },
            { "perform","social" },
            { "resolve","social" },
            { "control","magic" },
            { "maim","magic" },
            { "mend","magic" },
            { "create","magic" }
        };
        public static Dictionary<string, string> SubSkills { get; set; } = new Dictionary<string, string>()
        {
            {"awareness","exploration" },
            {"balance","exploration" },
            {"cartography","exploration" },
            { "climb","exploration" },
            { "jump","exploration" },
            { "lift","exploration" },
            { "reflex","exploration" },
            { "swim","exploration" },
            { "track","exploration" },
            { "cook","survival" },
            { "craft","survival" },
            { "forage","survival" },
            { "fortitude","survival" },
            { "heal","survival" },
            { "nature","survival" },
            { "sneak","survival" },
            { "aim","combat" },
            { "defend","combat" },
            { "fight","combat" },
            { "maneuver","combat" },
            { "empathy","social" },
            { "handle-animal","social" },
            { "influence","social" },
            { "intimidate","social"},
            { "lead","social" },
            { "negotiate","social" },
            { "perform","social" },
            { "resolve","social" },
            { "control","magic" },
            { "maim","magic" },
            { "mend","magic" },
            { "create","magic" }
        };

        public static Dictionary<int, string> TileNames { get; set; } = new Dictionary<int, string>()
        {
            { 1,"Upper Edge" },
            { 2,"North-Western Flank" },
            { 3,"North-Eastern Flank" },
            { 4,"Inner Edge" },
            { 5,"Heat" },
            { 6,"Outer Edge" },
            { 7,"South-Western Flank" },
            { 8,"South-Eastern Flank" },
            { 9,"Lower Edge" },
        };
    }
}
