using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Batería de Volt (0-100 %). La gasta la linterna, cada cambio de gravedad y, mucho, el agua ("esta unidad no flota").
    // Se recarga en los cargadores ('c' en el mapa).
    // Sin batería: la linterna se apaga, no puede invertir la gravedad (si estaba en el techo, cae),
    // anda más lento y, si está en el agua, sufre un cortocircuito y pierde una vida.
    [RequireComponent(typeof(PlayerController2D))]
    public class VoltBattery : MonoBehaviour
    {
        public float maxCharge = 100f;
        [Tooltip("Carga al empezar el nivel.")]
        public float startCharge = 100f;
        [Tooltip("% por segundo con la linterna encendida.")]
        public float flashlightDrain = 1f;
        [Tooltip("% por segundo dentro del agua.")]
        public float waterDrain = 12f;
        [Tooltip("Por debajo de este % Volt avisa (bark_bateria_baja).")]
        public float lowThreshold = 20f;
        [Tooltip("Carga mínima con la que reaparece después de perder una vida.")]
        public float respawnMinimum = 35f;
        [Range(0.1f, 1f)] public float reserveSpeedMultiplier = 0.6f;

        public float Charge { get; private set; }
        public float Normalized => maxCharge > 0f ? Charge / maxCharge : 0f;
        public bool IsEmpty => Charge <= 0f;
        public bool IsLow => Charge < lowThreshold;
        public bool IsCharging => chargingTimer > 0f;

        PlayerController2D player;
        PlayerHealth health;
        VoltFlashlight flashlight;
        bool lowWarned;
        float chargingTimer;

        void Awake()
        {
            player = GetComponent<PlayerController2D>();
            Charge = Mathf.Clamp(startCharge, 0f, maxCharge);
        }

        void OnEnable() { if (player != null) player.Respawned += OnRespawn; }
        void OnDisable() { if (player != null) player.Respawned -= OnRespawn; }

        void OnRespawn(PlayerController2D _) => Charge = Mathf.Max(Charge, respawnMinimum);

        public void ResetCharge()
        {
            Charge = Mathf.Clamp(startCharge, 0f, maxCharge);
            lowWarned = false;
        }

        // Para eventos del guion, p. ej. c3_03_bateria_critica fuerza el 9 %.
        public void SetCharge(float value) => Charge = Mathf.Clamp(value, 0f, maxCharge);

        public bool TryConsume(float amount)
        {
            if (Charge < amount) return false;
            Charge -= amount;
            return true;
        }

        public void AddCharge(float amount)
        {
            bool wasFull = Charge >= maxCharge;
            Charge = Mathf.Min(maxCharge, Charge + amount);
            chargingTimer = 0.2f;
            if (!wasFull && Charge >= maxCharge && DialogueManager.Instance != null)
                DialogueManager.Instance.PlayBark("bark_recarga");
        }

        void Update()
        {
            chargingTimer -= Time.deltaTime;
            if (DialogueManager.IsCinematicPlaying) return;
            if (flashlight == null) flashlight = GetComponent<VoltFlashlight>();
            if (health == null) health = GetComponent<PlayerHealth>();

            float drain = 0f;
            if (flashlight != null && flashlight.IsLit) drain += flashlightDrain;
            if (player.IsInWater) drain += waterDrain;
            if (!IsCharging) Charge = Mathf.Max(0f, Charge - drain * Time.deltaTime);

            if (!lowWarned && IsLow)
            {
                lowWarned = true;
                if (DialogueManager.Instance != null) DialogueManager.Instance.PlayBark("bark_bateria_baja");
            }
            else if (Charge > lowThreshold + 10f) lowWarned = false;

            if (IsEmpty)
            {
                player.SpeedMultiplier = reserveSpeedMultiplier;
                if (player.IsGravityFlipped) player.ForceNormalGravity();
                if (player.IsInWater && health != null) health.Damage(DamageType.Cortocircuito);
            }
            else player.SpeedMultiplier = 1f;
        }
    }
}
