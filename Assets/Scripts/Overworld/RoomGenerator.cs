
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    public TileBase _tile;
    public static TileBase tile;

    public TileBase _doorTile;
    public static TileBase doorTile;

    public Transform _player;
    public static Transform player;

    public static Tilemap tilemap;

    private static Vector2Int currentRoom;

    private static Room[,] area;

    public static void ChangeRoom(Vector2Int roomIndex)
    {
        area[currentRoom.x, currentRoom.y].ClearRoom();
        area[roomIndex.x, roomIndex.y].LoadRoom();

        Vector2Int roomVector = new Vector2Int(currentRoom.x - roomIndex.x, currentRoom.y - roomIndex.y);
        Vector3 doorPos = area[roomIndex.x, roomIndex.y].getDoorPos(roomVector);

        player.position = tilemap.CellToWorld(tilemap.WorldToCell(doorPos) -  new Vector3Int(roomVector.x, roomVector.y, 0)) + new Vector3(0, 0.5f, 0);

        currentRoom = roomIndex;
        Manager.SetCurrentRoom(currentRoom);
    }

    public static void ChangeRoom()
    {
        area[currentRoom.x, currentRoom.y].LoadRoom();
    }

    private void Start()
    {
        tilemap = transform.GetComponent<Tilemap>();
        tile = _tile;
        doorTile = _doorTile;
        player = _player;

        if (area == null)
        {
            area = Manager.GetArea();
            currentRoom = Manager.GetCurrentRoom();
        }

        area[currentRoom.x, currentRoom.y].LoadRoom();
    }

    public static Room[,] GenerateArea()
    {
        area = new Room[7, 7];

        Room startRoom = new Room( new Vector2Int( Random.Range(1, 6), Random.Range(1, 3) ) );
        Vector2Int startIndex = startRoom.GetRoomIndex();
        currentRoom = startIndex;
        Manager.SetCurrentRoom(currentRoom);

        Room endRoom = new Room( new Vector2Int( Random.Range(1, 6), Random.Range(4, 6) ) );
        Vector2Int endIndex = endRoom.GetRoomIndex();

        area[startIndex.x, startIndex.y] = startRoom;
        area[endIndex.x, endIndex.y] = endRoom;

        Vector2Int currentIndex = startIndex;
        List<Room> mainPath = new List<Room> { startRoom };
        List<Room> allRooms = new List<Room> { startRoom , endRoom };

        while (!currentIndex.Equals(endIndex))
        {
            if (currentIndex.x != endIndex.x && currentIndex.y != endIndex.y)
            {
                if (Random.Range(0, 2) == 0)
                {
                    currentIndex += new Vector2Int(Mathf.Clamp(endIndex.x - currentIndex.x, -1, 1), 0);
                }
                else
                {
                    currentIndex += new Vector2Int(0, Mathf.Clamp(endIndex.y - currentIndex.y, -1, 1));
                }
            }
            else
            {
                currentIndex += new Vector2Int(Mathf.Clamp(endIndex.x - currentIndex.x, -1, 1), Mathf.Clamp(endIndex.y - currentIndex.y, -1, 1));
            }

            if (area[currentIndex.x, currentIndex.y] == null)
            {
                area[currentIndex.x, currentIndex.y] = new Room(currentIndex);
                mainPath.Add(area[currentIndex.x, currentIndex.y]);
                allRooms.Add(area[currentIndex.x, currentIndex.y]);
            }
        }

        foreach (Room room in mainPath)//add extra rooms
        {
            Vector2Int roomIndex = room.GetRoomIndex();

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector2Int checkPos = new Vector2Int(roomIndex.x + x, roomIndex.y + y);

                    if (Random.Range(0, 3) == 0 && area[checkPos.x, checkPos.y] == null)
                    {
                        area[checkPos.x, checkPos.y] = new Room(checkPos);
                        allRooms.Add(area[checkPos.x, checkPos.y]);
                    }
                }
            }
        }

        foreach (Room room in allRooms)//make doors
        {
            room.AddDoors( GetDoors( room.GetRoomIndex() ) );
        }

        GameObject plagueCaster = Instantiate(Resources.Load<GameObject>("CombatPrefabs/PlayerAdder"));

        Debug.Log(allRooms.Count);

        allRooms[Random.Range(0, allRooms.Count)].AddObject(plagueCaster);

        GameObject enemyPrefab = Resources.Load<GameObject>("CombatPrefabs/Enemy");

        int amount = Random.Range(5, 9);

        for (int i = 0; i < amount; i++)
        {
            GameObject enemy = Instantiate<GameObject>(enemyPrefab);
            allRooms[Random.Range(0, allRooms.Count)].AddObject(enemy);
        }

        return area;
    }

    public static List<Vector2Int> GetDoors(Vector2Int roomIndex)
    {
        List<Vector2Int> doors = new List<Vector2Int>();

        for (int x = -1; x <= 1; x += 2)
        {
            Vector2Int checkPos = new Vector2Int(roomIndex.x + x, roomIndex.y);

            if (checkPos.x >= 0 && checkPos.x < 7 && area[checkPos.x, checkPos.y] != null)
            {
                doors.Add(new Vector2Int(x, 0));
            }
        }

        for (int y = -1; y <= 1; y += 2)
        {
            Vector2Int checkPos = new Vector2Int(roomIndex.x, roomIndex.y + y);

            if (checkPos.y >= 0 && checkPos.y < 7 && area[checkPos.x, checkPos.y] != null)
            {
                doors.Add(new Vector2Int(0, y));
            }
        }

        return doors;
    }
}

