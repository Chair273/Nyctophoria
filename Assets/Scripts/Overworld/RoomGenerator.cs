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

    private float slopeRatio = 86f / 150f;

    private Tilemap tilemap;

    private List<TileBase>[] tileCatagory = new List<TileBase>[6];// 0:floor, 1:wall, 2:cap, 3:corner, 4:edge, 5: front wall

    private List<Vector3Int> validTiles = new List<Vector3Int>();

    private TileBase[,,] tileTemplate = new TileBase[3, 3, 3];

    private float[,,,] sortLines = new float[3, 3, 3, 3];// first 3 indexes are the indexs of a tile sprite, last index is 0:slope, 1:intercept, 2:slope type

    private Vector3Int[,,,] edgeTemplate = new Vector3Int[3, 3, 3, 3]; // Cordinates of a corner and wall used to get an edge, the Z index of both are ignored as they are already known

    private bool spaceDebounce = false;
    private bool canUpdate = false;

    public int randomness;
    public int randomSizeLimit;

    public bool DebugMode;

    public float DebugSpeed;

    public GameObject objectContainer;

    bool IsInList(List<TileBase> list, TileBase tile)//returns true if the tile is in the list
    {
        foreach (TileBase checkTile in list)
        {
            if (checkTile == tile)
            {
                return true;
            }
        }

        return false;
    }

    Vector3Int GetTileIndex(TileBase tile)//returns the index of the tile, or (1, 1, 0) if it is not found.
    {
        for (int z = 0; z < 3; z++)
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

        Debug.Log("Tile not found:");
        Debug.Log(tile.name);

        return new Vector3Int(1, 1, 0);
    }

    Vector3Int GetCorner(Vector3Int firstPos, Vector3Int secondPos)//returns the corner of both grid positions by returning 0 if either value is 0, or the largest value if there is none for the x and y axis. Returns 1 for the z index due to this function being used to get corners, which are stored above the walls.
    {
        int returnX;
        int returnY;
        if ((firstPos.x == secondPos.x && firstPos.y != secondPos.y) || (firstPos.x != secondPos.x && firstPos.y == secondPos.y))
        {
            if (firstPos.x == secondPos.x)
            {
                return new Vector3Int(2, 1, 1);
            }
            else
            {
                return new Vector3Int(1, 2, 1);
            }
        }
        else
        {
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
        }

        return new Vector3Int(returnX, returnY, 1);

    }

    Vector3Int GetEdge(Vector3Int firstPos, Vector3Int secondPos)//ensures the first index is the corner and the second is a wall
    {
        if (firstPos.z == 1)// if the first argument is the corner
        {
            return edgeTemplate[firstPos.x, firstPos.y, secondPos.x, secondPos.y];
        }
        else if(secondPos.z == 1)// if the second argument is the corner
        {
            return edgeTemplate[secondPos.x, secondPos.y,  firstPos.x, firstPos.y];
        }
        else
        {
            Debug.Log("Neither tile is a corner");
            return new Vector3Int(0, 0, 0);
        }
    }

    IEnumerator PaintTile(Vector3Int pos)//paint the argument tile as a floor, loop through neighbors and paint them depending on previous tile and relative position compared to the argument tile
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkPos = pos + new Vector3Int(x, y, 0);
                TileBase CheckTile = tilemap.GetTile(checkPos);

                if (CheckTile == null)
                {
                    validTiles.Add(checkPos);
                }

                if (x == 0 && y == 0) //if the tile that is trying to be drawn is a floor, then override the old tile
                {
                    tilemap.SetTile(checkPos, tileTemplate[1, 1, 0]);

                    if (DebugMode)
                    {
                        yield return new WaitForSeconds(DebugSpeed / 9);
                    }
                    
                }
                else if (CheckTile == null || IsInList(tileCatagory[2], CheckTile) ) //if there is no tile, or the tile is a cap then override it
                {
                    tilemap.SetTile(checkPos, tileTemplate[x + 1, y + 1, 0]);

                    if (DebugMode)
                    {
                        yield return new WaitForSeconds(DebugSpeed / 9);
                    }

                }
                else if ((x != 0 && y == 0) || (x == 0 && y != 0))//if the tile trying to be drawn is a wall
                {
                    if (IsInList(tileCatagory[1], CheckTile) && !(GetTileIndex(CheckTile) == new Vector3Int(x + 1, y + 1, 0))) //if the old tile is a wall, and the old tile is not the same as the current tile
                    {
                        Vector3Int CornerIndex = GetCorner(GetTileIndex(CheckTile), new Vector3Int(x + 1, y + 1, 0));//get the corresponding corner sprite


                        tilemap.SetTile(checkPos, tileTemplate[CornerIndex.x, CornerIndex.y, 1]);//override the old tile

                        if (DebugMode)
                        {
                            yield return new WaitForSeconds(DebugSpeed / 9);
                        }

                    }
                    else if (IsInList(tileCatagory[3], CheckTile)) //if the old tile is a corner
                    {
                        Vector3Int edgeIndex = GetEdge(GetTileIndex(CheckTile), new Vector3Int(x + 1, y + 1, 0) );

                        tilemap.SetTile(checkPos, tileTemplate[edgeIndex.x, edgeIndex.y, edgeIndex.z]);

                        if (DebugMode)
                        {
                            yield return new WaitForSeconds(DebugSpeed / 9);
                        }

                    }
                }

            }
        }

    }

    IEnumerator MakeRoom()//generate a random room
    {
        canUpdate = false;

        testRoomSizeX = Random.Range(2, 6) * 2;
        testRoomSizeY = Random.Range(2, 6) * 2;

        validTiles = new List<Vector3Int>();

        for (int x = -testRoomSizeX / 2; x < testRoomSizeX / 2; x++)//create base room area
        {
            for (int y = -testRoomSizeY / 2; y < testRoomSizeY / 2; y++)
            {
                StartCoroutine(PaintTile(new Vector3Int(x, y, 0)));

                if (DebugMode)
                {
                    yield return new WaitForSeconds(DebugSpeed);
                }
            }
        }

        for (int i = 0; i < randomness; i++)//loop through random points near the edges of the base area and generate smaller areas around those points
        {
            int randomPosX;
            int randomPosY;

            int randomSizeX = Random.Range(1, randomSizeLimit / 2 + 1) * 2;
            int randomSizeY = Random.Range(1, randomSizeLimit / 2 + 1) * 2;

            //gets a random point along the edge
            if (Random.Range(0, 2) == 1) //chooses if the point is along the positive or negative position
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



            for (int x = -randomSizeX / 2; x < randomSizeX / 2; x++)//create the smaller area around the random point
            {
                for (int y = -randomSizeY / 2; y < randomSizeY / 2; y++)
                {
                    StartCoroutine(PaintTile(new Vector3Int(x + randomPosX, y + randomPosY, 0)));

                    if (DebugMode)
                    {

                        yield return new WaitForSeconds(DebugSpeed);
                    }

                }
            }
        }

        if (DebugMode)
        {
            yield return new WaitForSeconds(DebugSpeed * 3);
        }

        canUpdate = true;
    }

    void UpdateDrawOrder()//sorts tiles and objects in order to give the illusion of 3d depth, tiles use a line to determine position while objects are just a point
    {
        List<Transform> drawOrder = new List<Transform>();//list of objects

        for (int childIndex = 0; childIndex < objectContainer.transform.childCount; childIndex++)
        {
            drawOrder.Add(objectContainer.transform.GetChild(childIndex));
        }

        int sortIndex = 1;
        while (sortIndex < drawOrder.Count)//sort from lowest object to highest
        {
            if (sortIndex != 0 && drawOrder[sortIndex].position.y < drawOrder[sortIndex - 1].position.y)
            {
                Transform tempVal = drawOrder[sortIndex - 1];
                drawOrder[sortIndex - 1] = drawOrder[sortIndex];
                drawOrder[sortIndex] = tempVal;

                sortIndex--;
            }
            else
            {
                sortIndex++;
            }
        }

        for (int i = 0; i < drawOrder.Count; i ++)//set object z index so that its in the correct order
        {
            Vector3 pos = drawOrder[i].position;
            drawOrder[i].position = new Vector3(pos.x, pos.y, i + 0.5f);
        }

        List<Vector3Int>[] heightOrder = new List<Vector3Int>[drawOrder.Count + 1];//array of lists of tiles (each array index is the relevant Z position, the lists contain every tile at that position)

        for (int i = 0; i < drawOrder.Count + 1; i ++)
        {
            heightOrder[i] = new List<Vector3Int>();//instantiate the array
        }

        foreach (Vector3Int tilePos in validTiles)//loop through every tile and determine which Z position it should be assigned to depending on its line and the position of each object
        {
            int currentIndex = -1;

            Vector3Int tileIndex = GetTileIndex(tilemap.GetTile(tilePos));
            Vector3 tileWorldPos = tilemap.CellToWorld(tilePos);

            int slopeType = (int) sortLines[tileIndex.x, tileIndex.y, tileIndex.z, 2];//0 means the line is linear (makes a / shape), -1/1 means the line is absolute (makes a V shape)
            float slope = sortLines[tileIndex.x, tileIndex.y, tileIndex.z, 0] * (slopeType == -1 ? -1 : 1);
            float intercept = tileWorldPos.y + sortLines[tileIndex.x, tileIndex.y, tileIndex.z, 1] / 150 - tileWorldPos.x * slope;
            
            while (currentIndex < drawOrder.Count - 1 && drawOrder[currentIndex + 1].position.y < (slopeType == 0 ? (slope *  drawOrder[currentIndex + 1].position.x) + intercept : (slope * Mathf.Abs(drawOrder[currentIndex + 1].position.x - tileWorldPos.x)) + tileWorldPos.y))
            { 
                currentIndex++;
            }


            heightOrder[currentIndex + 1].Add(tilePos);//once it finds a object that is below the tile, set the tiles Z position to the position above that object
            //note that while tiles and objects change Z position by the same amount, objects are offset by 0.5 to prevent overlap, that means that if a object and tile are on the same relative Z index, the object will be drawn in front.
        }

        for (int i = 0; i < heightOrder.Length; i++)//loop through every tile and set its Z position to the correct value, also updates validTiles to contain the correct position for the tile
        {
            foreach(Vector3Int tilePos in heightOrder[i])
            {
                Vector3Int tileIndex = GetTileIndex(tilemap.GetTile(tilePos));

                tilemap.SetTile(tilePos, null);
                tilemap.SetTile(new Vector3Int(tilePos.x, tilePos.y, i), tileTemplate[tileIndex.x, tileIndex.y, tileIndex.z]);

                validTiles.RemoveAt(validTiles.IndexOf(tilePos));
                validTiles.Add(new Vector3Int(tilePos.x, tilePos.y, i));
            }
        }

    }

    void Start()//called once at the start of the game
    {

        tilemap = gameObject.GetComponent<Tilemap>();

        TileBase[] tiles = Resources.LoadAll<TileBase>("CryptTileset");//load the tileset, currently only loads specifically the crypt one but later it shouldnt be too hard to have it choose by having a list of names and just referencing the list and redefining the tileset, or something along those lines.

        for (int i = 0; i < tileCatagory.Length; i++)//instantiate the tileCatagory array
        {
            tileCatagory[i] = new List<TileBase>();
        }

        foreach (TileBase tile in tiles)//this works, but there has to be a better way of doing it than manually assigning each one
        {
            if (tile.name.Equals("Floor"))//the name of the tile
            {
                tileTemplate[1, 1, 0] = tile;//which index references this tile
                sortLines[1, 1, 0, 0] = 0;//the slope for the sort line equation
                sortLines[1, 1, 0, 1] = 150000;//the offset relative to the center for the sort line equation (used to get the intercept, but isnt the intercept by itself)

                tileCatagory[0].Add(tile);//the catagory of the tile, used to determine which tiles can override others and which can combine to make new tiles
            }
            else if (tile.name.Equals("FrontWall"))
            {
                tileTemplate[2, 1, 0] = tile;
                sortLines[2, 1, 0, 0] = -slopeRatio;
                sortLines[2, 1, 0, 1] = 0;

                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("BackWall"))
            {
                tileTemplate[0, 1, 0] = tile;
                sortLines[0, 1, 0, 0] = -slopeRatio;
                sortLines[0, 1, 0, 1] = 72;

                tileCatagory[1].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("LeftWall"))
            {
                tileTemplate[1, 2, 0] = tile;
                sortLines[1, 2, 0, 0] = slopeRatio;
                sortLines[1, 2, 0, 1] = 0;

                tileCatagory[1].Add(tile);
            }
            else if (tile.name.Equals("RightWall"))
            {
                tileTemplate[1, 0, 0] = tile;
                sortLines[1, 0, 0, 0] = slopeRatio;
                sortLines[1, 0, 0, 1] = 72;

                tileCatagory[1].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("DownCap"))
            {
                tileTemplate[0, 0, 0] = tile;
                sortLines[0, 0, 0, 0] = 0;
                sortLines[0, 0, 0, 1] = -150000;

                tileCatagory[2].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("UpCap"))
            {
                tileTemplate[2, 2, 0] = tile;
                sortLines[2, 2, 0, 0] = 0;
                sortLines[2, 2, 0, 1] = -150000;

                tileCatagory[2].Add(tile);
            }
            else if (tile.name.Equals("LeftCap"))
            {
                tileTemplate[0, 2, 0] = tile;
                sortLines[0, 2, 0, 0] = slopeRatio;
                sortLines[0, 2, 0, 1] = 0;

                tileCatagory[2].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("RightCap"))
            {
                tileTemplate[2, 0, 0] = tile;
                sortLines[2, 0, 0, 0] = -slopeRatio;
                sortLines[2, 0, 0, 1] = 0;

                tileCatagory[2].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("UpCorner"))
            {
                tileTemplate[2, 2, 1] = tile;
                sortLines[2, 2, 1, 0] = slopeRatio;
                sortLines[2, 2, 1, 1] = 0;
                sortLines[2, 2, 1, 2] = 1;

                edgeTemplate[2, 2, 2, 1] = new Vector3Int(2, 2, 1);//same idea as the tileTemplate, but it takes the x and y of 2 different tiles (corner + wall or hall + wall) and returns the edge that would be created from it
                edgeTemplate[2, 2, 1, 2] = new Vector3Int(2, 2, 1);

                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("LeftCorner"))
            {
                tileTemplate[0, 2, 1] = tile;
                sortLines[0, 2, 1, 0] = slopeRatio;
                sortLines[0, 2, 1, 1] = 0;

                edgeTemplate[0, 2, 0, 1] = new Vector3Int(0, 2, 1);
                edgeTemplate[0, 2, 1, 2] = new Vector3Int(0, 2, 1);

                tileCatagory[3].Add(tile);
                tileCatagory[5].Add(tile);
            }
            else if (tile.name.Equals("RightCorner"))
            {
                tileTemplate[2, 0, 1] = tile;
                sortLines[2, 0, 1, 0] = -slopeRatio;
                sortLines[2, 0, 1, 1] = 0;

                tileCatagory[5].Add(tile);

                edgeTemplate[2, 0, 2, 1] = new Vector3Int(2, 0, 1);
                edgeTemplate[2, 0, 1, 0] = new Vector3Int(2, 0, 1);

                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("DownCorner"))
            {
                tileTemplate[0, 0, 1] = tile;
                sortLines[0, 0, 1, 0] = slopeRatio;
                sortLines[0, 0, 1, 1] = 0;
                sortLines[0, 0, 1, 2] = -1;

                tileCatagory[5].Add(tile);

                edgeTemplate[0, 0, 1, 0] = new Vector3Int(0, 0, 1);
                edgeTemplate[0, 0, 0, 1] = new Vector3Int(0, 0, 1);

                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("HorHall"))
            {
                tileTemplate[1, 2, 1] = tile;
                sortLines[1, 2, 1, 0] = -slopeRatio;
                sortLines[1, 2, 1, 1] = 0;

                edgeTemplate[1, 2, 0, 1] = new Vector3Int(1, 2, 1);
                edgeTemplate[1, 2, 2, 1] = new Vector3Int(1, 2, 1);

                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("VertHall"))
            {
                tileTemplate[2, 1, 1] = tile;
                sortLines[2, 1, 1, 0] = slopeRatio;
                sortLines[2, 1, 1, 1] = 0;

                edgeTemplate[2, 1, 0, 1] = new Vector3Int(2, 1, 1);
                edgeTemplate[2, 1, 2, 1] = new Vector3Int(2, 1, 1);

                tileCatagory[3].Add(tile);
            }
            else if (tile.name.Equals("UpEdge"))
            {
                tileTemplate[2, 1, 2] = tile;
                sortLines[2, 1, 2, 0] = slopeRatio;
                sortLines[2, 1, 2, 1] = 0;
                sortLines[2, 1, 2, 1] = 1;

                edgeTemplate[2, 0, 1, 2] = new Vector3Int(2, 1, 2);
                edgeTemplate[2, 2, 1, 0] = new Vector3Int(2, 1, 2);
                edgeTemplate[2, 1, 2, 1] = new Vector3Int(2, 1, 2);

                tileCatagory[4].Add(tile);
            }
            else if (tile.name.Equals("LeftEdge"))
            {
                tileTemplate[1, 2, 2] = tile;
                sortLines[1, 2, 2, 0] = slopeRatio;
                sortLines[1, 2, 2, 1] = 0;
                sortLines[1, 2, 2, 2] = 1;

                edgeTemplate[0, 2, 2, 1] = new Vector3Int(1, 2, 2);
                edgeTemplate[2, 2, 0, 1] = new Vector3Int(1, 2, 2);
                edgeTemplate[1, 2, 1, 2] = new Vector3Int(1, 2, 2);

                tileCatagory[4].Add(tile);
            }
            else if (tile.name.Equals("RightEdge"))
            {
                tileTemplate[1, 0, 2] = tile;
                sortLines[1, 0, 2, 0] = -slopeRatio;
                sortLines[1, 0, 2, 1] = 0;

                edgeTemplate[2, 0, 0, 1] = new Vector3Int(1, 0, 2);
                edgeTemplate[0, 0, 2, 1] = new Vector3Int(1, 0, 2);
                edgeTemplate[1, 2, 1, 0] = new Vector3Int(1, 0, 2);

                tileCatagory[4].Add(tile);
            }
            else if (tile.name.Equals("DownEdge"))
            {
                tileTemplate[0, 1, 2] = tile;
                sortLines[0, 1, 2, 0] = slopeRatio;
                sortLines[0, 1, 2, 1] = 72;

                edgeTemplate[0, 0, 1, 2] = new Vector3Int(0, 1, 2);
                edgeTemplate[0, 2, 1, 0] = new Vector3Int(0, 1, 2);
                edgeTemplate[2, 1, 0, 1] = new Vector3Int(0, 1, 2);

                tileCatagory[4].Add(tile);
            }
            else
            {
                Debug.Log("Unknown tile: " + tile.name);
            }
        }

        StartCoroutine(MakeRoom());
    }

    void Update()//called once per frame automatically
    {
        if (!spaceDebounce && Input.GetKeyDown("space"))//if the user presses space it makes a new room, can only generate a new room again once space is released for at least 1 frame
        {
            spaceDebounce = true;
            tilemap.ClearAllTiles();
            StartCoroutine(MakeRoom());
        }
        else if (spaceDebounce && !Input.GetKeyDown("space"))
        {
            spaceDebounce = false;
        }

        if (canUpdate)//if the room is generated then update the Z position of every object and tile each frame
        {
            UpdateDrawOrder();//remember to optimise by making it check the current object positions against the previous object positions
        }
    }
}