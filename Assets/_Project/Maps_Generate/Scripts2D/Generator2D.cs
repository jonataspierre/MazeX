using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;
using Photon.Pun;
using UnityEngine.Events;


public class Generator2D : MonoBehaviourPun
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

    bool canSpawnerPlayer = false;

    public static UnityEvent<Vector3> OnRoomCreate = new UnityEvent<Vector3>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (!fixedSize)
            {
                SetSize();
            }

            if (!fixedRoomCount)
            {
                SetRoomCount();
            }

            Generate();

            canSpawnerPlayer = true;
        }

        if(canSpawnerPlayer)
            SetSpawnPlayer();

        //if (!fixedSize)
        //{
        //    SetSize();
        //}

        //if (!fixedRoomCount)
        //{
        //    SetRoomCount();
        //}

        //Generate();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ResetMap();

        if (!fixedSize)
        {
            SetSize();
        }

        if (!fixedRoomCount)
        {
            SetRoomCount();
        }

        Generate();
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
        random = new Random(0);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
        //roomsObj = new List<GameObject>();

        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
        AddWallsAndCeilings();

        
    }

    void PlaceRooms()
    {
        for (int i = 0; i < roomCount; i++) {
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x),
                random.Next(0, size.y)
            );

            Vector2Int roomSize = new Vector2Int(
                random.Next(1, roomMaxSize.x + 1),
                random.Next(1, roomMaxSize.y + 1)
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

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y) {
                add = false;
            }

            if (add) {
                rooms.Add(newRoom);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }

        SetExitPosition();
        //SetSpawnPlayer();
    }

    void SetExitPosition()
    {
        indexExitRoom = UnityEngine.Random.Range(0, rooms.Count);

        GameObject go = PhotonNetwork.Instantiate(exitPrefab.name, new Vector3(rooms[indexExitRoom].bounds.x, 1, rooms[indexExitRoom].bounds.y), Quaternion.identity);
        go.transform.SetParent(triggersContainerParent.transform, false);        
        go.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.5f, 0.5f);
        go.GetComponent<MeshRenderer>().material = greenMaterial;
        go.name = "Exit";
    }

    void SetSpawnPlayer()
    {
        //if(roomCount == 0) roomCount = 2;
        
        indexPlayerSpawnRoom = UnityEngine.Random.Range(0, rooms.Count);

        while(indexPlayerSpawnRoom == indexExitRoom)
        {
            indexPlayerSpawnRoom = UnityEngine.Random.Range(0, rooms.Count);
        }

        Vector3 pos = new Vector3(rooms[indexPlayerSpawnRoom].bounds.x + 1, 5f, rooms[indexPlayerSpawnRoom].bounds.y + 1);

        OnRoomCreate?.Invoke(pos);

        //GameObject player = Instantiate(playerPrefab, new Vector3(rooms[indexPlayerSpawnRoom].bounds.x + 1, 1.05f, rooms[indexPlayerSpawnRoom].bounds.y + 1), Quaternion.identity);
        //player.transform.SetParent(playersContainerParent.transform, false);
        //player.GetComponent<Transform>().localScale = new Vector3(0.25f, 0.25f, 0.25f);        
        //player.name = "Player";

        //GameObject localPlayer = PhotonNetwork.Instantiate("PlayerManager", new Vector3(rooms[indexPlayerSpawnRoom].bounds.x + 1, 1.05f, rooms[indexPlayerSpawnRoom].bounds.y + 1), Quaternion.identity, 0);
        //localPlayer.name = "Manager_" + PhotonNetwork.LocalPlayer.NickName;

        //player.GetComponent<MeshRenderer>().material = greenMaterial;
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

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path) {
                    if (grid[pos] == CellType.Hallway) {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }

    void PlaceCube(Vector2Int location, Vector2Int size, Material material, bool isRoom = false)
    {
        GameObject go = PhotonNetwork.Instantiate(cubePrefab.name, new Vector3(location.x, 0, location.y), Quaternion.identity);
        go.transform.SetParent(floorContainerParent.transform, false);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        go.GetComponent<MeshRenderer>().material = material;        
    }

    void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size, redMaterial, true);        
    }

    void PlaceHallway(Vector2Int location)
    {
        PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
    }

    #region Paredes e Teto
    void AddWallsAndCeilings()
    {
        // Marcar as células onde as paredes devem ser colocadas
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                // Verificar se a célula está em uma das bordas do mapa
                bool onBorder = x == 0 || x == size.x - 1 || y == 0 || y == size.y - 1;

                // Marcar as células ao redor das salas ou corredores
                if (grid[x, y] == CellType.Room || grid[x, y] == CellType.Hallway)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            // Verificar se a célula está dentro dos limites do mapa e se é uma célula vazia
                            if (nx >= 0 && nx < size.x && ny >= 0 && ny < size.y && grid[nx, ny] == CellType.None)
                            {
                                // Marcar a célula adjacente como uma parede
                                grid[nx, ny] = CellType.Wall;
                            }
                        }
                    }
                }

                // Se a célula estiver na borda do mapa, marcá-la como uma parede
                if (onBorder)
                {
                    grid[x, y] = CellType.Wall;
                }
            }
        }

        if (hasCeiling)
        {
            // Instanciar o teto como um painel que cobre toda a área do mapa
            GameObject ceilingObject = PhotonNetwork.Instantiate(ceilingPrefab.name, new Vector3(0f, 2f, 0f), Quaternion.identity);
            ceilingObject.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            ceilingObject.transform.SetParent(ceilingContainerParent.transform, false);
            // Adicionar um componente de colisão ao teto
            ceilingObject.AddComponent<BoxCollider>();
        }

        // Instanciar as paredes
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == CellType.Wall)
                {
                    // Instanciar a parede sobre a célula
                    GameObject wallObject = PhotonNetwork.Instantiate(wallPrefab.name, new Vector3(x, 1f, y), Quaternion.identity);
                    wallObject.transform.SetParent(wallContainerParent.transform, false);
                    // Adicionar um componente de colisão à parede
                    //wallObject.AddComponent<BoxCollider>();
                }
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