public class Room
{
    private static GameObject doorPrefab = Resources.Load<GameObject>("Overworld/Prefabs/Door");

    private bool loadedBefore;

    private Vector2Int roomIndex;//the grid position of the room

    private List<GameObject> doors;

    private List<Vector2Int> doorIndexes;//indicates which doors the room should have

    private Dictionary<Vector2Int, Vector3> doorPositions;//world positions of each door

    private List<GameObject> roomContents = new List<GameObject>();

    private static Dictionary<string, Vector2Int> wallToIndex = new Dictionary<string, Vector2Int>//converts a tile name to the doorIndex it corrisponds too
    {
        {"FrontWall" ,new Vector2Int(1, 0)},
        {"BackWall" ,new Vector2Int(-1, 0)},
        {"RightWall" ,new Vector2Int(0, -1)},
        {"LeftWall" ,new Vector2Int(0, 1)}
    };

    private static Dictionary<Vector2Int, TileBase> IndexToTile;

    private Dictionary<Vector2Int, Vector2Int> roomPoints;//keys are the position of the point, values are the size. Used to create random room shapes

    public Room (Vector2Int roomIndex)
    {
        this.roomIndex = roomIndex;

        doorIndexes = new List<Vector2Int>();

        Generate();
    }

    public void Generate()
    {
        IndexToTile = new Dictionary<Vector2Int, TileBase>
        {
            {new Vector2Int(1, 0), Resources.Load<TileBase>("OverWorld/Tiles/FrontDoor")},
            {new Vector2Int(-1, 0), Resources.Load<TileBase>("OverWorld/Tiles/BackDoor")},
            {new Vector2Int(0, -1), Resources.Load<TileBase>("OverWorld/Tiles/RightDoor")},
            {new Vector2Int(0, 1), Resources.Load<TileBase>("OverWorld/Tiles/LeftDoor")}
        };

        doorPositions = new Dictionary<Vector2Int, Vector3>();
        doors = new List<GameObject>();

        Vector2Int baseSize = new Vector2Int(Random.Range(4, 9), Random.Range(4, 9));

        roomPoints = new Dictionary<Vector2Int, Vector2Int>
        {
            {Vector2Int.zero, baseSize}
        };

        int pointAmount = Random.Range(3, 6);

        for (int i = 0; i < pointAmount; i++)
        {
            Vector2Int randomSize = new Vector2Int(Random.Range(4, 7), Random.Range(4, 7));
            Vector2Int randomPoint = new Vector2Int
                (
                    Random.Range((randomSize.x - baseSize.x) / 2 + 1, (baseSize.x + randomSize.x)  / 2 - 1),
                    Random.Range((randomSize.y - baseSize.y) / 2 + 1, (baseSize.y + randomSize.y)  / 2 - 1)
                );

            roomPoints[randomPoint] = randomSize;
        }
    }

