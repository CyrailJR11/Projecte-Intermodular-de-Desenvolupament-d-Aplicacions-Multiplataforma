using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using EntreDosMons.Dialogue;
using EntreDosMons.Mechanics;
using EntreDosMons.UI;

namespace EntreDosMons.Levels
{
    // Construye el nivel por código a partir de los mapas de texto de LevelLibrary:
    // pone los tiles en los Tilemaps, coloca a Nil y Volt, crea puntos de control, salida, zonas de diálogo,
    // y los objetos con comportamiento (pinchos, agua, cuerdas, escaleras, péndulos, cajas, palancas, compuertas,
    // cargadores y luces), paredes invisibles en los bordes y limita la cámara al tamaño del nivel.
    // Al darle a Play también añade a los personajes las vidas, la respiración, la batería y la linterna, y crea el HUD.
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Tilemaps del Grid")]
        public Tilemap ground;
        public Tilemap decoration;
        public Tilemap background;
        [Tooltip("Plataformas de madera de un solo sentido. Si está vacío se crea solo, al lado de Suelo.")]
        public Tilemap oneWay;

        [Header("Tiles (los rellena el menú Entre Dos Mons > 3)")]
        public TileBase[] tiles;

        [Header("Escena")]
        public PlayerController2D nil;
        public PlayerController2D volt;
        public CameraFollow2D cameraFollow;

        [Header("Nivel")]
        [Tooltip("Número del nivel con el que empieza la partida (0 = Prólogo).")]
        public int startLevel;

        [Header("Luces del nivel")]
        public bool createLights = true;

        public int CurrentLevel { get; private set; }
        public LevelDefinition Current => LevelLibrary.Levels[CurrentLevel];

        readonly Dictionary<string, TileBase> tilesByName = new Dictionary<string, TileBase>();
        Transform generated;
        Material spriteMaterial;
        int caveTopRow = -1;

        void Start()
        {
            SetupGameplay();
            Build(startLevel);
        }

        public void NextLevel()
        {
            if (CurrentLevel + 1 < LevelLibrary.Levels.Length) Build(CurrentLevel + 1);
            else Debug.Log("Entre Dos Mons: no hay más niveles.");
        }

        // Al quedarse sin vidas: el mismo nivel desde el principio.
        public void RestartLevel() => Build(CurrentLevel);

        // Clic derecho en el componente > "Construir nivel" para verlo sin darle a Play.
        [ContextMenu("Construir nivel")]
        void BuildInEditor() => Build(startLevel);

        // ---------- Personajes, HUD y luz (solo en Play) ----------

        void SetupGameplay()
        {
            if (nil != null)
            {
                nil.canClimbRopes = true;
                nil.canClimbLadders = true;
                nil.canSwim = true;
                Ensure<PlayerHealth>(nil);
                Ensure<PlayerBreath>(nil);
            }
            if (volt != null)
            {
                volt.canFlipGravity = true;
                volt.canClimbRopes = false;
                volt.canClimbLadders = true;
                volt.canSwim = false;
                Ensure<PlayerHealth>(volt);
                Ensure<VoltBattery>(volt);
                Ensure<VoltFlashlight>(volt);
            }
            var hud = FindAnyObjectByType<GameHUD>();
            if (hud == null) hud = new GameObject("HUD").AddComponent<GameHUD>();
            hud.nil = nil;
            hud.volt = volt;
            Ensure<LevelLighting>(this);
        }

        static T Ensure<T>(Component target) where T : Component
        {
            var c = target.GetComponent<T>();
            return c != null ? c : target.gameObject.AddComponent<T>();
        }

        // ---------- Construcción ----------

