using UnityEditor;
using UnityEngine;

// Configura automàticament els PNG de Assets/Art/Tiles/ i Assets/Art/Characters/ com a sprites pixel art de 32 px.
// Només s'aplica la primera vegada que s'importa cada fitxer, així que després els pots retocar a mà.
public class EntreDosMonsTileImporter : AssetPostprocessor
{
    static readonly string[] Folders = { "/Art/Tiles/", "/Art/Characters/" };

    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!assetImporter.importSettingsMissing || !System.Array.Exists(Folders, f => path.Contains(f)))
            return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = path.Contains("EntreDosMons_tiles_32px")
            ? SpriteImportMode.Multiple   // el full sencer: Sprite Editor > Slice > Grid By Cell Size 32x32
            : SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
    }

    // Fuentes de Assets/Resources/Fonts: la pixel (Press Start 2P) sin suavizado para que se vea nítida.
    void OnPreprocessAsset()
    {
        if (!(assetImporter is TrueTypeFontImporter font) || !assetImporter.importSettingsMissing) return;
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Fonts/")) return;
        font.fontTextureCase = FontTextureCase.Dynamic;
        font.includeFontData = true;
        if (assetPath.Contains("PressStart2P")) font.fontRenderingMode = FontRenderingMode.HintedRaster;
    }
}
