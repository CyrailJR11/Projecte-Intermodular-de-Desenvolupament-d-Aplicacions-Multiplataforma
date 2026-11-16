using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using EntreDosMons.Dialogue;
using EntreDosMons.Levels.Validation;
using EntreDosMons.Mechanics;
using EntreDosMons.UI;

namespace EntreDosMons.Levels
{
    // Construye el nivel por código a partir de los mapas de texto de LevelLibrary (nunca se pinta a mano):
    //   1) Terreno y decoración → tiles en los Tilemaps.
    //   2) Recorrido → zonas de agua, cuerdas, escaleras, pinchos y plataformas de madera.
    //   3) Objetos con función → se INSTANCIAN SUS PREFABS (Assets/Resources/Prefabs): Nil, Volt, cajas, válvulas,
    //      terminales, placas, cristales, compuertas, montacargas, cargadores, hogueras y salidas.
    //   4) La capa de cables conecta fuentes y receptores (SignalNetwork).
    // También pone paredes invisibles en los bordes, limita la cámara y, al darle a Play, crea el HUD y los diálogos.
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Tilemaps del Grid")]
        [SerializeField] Tilemap ground;
        [SerializeField] Tilemap decoration;
        [SerializeField] Tilemap background;
        [Tooltip("Plataformas de madera de un solo sentido. Si está vacío se crea solo, al lado de Suelo.")]
        [SerializeField] Tilemap oneWay;

        [Header("Tiles (los rellena el menú Entre Dos Mons > 3)")]
        [SerializeField] TileBase[] tiles;

        [Header("Escena")]
        [SerializeField] CameraFollow2D cameraFollow;

        [Header("Nivel")]
        [Tooltip("Número del nivel con el que empieza la partida (0 = Prólogo).")]
        [SerializeField] int startLevel;
        [SerializeField] bool createLights = true;

        public int CurrentLevel { get; private set; }
        public LevelDefinition Current => LevelLibrary.Levels[CurrentLevel];
        public IReadOnlyList<PlayerCharacter> Characters => characters;

        // Para el menú del editor y los tests.
        public void Configure(Tilemap groundMap, Tilemap decorationMap, Tilemap backgroundMap, TileBase[] tileList, CameraFollow2D follow)
        {
            ground = groundMap;
            decoration = decorationMap;
            background = backgroundMap;
            tiles = tileList;
            cameraFollow = follow;
        }

        readonly Dictionary<string, TileBase> tilesByName = new Dictionary<string, TileBase>();
        readonly List<PlayerCharacter> characters = new List<PlayerCharacter>();
        readonly HashSet<PlayerCharacter> arrived = new HashSet<PlayerCharacter>();
        Transform generated;
        LevelModel model;
        bool changingLevel;

        void Start()
        {
            SetupGameplay();
            Build(startLevel);
        }

        void Update()
        {
            if (GameInput.RestartPressed() && !DialogueManager.IsCinematicPlaying)
            {
                GameHUD.Say("Reiniciando el nivel...", 1f);
                RestartLevel();
            }
        }

        public void NextLevel()
        {
            if (CurrentLevel + 1 < LevelLibrary.Levels.Length) Build(CurrentLevel + 1);
            else GameHUD.Say("¡Fin de la demo! Nil y Volt lo han conseguido juntos.", 5f);
        }

        // Al quedarse sin vidas o al pulsar R: el mismo nivel desde el principio.
        public void RestartLevel() => Build(CurrentLevel);

        // Lo llama cada salida. El nivel acaba cuando han llegado los dos personajes.
        public void ReportArrived(PlayerCharacter who)
        {
            if (changingLevel || !arrived.Add(who)) return;
            GameAudio.Play("carga");
            foreach (PlayerCharacter p in characters)
            {
                if (p == null || arrived.Contains(p)) continue;
                GameHUD.Say($"{who.DisplayName} ha llegado. Falta {p.DisplayName}.", 2.5f);
                var switcher = FindAnyObjectByType<CharacterSwitcher>();
                if (switcher != null && who.IsControlled) switcher.Select(p);   // seguimos con el que falta
                return;
            }
            changingLevel = true;
            NextLevel();
        }