        public void Build(int index)
        {
            CurrentLevel = Mathf.Clamp(index, 0, LevelLibrary.Levels.Length - 1);
            LevelDefinition level = Current;
            IndexTiles();
            EnsureOneWayTilemap();
            Clear();
            spriteMaterial = FindSpriteMaterial();
            caveTopRow = FindCaveTopRow(level);

            int height = level.Height;
            var levers = new List<Lever>();
            var doors = new List<Door>();

            for (int row = 0; row < height; row++)
            {
                string line = level.Map[row];
                int y = height - 1 - row;   // la primera fila del texto es la de arriba
                for (int x = 0; x < line.Length; x++)
                {
                    char c = line[x];
                    if (c == LevelLegend.Empty) continue;
                    var cell = new Vector3Int(x, y, 0);
                    Vector3 center = ground.GetCellCenterWorld(cell);

                    if (c == LevelLegend.OneWay)
                        oneWay.SetTile(cell, Find("ext_plataforma_fusta_" + EdgePiece(line, x)));
                    else if (LevelLegend.EdgeTiles.TryGetValue(c, out string prefix))
                        ground.SetTile(cell, Find(prefix + EdgePiece(line, x)));
                    else if (c == LevelLegend.Spikes) CreateSpikes(level, row, x, cell, center);
                    else if (c == LevelLegend.Box) CreateBox(center);
                    else if (c == LevelLegend.Door)
                    {
                        // Una compuerta por columna de 'D': se crea en la casilla de abajo del todo.
                        bool below = row + 1 < height && level.Map[row + 1][x] == LevelLegend.Door;
                        if (!below) doors.Add(CreateDoor(level, row, x, center));
                    }
                    else if (c == LevelLegend.SwingAnchor) CreateSwingRope(center);
                    else if (LevelLegend.Tiles.TryGetValue(c, out var entry))
                    {
                        Layer(entry.layer).SetTile(cell, Find(entry.tile));
                        if (c == LevelLegend.Lever) levers.Add(CreateLever(cell, center));
                        else if (c == LevelLegend.Charger) CreateCharger(center);
                        CreateDecorLight(c, center);
                    }
                    else if (c == LevelLegend.NilSpawn) Place(nil, center);
                    else if (c == LevelLegend.VoltSpawn) Place(volt, center);
                    else if (c == LevelLegend.Checkpoint) CreateZone("Checkpoint", center, new Vector2(1f, 4f)).AddComponent<Checkpoint>();
                    else if (c == LevelLegend.Exit) CreateZone("Salida", center, new Vector2(1f, 4f)).AddComponent<LevelExit>().builder = this;
                    else if (!TryCreateDialogue(level, c, center))
                        Debug.LogWarning($"Entre Dos Mons: carácter '{c}' desconocido en el nivel {CurrentLevel}, fila {row}, columna {x}.");
                }
            }

            CreateWater(level);
            CreateClimbables(level);
            LinkLevers(levers, doors);
            CreateBorders(level.Width, height);
            FitCamera(level.Width, height);
            ConfigureLighting(height);
            if (Application.isPlaying) ResetPlayers();
            Debug.Log($"Entre Dos Mons: nivel {CurrentLevel} construido ({level.Name}, {level.Width}×{height}).");
        }

        // Elige la pieza izquierda, central o derecha según los vecinos iguales.
        static string EdgePiece(string line, int x)
        {
            char c = line[x];
            bool left = x > 0 && line[x - 1] == c;
            bool right = x < line.Length - 1 && line[x + 1] == c;
            if (left == right) return "mig";
            return left ? "dreta" : "esquerra";
        }

        // La cueva empieza en la primera fila que es todo roca 'R' (el techo de la cueva).
        static int FindCaveTopRow(LevelDefinition level)
        {
            for (int row = 0; row < level.Height; row++)
            {
                string line = level.Map[row];
                if (line.Length > 0 && line.Trim('R').Length == 0) return row;
            }
            return -1;
        }

        bool IsCave(int row) => caveTopRow >= 0 && row >= caveTopRow;

        static bool IsSolid(char c)
        {
            if (LevelLegend.EdgeTiles.ContainsKey(c) && c != LevelLegend.OneWay) return true;
            if (c == LevelLegend.Box || c == LevelLegend.Door) return true;
            return LevelLegend.Tiles.TryGetValue(c, out var e) && e.layer == LevelLayer.Suelo;
        }

        static char At(LevelDefinition level, int row, int x)
        {
            if (row < 0 || row >= level.Height || x < 0 || x >= level.Map[row].Length) return LevelLegend.Empty;
            return level.Map[row][x];
        }

        // ---------- Objetos ----------

