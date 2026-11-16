using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using EntreDosMons;
using EntreDosMons.Dialogue;
using EntreDosMons.Levels;

// Menú "Entre Dos Mons" para montar el proyecto en dos clics:
//   1. Crear tiles y paletas: un Tile por cada PNG de Assets/Art/Tiles y dos Tile Palettes (Exterior y Coves).
//   2. Preparar escena de prueba: Grid con 3 Tilemaps, suelo de prueba, cámara pixel perfect y diálogos.
//   3. Usar niveles por código: añade el LevelBuilder, que construye los niveles de LevelLibrary al darle a Play.
//   4. Crear prefabs (PrefabFactory.cs) y 5. Validar niveles (LevelValidationMenu.cs).
public static class EntreDosMonsSetup
{
    const string TilesFolder = "Assets/Art/Tiles";
    const string TileAssetsFolder = "Assets/Art/Tiles/TileAssets";
    const string PalettesFolder = "Assets/Art/Palettes";
        const int PPU = 32;
    const int RefWidth = 640, RefHeight = 360;   // 20 × 11 casillas; x3 en 1080p, x4 en 1440p

    // Tiles que chocan con los personajes. El resto (arbustos, flores, cuerdas, agua, fanal, tubos...) son decoración.
    static readonly HashSet<string> SolidTiles = new HashSet<string>
    {
        "ext_herba_esquerra", "ext_herba_mig", "ext_herba_dreta", "ext_terra_ple", "ext_bloc_pedra", "ext_caixa_fusta",
        "ext_plataforma_fusta_esquerra", "ext_plataforma_fusta_mig", "ext_plataforma_fusta_dreta",
        "cova_roca_esquerra", "cova_roca_mig", "cova_roca_dreta", "cova_roca_ple", "cova_roca_molsa", "cova_plataforma_metall",
    };

    [MenuItem("Entre Dos Mons/1. Crear tiles y paletas")]
    public static void CreateTilesAndPalettes()
    {
        EnsureFolder(TileAssetsFolder);
        EnsureFolder(PalettesFolder);

        var exterior = new List<TileBase>();
        var coves = new List<TileBase>();
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { TilesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(path).Replace('\\', '/') != TilesFolder) continue;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) continue;