        // Clic derecho en el componente > "Construir nivel" para verlo sin darle a Play.
        [ContextMenu("Construir nivel")]
        void BuildInEditor() => Build(startLevel);

        // ---------- HUD, diálogos, luz y cambio de personaje (solo en Play) ----------

        void SetupGameplay()
        {
            if (FindAnyObjectByType<GameHUD>() == null) new GameObject("HUD").AddComponent<GameHUD>();
            if (GetComponent<LevelLighting>() == null) gameObject.AddComponent<LevelLighting>();
            if (FindAnyObjectByType<CharacterSwitcher>() == null) new GameObject("Cambio de personaje").AddComponent<CharacterSwitcher>();

            // Diálogos: gestor + panel creado por código (si no habéis montado un DialogueUI con TextMeshPro).
            if (FindAnyObjectByType<DialogueManager>() == null) new GameObject("Dialogos").AddComponent<DialogueManager>();
            if (FindAnyObjectByType<DialogueUI>() == null && FindAnyObjectByType<DialoguePanel>() == null)
                new GameObject("Panel de dialogos").AddComponent<DialoguePanel>();
        }

        // ---------- Construcción ----------

        public void Build(int index)
        {
            CurrentLevel = Mathf.Clamp(index, 0, LevelLibrary.Levels.Length - 1);
            LevelDefinition level = Current;
            model = new LevelModel(level);
            foreach (string error in model.Errors) Debug.LogError($"Entre Dos Mons ({level.Name}): {error}");

            IndexTiles();
            EnsureOneWayTilemap();
            Clear();
            SignalNetwork.Reset(level.Channels);
            WorldMap.Clear();
            WorldMap.CaveTopY = model.CaveTop >= 0 ? level.Height - model.CaveTop : float.NegativeInfinity;
            ChannelLamp.LampSprite = SpriteOf("senyal_canal");
            characters.Clear();
            arrived.Clear();
            changingLevel = false;

            PlaceTilesAndObjects(level);
            CreateSignalObjects();
            CreateWater(level);
            CreateClimbables(level);
            CreateBorders(level.Width, level.Height);
            FitCamera(level.Width, level.Height);
            if (Application.isPlaying) StartCharacters();
            Debug.Log($"Entre Dos Mons: nivel {CurrentLevel} construido ({level.Name}, {level.Width}×{level.Height}).");
        }

        Vector3Int CellOf(int x, int row) => new Vector3Int(x, Current.Height - 1 - row, 0);   // la fila 0 del texto es la de arriba
        Vector3 CenterOf(int x, int row) => ground.GetCellCenterWorld(CellOf(x, row));

        void PlaceTilesAndObjects(LevelDefinition level)
        {
            for (int row = 0; row < level.Height; row++)
            {
                string line = level.Map[row];
                for (int x = 0; x < line.Length; x++)
                {
                    char c = line[x];
                    if (c == LevelLegend.Empty || c == LevelLegend.LiftPath) continue;
                    Vector3Int cell = CellOf(x, row);
                    Vector3 center = CenterOf(x, row);

                    if (c == LevelLegend.OneWay)
                        oneWay.SetTile(cell, Find("ext_plataforma_fusta_" + EdgePiece(line, x)));
                    else if (LevelLegend.EdgeTiles.TryGetValue(c, out string prefix))
                        ground.SetTile(cell, Find(prefix + EdgePiece(line, x)));
                    else if (LevelLegend.Tiles.TryGetValue(c, out var entry))
                    {
                        Layer(entry.layer).SetTile(cell, Find(entry.tile));
                        CreateDecorLight(c, center);
                    }
                    else PlaceObject(level, c, x, row, cell, center);
                }
            }
        }

