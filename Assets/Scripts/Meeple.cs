using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public enum MeepleType
    {
        Road,
        Knight,
        Bishop
    }

    public class Meeple : MonoBehaviour
    {
        [SerializeField] private MeepleType meepleType;
        private PlayerColor playerColor;
        private SpriteRenderer spriteRenderer;

        public Meeple(PlayerColor color, MeepleType type)
        {
            playerColor = color;
            meepleType = type;
        }

        /// <summary>
        /// Updates the visual representation based on meeple type and player color.
        /// </summary>
        private void UpdateMeepleVisual(MeepleType type, PlayerColor color)
        {
            if (spriteRenderer == null)
                return;

            switch (type)
            {
                case MeepleType.Knight:
                    spriteRenderer.sprite = Resources.Load<Sprite>($"Art/Meeples/{color}Knight");
                    break;
                case MeepleType.Bishop:
                    spriteRenderer.sprite = Resources.Load<Sprite>($"Art/Meeples/{color}Bishop");
                    break;
                case MeepleType.Road:
                    spriteRenderer.sprite = Resources.Load<Sprite>($"Art/Meeples/{color}Meeple");
                    break;
                default:
                    spriteRenderer.sprite = null;
                    break;
            }
        }
    }


}
