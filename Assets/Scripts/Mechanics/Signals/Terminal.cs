using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons.Mechanics
{
    // Terminal eléctrico de la estación ('G'). Solo responde a la unidad VLT-05 → solo Volt (conexión de datos).
    // Devuelve la corriente a un montacargas, una verja o una trampilla (lo que diga su canal).
    public class Terminal : InteractableSource
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Light2D screenLight;
        [SerializeField] Color offColor = new Color32(0xEE, 0x6C, 0x2F, 0xFF);
        [SerializeField] Color onColor = new Color32(0x62, 0xB5, 0x6B, 0xFF);

        protected override CharacterAbility RequiredAbility => CharacterAbility.ConexionDatos;
        protected override string ActionText(bool turnOn) => turnOn ? "Conectar y dar corriente" : "Cortar la corriente";
        protected override string DeniedText => "Acceso denegado: solo unidades VLT";

        void Awake()
        {
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (screenLight == null) screenLight = GetComponentInChildren<Light2D>();
            OnActiveChanged(false);
        }

        protected override void OnActiveChanged(bool on)
        {
            if (Application.isPlaying && Time.timeSinceLevelLoad > 0.1f) GameAudio.Play("carga", 0.7f);
            if (screenLight != null) screenLight.color = on ? onColor : offColor;
            if (sprite != null) sprite.color = on ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }
}
