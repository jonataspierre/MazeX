using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;
using Photon.Pun;
using UnityEngine.Events;


public class Generator2D : MonoBehaviourPunCallbacks
{
    enum CellType {
        None,
        Room,
        Hallway,
        Wall,
        Ceiling
    }

    class Room {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size) {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    [Space(10)]
    [Header("Size Setup")]
    [SerializeField] Vector2Int size;
    [SerializeField] bool fixedSize;
    [SerializeField] Vector2Int randomSize;

    [Space(10)]
    [Header("Room Setup")]
    [SerializeField] int roomCount;
    [SerializeField] bool fixedRoomCount;
    [SerializeField] Vector2Int randomRoomCount;
    [SerializeField] int maxPlacementRetries = 10;

    [Space(10)]
    [Header("Seed Setup")]
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] int seed = 0;

    [Space(10)]
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;
    [SerializeField]
    Material greenMaterial;

    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;    
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    [Space(10)]
    [Header("Exit Pos Setup")]
    [SerializeField] GameObject exitPrefab;
    public int indexExitRoom;

    [Space(10)]
    [Header("Wall and Ceiling Setup")]
    [SerializeField] GameObject wallPrefab;    
    [SerializeField] GameObject ceilingPrefab;
    [SerializeField] bool hasCeiling;

    [Space(10)]
    [Header("Container Objects Setup")]
    [SerializeField] GameObject floorContainerParent;
    [SerializeField] GameObject wallContainerParent;
    [SerializeField] GameObject ceilingContainerParent;
    [SerializeField] GameObject triggersContainerParent;
    [SerializeField] GameObject playersContainerParent;

    [Space(10)]
    [Header("Player Setup")]
    [SerializeField] GameObject playerPrefab;
    public int indexPlayerSpawnRoom;
    public Vector3 lastSpawnPosition;

    bool canSpawnerPlayer = false;

    public static UnityEvent<Vector3> OnRoomCreate = new UnityEvent<Vector3>();

