using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EntreDosMons.Mechanics
{
    // Cargador de batería ('c' en el mapa). Volt se recarga mientras está encima y además guarda ahí su punto de control.
    [RequireComponent(typeof(Collider2D))]
    public class BatteryCharger : MonoBehaviour
    {
        [Tooltip("% por segundo.")]
        public float chargeRate = 30f;
        public Light2D glow;

        float pulse;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            var battery = other.GetComponentInParent<VoltBattery>();
            if (battery == null) return;
            var player = battery.GetComponent<PlayerController2D>();
            if (player != null) player.SetCheckpoint(transform.position);
            if (battery.Charge < battery.maxCharge) GameAudio.Play("carga");
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
