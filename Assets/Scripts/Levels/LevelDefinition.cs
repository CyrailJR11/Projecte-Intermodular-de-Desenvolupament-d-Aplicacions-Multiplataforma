using UnityEngine;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels
{
    // Un nivel: nombre, mapa de texto y qué diálogo va en cada número del mapa.
    public class LevelDefinition
    {
        public readonly string Name;
        public readonly string[] Map;
        public readonly DialogueSpot[] Dialogues;

        public LevelDefinition(string name, string[] map, DialogueSpot[] dialogues)
        {
            Name = name;
            Map = map;
            Dialogues = dialogues;
        }

        public int Width => Map.Length > 0 ? Map[0].Length : 0;
        public int Height => Map.Length;
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
