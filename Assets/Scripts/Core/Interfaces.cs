using UnityEngine;

namespace EntreDosMons
{
    // ================================================================================================
    // [POO · Abstracción] Interfaces: dicen QUÉ sabe hacer un objeto sin decir CÓMO lo hace.
    // El personaje no conoce las palancas ni los terminales: solo sabe que hay "algo con lo que interactuar"
    // (IInteractable) y lo usa. Cada clase concreta decide qué pasa (polimorfismo).
    // ================================================================================================

    // Algo que se acciona con E estando cerca: palancas (Nil) y terminales (Volt).
    public interface IInteractable
    {
        Vector3 Position { get; }
        // Texto que enseña el HUD ("E · Girar la válvula"), o por qué no se puede.
        string GetPrompt(PlayerCharacter who);
        bool CanBeUsedBy(PlayerCharacter who);
        void Interact(PlayerCharacter who);
    }

    // Algo que recibe la señal de un canal de la capa de cables: compuertas, montacargas, agua...
    public interface ISignalReceiver
    {
        char Channel { get; }
        void OnSignal(bool active);
    }

    // Algo que se puede empujar (las cajas). Devuelve si se ha movido.
    public interface IPushable
    {
        bool TryPush(PlayerCharacter by, float direction);
    }

    // Algo que puede recibir daño (los personajes, a través de CharacterHealth).
    public interface IDamageable
    {
        void Damage(DamageType type);
    }

    // Algo que reacciona a la luz de la linterna de Volt (los cristales).
    public interface ILightReceiver
    {
        void Illuminate(PlayerCharacter source, float deltaTime);
    }

    public enum DamageType { Pinchos, Ahogado, Cortocircuito, Caida }

    // Habilidades de cada personaje. [Flags] permite combinarlas: Nil = Nadar | Trepar | ...
    // Las comprueban los objetos para saber si un personaje puede usarlos (palanca → FuerzaManual, etc.).
    [System.Flags]
    public enum CharacterAbility
    {
        Ninguna = 0,
        Nadar = 1 << 0,            // Nil: flota y bucea (con respiración limitada)
        TreparCuerdas = 1 << 1,    // Nil: cuerdas verticales y péndulos
        TreparEscaleras = 1 << 2,  // los dos
        FuerzaManual = 1 << 3,     // Nil: empujar cajas, mover palancas y válvulas oxidadas
        ConexionDatos = 1 << 4,    // Volt: usar terminales de la estación
        BotasMagneticas = 1 << 5,  // Volt: invertir la gravedad y andar por el techo
        Linterna = 1 << 6,         // Volt: alumbrar y cargar cristales
    }
}
