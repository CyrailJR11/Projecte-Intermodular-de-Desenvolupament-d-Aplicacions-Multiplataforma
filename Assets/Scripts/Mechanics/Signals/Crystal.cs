using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons.Mechanics
{
    // Cristal ('k'). Volt lo carga apuntándole con la linterna un momento; luego se queda encendido para siempre.
    // [POO · Interfaces] Es a la vez SignalSource (enciende su canal) e ILightReceiver (lo llama la linterna).
    [RequireComponent(typeof(Collider2D))]
    public class Crystal : SignalSource, ILightReceiver
    {
        [Tooltip("Segundos de luz que necesita para cargarse.")]
        [SerializeField] float chargeTime = 1.2f;
        [SerializeField] Light2D glow;
        [SerializeField] float glowOff = 0.3f, glowOn = 1.6f;

        float charge;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (glow == null) glow = GetComponentInChildren<Light2D>();
            if (glow != null) glow.intensity = glowOff;
        }

        public void Illuminate(PlayerCharacter source, float deltaTime)
        {
            if (IsActive || source == null || !source.Has(CharacterAbility.Linterna)) return;
            charge += deltaTime;
            if (glow != null) glow.intensity = Mathf.Lerp(glowOff, glowOn, charge / chargeTime);
            if (charge >= chargeTime) SetActive(true);
        }

        protected override void OnActiveChanged(bool on)
        {
            if (on) GameAudio.Play("carga");
            if (glow != null) glow.intensity = on ? glowOn : glowOff;
        }
    }
}
