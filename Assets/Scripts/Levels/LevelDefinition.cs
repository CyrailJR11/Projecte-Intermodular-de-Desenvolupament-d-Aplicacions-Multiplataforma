using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels
{
    // Un nivel: nombre, mapa de texto, capa de cables, qué hace cada canal y qué diálogo va en cada número.
    // [POO · Encapsulamiento] Todos los campos son de solo lectura: un nivel no se puede modificar después de crearlo.
    public class LevelDefinition
    {
        public readonly string Name;
        public readonly string[] Map;
        // Capa de cables: mismas medidas que el mapa. Una letra (a-z) encima de una fuente (palanca, terminal, placa,
        // cristal) y la misma letra encima de un receptor (compuerta, montacargas, agua) los conecta. '.' = sin cable.
        public readonly string[] Wiring;
        public readonly ChannelInfo[] Channels;
        public readonly DialogueSpot[] Dialogues;

        public LevelDefinition(string name, string[] map, string[] wiring, ChannelInfo[] channels, DialogueSpot[] dialogues)
        {
            Name = name;
            Map = map;
            Wiring = wiring ?? new string[0];
            Channels = channels ?? new ChannelInfo[0];
            Dialogues = dialogues ?? new DialogueSpot[0];
        }

        public override string ToString() => Name;

        public int Width => Map.Length > 0 ? Map[0].Length : 0;
        public int Height => Map.Length;

        // Carácter del mapa (fila 0 = arriba). Fuera del mapa devuelve '.'.
        public char At(int x, int row)
        {
            if (row < 0 || row >= Map.Length || x < 0 || x >= Map[row].Length) return LevelLegend.Empty;
            return Map[row][x];
        }

        // Canal (letra) de la capa de cables en esa casilla, o '\0' si no hay cable.
        public char WireAt(int x, int row)
        {
            if (row < 0 || row >= Wiring.Length || x < 0 || x >= Wiring[row].Length) return '\0';
            char c = Wiring[row][x];
            return c >= 'a' && c <= 'z' ? c : '\0';
        }

        public ChannelInfo Channel(char id)
        {
            foreach (ChannelInfo ch in Channels)
                if (ch.Id == id) return ch;
            return new ChannelInfo(id, "(canal sin describir)");
        }

        // La cueva empieza en la primera fila que es casi toda roca 'R' (el techo de la cueva; puede tener algún
        // pozo que la atraviese). Por encima es el mundo de Nil y por debajo el de Volt. -1 si no hay cueva.
        public int CaveTopRow
        {
            get
            {
                for (int row = 0; row < Height; row++)
                {
                    string line = Map[row];
                    int rock = 0;
                    foreach (char c in line) if (c == 'R') rock++;
                    if (line.Length > 0 && rock >= line.Length * 0.8f) return row;
                }
                return -1;
            }
        }
    }

    // Qué hace un canal de la capa de cables. Si RequireAll es true, el canal solo se activa cuando TODAS sus
    // fuentes están activas (puerta Y); si no, basta con una (puerta O).
    public struct ChannelInfo
    {
        public readonly char Id;
        public readonly string Description;
        public readonly bool RequireAll;

        public ChannelInfo(char id, string description, bool requireAll = false)
        {
            Id = id;
            Description = description;
            RequireAll = requireAll;
        }
    }

    // Un número del mapa (1-9) convertido en una zona de diálogo del sistema de diálogos.
    public struct DialogueSpot
    {
        public readonly char Symbol;
        public readonly string Id;
        public readonly DialogueTrigger.Who Who;
        public readonly DialogueTrigger.Activation Activation;
        public readonly Vector2 Size;   // en casillas

        public DialogueSpot(char symbol, string id, DialogueTrigger.Who who,
            DialogueTrigger.Activation activation = DialogueTrigger.Activation.AlEntrar)
        {
            Symbol = symbol;
            Id = id;
            Who = who;
            Activation = activation;
            // Al entrar: franja alta para que no se la salte nadie. Al interactuar: solo junto al objeto.
            Size = activation == DialogueTrigger.Activation.AlEntrar ? new Vector2(1f, 6f) : new Vector2(2f, 2f);
        }
    }
}
