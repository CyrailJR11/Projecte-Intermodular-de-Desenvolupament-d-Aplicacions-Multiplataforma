using System;
using System.Collections.Generic;
using UnityEngine;
using EntreDosMons.Levels;

namespace EntreDosMons.Mechanics
{
    // "Cableado" del nivel. Cada letra de la capa de cables es un canal: las FUENTES de ese canal (palanca, terminal,
    // placa, cristal) lo encienden y sus RECEPTORES (compuerta, montacargas, agua) reaccionan.
    // Un canal normal se enciende si CUALQUIER fuente está activa (O); con RequireAll, solo si lo están TODAS (Y).
    // Así un mecanismo puede necesitar a los dos personajes a la vez.
    // [POO · Abstracción] Las fuentes no conocen a los receptores ni al revés: solo conocen su canal.
    public static class SignalNetwork
    {
        static readonly Dictionary<char, ChannelInfo> infos = new Dictionary<char, ChannelInfo>();
        static readonly Dictionary<char, List<SignalSource>> sources = new Dictionary<char, List<SignalSource>>();
        static readonly Dictionary<char, List<ISignalReceiver>> receivers = new Dictionary<char, List<ISignalReceiver>>();
        static readonly Dictionary<char, bool> state = new Dictionary<char, bool>();

        // Para las lucecitas de los canales y el HUD.
        public static event Action<char, bool> ChannelChanged;

        // Nivel nuevo: se olvidan todas las conexiones.
        public static void Reset(IEnumerable<ChannelInfo> channels)
        {
            infos.Clear();
            sources.Clear();
            receivers.Clear();
            state.Clear();
            if (channels == null) return;
            foreach (ChannelInfo ch in channels) infos[ch.Id] = ch;
        }

        public static void AddSource(SignalSource source)
        {
            if (source.Channel == '\0') return;
            ListOf(sources, source.Channel).Add(source);
        }

        public static void AddReceiver(ISignalReceiver receiver)
        {
            if (receiver.Channel == '\0') return;
            ListOf(receivers, receiver.Channel).Add(receiver);
        }

        public static bool IsActive(char channel) => state.TryGetValue(channel, out bool on) && on;

        public static string Describe(char channel) =>
            infos.TryGetValue(channel, out ChannelInfo info) ? info.Description : "";

        // Una fuente ha cambiado: se recalcula el canal y, si cambia, se avisa a sus receptores.
        public static void Refresh(char channel)
        {
            if (channel == '\0') return;
            bool requireAll = infos.TryGetValue(channel, out ChannelInfo info) && info.RequireAll;
            bool any = false, all = true;
            if (sources.TryGetValue(channel, out List<SignalSource> list))
                foreach (SignalSource s in list)
                {
                    if (s == null) continue;
                    any |= s.IsActive;
                    all &= s.IsActive;
                }
            bool on = requireAll ? any && all : any;
            if (IsActive(channel) == on) return;
            state[channel] = on;
            if (receivers.TryGetValue(channel, out List<ISignalReceiver> targets))
                foreach (ISignalReceiver r in targets)
                    r?.OnSignal(on);   // [Polimorfismo] cada receptor reacciona a su manera
            ChannelChanged?.Invoke(channel, on);
        }

        // Color de cada canal (mismos colores en la lucecita de la fuente y en la del receptor).
        static readonly Color32[] palette =
        {
            new Color32(0xF5, 0xD0, 0x4C, 0xFF), // amarillo
            new Color32(0x4B, 0xC1, 0xE6, 0xFF), // azul
            new Color32(0xEE, 0x6C, 0x2F, 0xFF), // naranja
            new Color32(0x8E, 0xD8, 0x5A, 0xFF), // verde
            new Color32(0xD0, 0x6C, 0xE0, 0xFF), // lila
            new Color32(0xF2, 0xF2, 0xF2, 0xFF), // blanco
        };

        public static Color ColorOf(char channel) =>
            channel >= 'a' && channel <= 'z' ? (Color)palette[(channel - 'a') % palette.Length] : Color.gray;

        static List<T> ListOf<T>(Dictionary<char, List<T>> dict, char key)
        {
            if (!dict.TryGetValue(key, out List<T> list)) dict[key] = list = new List<T>();
            return list;
        }
    }
}
