using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // ================================================================================================
    // [POO · Abstracción + Herencia] Clase base ABSTRACTA de todo lo que obedece a un canal:
    //   SignalReceiver
    //    ├─ Door            → compuerta (sube) o trampilla (se abre hacia un lado)
    //    ├─ Lift            → montacargas (sube a la posición de arriba)
    //    └─ DrainableWater  → agua que se vacía al abrir la válvula de desagüe
    // [POO · Polimorfismo] La red de señales solo llama a OnSignal(): qué pasa lo decide cada subclase.
    // ================================================================================================
    public abstract class SignalReceiver : MonoBehaviour, ISignalReceiver
    {
        public char Channel { get; private set; }

        public void Connect(char channel, Vector3 lampOffset)
        {
            Channel = channel;
            SignalNetwork.AddReceiver(this);
            ChannelLamp.Attach(transform, channel, lampOffset);
        }

        public abstract void OnSignal(bool active);
    }
}
