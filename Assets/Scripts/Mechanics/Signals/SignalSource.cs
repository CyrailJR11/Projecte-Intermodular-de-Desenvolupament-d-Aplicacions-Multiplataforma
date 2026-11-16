using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // ================================================================================================
    // [POO · Abstracción + Herencia] Clase base ABSTRACTA de todo lo que enciende un canal:
    //   SignalSource
    //    ├─ InteractableSource (abstracta: se usa con E)
    //    │    ├─ Lever     → válvula/palanca oxidada: solo Nil (fuerza)
    //    │    └─ Terminal  → terminal de la estación: solo Volt (conexión de datos)
    //    ├─ PressurePlate → placa de presión: se activa con peso (personaje o caja)
    //    └─ Crystal       → cristal: se carga con la linterna de Volt y se queda encendido
    // [POO · Encapsulamiento] IsActive solo lo cambia la propia fuente (SetActive es protected).
    // ================================================================================================
    public abstract class SignalSource : MonoBehaviour
    {
        public char Channel { get; private set; }
        public bool IsActive { get; private set; }

        // Lo llama el LevelBuilder al crear la fuente con la letra de la capa de cables.
        public void Connect(char channel)
        {
            Channel = channel;
            SignalNetwork.AddSource(this);
            ChannelLamp.Attach(transform, channel, new Vector3(0f, 0.7f, 0f));
        }

        protected void SetActive(bool on)
        {
            if (IsActive == on) return;
            IsActive = on;
            OnActiveChanged(on);                 // [Polimorfismo] cada fuente lo enseña a su manera
            SignalNetwork.Refresh(Channel);
        }

        // Aspecto y sonido al encenderse o apagarse.
        protected abstract void OnActiveChanged(bool on);
    }
}
