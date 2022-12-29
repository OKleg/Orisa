using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIMLbot;
using AIMLbot.AIMLTagHandlers;

namespace NeuralNetwork1
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AIMLbot.User AIMLUser { get; set; }
        public FigureType LastFigure { get; set; }
        public User(string id, string name, AIMLbot.User aIMLUser)
        {
            Id = id;
            Name = name;
            AIMLUser = aIMLUser;
            LastFigure = FigureType.Undef;
        }
    }
}
