using UnityEngine;

namespace EntreDosMons
{
    // ================================================================================================
    // [POO · Herencia] Nil es un PlayerCharacter. Hereda todo el movimiento y solo dice lo que le hace distinto.
    //
    // Por qué Nil necesita a Volt (y al revés) — justificado en la historia y en el GDD:
    //   · Nil es un chaval explorando la montaña: tiene FUERZA para empujar cajas y mover válvulas y palancas
    //     oxidadas, NADA (aguantando la respiración) y TREPA cuerdas y péndulos.
    //   · Pero no sabe nada de la estación: los terminales solo responden a la unidad VLT-05, y sin corriente
    //     los montacargas, verjas y trampillas de la estación no se mueven. Eso lo arregla Volt desde abajo.
    //   · Vive en el bosque: si se cae a las cuevas (por un pozo), vuelve a su último punto de control.
    // ================================================================================================
    [RequireComponent(typeof(NilBreath))]
    public sealed class NilCharacter : PlayerCharacter
    {
        public override string DisplayName => "Nil";
        public override World HomeWorld => World.Exterior;
        public override CharacterAbility Abilities =>
            CharacterAbility.Nadar | CharacterAbility.TreparCuerdas | CharacterAbility.TreparEscaleras | CharacterAbility.FuerzaManual;
    }
}
