using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Batería de Volt (0-100 %). La gastan la linterna, cada cambio de gravedad y, mucho, el agua ("esta unidad no flota").
    // Se recarga en los cargadores ('c' en el mapa).
    // Sin batería: la linterna se apaga, no puede invertir la gravedad (si estaba en el techo, cae),
    // anda más lento y, si está en el agua, sufre un cortocircuito y pierde una vida.
    // [POO · Herencia] Es un CharacterResource (igual que la respiración de Nil): el HUD la pinta sin saber qué es.
    // [POO · Encapsulamiento] Charge solo cambia con TryConsume / AddCharge / SetCharge.
    public class VoltBattery : CharacterResource
    {
        [SerializeField] float maxCharge = 100f;
        [Tooltip("Carga al empezar el nivel.")]
        [SerializeField] float startCharge = 100f;
        [Tooltip("% por segundo con la linterna encendida.")]
        [SerializeField] float flashlightDrain = 1f;
        [Tooltip("% por segundo dentro del agua.")]
        [SerializeField] float waterDrain = 12f;
        [Tooltip("Por debajo de este % Volt avisa (bark_bateria_baja).")]
        [SerializeField] float lowThreshold = 20f;
        [Tooltip("Carga mínima con la que reaparece después de perder una vida.")]
        [SerializeField] float respawnMinimum = 35f;
        [Range(0.1f, 1f)] [SerializeField] float reserveSpeedMultiplier = 0.6f;

        public float Charge { get; private set; }
        public bool IsEmpty => Charge <= 0f;
        public bool IsLow => Charge < lowThreshold;
        public bool IsCharging => chargingTimer > 0f;

        public override float Normalized => maxCharge > 0f ? Charge / maxCharge : 0f;
        public override string HudLabel => (IsCharging ? "+" : "") + Mathf.CeilToInt(Charge) + "%";
        public override Color HudColor => IsLow ? (Color)new Color32(0xEE, 0x6C, 0x2F, 0xFF) : (Color)new Color32(0xF5, 0xD0, 0x4C, 0xFF);

        VoltFlashlight flashlight;
        bool lowWarned;
        float chargingTimer;

        protected override void Awake()
        {
            base.Awake();
            flashlight = GetComponent<VoltFlashlight>();
            Charge = Mathf.Clamp(startCharge, 0f, maxCharge);
        }

        void OnEnable() { if (Character != null) Character.Respawned += OnRespawned; }
        void OnDisable() { if (Character != null) Character.Respawned -= OnRespawned; }
        void OnRespawned(PlayerCharacter _) => Charge = Mathf.Max(Charge, respawnMinimum);

        public override void Refill()
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

            float drain = 0f;
            if (flashlight != null && flashlight.IsLit) drain += flashlightDrain;
            if (Character.IsInWater) drain += waterDrain;
            if (!IsCharging) Charge = Mathf.Max(0f, Charge - drain * Time.deltaTime);

            if (!lowWarned && IsLow)
            {
                lowWarned = true;
                if (DialogueManager.Instance != null) DialogueManager.Instance.PlayBark("bark_bateria_baja");
            }
            else if (Charge > lowThreshold + 10f) lowWarned = false;

            if (IsEmpty)
            {
                Character.SpeedMultiplier = reserveSpeedMultiplier;
                if (Character.IsGravityFlipped) Character.ForceNormalGravity();
                if (Character.IsInWater) Character.Health.Damage(DamageType.Cortocircuito);
            }
            else Character.SpeedMultiplier = 1f;
        }
    }
}