    void Start()
    {
        lastSpawnPosition = Vector3.zero;
        Debug.Log($"[Generator2D] Start. IsMaster: {PhotonNetwork.IsMasterClient}, InRoom: {PhotonNetwork.InRoom}");
        
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[Generator2D] Master Client setting up map...");
                if (!fixedSize) SetSize();
                if (!fixedRoomCount) SetRoomCount();
                
                if (useRandomSeed) seed = UnityEngine.Random.Range(0, int.MaxValue);

                Debug.Log($"[Generator2D] Seed: {seed}, Size: {size}, Rooms: {roomCount}");

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "MapSeed", seed },
                    { "MapWidth", size.x },
                    { "MapHeight", size.y },
                    { "RoomCount", roomCount }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                
                // Master gera o mapa e DEPOIS spawna
                GenerateMap();
                StartCoroutine(DelayedSpawn());
            }
            else
            {
                Debug.Log("[Generator2D] Client waiting for properties...");
                ReadPropertiesAndGenerate();
            }
        }
        else
        {
            Debug.Log("[Generator2D] Offline mode generation.");
            GenerateMap();
            StartCoroutine(DelayedSpawn());
        }
    }

    IEnumerator DelayedSpawn()
    {
        // Espera um frame para garantir que toda a geometria foi instanciada e colisores estão ativos
        yield return new WaitForEndOfFrame();
        SetSpawnPlayer();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("[Generator2D] Room properties updated.");
        if (!PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey("MapSeed"))
        {
            ReadPropertiesAndGenerate();
        }
    }

    void ReadPropertiesAndGenerate()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MapSeed", out object s))
        {
            seed = (int)s;
            size.x = (int)PhotonNetwork.CurrentRoom.CustomProperties["MapWidth"];
            size.y = (int)PhotonNetwork.CurrentRoom.CustomProperties["MapHeight"];
            roomCount = (int)PhotonNetwork.CurrentRoom.CustomProperties["RoomCount"];
            
            Debug.Log($"[Generator2D] Received Seed: {seed}. Generating...");
            useRandomSeed = false;
            GenerateMap();
            StartCoroutine(DelayedSpawn());
        }
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ResetMap();
        Generate();
        canSpawnerPlayer = true;
    }

    void ResetMap()
    {
        var floor = new Transform[floorContainerParent.transform.childCount];
        for (var i = 0; i < floor.Length; i++)
        {
            floor[i] = floorContainerParent.transform.GetChild(i);
            Destroy(floor[i].gameObject);
        }

        var wall = new Transform[wallContainerParent.transform.childCount];
        for (var i = 0; i < wall.Length; i++)
        {
            wall[i] = wallContainerParent.transform.GetChild(i);
            Destroy(wall[i].gameObject);
        }

        var ceiling = new Transform[ceilingContainerParent.transform.childCount];
        for (var i = 0; i < ceiling.Length; i++)
        {
            ceiling[i] = ceilingContainerParent.transform.GetChild(i);
            Destroy(ceiling[i].gameObject);
        }

        var triggers = new Transform[triggersContainerParent.transform.childCount];
        for (var i = 0; i < triggers.Length; i++)
        {
            triggers[i] = triggersContainerParent.transform.GetChild(i);
            Destroy(triggers[i].gameObject);
        }

        var players = new Transform[playersContainerParent.transform.childCount];
        for (var i = 0; i < players.Length; i++)
        {
            players[i] = playersContainerParent.transform.GetChild(i);
            Destroy(players[i].gameObject);
        }
    }

    void SetSize()
    {
        size.x = UnityEngine.Random.Range(randomSize.x, randomSize.y + 1);
        size.y = UnityEngine.Random.Range(randomSize.x, randomSize.y + 1);
    }

    void SetRoomCount()
    {
        roomCount = UnityEngine.Random.Range(randomRoomCount.x, randomRoomCount.y + 1);
    }

    void Generate() {
        if (useRandomSeed) {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
        }
        random = new Random(seed);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
        
        // Ensure every walkable tile has a floor object
        GenerateFloor();
        
        AddWallsAndCeilings();
    }

    void GenerateFloor()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == CellType.Room)
                {
                    PlaceFloorTile(new Vector2Int(x, y), redMaterial);
                }
                else if (grid[x, y] == CellType.Hallway)
                {
                    PlaceFloorTile(new Vector2Int(x, y), blueMaterial);
                }
            }
        }
    }

    void PlaceFloorTile(Vector2Int location, Material material)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
        go.transform.SetParent(floorContainerParent.transform, false);
        go.transform.localScale = Vector3.one; 
        go.GetComponent<MeshRenderer>().material = material;
        
        // Ensure floor has a collider
        if (go.GetComponent<BoxCollider>() == null)
        {
            go.AddComponent<BoxCollider>();
        }
        
        go.name = $"Floor_{location.x}_{location.y}";
    }

    void PlaceRooms()
    {
        for (int i = 0; i < roomCount; i++) {
            bool placed = false;
            for (int retry = 0; retry < maxPlacementRetries; retry++) {
                Vector2Int roomSize = new Vector2Int(
                    random.Next(3, roomMaxSize.x + 1), // Min size 3 to ensure internal space
                    random.Next(3, roomMaxSize.y + 1)
                );

                Vector2Int location = new Vector2Int(
                    random.Next(1, size.x - roomSize.x - 1), // Buffer for walls
                    random.Next(1, size.y - roomSize.y - 1)
                );

                bool add = true;
                Room newRoom = new Room(location, roomSize);
                Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

                foreach (var room in rooms) {
                    if (Room.Intersect(room, buffer)) {
                        add = false;
                        break;
                    }
                }

                if (add) {
                    rooms.Add(newRoom);
                    foreach (var pos in newRoom.bounds.allPositionsWithin) {
                        grid[pos] = CellType.Room;
                    }
                    placed = true;
                    break;
                }
            }
        }

        SetExitPosition();
    }

    void SetExitPosition()
    {
        // Voltar para a lógica original que o usuário confirmou que funciona
        indexExitRoom = random.Next(0, rooms.Count);

        GameObject go = Instantiate(exitPrefab, new Vector3(rooms[indexExitRoom].bounds.x, 1, rooms[indexExitRoom].bounds.y), Quaternion.identity);
        go.transform.SetParent(triggersContainerParent.transform, false);        
        go.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
        go.GetComponent<MeshRenderer>().material = greenMaterial;
        go.name = "Exit";
    }

    void SetSpawnPlayer()
    {
        // Use the global 'random' instance to keep it synchronized and deterministic with the seed
        // We pick a room index that isn't the exit
        int roomIdx = random.Next(0, rooms.Count);
        int attempts = 0;
        while (roomIdx == indexExitRoom && attempts < 10 && rooms.Count > 1) {
            roomIdx = random.Next(0, rooms.Count);
            attempts++;
        }
        
        indexPlayerSpawnRoom = roomIdx;
        Room targetRoom = rooms[indexPlayerSpawnRoom];

        // Calculate the center of the room
        float centerX = targetRoom.bounds.x + (targetRoom.bounds.size.x / 2f);
        float centerZ = targetRoom.bounds.y + (targetRoom.bounds.size.y / 2f);

        // Position slightly above the floor (floor is at Y=0)
        Vector3 spawnPos = new Vector3(centerX, 1.2f, centerZ);
        lastSpawnPosition = spawnPos;

        Debug.Log($"[Generator2D] Player Spawn: Room {indexPlayerSpawnRoom} at {spawnPos}. Seed: {seed}");
        OnRoomCreate?.Invoke(spawnPos);
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();
                
                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            });

            if (path != null) {
                for (int i = 0; i < path.Count; i++) {
                    var current = path[i];

                    if (grid[current] == CellType.None) {
                        grid[current] = CellType.Hallway;
                    }
                }
            }
        }
    }

    void PlaceCube(Vector2Int location, Vector2Int size, Material material, bool isRoom = false)
    {
        // This method is now legacy as we use PlaceFloorTile for tile-by-tile generation
        // But we'll keep it for compatibility if needed elsewhere, updated to tile logic
        for(int x = 0; x < size.x; x++) {
            for(int y = 0; y < size.y; y++) {
                PlaceFloorTile(new Vector2Int(location.x + x, location.y + y), material);
            }
        }
    }

    void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        // Legacy call
    }

    void PlaceHallway(Vector2Int location)
    {
        // Legacy call
    }

    #region Paredes e Teto
    void AddWallsAndCeilings()
    {
        // First pass: Clear any accidental wall markings to start clean
        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                if (grid[x, y] == CellType.Wall) grid[x, y] = CellType.None;
            }
        }

        // Second pass: Identify all tiles adjacent to walkable areas (Rooms/Hallways)
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == CellType.Room || grid[x, y] == CellType.Hallway)
                {
                    // Check all 8 directions around the walkable tile
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            // If adjacent tile is outside or empty, it must be a wall
                            if (nx < 0 || nx >= size.x || ny < 0 || ny >= size.y) continue;
                            
                            if (grid[nx, ny] == CellType.None)
                            {
                                grid[nx, ny] = CellType.Wall;
                            }
                        }
                    }
                }
            }
        }

        // Third pass: Create wall objects
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == CellType.Wall)
                {
                    GameObject wallObject = Instantiate(wallPrefab, new Vector3(x, 1f, y), Quaternion.identity);
                    wallObject.transform.SetParent(wallContainerParent.transform, false);
                    wallObject.name = $"Wall_{x}_{y}";
                }
            }
        }

        if (hasCeiling)
        {
            // Ceiling now covers the entire bounding box of the grid for complete closure
            Vector3 ceilingPos = new Vector3((size.x - 1) / 2f, 2f, (size.y - 1) / 2f);
            GameObject ceilingObject = Instantiate(ceilingPrefab, ceilingPos, Quaternion.identity);
            ceilingObject.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            ceilingObject.transform.SetParent(ceilingContainerParent.transform, false);
            if (ceilingObject.GetComponent<BoxCollider>() == null)
            {
                ceilingObject.AddComponent<BoxCollider>();
            }
        }
    }
    #endregion

    //#region Paredes e Teto Perfeito
    //void AddWallsAndCeilings()
    //{
    //    // Marcar as células onde as paredes devem ser colocadas
    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (grid[x, y] == CellType.Room || grid[x, y] == CellType.Hallway)
    //            {
    //                // Verificar células ao redor da sala ou corredor
    //                for (int dx = -1; dx <= 1; dx++)
    //                {
    //                    for (int dy = -1; dy <= 1; dy++)
    //                    {
    //                        int nx = x + dx;
    //                        int ny = y + dy;

    //                        // Verificar se a célula está dentro dos limites do mapa e se é uma célula vazia
    //                        if (nx >= 0 && nx < size.x && ny >= 0 && ny < size.y && grid[nx, ny] == CellType.None)
    //                        {
    //                            // Marcar a célula adjacente como uma parede
    //                            grid[nx, ny] = CellType.Wall;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    // Instanciar o teto como um painel que cobre toda a área do mapa
    //    GameObject ceilingObject = Instantiate(ceilingPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
    //    ceilingObject.transform.localScale = new Vector3(size.x, 0.1f, size.y);
    //    ceilingObject.transform.SetParent(ceilingContainerParent.transform, false);
    //    // Adicionar um componente de colisão ao teto
    //    ceilingObject.AddComponent<BoxCollider>();

    //    // Instanciar as paredes
    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (grid[x, y] == CellType.Wall)
    //            {
    //                // Instanciar a parede sobre a célula
    //                GameObject wallObject = Instantiate(wallPrefab, new Vector3(x, 1f, y), Quaternion.identity);
    //                wallObject.transform.SetParent(wallContainerParent.transform, false);
    //                // Adicionar um componente de colisão à parede
    //                wallObject.AddComponent<BoxCollider>();
    //            }
    //        }
    //    }
    //}
    //#endregion

    //#region Paredes e Teto OK
    //void AddWallsAndCeilings()
    //{
    //    // Marcar as células onde as paredes devem ser colocadas
    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (grid[x, y] == CellType.Room || grid[x, y] == CellType.Hallway)
    //            {
    //                // Verificar células ao redor da sala ou corredor
    //                for (int dx = -1; dx <= 1; dx++)
    //                {
    //                    for (int dy = -1; dy <= 1; dy++)
    //                    {
    //                        int nx = x + dx;
    //                        int ny = y + dy;

    //                        // Verificar se a célula está dentro dos limites do mapa e se é uma célula vazia
    //                        if (nx >= 0 && nx < size.x && ny >= 0 && ny < size.y && grid[nx, ny] == CellType.None)
    //                        {
    //                            // Marcar a célula adjacente como uma parede
    //                            grid[nx, ny] = CellType.Wall;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    // Instanciar o teto sobre as células de salas e corredores
    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (grid[x, y] == CellType.Room || grid[x, y] == CellType.Hallway)
    //            {
    //                // Instanciar o teto sobre a célula
    //                GameObject ceilingObject = Instantiate(ceilingPrefab, new Vector3(x, 2f, y), Quaternion.identity);
    //                ceilingObject.transform.SetParent(ceilingContainerParent.transform, false);
    //                // Adicionar um componente de colisão ao teto
    //                //ceilingObject.AddComponent<BoxCollider>();
    //            }
    //        }
    //    }

    //    // Instanciar as paredes
    //    for (int x = 0; x < size.x; x++)
    //    {
    //        for (int y = 0; y < size.y; y++)
    //        {
    //            if (grid[x, y] == CellType.Wall)
    //            {
    //                // Instanciar a parede sobre a célula
    //                GameObject wallObject = Instantiate(wallPrefab, new Vector3(x, 1f, y), Quaternion.identity);
    //                wallObject.transform.SetParent(wallContainerParent.transform, false);
    //                // Adicionar um componente de colisão à parede
    //                //wallObject.AddComponent<BoxCollider>();
    //            }
    //        }
    //    }
    //}
    //#endregion    
}
