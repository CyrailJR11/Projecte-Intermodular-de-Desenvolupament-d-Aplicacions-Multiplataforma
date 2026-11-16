using UnityEngine;

namespace EntreDosMons.Mechanics
{
    // Lucecita del color del canal encima de cada fuente y de cada receptor: así el jugador ve qué palanca mueve
    // qué compuerta (aunque estén en mundos distintos). Brilla cuando el canal está activo.
    public class ChannelLamp : MonoBehaviour
    {
        // La pone el LevelBuilder (tile "senyal_canal").
        public static Sprite LampSprite;

        char channel;
        SpriteRenderer sr;

        public static ChannelLamp Attach(Transform parent, char channel, Vector3 localOffset)
        {
            if (channel == '\0' || LampSprite == null) return null;
            var go = new GameObject("Señal " + channel);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            var lamp = go.AddComponent<ChannelLamp>();
            lamp.channel = channel;
            lamp.sr = go.AddComponent<SpriteRenderer>();
            lamp.sr.sprite = LampSprite;
            lamp.sr.sortingOrder = 20;
            lamp.Set(SignalNetwork.IsActive(channel));
            return lamp;
        }

        void OnEnable() => SignalNetwork.ChannelChanged += OnChanged;
        void OnDisable() => SignalNetwork.ChannelChanged -= OnChanged;

        void OnChanged(char ch, bool on) { if (ch == channel) Set(on); }

        void Set(bool on)
        {
            if (sr == null) return;
            Color c = SignalNetwork.ColorOf(channel);
            sr.color = on ? c : new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f);
        }
    }
}
