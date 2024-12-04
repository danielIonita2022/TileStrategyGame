using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{

    public class Meeple : MonoBehaviour
    {
        
        [SerializeField] public SpriteRenderer SpriteRenderer;
        public MeepleData MeepleData;

        public event Action<Meeple> OnGrayMeepleClicked;

        private void Awake()
        {
            MeepleData = new MeepleData(PlayerColor.GRAY, MeepleType.Road, -1);
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnMouseDown()
        {
            PlayerColor playerColor = MeepleData.GetPlayerColor();
            if (playerColor == PlayerColor.GRAY)
            {
                Debug.Log("Meeple: Gray meeple clicked");
                OnGrayMeepleClicked?.Invoke(this);
            }
            else
            {
                Debug.Log("Meeple: Player meeple clicked, nothing happens");
            }

        }

        /// <summary>
        /// Updates the visual representation based on meeple type and player color.
        /// </summary>
        public void UpdateMeepleVisual(MeepleType type, PlayerColor color)
        {
            if (SpriteRenderer == null)
            {
                SpriteRenderer = GetComponent<SpriteRenderer>();
            }
            MeepleData.SetMeepleType(type);
            MeepleData.SetPlayerColor(color);

            string colorString = Converters.ConvertPlayerColorToString(color);

            switch (type)
            {
                case MeepleType.Knight:
                    SpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Knight");
                    break;
                case MeepleType.Bishop:
                    SpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Bishop");
                    break;
                case MeepleType.Road:
                    SpriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Meeples/{colorString}Meeple");
                    break;
                default:
                    SpriteRenderer.sprite = null;
                    break;
            }
        }
    }


}
