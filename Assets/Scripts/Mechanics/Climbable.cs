using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons.Mechanics
{
    public enum ClimbKind { Cuerda, Escalera }

    // Cuerda vertical ('T' + '|') o escalera de madera ('e') por la que se puede trepar con W/S.
    // Quién puede trepar lo decide cada personaje (PlayerCharacter.CanClimb): Nil cuerdas y escaleras, Volt solo escaleras.
    public class Climbable : MonoBehaviour
    {
        static readonly List<Climbable> all = new List<Climbable>();

        [SerializeField] ClimbKind kind;
        [Tooltip("Rectángulo en coordenadas del mundo.")]
        [SerializeField] Rect area;

        public ClimbKind Kind => kind;
        public Rect Area => area;
        public float CenterX => area.center.x;

        public void Init(ClimbKind climbKind, Rect worldArea)
        {
            kind = climbKind;
            area = worldArea;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool Contains(Vector2 point) => area.Contains(point);

        public static Climbable Find(Vector2 point, PlayerCharacter who)
        {
            foreach (Climbable c in all)
                if (c.Contains(point) && who.CanClimb(c.kind)) return c;
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == ClimbKind.Cuerda ? new Color(0.9f, 0.7f, 0.3f, 0.6f) : new Color(0.6f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
