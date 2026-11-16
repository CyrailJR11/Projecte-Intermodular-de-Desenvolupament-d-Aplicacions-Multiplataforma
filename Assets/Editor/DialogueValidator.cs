using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EntreDosMons.Dialogue.EditorTools
{
    // Menú Entre Dos Mons > Validar diálogos de la escena:
    // comprueba que cada DialogueTrigger / DialogueEvent apunta a un id que existe en el JSON
    // y lista los diálogos que todavía no están colocados en la escena abierta.
    public static class DialogueValidator
    {
        [MenuItem("Entre Dos Mons/Validar diálogos de la escena")]
        static void Validate()
        {
            var asset = Resources.Load<TextAsset>("Dialogues/dialogos_es");
            if (asset == null) { Debug.LogError("[Diálogos] No encuentro Resources/Dialogues/dialogos_es.json"); return; }
            var file = JsonUtility.FromJson<DialogueFile>(asset.text);

            var all = new HashSet<string>();
            foreach (var d in file.dialogues) all.Add(d.id);

            var used = new HashSet<string>();
            int errors = 0;

            foreach (var t in Object.FindObjectsByType<DialogueTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                errors += Check(t.DialogueId, t, all, used);
            foreach (var e in Object.FindObjectsByType<DialogueEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(e.DialogueId)) errors += Check(e.DialogueId, e, all, used);

            var missing = new List<string>();
            foreach (var d in file.dialogues)
                if (!used.Contains(d.id) && d.chapter != "barks") missing.Add($"{d.id}  ({d.trigger.where})");

            Debug.Log($"[Diálogos] {used.Count} diálogos colocados, {errors} errores. " +
                      $"Sin colocar en esta escena ({missing.Count}):\n" + string.Join("\n", missing));
        }

        static int Check(string id, Object ctx, HashSet<string> all, HashSet<string> used)
        {
            if (string.IsNullOrEmpty(id)) { Debug.LogError("[Diálogos] Trigger sin id", ctx); return 1; }
            if (!all.Contains(id)) { Debug.LogError($"[Diálogos] El id '{id}' no existe en el JSON", ctx); return 1; }
            used.Add(id);
            return 0;
        }
    }
}
