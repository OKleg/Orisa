using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIMLbot;
using AIMLbot.AIMLTagHandlers;
using Telegram.Bot.Types;
using Newtonsoft.Json;

namespace NeuralNetwork1
{
    public class UserData
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public FigureType LastFigure { get; set; }
        public UserData(string id, string name, FigureType fig)
        {
            Id = id;
            Name = name;
            LastFigure = fig;
        }
    }

    public class AIMLBotik
    {
        public Bot myBot;
        public Dictionary<string, User> Users = new Dictionary<string, User>();
        public string UsersPath = "../../aiml/users.json";

        public AIMLBotik()
        {
            myBot = new Bot();
            myBot.loadSettings();
            var users_data = JsonConvert.DeserializeObject<List<UserData>>(System.IO.File.ReadAllText(UsersPath));
            if (users_data == null)
            {
                Users = new Dictionary<string, User>();
                Dump();
            }
            else
            {
                Users.Clear();
                for (int i = 0; i < users_data.Count; i++)
                {
                    var u = new User(users_data[i].Id, users_data[i].Name, new AIMLbot.User(users_data[i].Id, myBot));
                    u.LastFigure = users_data[i].LastFigure;
                    Users.Add(u.Id, u);
                }
            }
            var id = "default_user_id";
            if (!Users.ContainsKey(id))
            {
                Users[id] = new User(id, "TLGBot", new AIMLbot.User(id, myBot));
                Dump();
            }
            myBot.isAcceptingUserInput = false;
            myBot.loadAIMLFromFiles();
            myBot.isAcceptingUserInput = true;
        }

        public void AddUser(string id, string name)
        {
            var user = new User(id, name, new AIMLbot.User(id, myBot));
            Users[id] = user;
        }

        public void Dump()
        {
            var ul = new List<UserData>();
            foreach (var u in Users.Values)
                ul.Add(new UserData(u.Id, u.Name, u.LastFigure));
            System.IO.File.WriteAllText(UsersPath, JsonConvert.SerializeObject(ul));
        }

        public string Talk(string userId, string phrase)
        {
            if (!Users.ContainsKey(userId))
                return "";
            var result = myBot.Chat(new Request(phrase, Users[userId].AIMLUser, myBot)).Output;
            result = result.Replace("\r\n\t\t", "\n");
            result = result.Replace("\\n", "\n");
            return result;
        }
    }
}
