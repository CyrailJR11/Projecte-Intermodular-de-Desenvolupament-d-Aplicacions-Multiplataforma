using UnityEngine;
using EntreDosMons.UI;

namespace EntreDosMons
{
    // ================================================================================================
    // [POO · Herencia + Polimorfismo] Volt es un PlayerCharacter con habilidades propias.
    // Sobrescribe (override) los "huecos" de la clase base:
    //   · UpdateAbilities → F invierte la gravedad (botas magnéticas) gastando batería.
    //   · OnEnterWater    → Volt no flota: chispazo y aviso (la batería se gasta en VoltBattery).
    //   · OnPushBlocked   → no tiene fuerza para las cajas: lo dice por el HUD.
    //
    // Por qué Volt necesita a Nil (y al revés):
    //   · Volt es una unidad de mantenimiento pequeña: se conecta a los TERMINALES (devuelve la corriente a
    //     montacargas, verjas y trampillas), lleva LINTERNA (carga los cristales) y anda por el TECHO.
    //   · Pero pesa poco y sus motores no mueven cajas ni válvulas, y en el agua se hunde y se cortocircuita.
    //     El río que inunda sus cuevas, las placas que abren sus compuertas... eso lo resuelve Nil desde arriba.
    // ================================================================================================
    [RequireComponent(typeof(VoltBattery))]
    [RequireComponent(typeof(VoltFlashlight))]
    public sealed class VoltCharacter : PlayerCharacter
    {
        [Tooltip("Batería que gasta cada cambio de gravedad.")]
        [SerializeField] float flipBatteryCost = 5f;

        public override string DisplayName => "Volt";
        public override World HomeWorld => World.Cueva;
        public override CharacterAbility Abilities =>
            CharacterAbility.ConexionDatos | CharacterAbility.BotasMagneticas | CharacterAbility.Linterna | CharacterAbility.TreparEscaleras;

        VoltBattery battery;
        VoltFlashlight flashlight;
        float pushWarningCooldown;

        public VoltBattery Battery => battery;
        public VoltFlashlight Flashlight => flashlight;

        protected override void Awake()
        {
            base.Awake();   // primero lo de la clase base
            battery = GetComponent<VoltBattery>();
            flashlight = GetComponent<VoltFlashlight>();
        }

        protected override void UpdateAbilities(bool canAct)
        {
            pushWarningCooldown -= Time.deltaTime;
            if (!canAct) return;
            if (SecondaryPressedThisFrame && flashlight != null) flashlight.Toggle();   // L: linterna
            if (AbilityPressedThisFrame && IsGrounded && !IsClimbing)                    // F: botas magnéticas
            {
                if (battery == null || battery.TryConsume(flipBatteryCost))
                {
                    FlipGravity();
                    GameAudio.Play("gravedad");
                }
                else GameHUD.Say("Volt: Batería insuficiente para las botas magnéticas.", 1.5f);
            }
        }

        protected override void OnEnterWater()
        {
            base.OnEnterWater();
            GameHUD.Say("Volt: ¡Agua! Esta unidad no flota.", 1.5f);
        }

        protected override void OnPushBlocked(IPushable pushable)
        {
            if (pushWarningCooldown > 0f) return;
            pushWarningCooldown = 3f;
            GameHUD.Say("Volt: Peso de la caja > fuerza de esta unidad. Necesito a Nil.", 2f);
        }
    }
}
