using UnityEngine;

namespace EntreDosMons
{
    // Animación por fotogramas sin Animator: quieto, andando y en el aire.
    // Ponlo en el mismo objeto que el SpriteRenderer y arrastra los sprites a cada lista.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite[] airFrames;
        public float walkFramesPerSecond = 8f;
        public float idleFramesPerSecond = 2f;

        SpriteRenderer sr;
        Rigidbody2D rb;
        PlayerController2D player;
        Sprite[] current;
        float timer;
        int frame;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            rb = GetComponentInParent<Rigidbody2D>();
            player = GetComponentInParent<PlayerController2D>();
        }

        void Update()
        {
            Sprite[] anim = idleFrames;
            float fps = idleFramesPerSecond;
            if (player != null && !player.IsGrounded && Has(airFrames))
                anim = airFrames;
            else if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f && Has(walkFrames))
            {
                anim = walkFrames;
                fps = walkFramesPerSecond;
            }
            if (!Has(anim)) return;

            if (anim != current)
            {
                current = anim;
                frame = 0;
                timer = 0f;
            }
            timer += Time.deltaTime;
            if (fps > 0f && timer >= 1f / fps)
            {
                timer -= 1f / fps;
                frame = (frame + 1) % anim.Length;
            }
            sr.sprite = anim[frame];
        }

        static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
    }
}
