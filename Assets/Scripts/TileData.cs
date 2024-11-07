using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileData", menuName = "Tiles/Tile Data", order = 51)]
public class TileData : ScriptableObject
{
    [Header("Center Feature")]
    public FeatureType centerFeature;

    [Header("Edge Features")]
    public FeatureType northEdge;
    public FeatureType eastEdge;
    public FeatureType southEdge;
    public FeatureType westEdge;

    public Sprite tileSprite;

    public int count;
}
