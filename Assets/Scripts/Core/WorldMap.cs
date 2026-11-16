using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons
{
    // Datos del nivel actual que necesitan varios objetos: dónde empieza la cueva y qué objetos interactivos hay.
    // La rellena el LevelBuilder al construir cada nivel.
    public static class WorldMap
    {
        // Por debajo de esta altura es la cueva (mundo de Volt); por encima, el bosque (mundo de Nil).
        public static float CaveTopY = float.NegativeInfinity;

        public static bool HasTwoWorlds => !float.IsNegativeInfinity(CaveTopY);
        public static World WorldAt(float y) => y < CaveTopY ? World.Cueva : World.Exterior;

        // ---------- Registro de interactuables ----------
        // [POO · Polimorfismo] La lista guarda IInteractable: palancas y terminales son clases distintas,
        // pero el personaje las trata igual.
        static readonly List<IInteractable> interactables = new List<IInteractable>();

        public static void Clear()
        {
            interactables.Clear();
            CaveTopY = float.NegativeInfinity;
        }

        public static void Register(IInteractable i) { if (!interactables.Contains(i)) interactables.Add(i); }
        public static void Unregister(IInteractable i) => interactables.Remove(i);

        public static IInteractable NearestInteractable(Vector2 position, float radius)
        {
            IInteractable best = null;
            float bestDistance = radius;
            foreach (IInteractable i in interactables)
            {
                float d = Vector2.Distance(position, i.Position);
                if (d <= bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }
    }
}
