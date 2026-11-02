using System;

namespace EntreDosMons.Dialogue
{
    // Estructura de dialogos_es.json. Compatible con JsonUtility (sin diccionarios).

    [Serializable]
    public class DialogueFile
    {
        public string language;
        public SpeakerInfo[] speakers;
        public DialogueEntry[] dialogues;
    }

    [Serializable]
    public class SpeakerInfo
    {
        public string id;      // NIL, VOLT, ESTACION, CUADERNO, AVI_ROC, PADRE
        public string name;    // nombre que se ve en pantalla
        public string color;   // color del nombre, en hex (#RRGGBB)
    }

    [Serializable]
    public class TriggerInfo
    {
        public string type;      // zona | interactuar | evento (solo informativo)
        public string character; // Nil | Volt | Cualquiera (solo informativo)
        public string where;     // dónde colocarlo en el nivel
    }

    [Serializable]
    public class DialogueEntry
    {
        public string id;
        public string chapter;
        public TriggerInfo trigger;
        public string mode;          // "cinematica" bloquea el control; "ambiente" no
        public bool once;            // solo se reproduce una vez por partida
        public bool randomLine;      // bark: se dice una sola línea al azar
        public string[] requires;    // flags necesarias para que suene
        public string[] sets;        // flags que activa al terminar
        public string note;
        public DialogueLine[] lines;

        public bool IsCinematic => mode == "cinematica";
    }

    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
        public string channel;       // radio | local | terminal | cuaderno
        public string emotion;       // neutral | feliz | triste | miedo | sorpresa | decidido
        public float interference;   // 0..1, ruido de la radio
        public float duration;       // segundos en modo ambiente (0 = automático)
        public string sfx;           // nombre de un efecto de sonido opcional
    }

    // Lo que hay que guardar en la partida (añadidlo a vuestro JSON de guardado).
    [Serializable]
    public class DialogueSaveData
    {
        public string[] played;
        public string[] flags;
    }
}
