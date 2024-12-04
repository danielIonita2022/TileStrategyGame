using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class Player
    {
        public string PlayerName { get; private set; }
        public PlayerColor PlayerColor { get; private set; }
        public int Score { get; set; }
        public int MeepleCount { get; set; }
        public int PlayerID { get; private set; } // Unique identifier


        public Player(string name, PlayerColor color, int id)
        {
            PlayerName = name;
            PlayerColor = color;
            PlayerID = id;
            Score = 0;
            MeepleCount = 6;
        }

    }
}