        void CreateSpikes(LevelDefinition level, int row, int x, Vector3Int cell, Vector3 center)
        {
            decoration.SetTile(cell, Find(IsCave(row) ? LevelLegend.SpikesCave : LevelLegend.SpikesExterior));
            // Si hay suelo encima y no debajo, cuelgan del techo (para Volt boca abajo).
            bool hanging = IsSolid(At(level, row - 1, x)) && !IsSolid(At(level, row + 1, x));
            if (hanging) decoration.SetTransformMatrix(cell, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 180f)));
            Vector3 offset = new Vector3(0f, hanging ? 0.25f : -0.25f, 0f);
            CreateZone("Pinchos", center + offset, new Vector2(0.8f, 0.45f)).AddComponent<Hazard>();
        }

        void CreateBox(Vector3 center)
        {
            var go = new GameObject("Caja");
            go.layer = ground.gameObject.layer;   // se puede pisar
            go.transform.SetParent(generated, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteOf(LevelLegend.BoxTile);
            sr.sortingOrder = 1;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 2f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.96f, 0.98f);
            col.sharedMaterial = new PhysicsMaterial2D("Caja") { friction = 0.6f, bounciness = 0f };
            if (Application.isPlaying) go.AddComponent<PushableBox>();
            else body.bodyType = RigidbodyType2D.Kinematic;   // vista previa en el editor: que no se caiga
        }

        Door CreateDoor(LevelDefinition level, int bottomRow, int x, Vector3 bottomCenter)
        {
            int cells = 0;
            for (int r = bottomRow; r >= 0 && At(level, r, x) == LevelLegend.Door; r--) cells++;
            var go = new GameObject("Compuerta");
            go.layer = ground.gameObject.layer;
            go.transform.SetParent(generated, false);
            go.transform.position = bottomCenter;
            var door = go.AddComponent<Door>();
            door.Init(SpriteOf(LevelLegend.DoorTile), cells, spriteMaterial, -1);   // detrás del Suelo: al abrirse se esconde en el techo
            return door;
        }

        Lever CreateLever(Vector3Int cell, Vector3 center)
        {
            GameObject zone = CreateZone("Palanca", center, new Vector2(1.6f, 1.6f));
            var lever = zone.AddComponent<Lever>();
            lever.tilemap = decoration;
            lever.cell = cell;
            return lever;
        }

        // Cada compuerta obedece a la palanca más cercana.
        static void LinkLevers(List<Lever> levers, List<Door> doors)
        {
            if (levers.Count == 0) return;
            foreach (Door door in doors)
            {
                Lever best = null;
                float bestDistance = float.MaxValue;
                foreach (Lever lever in levers)
                {
                    float d = Vector2.Distance(lever.transform.position, door.transform.position);
                    if (d < bestDistance) { bestDistance = d; best = lever; }
                }
                best.doors.Add(door);
            }
        }

        void CreateCharger(Vector3 center)
        {
            GameObject zone = CreateZone("Cargador", center, new Vector2(1.4f, 1.4f));
            var charger = zone.AddComponent<BatteryCharger>();
            charger.glow = CreateLight("Luz cargador", center, new Color32(0x62, 0xB5, 0x6B, 0xFF), 2.5f, 0.8f);
        }

        void CreateSwingRope(Vector3 center)
        {
            var go = new GameObject("Cuerda péndulo");
            go.transform.SetParent(generated, false);
            go.transform.position = center + new Vector3(0f, 0.4f, 0f);
            go.AddComponent<SwingRope>();
            decoration.SetTile(decoration.WorldToCell(center), Find("ext_corda_ancoratge"));
        }

        // Agua: un rectángulo por cada tramo horizontal de '~' / 'w'. La superficie empieza un poco por debajo del borde del tile.
        void CreateWater(LevelDefinition level)
        {
            int height = level.Height;
            for (int row = 0; row < height; row++)
            {
                string line = level.Map[row];
                int x = 0;
                while (x < line.Length)
                {
                    if (!IsWater(line[x])) { x++; continue; }
                    int start = x;
                    bool surface = false;
                    while (x < line.Length && IsWater(line[x]))
                    {
                        surface |= line[x] == LevelLegend.WaterSurface;
                        x++;
                    }
                    Vector3 corner = ground.CellToWorld(new Vector3Int(start, height - 1 - row, 0));
                    float top = surface ? 0.7f : 1f;
                    var go = new GameObject("Agua");
                    go.transform.SetParent(generated, false);
                    go.transform.position = corner;
                    go.AddComponent<WaterZone>().area = new Rect(corner.x, corner.y, x - start, top);
                }
            }
        }

        static bool IsWater(char c) => c == LevelLegend.WaterSurface || c == LevelLegend.WaterFill;

        // Cuerdas ('T' + '|') y escaleras ('e'): una zona por cada columna seguida. Sobresale media casilla por arriba
        // para poder terminar de subir a la plataforma.
        void CreateClimbables(LevelDefinition level)
        {
            int height = level.Height;
            for (int x = 0; x < level.Width; x++)
            {
                int row = 0;
                while (row < height)
                {
                    ClimbKind? kind = ClimbKindOf(At(level, row, x));
                    if (kind == null) { row++; continue; }
                    int top = row;
                    while (row < height && ClimbKindOf(At(level, row, x)) == kind) row++;
                    int cells = row - top;
                    Vector3 corner = ground.CellToWorld(new Vector3Int(x, height - row, 0));   // esquina de abajo
                    var go = new GameObject(kind == ClimbKind.Cuerda ? "Cuerda" : "Escalera");
                    go.transform.SetParent(generated, false);
                    go.transform.position = corner;
                    var climb = go.AddComponent<Climbable>();
                    climb.kind = kind.Value;
                    climb.area = new Rect(corner.x + 0.2f, corner.y, 0.6f, cells + 0.5f);
                }
            }
        }

        static ClimbKind? ClimbKindOf(char c)
        {
            if (c == LevelLegend.Rope || c == LevelLegend.RopeAnchor) return ClimbKind.Cuerda;
            if (c == LevelLegend.Ladder) return ClimbKind.Escalera;
            return null;
        }

        // ---------- Luces ----------

        void ConfigureLighting(int height)
        {
            var lighting = GetComponent<LevelLighting>();
            if (lighting == null) return;
            lighting.caveTopY = caveTopRow >= 0 ? ground.CellToWorld(new Vector3Int(0, height - caveTopRow, 0)).y : float.NegativeInfinity;
        }

        // Cristales, setas y farolas dan un poco de luz (en las cuevas es casi la única que hay, aparte de Volt).
        void CreateDecorLight(char c, Vector3 center)
        {
            switch (c)
            {
                case 'k': CreateLight("Luz cristal", center, new Color32(0x4B, 0x89, 0xA6, 0xFF), 2.2f, 0.9f); break;
                case 'b': CreateLight("Luz setas", center, new Color32(0x62, 0xB5, 0x6B, 0xFF), 1.6f, 0.6f); break;
                case 'L': CreateLight("Luz farola", center + new Vector3(0f, 0.3f, 0f), new Color32(0xFF, 0xD3, 0x4E, 0xFF), 3f, 0.8f); break;
            }
        }

        Light2D CreateLight(string name, Vector3 position, Color color, float radius, float intensity)
        {
            if (!createLights) return null;
            var go = new GameObject(name);
            go.transform.SetParent(generated, false);
            go.transform.position = position;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.7f;
            return light;
        }

        // ---------- Diálogos y personajes ----------

        bool TryCreateDialogue(LevelDefinition level, char c, Vector3 center)
        {
            foreach (DialogueSpot spot in level.Dialogues)
            {
                if (spot.Symbol != c) continue;
                // Sin panel de diálogos, una cinemática dejaría al personaje congelado: no se crean las zonas.
                if (FindAnyObjectByType<DialogueUI>() == null) return true;
                Vector3 offset = new Vector3(0f, (spot.Size.y - 1f) / 2f, 0f);
                GameObject zone = CreateZone("Dialogo " + spot.Id, center + offset, spot.Size);
                zone.AddComponent<DialogueTrigger>().Setup(spot.Id, spot.Who, spot.Activation);
                return true;
            }
            return false;
        }

        void Place(PlayerController2D player, Vector3 position)
        {
            if (player == null) return;
            if (Application.isPlaying) player.ResetMotion();
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            player.fallLimitBottom = -5f;
            player.fallLimitTop = Current.Height + 5f;
            if (Application.isPlaying) player.SetCheckpoint(position);
        }

        // Nivel nuevo o reiniciado: vidas, respiración y batería al máximo.
        void ResetPlayers()
        {
            foreach (PlayerController2D p in new[] { nil, volt })
            {
                if (p == null) continue;
                var health = p.GetComponent<PlayerHealth>();
                if (health != null) health.ResetLives();
                var breath = p.GetComponent<PlayerBreath>();
                if (breath != null) breath.ResetBreath();
                var battery = p.GetComponent<VoltBattery>();
                if (battery != null) battery.ResetCharge();
            }
        }

        GameObject CreateZone(string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(generated, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        // Paredes invisibles a izquierda y derecha para no salirse del nivel.
        void CreateBorders(int width, int height)
        {
            for (int side = 0; side < 2; side++)
            {
                var wall = new GameObject(side == 0 ? "Borde izquierdo" : "Borde derecho");
                wall.layer = ground.gameObject.layer;
                wall.transform.SetParent(generated, false);
                wall.transform.position = new Vector3(side == 0 ? -0.5f : width + 0.5f, height / 2f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, height + 20f);
            }
        }

        void FitCamera(int width, int height)
        {
            if (cameraFollow == null) return;
            var cam = cameraFollow.GetComponent<Camera>();
            float halfH = cam != null ? cam.orthographicSize : 5.625f;
            float halfW = cam != null ? halfH * cam.aspect : 10f;
            cameraFollow.useBounds = true;
            cameraFollow.min = new Vector2(Mathf.Min(halfW, width / 2f), Mathf.Min(halfH, height / 2f));
            cameraFollow.max = new Vector2(Mathf.Max(width - halfW, width / 2f), Mathf.Max(height - halfH, height / 2f));
        }

        // ---------- Tilemaps y tiles ----------

        // Tilemap de plataformas de madera con PlatformEffector2D: se suben desde abajo y se bajan con abajo + saltar.
        void EnsureOneWayTilemap()
        {
            if (oneWay != null) return;
            Transform existing = ground.transform.parent != null ? ground.transform.parent.Find("Plataformas") : null;
            if (existing != null && existing.TryGetComponent(out oneWay)) return;

            var go = new GameObject("Plataformas", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(ground.transform.parent, false);
            go.layer = ground.gameObject.layer;
            var groundRenderer = ground.GetComponent<TilemapRenderer>();
            go.GetComponent<TilemapRenderer>().sortingOrder = groundRenderer != null ? groundRenderer.sortingOrder : 0;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = go.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            oneWay = go.GetComponent<Tilemap>();
        }

        Material FindSpriteMaterial()
        {
            // El mismo material que los personajes (Sprite-Lit-Default de URP 2D), para que les afecten las luces.
            var source = nil != null ? nil.GetComponentInChildren<SpriteRenderer>() : null;
            return source != null ? source.sharedMaterial : null;
        }

        Tilemap Layer(LevelLayer layer)
        {
            switch (layer)
            {
                case LevelLayer.Decoracion: return decoration;
                case LevelLayer.Fondo: return background;
                default: return ground;
            }
        }

        TileBase Find(string tileName)
        {
            if (tilesByName.TryGetValue(tileName, out TileBase tile)) return tile;
            Debug.LogWarning($"Entre Dos Mons: falta el tile '{tileName}' en la lista Tiles del LevelBuilder (usa el menú Entre Dos Mons > 3).");
            return null;
        }

        Sprite SpriteOf(string tileName) => Find(tileName) is Tile t ? t.sprite : null;

        void IndexTiles()
        {
            tilesByName.Clear();
            foreach (TileBase tile in tiles)
                if (tile != null) tilesByName[tile.name] = tile;
        }

        void Clear()
        {
            ground.ClearAllTiles();
            decoration.ClearAllTiles();
            background.ClearAllTiles();
            if (oneWay != null) oneWay.ClearAllTiles();

            Transform old = transform.Find("Generado");
            if (old != null)
            {
                old.name = "Generado (borrando)";
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }
            generated = new GameObject("Generado").transform;
            generated.SetParent(transform, false);
        }
    }
}
