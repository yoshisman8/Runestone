using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Runestone.Collections;

namespace Runestone.Services
{
    public class ButtonService
    {
        private LiteDatabase database;
        private Utilities utils;
        public ButtonService(DiscordClient client, LiteDatabase _db, Utilities _utils)
        {
            database = _db;
            utils = _utils;
        }


        public async Task HandleButtonAsync(DiscordClient c, ComponentInteractionCreateEventArgs e)
        {
            var u = utils.GetUser(e.User.Id);
            if (e.Id.StartsWith("dl"))
            {
                int id = int.Parse(e.Id.Substring(2));

                var col = database.GetCollection<Actor>("Actors");

                var C = col.FindById(id);

                var User = utils.GetUser(e.User.Id);

                col.Delete(id);

                if (User.Active.Id == id)
                {
                    User.Active = null;
                    utils.UpdateUser(User);
                }
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                    .WithContent("Character **" + C.Name + "** has been deleted. If this was your active character, you no longer have an active character."));
                return;
            }
            switch (e.Id)
            {
                case "sheet_main_page":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, u.Active.BuildSheet(0)
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_main_page","Main Page"),
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_skills_page","Skills")
                        }));
                    break;
                case "sheet_skills_page":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, u.Active.BuildSheet(1)
                        .AddComponents(new DiscordComponent[]{
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_main_page","Main Page"),
                            new DiscordButtonComponent(ButtonStyle.Primary,"sheet_skills_page","Skills")
                        }));
                    break;
                case "cancel":
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().WithContent("Operation Cancelled!"));
                    break;
            }
        }
    }
}
