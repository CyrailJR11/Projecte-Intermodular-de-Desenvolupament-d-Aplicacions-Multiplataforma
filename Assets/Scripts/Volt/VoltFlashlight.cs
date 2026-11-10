using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons
{
    // Linterna frontal de Volt (Light 2D de URP, tipo Point en forma de cono) y un halo pequeño a su alrededor.
    // Apunta hacia donde mira Volt, también boca abajo en el techo. Se enciende y apaga con L (LB en el mando).
    // Gasta batería: con poca carga alumbra menos y parpadea; sin carga se apaga y el halo casi desaparece.
    [RequireComponent(typeof(PlayerController2D))]
    public class VoltFlashlight : MonoBehaviour
    {
        public bool startOn = true;
        public Color beamColor = new Color32(0xF5, 0xF0, 0xD8, 0xFF);
        public float beamIntensity = 1.2f;
        [Tooltip("Alcance en casillas con la batería llena.")]
        public float beamRange = 7f;
        public float beamInnerAngle = 35f;
        public float beamOuterAngle = 70f;
        [Tooltip("Inclinación hacia el suelo (0 = recto).")]
        public float beamTilt = 0.3f;
        public Color haloColor = new Color32(0xA8, 0xBD, 0xC7, 0xFF);
        public float haloIntensity = 0.6f;
        public float haloRadius = 1.6f;

        public bool IsOn { get; private set; }
        public bool IsLit => IsOn && (battery == null || !battery.IsEmpty);

        PlayerController2D player;
        VoltBattery battery;
        Light2D beam;
        Light2D halo;

        void Awake()
        {
            player = GetComponent<PlayerController2D>();
            IsOn = startOn;
            beam = CreateLight("Linterna", beamColor, beamIntensity, beamRange, beamInnerAngle, beamOuterAngle);
            halo = CreateLight("Halo", haloColor, haloIntensity, haloRadius, 360f, 360f);
        }

        Light2D CreateLight(string name, Color color, float intensity, float radius, float inner, float outer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerAngle = inner;
            light.pointLightOuterAngle = outer;
            light.falloffIntensity = 0.6f;
            return light;
        }

        public void SetOn(bool on) => IsOn = on;

        void Update()
        {
            if (battery == null) battery = GetComponent<VoltBattery>();
            if (player.CanAct && GameInput.FlashlightPressed())
            {
                IsOn = !IsOn;
                GameAudio.Play("linterna");
            }

            float charge = battery != null ? battery.Normalized : 1f;
            float strength = Mathf.Clamp01(charge / 0.3f);   // por debajo del 30 % empieza a perder fuerza
            float flicker = 1f;
            if (charge < 0.15f) flicker = Mathf.PerlinNoise(Time.time * 12f, 0f) > 0.35f ? 1f : 0.2f;

            beam.enabled = IsLit;
            beam.intensity = beamIntensity * Mathf.Lerp(0.4f, 1f, strength) * flicker;
            beam.pointLightOuterRadius = Mathf.Lerp(beamRange * 0.5f, beamRange, strength);
            halo.intensity = battery != null && battery.IsEmpty ? haloIntensity * 0.15f : haloIntensity;

            float g = player.GravitySign;
            Vector2 dir = new Vector2(player.Facing, -beamTilt * g).normalized;
            beam.transform.localPosition = new Vector3(0.15f * player.Facing, 0.1f * g, 0f);
            beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
    }
}