            string name = Path.GetFileNameWithoutExtension(path);
            string tilePath = $"{TileAssetsFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.colliderType = SolidTiles.Contains(name) ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            (name.StartsWith("cova_") ? coves : exterior).Add(tile);
        }
        AssetDatabase.SaveAssets();

        FillPalette("Exterior", exterior);
        FillPalette("Coves", coves);
        AssetDatabase.Refresh();
        Debug.Log($"Entre Dos Mons: {exterior.Count + coves.Count} tiles creados y paletas Exterior / Coves en {PalettesFolder}. " +
                  "Ábrelas en Window > 2D > Tile Palette.");
    }

    static void FillPalette(string paletteName, List<TileBase> tiles)
    {
        string path = $"{PalettesFolder}/{paletteName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            GridPaletteUtility.CreateNewPalette(PalettesFolder, paletteName,
                GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, new Vector3(1f, 1f, 0f),
                GridLayout.CellSwizzle.XYZ, TransparencySortMode.Default, new Vector3(0f, 0f, 1f));
        }

        tiles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        var tilemap = root.GetComponentInChildren<Tilemap>();
        tilemap.ClearAllTiles();
        for (int i = 0; i < tiles.Count; i++)
            tilemap.SetTile(new Vector3Int(i % 8, -(i / 8), 0), tiles[i]);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    [MenuItem("Entre Dos Mons/2. Preparar escena de prueba")]
    public static void SetupScene()
    {
        AddTag("Nil");
        AddTag("Volt");
        int groundLayer = AddLayer("Suelo");
        int playerLayer = AddLayer("Jugador");
        if (groundLayer < 0 || playerLayer < 0)
        {
            Debug.LogError("Entre Dos Mons: no quedan capas libres en Tags & Layers.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<Tile>($"{TileAssetsFolder}/ext_herba_mig.asset") == null)
            CreateTilesAndPalettes();

        // Grid con tres capas: fondo (detrás), suelo (con colisión) y decoración (delante del fondo, detrás de los personajes).
        var grid = new GameObject("Grid", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(grid, "Preparar escena");
        Tilemap fondo = CreateTilemap(grid.transform, "Fondo", -10);
        Tilemap suelo = CreateTilemap(grid.transform, "Suelo", 0);
        CreateTilemap(grid.transform, "Decoracion", 5);
        suelo.gameObject.layer = groundLayer;

        var body = suelo.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = suelo.gameObject.AddComponent<TilemapCollider2D>();
        suelo.gameObject.AddComponent<CompositeCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        // Suelo de prueba: hierba y tierra a la izquierda (Nil), cueva con techo a la derecha (Volt).
        for (int x = -10; x < 18; x++)
        {
            suelo.SetTile(new Vector3Int(x, -3, 0), LoadTile("ext_herba_mig"));
            suelo.SetTile(new Vector3Int(x, -4, 0), LoadTile("ext_terra_ple"));
        }
        for (int x = 18; x < 34; x++)
        {
            suelo.SetTile(new Vector3Int(x, -3, 0), LoadTile("cova_roca_mig"));
            suelo.SetTile(new Vector3Int(x, -4, 0), LoadTile("cova_roca_ple"));
            suelo.SetTile(new Vector3Int(x, 3, 0), LoadTile("cova_roca_ple"));
            for (int y = -2; y < 3; y++) fondo.SetTile(new Vector3Int(x, y, 0), LoadTile("cova_engranatge_fons"));
        }
        for (int x = 4; x < 7; x++)
            suelo.SetTile(new Vector3Int(x, -1, 0), LoadTile(x == 4 ? "ext_plataforma_fusta_esquerra" : x == 6 ? "ext_plataforma_fusta_dreta" : "ext_plataforma_fusta_mig"));

        // Cámara pixel perfect (URP).
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
        cam.backgroundColor = new Color32(0x07, 0x18, 0x27, 0xFF);
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        ScreenSetup.ConfigureCamera(cam);   // Pixel Perfect 640x360 ampliado sin bandas negras (x4 en 2560x1440)
        var follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();

        // Los personajes ya no están en la escena: son PREFABS (Assets/Resources/Prefabs/Nil y Volt) y el
        // LevelBuilder los coloca en la 'N' y la 'V' de cada mapa. Aquí solo va quien los cambia (Q).
        var game = new GameObject("Juego");
        Undo.RegisterCreatedObjectUndo(game, "Preparar escena");
        game.AddComponent<CharacterSwitcher>().CameraFollow = follow;
        PrefabFactory.CreateMissingPrefabs();

        if (Object.FindAnyObjectByType<DialogueManager>() == null)
        {
            var dialogos = new GameObject("Dialogos");
            Undo.RegisterCreatedObjectUndo(dialogos, "Preparar escena");
            dialogos.AddComponent<DialogueManager>();
        }

        Selection.activeGameObject = grid;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(grid.scene);
        Debug.Log("Entre Dos Mons: escena lista. Ahora usa la opción 3 (niveles por código) y dale a Play: A/D andar, Espacio saltar, W/S trepar, " +
                  "Q cambiar de personaje, F invertir la gravedad de Volt, L linterna, E interactuar, R reiniciar el nivel.");
    }

    [MenuItem("Entre Dos Mons/3. Usar niveles por código")]
    public static void SetupLevelBuilder()
    {
        Tilemap ground = null, decoration = null, background = null;
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.name == "Suelo") ground = tm;
            else if (tm.name == "Decoracion") decoration = tm;
            else if (tm.name == "Fondo") background = tm;
        }
        if (ground == null || decoration == null || background == null)
        {
            Debug.LogError("Entre Dos Mons: no encuentro los Tilemaps Suelo, Decoracion y Fondo. Usa antes la opción 2.");
            return;
        }

        var tiles = new List<TileBase>();
        foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { TileAssetsFolder }))
            tiles.Add(AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid)));
        if (tiles.Count == 0)
        {
            CreateTilesAndPalettes();
            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { TileAssetsFolder }))
                tiles.Add(AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        var builder = Object.FindAnyObjectByType<LevelBuilder>();
        if (builder == null)
        {
            var go = new GameObject("Niveles");
            Undo.RegisterCreatedObjectUndo(go, "Usar niveles por código");
            builder = go.AddComponent<LevelBuilder>();
        }
        Undo.RecordObject(builder, "Usar niveles por código");
        // Incluye los tiles de los objetos nuevos: terminal, válvula, placa, montacargas, trampilla y señal de canal.
        builder.Configure(ground, decoration, background, tiles.ToArray(), Object.FindAnyObjectByType<CameraFollow2D>());
        PrefabFactory.CreateMissingPrefabs();

        // Vista previa en el editor (el suelo de prueba de la opción 2 se borra). Al darle a Play se vuelve a construir.
        builder.Build(0);
        EditorUtility.SetDirty(builder);
        Selection.activeGameObject = builder.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        Debug.Log("Entre Dos Mons: LevelBuilder listo. Los mapas están en Assets/Scripts/Levels/LevelLibrary.cs.");
    }

    static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        return go.GetComponent<Tilemap>();
    }

    static Tile LoadTile(string name) => AssetDatabase.LoadAssetAtPath<Tile>($"{TileAssetsFolder}/{name}.asset");

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static SerializedObject TagManager() =>
        new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

    static void AddTag(string tag)
    {
        var so = TagManager();
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    // Devuelve el número de la capa (la crea en el primer hueco libre si no existe), o -1 si no hay sitio.
    static int AddLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;
        var so = TagManager();
        var layers = so.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(element.stringValue)) continue;
            element.stringValue = layerName;
            so.ApplyModifiedProperties();
            return i;
        }
        return -1;
    }
}
