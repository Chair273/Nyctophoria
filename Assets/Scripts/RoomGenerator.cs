using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;
using static UnityEditor.PlayerSettings;

public class RoomGenerator : MonoBehaviour
{
    private int testRoomSizeX;
    private int testRoomSizeY;

    private Tilemap tilemap;

    private TileBase[,,] tileTemplate = new TileBase[3, 3, 2];
    private List<TileBase>[] tileCatagory = new List<TileBase>[4];// 0:floor, 1:wall, 2:cap, 3:corner

    public int randomness;
    public int randomSizeLimit;

    bool IsInList(List<TileBase> list, TileBase tile)//returns true if the tile is in the list
    {
        foreach (TileBase checkTile in list)
        {
            if (checkTile == tile)
            {
                Debug.Log(tile.name + " is in list");
                return true;
            }
        }

        Debug.Log(tile.name + " isnt in list");

        return false;
    }

    Vector3Int GetTileIndex(TileBase tile)//returns the index of the tile, or (1, 1, 0) if it is not found.
    {
        for (int z = 0; z < 2; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (tile == tileTemplate[x, y, z])
                    {
                        return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        Debug.Log("Tile not found: " + tile.name);

        return new Vector3Int(1, 1, 0);
    }

    Vector3Int GetCorner(Vector3Int firstPos, Vector3Int secondPos)//returns the corner of both grid positions by returning 0 if either value is 0, or the largest value if there is none for the x and y axis. Returns 1 for the z index due to this function being used to get corners, which are stored above the walls.
    {
        int returnX;
        int returnY;

        if (firstPos.x == 0 || secondPos.x == 0)
        {
            returnX = 0;
        }
        else
        {
            if (firstPos.x > secondPos.x)
            {
                returnX = firstPos.x;
            }
            else
            {
                returnX = secondPos.x;
            }
        }


        if (firstPos.y == 0 || secondPos.y == 0)
        {
            returnY = 0;
        }
        else
        {
            if (firstPos.y > secondPos.y)
            {
                returnY = firstPos.y;
            }
            else
            {
                returnY = secondPos.y;
            }
        }

        Debug.Log(returnX + ", " + returnY);

        return new Vector3Int(returnX, returnY, 1);

    }

    void PaintTile(Vector3Int pos)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, x + y);
                TileBase CheckTile = tilemap.GetTile(checkPos);

                if (CheckTile == null || IsInList(tileCatagory[2], CheckTile)) //if there is no tile, or the tile is a cap then override it
                {
                    tilemap.SetTile(checkPos, tileTemplate[x + 1, y + 1, 0]);
                }
                else if (x == 0 && y == 0) //if the tile that is trying to be drawn is a floor, then override the old tile
                {
                    tilemap.SetTile(checkPos, tileTemplate[1, 1, 0]);
                }
                else if (((x != 0 && y == 0) || (x == 0 && y != 0)) && IsInList(tileCatagory[1], CheckTile) && !(GetTileIndex(CheckTile) == new Vector3Int(x + 1, y + 1, 0)) )//if the tile trying to be drawn is a wall, and the old tile is a wall, and the old tile is not the same as the current tile
                {
                    Debug.Log("Replacing: " + CheckTile.name);
                    Vector3Int CornerIndex = GetCorner(GetTileIndex(CheckTile), new Vector3Int(x + 1, y + 1, 0) );//get the corresponding corner sprite
                    tilemap.SetTile(checkPos, tileTemplate[ CornerIndex.x, CornerIndex.y, 1 ] );//override the old tile
                }
            }
        }
    }

    void Start()
    {
        testRoomSizeX = Random.Range(2, 6) * 2;
        testRoomSizeY = Random.Range(2, 6) * 2;

        tilemap = gameObject.GetComponent<Tilemap>();

        TileBase[] tiles = Resources.LoadAll<TileBase>("CryptTileset");

        for (int i = 0; i < tileCatagory.Length; i++)
        {
            tileCatagory[i] = new List<TileBase>();
        }

        foreach (TileBase tile in tiles)
        {
            if (tile.name.Equals("Floor"))//works, but there has to be a better way of doing it than manually assigning each one
            {
                tileTemplate[1, 1, 0] = tile;
                tileCatagory[0].Add(tile);
            }
            else if (tile.name.Equals("FrontWall"))
            {
                tileTemplate[2, 1, 0] = tile;
                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("BackWall"))
            {
                tileTemplate[0, 1, 0] = tile;
                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("LeftWall"))
            {
                tileTemplate[1, 2, 0] = tile;
                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("RightWall"))
            {
                tileTemplate[1, 0, 0] = tile;
                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("DownCap"))
            {
                tileTemplate[0, 0, 0] = tile;
                tileCatagory[2].Add(tile);
            }
            else if (tile.name.Equals("UpCap"))
            {
                tileTemplate[2, 2, 0] = tile;
                tileCatagory[2].Add(tile);
            }
            else if (tile.name.Equals("LeftCap"))
            {
                tileTemplate[0, 2, 0] = tile;
                tileCatagory[2].Add(tile);
            }
            else if (tile.name.Equals("RightCap"))
            {
                tileTemplate[2, 0, 0] = tile;
                tileCatagory[2].Add(tile);
            }
            else if (tile.name.Equals("UpCorner"))
            {
                tileTemplate[2, 2, 1] = tile;
                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("LeftCorner"))
            {
                tileTemplate[0, 2, 1] = tile;
                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("RightCorner"))
            {
                tileTemplate[2, 0, 1] = tile;
                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("DownCorner"))
            {
                tileTemplate[0, 0, 1] = tile;
                tileCatagory[3].Add(tile);
            }
            else
            {
                Debug.Log("Unknown tile: " + tile.name);
            }
        }

        for (int x = -testRoomSizeX / 2; x < testRoomSizeX / 2; x++)
        {
            for (int y = -testRoomSizeY / 2; y < testRoomSizeY / 2; y++)
            {
                PaintTile(new Vector3Int(x, y, x + y));
            }
        }

        for (int i = 0; i < randomness; i++)
        {
            int randomPosX;
            int randomPosY;

            int randomSizeX = Random.Range(1, randomSizeLimit / 2 + 1) * 2;
            int randomSizeY = Random.Range(1, randomSizeLimit / 2 + 1) * 2;

            //gets a random point along the edge
            if (Random.Range(0,2) == 1) //chooses if the point is along the positive or negative position
            {
                if (Random.Range(0, 2) == 1) //choses which face the point is near
                {
                    randomPosX = (testRoomSizeX / 2) - Random.Range(0, 3);
                    randomPosY = Random.Range(-testRoomSizeY / 2, testRoomSizeY / 2);
                }
                else
                {
                    randomPosX = Random.Range(-testRoomSizeX / 2, testRoomSizeX / 2);
                    randomPosY = (testRoomSizeY / 2) - Random.Range(0, 3);
                }
                    
            }
            else
            {
                if (Random.Range(0, 2) == 1)
                {
                    randomPosX = (-testRoomSizeX / 2) + Random.Range(0, 3);
                    randomPosY = Random.Range(-testRoomSizeY / 2, testRoomSizeY / 2);
                }
                else
                {
                    randomPosX = Random.Range(-testRoomSizeX / 2, testRoomSizeX / 2);
                    randomPosY = (-testRoomSizeY / 2) + Random.Range(0, 3);
                }
            }



            for (int x = -randomSizeX / 2; x < randomSizeX / 2; x++)
            {
                for (int y = -randomSizeY / 2; y < randomSizeY / 2; y++)
                {
                    PaintTile(new Vector3Int(x + randomPosX, y + randomPosY, x + randomPosX + y + randomPosY));
                }
            }
        }
    }

    void Update()
    {
        
    }
}