        // [Prefabs] Cada carácter con función instancia su prefab.
        void PlaceObject(LevelDefinition level, char c, int x, int row, Vector3Int cell, Vector3 center)
        {
            switch (c)
            {
                case LevelLegend.NilSpawn: characters.Add(Spawn(PrefabIds.Nil, center).GetComponent<PlayerCharacter>()); break;
                case LevelLegend.VoltSpawn: characters.Add(Spawn(PrefabIds.Volt, center).GetComponent<PlayerCharacter>()); break;
                case LevelLegend.Box: Spawn(PrefabIds.Box, center); break;
                case LevelLegend.Charger: Spawn(PrefabIds.Charger, center); break;
                case LevelLegend.Checkpoint: Spawn(PrefabIds.Checkpoint, center); break;
                case LevelLegend.Exit: Spawn(PrefabIds.Exit, center).GetComponent<LevelExit>().Init(this); break;
                case LevelLegend.SwingAnchor:
                    Spawn(PrefabIds.Swing, center + new Vector3(0f, 0.4f, 0f));
                    decoration.SetTile(cell, Find("ext_corda_ancoratge"));
                    break;
                case LevelLegend.Spikes: CreateSpikes(level, x, row, cell, center); break;
                case LevelLegend.Door:
                case LevelLegend.Lift:
                case LevelLegend.Lever:
                case LevelLegend.Terminal:
                case LevelLegend.Plate:
                case LevelLegend.Crystal:
                    break;   // los crea CreateSignalObjects() a partir del LevelModel
                default:
                    if (!TryCreateDialogue(level, c, center))
                        Debug.LogWarning($"Entre Dos Mons: carácter '{c}' desconocido en el nivel {CurrentLevel}, fila {row}, columna {x}.");
                    break;
            }
        }

