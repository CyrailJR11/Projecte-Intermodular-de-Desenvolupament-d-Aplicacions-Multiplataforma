using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels
{
    // Construye el nivel por código a partir de los mapas de texto de LevelLibrary:
    // pone los tiles en los Tilemaps, coloca a Nil y Volt, crea puntos de control, salida, zonas de diálogo,
    // paredes invisibles en los bordes y limita la cámara al tamaño del nivel.
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Tilemaps del Grid")]
        public Tilemap ground;
        public Tilemap decoration;
        public Tilemap background;

        [Header("Tiles (los rellena el menú Entre Dos Mons > 3)")]
        public TileBase[] tiles;

        [Header("Escena")]
        public PlayerController2D nil;
        public PlayerController2D volt;
        public CameraFollow2D cameraFollow;

        [Header("Nivel")]
        [Tooltip("Número del nivel con el que empieza la partida (0 = Prólogo).")]
        public int startLevel;

        public int CurrentLevel { get; private set; }
        public LevelDefinition Current => LevelLibrary.Levels[CurrentLevel];

        readonly Dictionary<string, TileBase> tilesByName = new Dictionary<string, TileBase>();
        Transform generated;

        void Start() => Build(startLevel);

        public void NextLevel()
        {
            if (CurrentLevel + 1 < LevelLibrary.Levels.Length) Build(CurrentLevel + 1);
            else Debug.Log("Entre Dos Mons: no hay más niveles.");
        }

        // Clic derecho en el componente > "Construir nivel" para verlo sin darle a Play.
        [ContextMenu("Construir nivel")]
        void BuildInEditor() => Build(startLevel);

        public void Build(int index)
        {
            CurrentLevel = Mathf.Clamp(index, 0, LevelLibrary.Levels.Length - 1);
            LevelDefinition level = Current;
            IndexTiles();
            Clear();

            int height = level.Height;
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

                    if (LevelLegend.EdgeTiles.TryGetValue(c, out string prefix))
                        ground.SetTile(cell, Find(prefix + EdgePiece(line, x)));
                    else if (LevelLegend.Tiles.TryGetValue(c, out var entry))
                        Layer(entry.layer).SetTile(cell, Find(entry.tile));
                    else if (c == LevelLegend.NilSpawn) Place(nil, center);
                    else if (c == LevelLegend.VoltSpawn) Place(volt, center);
                    else if (c == LevelLegend.Checkpoint) CreateZone("Checkpoint", center, new Vector2(1f, 4f)).AddComponent<Checkpoint>();
                    else if (c == LevelLegend.Exit) CreateZone("Salida", center, new Vector2(1f, 4f)).AddComponent<LevelExit>().builder = this;
                    else if (!TryCreateDialogue(level, c, center))
                        Debug.LogWarning($"Entre Dos Mons: carácter '{c}' desconocido en el nivel {CurrentLevel}, fila {row}, columna {x}.");
                }
            }

            CreateBorders(level.Width, height);
            FitCamera(level.Width, height);
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
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
                if (body.gravityScale < 0f) player.FlipGravity();
            }
            player.fallLimitBottom = -5f;
            player.fallLimitTop = Current.Height + 5f;
            if (Application.isPlaying) player.SetCheckpoint(position);
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
            Debug.LogWarning($"Entre Dos Mons: falta el tile '{tileName}' en la lista Tiles del LevelBuilder.");
            return null;
        }

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
