using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons.Mechanics
{
    public enum ClimbKind { Cuerda, Escalera }

    // Cuerda vertical ('T' + '|') o escalera de madera ('e') por la que se puede trepar con W/S.
    // Nil trepa cuerdas y escaleras; Volt solo escaleras (lo decide PlayerController2D).
    public class Climbable : MonoBehaviour
    {
        static readonly List<Climbable> all = new List<Climbable>();

        public ClimbKind kind;
        [Tooltip("Rectángulo en coordenadas del mundo.")]
        public Rect area;

        public float CenterX => area.center.x;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool Contains(Vector2 point) => area.Contains(point);

        public static Climbable Find(Vector2 point, bool ropes, bool ladders)
        {
            foreach (Climbable c in all)
            {
                if (c.kind == ClimbKind.Cuerda && !ropes) continue;
                if (c.kind == ClimbKind.Escalera && !ladders) continue;
                if (c.Contains(point)) return c;
            }
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == ClimbKind.Cuerda ? new Color(0.9f, 0.7f, 0.3f, 0.6f) : new Color(0.6f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
