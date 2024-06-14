using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class RoomManager : MonoBehaviour
{
    public bool transitionDebounce = false;

    private Vector2Int currentRoom;

    private Room[,] area;

    public Room[,] GetArea()
    {
        return area;
    }

    public Vector2Int GetRoom()
    {
        return currentRoom;
    }

    public List<GameObject> GetContents()
    {
        return area[currentRoom.x, currentRoom.y].GetContents();
    }

    public IEnumerator ChangeRoom(Vector2Int roomVector)
    {
        if (transitionDebounce)
        {
            yield break;
        }
        transitionDebounce = true;

        Vector2Int newRoom = new Vector2Int(currentRoom.x + roomVector.x, currentRoom.y + roomVector.y);

        if (area[newRoom.x, newRoom.y] == null)
        {
            Debug.LogError("Invalid room");
            yield break;
        }

        StartCoroutine(area[currentRoom.x, currentRoom.y].UnloadRoom());
        StartCoroutine(area[newRoom.x, newRoom.y].LoadRoom());

        currentRoom = newRoom;

        Vector3 doorPos = area[newRoom.x, newRoom.y].GetDoorPos(-roomVector);

        Transform player = RoomGenerator.main.player;

        player.GetComponent<PlayerController>().canMove = false;
        StartCoroutine(Tween.New(new Color32(255, 255, 255, 0), player.GetComponent<SpriteRenderer>(), 0.25f));

        yield return new WaitForSecondsRealtime(0.5f);

        player.position = doorPos;
        StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), player.GetComponent<SpriteRenderer>(), 0.25f));

        yield return new WaitForSecondsRealtime(0.25f);

        if (player != null)
        {
            player.GetComponent<PlayerController>().canMove = true;
        }

        transitionDebounce = false;
    }

    public void RemoveObject(GameObject gameObject)
    {
        Debug.Log(gameObject);
        area[currentRoom.x, currentRoom.y].RemoveObject(gameObject);
    }

    public void LoadCurrent()
    {
        StartCoroutine(area[currentRoom.x, currentRoom.y].LoadRoom());
    }

    public void UnloadCurrent()
    {
        StartCoroutine(area[currentRoom.x, currentRoom.y].UnloadRoom());
    }

    public void UnloadInstant()
    {
        area[currentRoom.x, currentRoom.y].UnloadInstant();
    }

    public void SetCurrentRoom(Vector2Int currentRoom)
    {
        this.currentRoom = currentRoom;
    }

    public void GenerateArea()
    {
        for (int i = 0; i < MainManager.thePit.childCount; i++)
        {
            Destroy(MainManager.thePit.GetChild(i).gameObject);
        }

        area = new Room[7, 7];

        Room startRoom = new Room(new Vector2Int(Random.Range(1, 6), Random.Range(1, 3)));
        Vector2Int startIndex = startRoom.GetRoomIndex();
        currentRoom = startIndex;

        Room endRoom = new Room(new Vector2Int(Random.Range(1, 6), Random.Range(4, 6)));
        Vector2Int endIndex = endRoom.GetRoomIndex();

        area[startIndex.x, startIndex.y] = startRoom;
        area[endIndex.x, endIndex.y] = endRoom;

        Vector2Int currentIndex = startIndex;
        List<Room> mainPath = new List<Room> { startRoom };
        List<Room> allRooms = new List<Room> { startRoom, endRoom };

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
                Vector2Int checkPos = new Vector2Int(roomIndex.x + x, roomIndex.y);

                if (Random.Range(0, 3) == 0 && area[checkPos.x, checkPos.y] == null)
                {
                    area[checkPos.x, checkPos.y] = new Room(checkPos);
                    allRooms.Add(area[checkPos.x, checkPos.y]);
                }

            }

            for (int y = -1; y <= 1; y += 2)
            {
                Vector2Int checkPos = new Vector2Int(roomIndex.x, roomIndex.y + y);

                if (Random.Range(0, 3) == 0 && area[checkPos.x, checkPos.y] == null)
                {
                    area[checkPos.x, checkPos.y] = new Room(checkPos);
                    allRooms.Add(area[checkPos.x, checkPos.y]);
                }
            }
        }

        foreach (Room room in allRooms)//Generate
        {
            room.AddDoors(GetDoors(room.GetRoomIndex()));
            room.Generate();
        }

        GameObject plagueCaster = Instantiate(Resources.Load<GameObject>("Overworld/Prefabs/PlayerAdder"));

        allRooms[Random.Range(0, allRooms.Count)].AddObject(plagueCaster);

        MainManager.sceneManager.Banish(plagueCaster);

        GameObject enemyPrefab = Resources.Load<GameObject>("Overworld/Prefabs/Enemy");

        int amount = Random.Range(5, 11) + allRooms.Count;

        for (int i = 0; i < amount; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);

            Room randomRoom = allRooms[Random.Range(0, allRooms.Count)];

            while (randomRoom.Equals(startRoom))
            {
                randomRoom = allRooms[Random.Range(0, allRooms.Count)];
            }

            randomRoom.AddObject(enemy);

            MainManager.sceneManager.Banish(enemy);
        }

        string map = "Map:\n";

        for (int y = 6; y >= 0; y--)
        {
            for (int x = 0; x < 7; x++)
            {
                if (area[x, y] != null)
                {
                    Vector2Int checkPos = new Vector2Int(x, y);
                    if (checkPos.Equals(currentRoom))
                    {
                        map += "▣  ";
                    }
                    else if (checkPos.Equals(endIndex))
                    {
                        map += "▤  ";
                    }
                    else
                    {
                        map += "■  ";
                    }
                }
                else
                {
                    map += "□  ";
                }
            }

            map += "\n";
        }

        Debug.Log(map);
    }

    public List<Vector2Int> GetDoors(Vector2Int roomIndex)
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
    public Transform roomObject;

    public Tilemap tilemap;
    private RuleTile tile;

    private static GameObject doorPrefab = Resources.Load<GameObject>("Overworld/Prefabs/Door");

    private int attempt = 0;

    private const float slopeVal = 86f / 150f;

    private bool loaded = false;
    private bool loadDebounce = false;

    private Vector2Int roomIndex;//the grid position of the room

    private List<GameObject> doors;
    private List<GameObject> roomContents = new List<GameObject>();
    private List<Vector2Int> doorIndexes;//indicates which doors the room should have
    private List<Vector3Int> validFloors;
    private List<Vector3Int> sortWalls;

    private static Dictionary<string, Vector3> offset = new Dictionary<string, Vector3>
    {
        {"LeftWall", Vector3.zero},
        {"LeftDoor", Vector3.zero},
        {"RightWall", new Vector3(12, 78, 0) / 150},
        {"RightDoor", new Vector3(12, 78, 0) / 150},
        {"FrontWall", Vector3.zero},
        {"FrontDoor", Vector3.zero},
        {"BackWall", new Vector3(-12, 78, 0) / 150},
        {"BackDoor", new Vector3(-12, 78, 0) / 150},
        {"LeftCorner", new Vector3(75, 42, 0) / 150},
        {"RightCorner", new Vector3(-75, 42, 0) / 150},
        {"UpCorner", Vector3.zero},
        {"DownCorner", new Vector3(0, 71, 0) / 150},
        {"LeftCap", new Vector3(63, 36, 0) / 150},
        {"RightCap", new Vector3(-59, 36, 0) / 150},
        {"UpCap", new Vector3(0, 87, 0) / 150},
        {"DownCap", new Vector3(0, 71, 0) / 150},
    };

    private static Dictionary<string, float> tileLines = new Dictionary<string, float>
    {
        {"LeftWall", slopeVal},
        {"LeftDoor", slopeVal},
        {"RightWall", -slopeVal},
        {"RightDoor", -slopeVal},
        {"FrontWall", slopeVal},
        {"FrontDoor", slopeVal},
        {"BackWall", -slopeVal},
        {"BackDoor", -slopeVal},
        {"LeftCorner", slopeVal},
        {"RightCorner", slopeVal},
        {"LeftCap", -slopeVal},
        {"RightCap", -slopeVal},
        {"UpCap", -slopeVal},
        {"DownCap", slopeVal},
        {"UpCorner", slopeVal},
        {"DownCorner", -slopeVal},
    };

    private static Dictionary<string, TileBase> tiles;

    private Dictionary<Vector2Int, Vector3> doorPositions = new Dictionary<Vector2Int, Vector3>();//world positions of each door
    private Dictionary<Vector2Int, Vector3Int> indexToGrid = new Dictionary<Vector2Int, Vector3Int>();

    private static Dictionary<string, Vector2Int> wallToIndex = new Dictionary<string, Vector2Int>//converts a tile name to the doorIndex it corrisponds too
    {
        {"FrontWall" ,new Vector2Int(1, 0)},
        {"BackWall" ,new Vector2Int(-1, 0)},
        {"RightWall" ,new Vector2Int(0, -1)},
        {"LeftWall" ,new Vector2Int(0, 1)}
    };
    private static Dictionary<Vector2Int, TileBase> IndexToTile;
    private Dictionary<Vector2Int, Vector2Int> roomPoints;//keys are the position of the point, values are the size. Used to create random room shapes
    private Dictionary<GameObject, Vector3Int> contentPositions = new Dictionary<GameObject, Vector3Int>();//Positions of each thing in the room

    public Room(Vector2Int roomIndex)
    {
        this.roomIndex = roomIndex;

        roomObject = Object.Instantiate(Resources.Load<Transform>("Overworld/Prefabs/Room"), MainManager.theSquares);
        roomObject.GetComponent<ShadowCaster2D>().enabled = !MainManager.LowGraphicsMode;
        roomObject.name = "Room " + roomIndex;

        tile = Resources.Load<RuleTile>("Overworld/Prefabs/Crypt");
        tilemap = roomObject.GetComponent<Tilemap>();

        doorIndexes = new List<Vector2Int>();

        if (IndexToTile == null)
        {
            IndexToTile = new Dictionary<Vector2Int, TileBase>
            {
                {new Vector2Int(1, 0), Resources.Load<TileBase>("OverWorld/Tiles/FrontDoor")},
                {new Vector2Int(-1, 0), Resources.Load<TileBase>("OverWorld/Tiles/BackDoor")},
                {new Vector2Int(0, -1), Resources.Load<TileBase>("OverWorld/Tiles/RightDoor")},
                {new Vector2Int(0, 1), Resources.Load<TileBase>("OverWorld/Tiles/LeftDoor")}
            };
        }

        if (tiles == null)
        {
            tiles = new Dictionary<string, TileBase>();

            foreach (Tile tile in Resources.LoadAll("Overworld/Tiles", typeof(Tile)))
            {
                tiles.Add(tile.name, tile);
            }
        }
    }

    public void Generate()
    {
        attempt++;
        Debug.Log("Generating room " + roomIndex + ". Attempt " + attempt + ".");

        tilemap.ClearAllTiles();
        ResetDoorPositions();
        ResetContentPositions();

        Vector2Int baseSize = new Vector2Int(Random.Range(4, 7), Random.Range(4, 7));
        Dictionary<Vector2Int, List<Vector3Int>> validWalls = new Dictionary<Vector2Int, List<Vector3Int>>();

        doors = new List<GameObject>();
        roomPoints = new Dictionary<Vector2Int, Vector2Int> { {Vector2Int.zero, baseSize} };
        validFloors = new List<Vector3Int>();
        sortWalls = new List<Vector3Int>();

        int pointAmount = Random.Range(3, 6);

        for (int i = 0; i < pointAmount; i++)
        {
            Vector2Int randomSize = new Vector2Int(Random.Range(3, 7), Random.Range(3, 7));
            Vector2Int randomPoint = new Vector2Int
                (
                    Random.Range((randomSize.x - baseSize.x) / 2 + 1, (baseSize.x + randomSize.x) / 2 - 1),
                    Random.Range((randomSize.y - baseSize.y) / 2 + 1, (baseSize.y + randomSize.y) / 2 - 1)
                );

            roomPoints[randomPoint] = randomSize;
        }

        foreach (Vector2Int pos in roomPoints.Keys)
        {
            for (int x = pos.x - roomPoints[pos].x / 2; x <= pos.x + roomPoints[pos].x / 2; x++)
            {
                for (int y = pos.y - roomPoints[pos].y / 2; y <= pos.y + roomPoints[pos].y / 2; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        foreach (Vector2Int doorIndex in doorIndexes)
        {
            validWalls.Add(doorIndex, new List<Vector3Int>());
        }

        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax ; x++)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
            {
                Vector3Int checkPos = new Vector3Int(x, y, 0);

                if (tilemap.GetTile(checkPos) == null)
                {
                    continue;
                }

                string tileName = tilemap.GetSprite(new Vector3Int(x, y, 0)).name;

                if (tileName.Equals("Floor")) 
                {
                    bool invalid = false;

                    for (int x2 = -1; x2 <= 1 && !invalid; x2++)//Ensures all adjacent tiles are floors.
                    {
                        for (int y2 = -1; y2 <= 1 && !invalid; y2++)
                        {
                            Vector3Int checkPos2 = new Vector3Int(x + x2, y + y2, 0);
                            if (tilemap.GetTile(checkPos2) == null || !tilemap.GetSprite(new Vector3Int(x + x2, y + y2, 0)).name.Equals("Floor"))
                            {
                                invalid = true;
                                break;
                            }
                        }
                    }

                    for (int x2 = -2; x2 <= 2 && !invalid; x2++)//Prevents spawning next to doors.
                    {
                        for (int y2 = -2; y2 <= 2 && !invalid; y2++)
                        {
                            Vector3Int checkPos2 = new Vector3Int(x + x2, y + y2, -1);
                            if (tilemap.GetTile(checkPos2) != null)
                            {
                                invalid = true;
                                break;
                            }
                        }
                    }


                    if (!invalid)
                    {
                        validFloors.Add(new Vector3Int(x, y, 0));

                        if (Random.Range(0, 10) == 0)
                        {
                            tilemap.SetAnimationFrame(new Vector3Int(x, y, 0), 1);
                            
                        }
                    }
                }

                if (!wallToIndex.ContainsKey(tileName))
                {
                    continue;
                }

                if (doorIndexes.Contains(wallToIndex[tileName]))
                {
                    Vector2Int index = wallToIndex[tileName];

                    Vector3Int newPos = new Vector3Int(index.y, index.x, 0);

                    if (tilemap.GetTile(checkPos + newPos) != null && tilemap.GetTile(checkPos - newPos) != null && tilemap.GetSprite(checkPos + newPos).name.Equals(tileName) && tilemap.GetSprite(checkPos - newPos).name.Equals(tileName))
                    {
                        validWalls[wallToIndex[tileName]].Add(new Vector3Int(x, y, 0));
                    }
                }
            }
        }

        foreach (Vector2Int doorIndex in doorIndexes)
        {
            if (!validWalls.ContainsKey(doorIndex))
            {
                Debug.LogWarning("(1) Missing valid wall for door, regenerating room + " + roomIndex);

                Generate();

                return;
            }

            List<Vector3Int> doorList = validWalls[doorIndex];

            if (doorList.Count > 0)
            {
                Vector3Int randomDoor = doorList[Random.Range(0, doorList.Count)];
                Vector3Int clampedIndex = new Vector3Int(Mathf.Clamp(doorIndex.x, -1, 0), Mathf.Clamp(doorIndex.y, -1, 0), 0);
                Vector3Int absIndex = new Vector3Int(Mathf.Abs(doorIndex.y), Mathf.Abs(doorIndex.x), 0);

                Vector3 newPos = tilemap.CellToWorld(randomDoor) - tilemap.CellToWorld(clampedIndex) + tilemap.CellToWorld(absIndex) / 2;

                doorPositions[doorIndex] = newPos;
                indexToGrid[doorIndex] = randomDoor;
            }
            else
            {
                Debug.LogWarning("(2) Missing valid wall for door, regenerating room: " + roomIndex);

                Generate();

                return;
            }

        }

        foreach (Vector2Int doorIndex in doorIndexes)
        {
            if (!doorPositions.ContainsKey(doorIndex))
            {
                Debug.LogWarning("Missing door position, regenerating room : " + roomIndex);

                Generate();

                return;
            }

            tilemap.SetAnimationFrame(indexToGrid[doorIndex], 1);

            GameObject doorObject = Object.Instantiate(doorPrefab, roomObject);
            doors.Add(doorObject);

            doorObject.transform.localPosition = doorPositions[doorIndex];
            doorObject.GetComponent<RoomTransfer>().roomIndex = doorIndex;
            doorObject.transform.Find("Light").GetComponent<Light2D>().shadowsEnabled = !MainManager.LowGraphicsMode;
        }

        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
            {
                Sprite tile = tilemap.GetSprite(new Vector3Int(x, y, 0));

                if (tile == null){ continue; }

                Vector3Int newPos = new Vector3Int(x, y, 50);

                tilemap.SetTile(newPos, tiles[tile.name]);

                if (offset.ContainsKey(tile.name))
                {
                    sortWalls.Add(newPos);
                }
            }
        }

        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }

        UnloadInstant();
    }

    public void AddDoors(List<Vector2Int> doorIndexes)
    {
        this.doorIndexes = doorIndexes;

        ResetDoorPositions();
    }

    public void AddObject(GameObject newObject)
    {
        if (!roomContents.Contains(newObject))
        {
            roomContents.Add(newObject);
            contentPositions.Add(newObject, Vector3Int.zero);
        }

        if (validFloors.Count == 0)
        {
            Debug.LogWarning("Missing floor for object, regenerating room: " + roomIndex);

            Generate();

            return;
        }

        int randomIndex = Random.Range(0, validFloors.Count);

        newObject.transform.SetParent(roomObject);
        newObject.transform.position = MainManager.theSquares.GetComponent<Tilemap>().CellToWorld(validFloors[randomIndex]) + new Vector3(0, 0, -1);
        contentPositions[newObject] = validFloors[randomIndex];
        validFloors.RemoveAt(randomIndex);

        if (!loaded)
        {
            newObject.SetActive(false);
            MainManager.sceneManager.Banish(newObject);

            if (newObject.transform.TryGetComponent<CombatStarter>(out CombatStarter combatStarter))
            {
                combatStarter.mainCollider.enabled = false;
            }
        }
        else
        {
            newObject.SetActive(true);
        }
    }

    public void RemoveObject(GameObject newObject)
    {
        roomContents.Remove(newObject);
        Object.Destroy(newObject);
    }


    public IEnumerator LoadRoom()
    {
        yield return new WaitUntil(() => !loadDebounce);
        loadDebounce = true;

        MainManager.sceneManager.Summon(roomObject.gameObject, "Overworld");
        roomObject.SetParent(RoomGenerator.main.transform);

        foreach (GameObject thing in roomContents)
        {
            if (thing != null)
            {
                MainManager.sceneManager.Summon(thing, "Overworld");
                thing.transform.SetParent(RoomGenerator.main.ObjectContainer, true);

                SpriteRenderer spriteRenderer = thing.transform.GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color32(255, 255, 255, 0);
                MainManager.roomManager.StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), spriteRenderer, 1));

                if (thing.transform.TryGetComponent(out CombatStarter combatStarter))
                {
                    combatStarter.Disable();
                }

                thing.SetActive(true);
            }
        }

        foreach (GameObject door in doors)
        {
            door.transform.GetComponent<RoomTransfer>().Activate();
        }

        CenterCamera();

        loaded = true;

        MainManager.roomManager.StartCoroutine(Tween.New(new Color32(255, 255, 255, 255), tilemap, 1));
        MainManager.roomManager.StartCoroutine(StartSorting());

        yield return new WaitForSecondsRealtime(1.1f);

        foreach (GameObject thing in roomContents)
        {
            if (thing.transform.TryGetComponent(out CombatStarter combatStarter))
            {
                combatStarter.StartCoroutine(combatStarter.DelayedEnable());
            }
        }

        loadDebounce = false;
    }

    public IEnumerator UnloadRoom()
    {
        yield return new WaitUntil(() => !loadDebounce);

        loadDebounce = true;
        loaded = false;

        foreach (GameObject thing in roomContents)
        {
            SpriteRenderer spriteRenderer = thing.transform.GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            MainManager.roomManager.StartCoroutine(Tween.New(new Color32(255, 255, 255, 0), spriteRenderer, 1));

            if (thing.transform.TryGetComponent(out CombatStarter combatStarter))
            {
                combatStarter.Disable();
            }
        }

        MainManager.roomManager.StartCoroutine(Tween.New(new Color32(255, 255, 255, 0), tilemap, 1));

        yield return new WaitForSecondsRealtime(1.1f);

        foreach (GameObject thing in roomContents)
        {
            thing.SetActive(false);
            MainManager.sceneManager.Banish(thing);

        }

        MainManager.sceneManager.Banish(roomObject.gameObject);

        loadDebounce = false;
    }

    public void UnloadInstant()
    {
        loaded = false;

        foreach (GameObject thing in roomContents)
        {
            if (thing.transform.TryGetComponent(out CombatStarter combatStarter))
            {
                combatStarter.Disable();
            }

            thing.transform.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 0);
            thing.SetActive(false);
            MainManager.sceneManager.Banish(thing);
        }

        MainManager.sceneManager.Banish(roomObject.gameObject);
    }


    public Vector3 GetDoorPos(Vector2Int doorIndex)
    {
        return doorPositions[doorIndex];
    }

    public List<GameObject> GetContents()
    {
        return roomContents;
    }

    public List<Vector3Int> GetValidObjectPositions()
    {
        List<Vector3Int> posList = new List<Vector3Int>();

        return posList;
    }

    public Vector2Int GetRoomIndex()
    {
        return roomIndex;
    }


    private IEnumerator StartSorting()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        List<Transform> objects = new List<Transform>();
        int objectCount = RoomGenerator.main.ObjectContainer.childCount;

        foreach (Transform child in RoomGenerator.main.ObjectContainer)
        {
            objects.Add(child.transform);
        }

        while (loaded)
        {
            //prevent errors
            if (sortWalls.Count == 0)
            {
                Debug.LogError("No walls to sort.");
            }

            if (objectCount != RoomGenerator.main.ObjectContainer.childCount)
            {
                objectCount = RoomGenerator.main.ObjectContainer.childCount;
                objects = new List<Transform>();

                foreach (Transform child in RoomGenerator.main.ObjectContainer)
                {
                    objects.Add(child.transform);
                }
            }
            //

            //sort objects from lowest to highest, starting at 1 & jumping by 2
            objects = objects.OrderBy(obj => obj.position.y).ToList();

            for (int i = 0; i < objects.Count; i++)
            {
                Transform obj = objects[i];
                obj.position = new Vector3(obj.position.x, obj.position.y, i * 2 + 1);
            }
            //

            for (int i = 0; i < sortWalls.Count; i++)
            {
                string name = tilemap.GetSprite(sortWalls[i]).name;
                bool found = false;

                Vector3 tilePos = tilemap.CellToWorld(sortWalls[i]) + offset[name];

                //find the first object that is above the tile's sort line, then put the tile below (in front of) it.
                for (int v = 0; v < objects.Count; v++)
                {
                    if (objects[v].position.y >= tileLines[name] * Mathf.Abs(objects[v].position.x - tilePos.x) + tilePos.y)
                    {
                        tilemap.SetTile(sortWalls[i], null);

                        sortWalls[i] = new Vector3Int(sortWalls[i].x, sortWalls[i].y, v * 2);

                        tilemap.SetTile(sortWalls[i], tiles[name]);

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    tilemap.SetTile(sortWalls[i], null);

                    sortWalls[i] = new Vector3Int(sortWalls[i].x, sortWalls[i].y, objectCount * 2);

                    tilemap.SetTile(sortWalls[i], tiles[name]);
                }
                //
            }

            yield return wait;
        }

        //reset wall positions as the room unloads
        for (int i = 0; i < sortWalls.Count; i++)
        {
            Vector3Int pos = sortWalls[i];
            Vector3Int newPos = new Vector3Int(pos.x, pos.y, objectCount * 2);

            string name = tilemap.GetSprite(pos).name;

            tilemap.SetTile(pos, null);

            sortWalls[i] = newPos;

            tilemap.SetTile(newPos, tiles[name]);
        }
        //

    }

    private void ResetDoorPositions()
    {
        foreach (Vector2Int doorIndex in doorIndexes)
        {
            if (!doorPositions.ContainsKey(doorIndex))
            {
                doorPositions.Add(doorIndex, Vector3.zero);
                indexToGrid.Add(doorIndex, Vector3Int.zero);
            }
            else
            {
                doorPositions[doorIndex] = Vector3.zero;
                indexToGrid[doorIndex] = Vector3Int.zero;
            }
        }
    }

    private void ResetContentPositions()
    {
        List<GameObject> keys = new List<GameObject>(contentPositions.Keys);

        foreach(GameObject key in keys)
        {
            contentPositions[key] = Vector3Int.zero;
        }
    }

    private void CenterCamera()
    {
        List<Vector3> tilePos = new List<Vector3>();

        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; x++)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; y++)
            {
                if (tilemap.GetTile(new Vector3Int(x, y, 50)) != null)
                {
                    tilePos.Add(tilemap.CellToWorld(new Vector3Int(x, y, -500)));
                }
            }
        }

        Vector3 cameraPos = Vector3.zero;

        foreach (Vector3 pos in tilePos)
        {
            cameraPos += pos;
        }

        cameraPos /= tilePos.Count;
        cameraPos += new Vector3(0, 0.5f, 0);

        MainManager.sceneManager.StartCoroutine(Tween.New(cameraPos, MainManager.mainCamera.transform, 0.5f));
    }
}