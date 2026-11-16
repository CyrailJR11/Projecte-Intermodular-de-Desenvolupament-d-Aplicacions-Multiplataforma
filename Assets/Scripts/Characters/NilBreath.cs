using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons
{
    // Respiración de Nil: baja mientras tiene la cabeza bajo el agua y se recupera al salir.
    // Si llega a cero, Nil se ahoga: pierde una vida y vuelve al último punto de control.
    // [POO · Herencia] Es un CharacterResource.
    public class NilBreath : CharacterResource
    {
        [Tooltip("Segundos que aguanta bajo el agua.")]
        [SerializeField] float maxBreath = 8f;
        [Tooltip("Cuántas veces más rápido se recupera que se gasta.")]
        [SerializeField] float refillMultiplier = 3f;

        public float Breath { get; private set; }
        public bool IsFull => Breath >= maxBreath - 0.001f;

        public override float Normalized => maxBreath > 0f ? Breath / maxBreath : 1f;
        public override string HudLabel => "AIRE";
        public override Color HudColor => Normalized < 0.3f ? (Color)new Color32(0xEE, 0x6C, 0x2F, 0xFF) : (Color)new Color32(0x4B, 0x89, 0xA6, 0xFF);
        public override bool ShowInHud => !IsFull || Character.IsHeadUnderwater;

        protected override void Awake()
        {
            base.Awake();
            Breath = maxBreath;
        }

        void OnEnable() { if (Character != null) Character.Respawned += OnRespawned; }
        void OnDisable() { if (Character != null) Character.Respawned -= OnRespawned; }
        void OnRespawned(PlayerCharacter _) => Refill();

        public override void Refill() => Breath = maxBreath;

        void Update()
        {
            if (DialogueManager.IsCinematicPlaying) return;
            if (Character.IsHeadUnderwater) Breath -= Time.deltaTime;
            else Breath += Time.deltaTime * refillMultiplier;
            Breath = Mathf.Clamp(Breath, 0f, maxBreath);
            if (Breath <= 0f)
            {
                Breath = maxBreath;
                Character.Health.Damage(DamageType.Ahogado);
            }
        }
    }
}
