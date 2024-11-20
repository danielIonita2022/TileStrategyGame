using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    public enum MeepleType
    {
        Road,
        Knight,
        Bishop
    }
    public class MeepleData
    {
        private MeepleType _meepleType;
        private PlayerColor _playerColor;

        public MeepleData(PlayerColor color, MeepleType type)
        {
            _playerColor = color;
            _meepleType = type;
        }

        public MeepleType GetMeepleType()
        {
            return _meepleType;
        }

        public PlayerColor GetPlayerColor()
        {
            return _playerColor;
        }

        public void SetMeepleType(MeepleType meepleType)
        {
            _meepleType = meepleType;
        }

        public void SetPlayerColor(PlayerColor playerColor)
        {
            _playerColor = playerColor;
        }
    }
}
