using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons.Mechanics
{
    // Luz ambiente del nivel: de día en el exterior y casi a oscuras en las cuevas, para que la linterna de Volt
    // y las luces de cristales, setas, cargadores y farolas sean las que alumbran. Usa la Global Light 2D de la escena.
    public class LevelLighting : MonoBehaviour
    {
        [SerializeField] float exteriorIntensity = 1f;
        [SerializeField] float caveIntensity = 0.12f;
        [SerializeField] float fadeSpeed = 1.5f;

        Light2D global;
        CharacterSwitcher switcher;

        void Start() => FindLights();

        void FindLights()
        {
            foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) { global = l; break; }
            if (global == null)
            {
                global = new GameObject("Global Light 2D").AddComponent<Light2D>();
                global.lightType = Light2D.LightType.Global;
            }
            switcher = FindAnyObjectByType<CharacterSwitcher>();
        }

        void Update()
        {
            if (global == null) FindLights();
            PlayerCharacter who = switcher != null ? switcher.Current : null;
            bool inCave = who != null && WorldMap.HasTwoWorlds && WorldMap.WorldAt(who.transform.position.y) == World.Cueva;
            float target = inCave ? caveIntensity : exteriorIntensity;
            global.intensity = Mathf.MoveTowards(global.intensity, target, fadeSpeed * Time.deltaTime);
        }
    }
}
