using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/Terrain Rule Tile")]
public class TerrainRuleTile : RuleTile
{
    [Header("允许无缝连接的其他 Tile")]
    [Tooltip("把你的斜坡 Tile 拖到这里，平地就会把它们当成自己人，不再显示边框")]
    public TileBase[] connectedTiles;

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case TilingRule.Neighbor.This:
                if (tile == this) return true;
                
                if (connectedTiles != null)
                {
                    for (int i = 0; i < connectedTiles.Length; i++)
                    {
                        if (tile == connectedTiles[i]) return true;
                    }
                }
                return false;

            case TilingRule.Neighbor.NotThis:
                if (tile == this) return false;
                
                if (connectedTiles != null)
                {
                    for (int i = 0; i < connectedTiles.Length; i++)
                    {
                        if (tile == connectedTiles[i]) return false;
                    }
                }
                return true;
        }

        return base.RuleMatch(neighbor, tile);
    }
}