using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts
{
    public struct TileFeatureKeyDTO : INetworkSerializable
    {
        public Vector2Int tilePosition;
        public FeatureType featureType;
        public int featureIndex;

        public TileFeatureKeyDTO(Vector2Int tilePosition, FeatureType featureType, int featureIndex)
        {
            this.tilePosition = tilePosition;
            this.featureType = featureType;
            this.featureIndex = featureIndex;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tilePosition);
            serializer.SerializeValue(ref featureType);
            serializer.SerializeValue(ref featureIndex);
        }
    }
}