    public void LoadRoom()
    {
        Tilemap tilemap = RoomGenerator.tilemap;

        foreach(Vector2Int pos in roomPoints.Keys)
        {
            for (int x = pos.x - roomPoints[pos].x / 2; x <= pos.x + roomPoints[pos].x / 2; x++)
            {
                for (int y = pos.y - roomPoints[pos].y / 2; y <= pos.y + roomPoints[pos].y / 2; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), RoomGenerator.tile);
                }
            }
        }

        if(!loadedBefore)
        {
            Dictionary<Vector2Int, List<Vector3Int>> validWalls = new Dictionary<Vector2Int, List<Vector3Int>>();
            List<Vector3Int> floorPositions = new List<Vector3Int>();

            foreach (Vector2Int doorIndex in doorIndexes)
            {
                validWalls.Add(doorIndex, new List<Vector3Int>());
            }

            for (int x = tilemap.origin.x; x < tilemap.origin.x + tilemap.size.x; x++)
            {
                for (int y = tilemap.origin.x; y < tilemap.origin.x + tilemap.size.x; y++)
                {
                    Vector3Int checkPos = new Vector3Int(x, y, 0);

                    if (tilemap.GetTile(checkPos) == null)
                    {
                        continue;
                    }

                    string tileName = tilemap.GetSprite(new Vector3Int(x, y, 0)).name;

                    if (tileName.Equals("Floor"))
                    {
                        floorPositions.Add(new Vector3Int(x, y, 0));
                    }

                    if (!wallToIndex.ContainsKey(tileName))
                    {
                        continue;
                    }

                    if (doorIndexes.Contains( wallToIndex[ tileName ] ) )
                    {
                        validWalls[wallToIndex[tileName]].Add(new Vector3Int(x, y, 0));
                    }
                }
            }

            foreach (Vector2Int doorIndex in validWalls.Keys)
            {
                List<Vector3Int> doorList = validWalls[doorIndex];

                if (doorList.Count > 0)
                {
                    Vector3Int randomDoor = doorList[Random.Range(0, doorList.Count)];

                    doorPositions[doorIndex] = tilemap.CellToWorld(randomDoor);
                }
                else
                {
                    Debug.LogWarning("Missing valid wall for door");

                    tilemap.ClearAllTiles();

                    Generate();
                    LoadRoom();

                    return;
                }

            }

            foreach (GameObject thing in roomContents)
            {
                int randomIndex = Random.Range(0, floorPositions.Count);

                thing.transform.position = tilemap.CellToWorld(floorPositions[randomIndex]) + new Vector3(0, 0, -1);
                floorPositions.RemoveAt(randomIndex);

                thing.SetActive(true);
            }

            loadedBefore = true;
        }

        if (doorIndexes.Count == 0)
        {
            Debug.LogWarning("No doors.");

            doorIndexes = RoomGenerator.GetDoors(roomIndex);
        }

        foreach (Vector2Int doorIndex in doorIndexes)
        {
            Vector3 doorPosition = doorPositions[doorIndex];
            Vector3Int tilePosition = tilemap.WorldToCell(doorPosition);

            tilemap.SetTile(tilePosition + new Vector3Int(0, 0, -1), IndexToTile[doorIndex]);

            tilemap.SetTileFlags(tilePosition, TileFlags.None);
            tilemap.SetColor(tilePosition, new Color32(255, 255, 255, 0));


            GameObject doorObject = Object.Instantiate(doorPrefab);
            doorObject.transform.position = (tilemap.CellToWorld(tilePosition) + tilemap.CellToWorld(tilePosition - new Vector3Int(doorIndex.x, doorIndex.y, 0))) / 2 + new Vector3(0, 0.5f, 0);
            doorObject.GetComponent<RoomTransfer>().SetRoomIndex(roomIndex + doorIndex);

            doors.Add(doorObject);
        }
    }


    public Vector3 getDoorPos(Vector2Int doorIndex)
    {
        return doorPositions[doorIndex];
    }

    public Vector2Int GetRoomIndex()
    {
        return roomIndex;
    }

    public void AddDoors(List<Vector2Int> doorIndexes)
    {
        this.doorIndexes = doorIndexes;
    }

    public void ClearRoom()
    {
        RoomGenerator.tilemap.ClearAllTiles();

        for (int i = doors.Count - 1; i >= 0; i--)
        {
            Object.Destroy(doors[i]);
        }

        doors = new List<GameObject>();

        foreach(GameObject thing in roomContents)
        {
            thing.SetActive(false);
        }
    }

    public void AddObject(GameObject newObject)
    {
        roomContents.Add(newObject);
    }

    public void RemoveObject(GameObject newObject)
    {
        roomContents.Remove(newObject);
        Object.Destroy(newObject);
    }
}
