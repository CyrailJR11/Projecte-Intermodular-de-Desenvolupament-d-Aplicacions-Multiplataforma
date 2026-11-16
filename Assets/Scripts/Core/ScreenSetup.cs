using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EntreDosMons
{
    // Cómo se ve el juego en pantalla.
    //
    // El juego se dibuja a 640x360 (casillas de 32 px) y se amplía a la pantalla con la Pixel Perfect Camera:
    //   · 2560x1440 = 640x360 x 4 exacto: cada píxel del juego son 4x4 píxeles de pantalla. Nítido y sin bordes.
    //   · 1920x1080 = x 3 exacto, 3840x2160 = x 6 exacto. También perfectos.
    //   · Otras (1366x768, 2560x1080...): se amplía al múltiplo entero más grande y lo que falta se estira un poco
    //     (StretchFill) para que no salgan bandas negras alrededor.
    //
    // En la versión compilada (build) el juego arranca a pantalla completa con la resolución del monitor.
    // En el Editor no se toca la ventana: en la pestaña Game elige 2560x1440 (o "16:9") para verlo igual.
    //
    // [POO · Encapsulamiento] Clase estática: nadie tiene que crearla ni ponerla en la escena; Unity llama a Init solo.
    public static class ScreenSetup
    {
        public const int RefWidth = 640;
        public const int RefHeight = 360;
        public const int PixelsPerUnit = 32;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
#if !UNITY_EDITOR
            // Resolución nativa del monitor (en el de Carlos, 2560x1440), en ventana a pantalla completa.
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (w > 0 && h > 0) Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
#endif
            // Sin tirones al mover la cámara: un fotograma por refresco del monitor.
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            QualitySettings.antiAliasing = 0;

            ConfigureCamera(Camera.main);
            SceneManager.sceneLoaded += (scene, mode) => ConfigureCamera(Camera.main);
        }

        // Deja la cámara lista para pixel art. La usa también el menú "Preparar escena" del Editor.
        public static void ConfigureCamera(Camera cam)
        {
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = RefHeight / 2f / PixelsPerUnit;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            cam.allowDynamicResolution = false;

            var pp = cam.GetComponent<PixelPerfectCamera>();
            if (pp == null) pp = cam.gameObject.AddComponent<PixelPerfectCamera>();
            pp.assetsPPU = PixelsPerUnit;
            pp.refResolutionX = RefWidth;
            pp.refResolutionY = RefHeight;
            // Se dibuja a baja resolución y se amplía entero: los píxeles nunca se deforman ni tiemblan.
            pp.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            // Sin bandas negras: si la pantalla no es un múltiplo exacto, se estira lo justo para llenarla.
            pp.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            pp.filterMode = PixelPerfectCamera.PixelPerfectFilterMode.RetroAA;
        }
    }
}
