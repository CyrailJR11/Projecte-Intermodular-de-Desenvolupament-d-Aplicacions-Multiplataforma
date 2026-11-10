using System.Collections.Generic;
using UnityEngine;

namespace EntreDosMons
{
    // Efectos de sonido del juego. Busca el clip por nombre en Assets/Resources/Audio/ (salto, dano, chapuzon, carga,
    // palanca, gravedad, linterna, sin_vidas). Ahora hay versiones 8-bit generadas por código; para cambiarlas
    // basta con poner otro archivo con el mismo nombre (p. ej. los de ElevenLabs en .mp3 o .wav).
    // Si falta un clip, simplemente no suena.
    public static class GameAudio
    {
        public static float volume = 0.7f;

        static AudioSource source;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        public static void Play(string clipName, float volumeScale = 1f)
        {
            if (!Application.isPlaying) return;
            // El mismo sonido no se repite más de una vez cada 0,08 s (por ejemplo, pinchos o cargador).
            if (lastPlayed.TryGetValue(clipName, out float t) && Time.unscaledTime - t < 0.08f) return;
            lastPlayed[clipName] = Time.unscaledTime;

            if (!cache.TryGetValue(clipName, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + clipName);
                cache[clipName] = clip;
            }
            if (clip == null) return;

            if (source == null)
            {
                var go = new GameObject("Efectos de sonido");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, volume * volumeScale);
        }
    }
}
