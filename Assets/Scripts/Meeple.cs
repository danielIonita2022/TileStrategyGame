using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts
{

    public class Meeple : NetworkBehaviour
    {
        
        [SerializeField] public SpriteRenderer SpriteRenderer;
        public MeepleData MeepleData;

        public event Action<Meeple> OnGrayMeepleClicked;

        public void Awake()
        {
            MeepleData = new MeepleData(PlayerColor.GRAY, MeepleType.Road, -1);
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("Meeple: Entered OnNetworkSpawn");
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
        public void UpdateMeepleData(MeepleType type, PlayerColor color)
        {
            MeepleData.SetMeepleType(type);
            MeepleData.SetPlayerColor(color);
        }
    }


}
