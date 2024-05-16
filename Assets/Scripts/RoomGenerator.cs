
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    public TileBase _tile;
    public static TileBase tile;

    public Transform _player;
    public static Transform player;

    public static Tilemap tilemap;

    private static Room[,] area = new Room[7, 7];

    private static Vector2Int currentRoom;

    public static void ChangeRoom(Vector2Int roomIndex)
    {
        area[currentRoom.x, currentRoom.y].ClearRoom();
        area[roomIndex.x, roomIndex.y].LoadRoom();

        Vector2Int roomVector = new Vector2Int(currentRoom.x - roomIndex.x, currentRoom.y - roomIndex.y);
        Vector3 doorPos = area[roomIndex.x, roomIndex.y].getDoorPos(roomVector);

        player.position = tilemap.CellToWorld(tilemap.WorldToCell(doorPos) - 2 * new Vector3Int(roomVector.x, roomVector.y, 0));

        currentRoom = roomIndex;
    }

    private void Start()
    {
        tilemap = transform.GetComponent<Tilemap>();
        tile = _tile;
        player = _player;

        Begin();
    }

    protected static void Begin()
    {
        Room startRoom = new Room( new Vector2Int( Random.Range(1, 6), Random.Range(1, 3) ) );
        Vector2Int startIndex = startRoom.GetRoomIndex();
        currentRoom = startIndex;

        Room endRoom = new Room( new Vector2Int( Random.Range(1, 6), Random.Range(4, 6) ) );
        Vector2Int endIndex = endRoom.GetRoomIndex();

        area[startIndex.x, startIndex.y] = startRoom;
        area[endIndex.x, endIndex.y] = endRoom;

        Vector2Int currentIndex = startIndex;
        List<Room> mainPath = new List<Room> { startRoom };
        List<Room> allRooms = new List<Room> { startRoom };

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

        startRoom.LoadRoom();
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

    private static Dictionary<string, Vector2Int> wallToIndex = new Dictionary<string, Vector2Int>//converts a tile name to the doorIndex it corrisponds too
    {
        {"FrontWall" ,new Vector2Int(1, 0)},
        {"BackWall" ,new Vector2Int(-1, 0)},
        {"RightWall" ,new Vector2Int(0, -1)},
        {"LeftWall" ,new Vector2Int(0, 1)}
    };

    private static Dictionary<string, string> wallToDoor = new Dictionary<string, string> //Keys are the names of tiles, values are the doors that can be generated from them.
    {
        {"FrontWall", "FrontDoor"},
        {"BackWall", "BackDoor"},
        {"LeftWall", "LeftDoor"},
        {"RightWall", "RightDoor"}
    };//I could probably just check for the direction in the name using substring but having it be manual is safer

    private Dictionary<Vector2Int, Vector2Int> roomPoints;//keys are the position of the point, values are the size. Used to create random room shapes

    public Room (Vector2Int roomIndex)
    {
        this.roomIndex = roomIndex;

        doorIndexes = new List<Vector2Int>();

        Generate();
    }

    public void Generate()
    {
        doorPositions = new Dictionary<Vector2Int, Vector3>();
        doors = new List<GameObject>();

        Vector2Int baseSize = new Vector2Int(Random.Range(3, 5) * 3, Random.Range(3, 5) * 3);

        roomPoints = new Dictionary<Vector2Int, Vector2Int>
        {
            {Vector2Int.zero, baseSize}
        };

        int pointAmount = Random.Range(2, 5);

        for (int i = 0; i < pointAmount; i++)
        {
            Vector2Int randomSize = new Vector2Int(Random.Range(2, 5) * 3, Random.Range(2, 5) * 3);
            Vector2Int randomPoint = new Vector2Int
                (
                    Random.Range(-baseSize.x / 2, baseSize.x / 2),
                    Random.Range(-baseSize.y / 2, baseSize.y / 2)
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

                    doorPositions[doorIndex] = tilemap.CellToWorld(randomDoor) + new Vector3(0, 0, -1);
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

            loadedBefore = true;
        }

        if (doorIndexes.Count == 0)
        {
            Debug.LogWarning("No doors.");

            doorIndexes = RoomGenerator.GetDoors(roomIndex);
        }

        foreach (Vector2Int doorIndex in doorIndexes)
        {
            GameObject doorObject = Object.Instantiate(doorPrefab);
            doorObject.transform.position = doorPositions[doorIndex];

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
    }
}
