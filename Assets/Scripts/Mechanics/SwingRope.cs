using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Mechanics
{
    // Cuerda-péndulo ('P' en el mapa = punto de anclaje). Nil se agarra al tocarla en el aire,
    // se balancea con A/D, sube o baja por ella con W/S y se suelta con Saltar (sale impulsado).
    // Usa un DistanceJoint2D entre Nil y el anclaje; la cuerda se dibuja con un LineRenderer.
    public class SwingRope : MonoBehaviour
    {
        [Tooltip("Largo de la cuerda en casillas.")]
        public float length = 4f;
        public float minLength = 1.5f;
        [Tooltip("Distancia a la cuerda para agarrarse.")]
        public float grabRadius = 0.6f;
        [Tooltip("Fuerza con la que A/D balancean a Nil.")]
        public float pushForce = 14f;
        public float climbSpeed = 2f;
        [Tooltip("Impulso extra hacia arriba al soltarse (multiplica el salto de Nil).")]
        public float releaseJump = 0.7f;
        public Color ropeColor = new Color32(0xC8, 0x9A, 0x5A, 0xFF);

        public PlayerController2D Holder { get; private set; }

        LineRenderer line;
        DistanceJoint2D joint;
        float cooldown;
        static Material lineMaterial;

        Vector2 Anchor => transform.position;
        Vector2 IdleEnd => Anchor + new Vector2(Mathf.Sin(Time.time * 1.3f) * 0.15f, -length);

        void Awake()
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.09f;
            line.startColor = line.endColor = ropeColor;
            line.sortingOrder = 9;
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null) lineMaterial = new Material(shader);
            }
            line.sharedMaterial = lineMaterial;
        }

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (Holder == null)
            {
                Vector2 end = IdleEnd;
                line.SetPosition(0, Anchor);
                line.SetPosition(1, end);
                if (cooldown > 0f) return;
                foreach (PlayerController2D p in PlayerController2D.All)
                {
                    if (!p.canClimbRopes || p.IsGrounded || p.IsClimbing || p.IsSwinging || p.IsGravityFlipped) continue;
                    if (DistanceToSegment(p.Center, Anchor, end) < grabRadius) { Grab(p); break; }
                }
                return;
            }

            line.SetPosition(0, Anchor);
            line.SetPosition(1, Holder.Center);
            if (Holder.IsGrounded) { Release(false); return; }
            if (!Holder.CanAct || DialogueManager.IsCinematicPlaying) return;
            if (GameInput.JumpButtonPressed()) { Release(true); return; }
            float v = GameInput.Vertical();
            if (v != 0f) joint.distance = Mathf.Clamp(joint.distance - v * climbSpeed * Time.deltaTime, minLength, length);
        }

        void Grab(PlayerController2D p)
        {
            Holder = p;
            joint = p.gameObject.AddComponent<DistanceJoint2D>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = null;            // sin cuerpo: connectedAnchor está en coordenadas del mundo
            joint.connectedAnchor = Anchor;
            joint.autoConfigureDistance = false;
            joint.distance = Mathf.Clamp(Vector2.Distance(p.Center, Anchor), minLength, length);
            joint.maxDistanceOnly = false;
            p.AttachSwing(this);
        }

        public void Release(bool jump)
        {
            if (Holder == null) return;
            if (joint != null) Destroy(joint);
            Rigidbody2D rb = Holder.Body;
            if (jump && rb != null)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 1.2f, Mathf.Max(rb.linearVelocity.y, 0f) + Holder.jumpSpeed * releaseJump);
            Holder.DetachSwing();
            Holder = null;
            joint = null;
            cooldown = 0.4f;
        }

        void OnDisable() => Release(false);

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * length);
        }
    }
}
