using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons.Mechanics
{
    // Cargador de batería ('c' en el mapa). Volt se recarga mientras está encima y además guarda ahí su punto de control.
    [RequireComponent(typeof(Collider2D))]
    public class BatteryCharger : MonoBehaviour
    {
        [Tooltip("% por segundo.")]
        [SerializeField] float chargeRate = 30f;
        [SerializeField] Light2D glow;

        float pulse;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (glow == null) glow = GetComponentInChildren<Light2D>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var battery = other.GetComponentInParent<VoltBattery>();
            if (battery == null) return;
            battery.GetComponent<PlayerCharacter>().SetCheckpoint(transform.position);
            if (battery.Normalized < 1f) GameAudio.Play("carga");
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var battery = other.GetComponentInParent<VoltBattery>();
            if (battery == null) return;
            battery.AddCharge(chargeRate * Time.deltaTime);
            pulse = 0.3f;
        }

        void Update()
        {
            if (glow == null) return;
            pulse -= Time.deltaTime;
            float target = pulse > 0f ? 1.4f + Mathf.Sin(Time.time * 10f) * 0.4f : 0.8f;
            glow.intensity = Mathf.MoveTowards(glow.intensity, target, Time.deltaTime * 4f);
        }
    }
}
