using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;

public class RoomGenerator : MonoBehaviour
{
    public TileBase _tile;
    public static TileBase tile;

    public TileBase _doorTile;
    public static TileBase doorTile;

    public Transform _player;
    public static Transform player;

    public static Tilemap tilemap;

    private void Start()
    {
        tilemap = transform.GetComponent<Tilemap>();
        tile = _tile;
        doorTile = _doorTile;
        player = _player;

        transform.GetComponent<ShadowCaster2D>().enabled = !MainManager.LowGraphicsMode;
    }
}
