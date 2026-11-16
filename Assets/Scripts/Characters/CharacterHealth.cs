using System;
using System.Collections;
using UnityEngine;
using EntreDosMons.Levels;

namespace EntreDosMons
{
    // Vidas de Nil y de Volt. Cada golpe (pinchos, ahogarse, cortocircuito en el agua o caerse) quita una vida
    // y devuelve al personaje al último punto de control, con un momento de invulnerabilidad (parpadea).
    // Sin vidas, se reinicia el nivel entero y los dos personajes recuperan todas sus vidas.
    // [POO · Interfaz] Implementa IDamageable: los pinchos no saben qué es un personaje, solo que se le puede dañar.
    // [POO · Encapsulamiento] Lives tiene "private set": solo se cambia con Damage() o ResetLives().
    [RequireComponent(typeof(PlayerCharacter))]
    public class CharacterHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] int maxLives = 3;
        [Tooltip("Segundos sin recibir daño después de reaparecer.")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Pausa antes de reiniciar el nivel al quedarse sin vidas (en tiempo real).")]
        [SerializeField] float gameOverDelay = 2f;

        public int MaxLives => maxLives;
        public int Lives { get; private set; }
        public bool IsInvulnerable => invulnerableTimer > 0f;
        public PlayerCharacter Character { get; private set; }

        public event Action<CharacterHealth> Changed;
        public static event Action<CharacterHealth, DamageType> LifeLost;
        public static event Action<CharacterHealth> OutOfLives;

        static bool restarting;
        float invulnerableTimer;
        SpriteRenderer sr;

        void Awake()
        {
            Character = GetComponent<PlayerCharacter>();
            sr = GetComponentInChildren<SpriteRenderer>();
            Lives = maxLives;
        }

        void Update()
        {
            if (invulnerableTimer <= 0f) return;
            invulnerableTimer -= Time.deltaTime;
            if (sr != null) sr.enabled = invulnerableTimer <= 0f || Mathf.FloorToInt(invulnerableTimer * 12f) % 2 == 0;
        }

        public void Damage(DamageType type)
        {
            if (restarting) return;
            if (IsInvulnerable)
            {
                // Caerse del mapa durante la invulnerabilidad no quita vida, pero sí hay que volver.
                if (type == DamageType.Caida) Character.Respawn();
                return;
            }

            Lives = Mathf.Max(0, Lives - 1);
            GameAudio.Play(Lives > 0 ? "dano" : "sin_vidas");
            Changed?.Invoke(this);
            LifeLost?.Invoke(this, type);

            if (Lives <= 0)
            {
                StartCoroutine(RestartLevel());
                return;
            }
            Character.Respawn();
            invulnerableTimer = invulnerableTime;
        }

        public void ResetLives()
        {
            Lives = maxLives;
            invulnerableTimer = 0f;
            if (sr != null) sr.enabled = true;
            Changed?.Invoke(this);
        }

        IEnumerator RestartLevel()
        {
            restarting = true;
            OutOfLives?.Invoke(this);
            float oldScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(gameOverDelay);
            Time.timeScale = oldScale > 0f ? oldScale : 1f;
            restarting = false;

            var builder = FindAnyObjectByType<LevelBuilder>();
            if (builder != null) builder.RestartLevel();
            else
            {
                foreach (CharacterHealth h in FindObjectsByType<CharacterHealth>(FindObjectsSortMode.None)) h.ResetLives();
                Character.Respawn();
            }
        }
    }
}
