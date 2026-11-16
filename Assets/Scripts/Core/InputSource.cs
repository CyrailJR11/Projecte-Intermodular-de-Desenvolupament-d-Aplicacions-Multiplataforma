namespace EntreDosMons
{
    // [POO · Abstracción + Polimorfismo] De dónde salen las órdenes de un personaje.
    // En el juego es el teclado/mando (KeyboardInput); en los tests PlayMode es un guion (ScriptedInput),
    // así se puede comprobar que "Nil empuja la caja" sin nadie pulsando teclas.
    public interface IInputSource
    {
        float Horizontal { get; }
        float Vertical { get; }
        bool JumpPressed { get; }
        bool JumpReleased { get; }
        bool JumpButtonPressed { get; }   // solo Espacio/A (para soltarse de cuerdas sin confundirlo con W)
        bool InteractPressed { get; }     // E
        bool AbilityPressed { get; }      // F: habilidad especial (Volt: invertir la gravedad)
        bool SecondaryPressed { get; }    // L: segunda habilidad (Volt: linterna)
    }

    // Teclado y mando de verdad (lee GameInput).
    public sealed class KeyboardInput : IInputSource
    {
        public static readonly KeyboardInput Instance = new KeyboardInput();
        KeyboardInput() { }

        public float Horizontal => GameInput.Horizontal();
        public float Vertical => GameInput.Vertical();
        public bool JumpPressed => GameInput.JumpPressed();
        public bool JumpReleased => GameInput.JumpReleased();
        public bool JumpButtonPressed => GameInput.JumpButtonPressed();
        public bool InteractPressed => GameInput.InteractPressed();
        public bool AbilityPressed => GameInput.FlipGravityPressed();
        public bool SecondaryPressed => GameInput.FlashlightPressed();
    }

    // Entrada programada para los tests: se le dice qué "pulsar" y el personaje obedece.
    // Los botones de un solo toque (saltar, interactuar...) se consumen al leerlos.
    public sealed class ScriptedInput : IInputSource
    {
        public float Horizontal { get; set; }
        public float Vertical { get; set; }
        bool jump, jumpReleased, interact, ability, secondary, lastJump;

        public void PressJump() => jump = true;
        public void ReleaseJump() => jumpReleased = true;
        public void PressInteract() => interact = true;
        public void PressAbility() => ability = true;
        public void PressSecondary() => secondary = true;

        public bool JumpPressed { get { lastJump = Consume(ref jump); return lastJump; } }
        public bool JumpReleased => Consume(ref jumpReleased);
        public bool JumpButtonPressed => lastJump;
        public bool InteractPressed => Consume(ref interact);
        public bool AbilityPressed => Consume(ref ability);
        public bool SecondaryPressed => Consume(ref secondary);

        static bool Consume(ref bool flag)
        {
            bool v = flag;
            flag = false;
            return v;
        }
    }
}
