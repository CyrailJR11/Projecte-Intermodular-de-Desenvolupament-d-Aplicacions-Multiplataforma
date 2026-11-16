using System.IO;
using UnityEditor;
using UnityEngine;
using EntreDosMons.Levels;

// Crea los PREFABS del juego en Assets/Resources/Prefabs a partir de las recetas de PrefabRecipes.cs.
// Se ejecuta solo al abrir el proyecto si falta alguno (y con el menú, para volver a crearlos todos).
// Después se pueden abrir y retocar como cualquier prefab: el LevelBuilder instancia lo que haya guardado.
[InitializeOnLoad]
public static class PrefabFactory
{
    const string Folder = "Assets/Resources/" + PrefabIds.Folder;

    static PrefabFactory()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) CreateMissingPrefabs();
        };
    }

    [MenuItem("Entre Dos Mons/4. Crear prefabs (rehacerlos todos)")]
    public static void RecreateAll() => Create(true);

    public static void CreateMissingPrefabs() => Create(false);

    static void Create(bool overwrite)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", PrefabIds.Folder);

        int created = 0;
        foreach (string id in PrefabIds.All)
        {
            string path = $"{Folder}/{id}.prefab";
            if (!overwrite && File.Exists(path)) continue;
            GameObject go = PrefabRecipes.Create(id, LoadSprite);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(go, path);
                created++;
            }
            finally
            {
                Object.DestroyImmediate(go);   // nunca dejar el objeto temporal suelto en la escena
            }
        }
        if (created == 0) return;
        AssetDatabase.SaveAssets();
        Debug.Log($"Entre Dos Mons: {created} prefabs creados en {Folder}.");
    }

    // "ext_valvula" → Assets/Art/Tiles/ext_valvula.png · "Nil/nil_0" → Assets/Art/Characters/Nil/nil_0.png
    static Sprite LoadSprite(string name)
    {
        foreach (string folder in new[] { "Assets/Art/Tiles/", "Assets/Art/Characters/" })
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(folder + name + ".png");
            if (s != null) return s;
        }
        Debug.LogWarning("Entre Dos Mons: no encuentro el sprite " + name);
        return null;
    }
}
