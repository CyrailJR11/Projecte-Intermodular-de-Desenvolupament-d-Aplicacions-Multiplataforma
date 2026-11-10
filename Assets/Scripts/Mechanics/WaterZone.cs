using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Zona de agua (la crea el LevelBuilder con los '~' y 'w' del mapa). No usa físicas: los personajes
    // preguntan si su centro o su cabeza están dentro de algún rectángulo de agua.
    public class WaterZone : MonoBehaviour
    {
        static readonly List<WaterZone> zones = new List<WaterZone>();

        [Tooltip("Rectángulo en coordenadas del mundo.")]
        public Rect area;

        void OnEnable() => zones.Add(this);
        void OnDisable() => zones.Remove(this);

        public static bool Contains(Vector2 point)
        {
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].area.Contains(point)) return true;
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.55f, 0.9f, 0.35f);
            Gizmos.DrawCube(area.center, area.size);
        }
    }
}