        GameObject Spawn(string id, Vector3 position)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabIds.Folder + "/" + id);
            GameObject go;
            if (prefab != null) go = Instantiate(prefab, position, Quaternion.identity, generated);
            else
            {
                // Plan B: el prefab aún no existe (proyecto recién abierto). Se monta con la misma receta.
                go = PrefabRecipes.Create(id, SpriteOf);
                go.transform.SetParent(generated, false);
                go.transform.position = position;
            }
            go.name = id;
            return go;
        }

        // Fuentes y receptores, con su canal de la capa de cables.
        void CreateSignalObjects()
        {
            foreach (LevelModel.Source s in model.Sources)
            {
                string id = s.Kind == LevelLegend.Lever ? PrefabIds.Lever
                    : s.Kind == LevelLegend.Terminal ? PrefabIds.Terminal
                    : s.Kind == LevelLegend.Plate ? PrefabIds.Plate : PrefabIds.Crystal;
                Spawn(id, CenterOf(s.X, s.Row)).GetComponent<SignalSource>().Connect(s.Channel);
            }

            foreach (LevelModel.Receiver r in model.Receivers)
            {
                if (r.Kind == LevelLegend.Door)
                {
                    // Vertical: el objeto va en la casilla de abajo. Horizontal: en la de la izquierda.
                    int anchor = r.Horizontal ? r.Cells[0] : r.Cells[r.Cells.Count - 1];
                    GameObject go = Spawn(PrefabIds.Door, CenterOf(model.XOf(anchor), model.RowOf(anchor)));
                    var door = go.GetComponent<Door>();
                    door.Build(r.Cells.Count, r.Horizontal);
                    door.Connect(r.Channel, r.Horizontal ? new Vector3(0f, 0.7f, 0f) : new Vector3(0f, r.Cells.Count - 0.2f, 0f));
                }
                else if (r.Kind == LevelLegend.Lift)
                {
                    Vector3 left = CenterOf(r.LiftLeft, r.LiftBottom);
                    int width = r.LiftRight - r.LiftLeft + 1;
                    GameObject go = Spawn(PrefabIds.Lift, left + new Vector3((width - 1) / 2f, 0f, 0f));
                    var lift = go.GetComponent<Lift>();
                    lift.Build(width, r.LiftBottom - r.LiftTop);
                    lift.Connect(r.Channel, new Vector3(0f, 0.7f, 0f));
                }
                // El agua con desagüe la monta CreateWater().
            }
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

        static bool IsSolid(char c) => LevelLegend.IsSolidTerrain(c) || c == LevelLegend.Box || c == LevelLegend.Door;

        // ---------- Recorrido ----------

        void CreateSpikes(LevelDefinition level, int x, int row, Vector3Int cell, Vector3 center)
        {
            bool cave = model.WorldOf(row) == World.Cueva;
            decoration.SetTile(cell, Find(cave ? LevelLegend.SpikesCave : LevelLegend.SpikesExterior));
            // Si hay suelo encima y no debajo, cuelgan del techo (para Volt boca abajo).
            bool hanging = IsSolid(level.At(x, row - 1)) && !IsSolid(level.At(x, row + 1));
            if (hanging) decoration.SetTransformMatrix(cell, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 180f)));
            Spawn(PrefabIds.Spikes, center + new Vector3(0f, hanging ? 0.25f : -0.25f, 0f));
        }

        // Agua: un rectángulo por cada tramo horizontal de '~' / 'w'. Si el agua tiene cable, todos sus tramos
        // van a un DrainableWater que la vacía cuando se activa su canal.
        void CreateWater(LevelDefinition level)
        {
            var drainOf = new Dictionary<int, LevelModel.Receiver>();
            foreach (LevelModel.Receiver r in model.Receivers)
                if (LevelLegend.IsWater(r.Kind))
                    foreach (int i in r.Cells) drainOf[i] = r;
            var zonesOf = new Dictionary<LevelModel.Receiver, List<WaterZone>>();

            for (int row = 0; row < level.Height; row++)
            {
                string line = level.Map[row];
                int x = 0;
                while (x < line.Length)
                {
                    if (!LevelLegend.IsWater(line[x])) { x++; continue; }
                    int start = x;
                    bool surface = false;
                    while (x < line.Length && LevelLegend.IsWater(line[x]))
                    {
                        surface |= line[x] == LevelLegend.WaterSurface;
                        x++;
                    }
                    Vector3 corner = ground.CellToWorld(CellOf(start, row));
                    var go = new GameObject("Agua");
                    go.transform.SetParent(generated, false);
                    go.transform.position = corner;
                    var zone = go.AddComponent<WaterZone>();
                    zone.Area = new Rect(corner.x, corner.y, x - start, surface ? 0.7f : 1f);
                    if (drainOf.TryGetValue(model.Index(start, row), out LevelModel.Receiver r))
                    {
                        if (!zonesOf.TryGetValue(r, out List<WaterZone> list)) zonesOf[r] = list = new List<WaterZone>();
                        list.Add(zone);
                    }
                }
            }

            foreach (var pair in zonesOf)
            {
                LevelModel.Receiver r = pair.Key;
                var cells = new List<Vector3Int>();
                foreach (int i in r.Cells) cells.Add(CellOf(model.XOf(i), model.RowOf(i)));
                var go = new GameObject("Desague " + r.Channel);
                go.transform.SetParent(generated, false);
                go.transform.position = CenterOf(model.XOf(r.Cells[0]), model.RowOf(r.Cells[0]));
                var drain = go.AddComponent<DrainableWater>();
                drain.Build(pair.Value, decoration, cells);
                drain.Connect(r.Channel, new Vector3(0f, 0.9f, 0f));
            }
        }

        // Cuerdas ('T' + '|') y escaleras ('e'): una zona por cada columna seguida. Sobresale media casilla por arriba
        // para poder terminar de subir a la plataforma.
        void CreateClimbables(LevelDefinition level)
        {
            for (int x = 0; x < level.Width; x++)
            {
                int row = 0;
                while (row < level.Height)
                {
                    ClimbKind? kind = ClimbKindOf(level.At(x, row));
                    if (kind == null) { row++; continue; }
                    int top = row;
                    while (row < level.Height && ClimbKindOf(level.At(x, row)) == kind) row++;
                    Vector3 corner = ground.CellToWorld(CellOf(x, row - 1));   // esquina de abajo
                    var go = new GameObject(kind == ClimbKind.Cuerda ? "Cuerda" : "Escalera");
                    go.transform.SetParent(generated, false);
                    go.transform.position = corner;
                    go.AddComponent<Climbable>().Init(kind.Value, new Rect(corner.x + 0.2f, corner.y, 0.6f, row - top + 0.5f));
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

        // Setas y farolas dan un poco de luz (en las cuevas es casi la única que hay, aparte de Volt y los cristales).
        void CreateDecorLight(char c, Vector3 center)
        {
            if (!createLights) return;
            if (c == 'b') AddLight("Luz setas", center, new Color32(0x62, 0xB5, 0x6B, 0xFF), 1.6f, 0.6f);
            else if (c == 'L') AddLight("Luz farola", center + new Vector3(0f, 0.3f, 0f), new Color32(0xFF, 0xD3, 0x4E, 0xFF), 3f, 0.8f);
        }

        void AddLight(string name, Vector3 position, Color color, float radius, float intensity)
        {
            Light2D light = PrefabRecipes.AddLight(generated, name, Vector3.zero, color, radius, intensity);
            light.transform.position = position;
        }

        // ---------- Diálogos y personajes ----------

        bool TryCreateDialogue(LevelDefinition level, char c, Vector3 center)
        {
            foreach (DialogueSpot spot in level.Dialogues)
            {
                if (spot.Symbol != c) continue;
                // Sin panel de diálogos, una cinemática dejaría al personaje congelado: no se crean las zonas.
                if (FindAnyObjectByType<DialogueUI>() == null && FindAnyObjectByType<DialoguePanel>() == null) return true;
                Vector3 offset = new Vector3(0f, (spot.Size.y - 1f) / 2f, 0f);
                GameObject zone = CreateZone("Dialogo " + spot.Id, center + offset, spot.Size);
                zone.AddComponent<DialogueTrigger>().Setup(spot.Id, spot.Who, spot.Activation);
                return true;
            }
            return false;
        }

        // Personajes recién creados: límites de caída, punto de control y cámara.
        void StartCharacters()
        {
            foreach (PlayerCharacter p in characters)
            {
                p.FallLimitBottom = -5f;
                p.FallLimitTop = Current.Height + 5f;
                p.PlaceAt(p.transform.position);
            }
            var switcher = FindAnyObjectByType<CharacterSwitcher>();
            if (switcher != null)
            {
                if (switcher.CameraFollow == null) switcher.CameraFollow = cameraFollow;
                switcher.SetCharacters(characters);
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
            if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow2D>();
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

        // Sprites para las recetas de prefabs: de los tiles y, en el editor, de Assets/Art (los de los personajes).
        Sprite SpriteOf(string spriteName)
        {
            if (tilesByName.TryGetValue(spriteName, out TileBase t) && t is Tile tile) return tile.sprite;
#if UNITY_EDITOR
            foreach (string folder in new[] { "Assets/Art/Tiles/", "Assets/Art/Characters/" })
            {
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(folder + spriteName + ".png");
                if (s != null) return s;
            }
#endif
            return null;
        }

        void IndexTiles()
        {
            tilesByName.Clear();
            if (tiles == null) return;
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
                old.gameObject.SetActive(false);   // se quitan ya de las listas (personajes, interactuables...)
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }
            generated = new GameObject("Generado").transform;
            generated.SetParent(transform, false);
        }
    }
}
