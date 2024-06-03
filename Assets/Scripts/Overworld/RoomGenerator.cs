using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    public static RoomGenerator main;

    public TileBase tile;

    public Transform player;
    public Transform ObjectContainer;

    private void Start()
    {
        main = this;
    }
}
