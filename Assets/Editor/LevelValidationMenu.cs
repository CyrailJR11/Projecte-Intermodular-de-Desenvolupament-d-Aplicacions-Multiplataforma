using UnityEditor;
using UnityEngine;
using EntreDosMons.Levels;
using EntreDosMons.Levels.Validation;

// Menú "Entre Dos Mons > 5. Validar niveles": comprueba con el solucionador automático que cada nivel se puede
// terminar, que cada objeto del mapa hace falta y que los dos personajes se necesitan. El informe sale en la Consola.
// (Lo mismo lo hacen los tests EditMode de Assets/Tests/EditMode.)
public static class LevelValidationMenu
{
    [MenuItem("Entre Dos Mons/5. Validar niveles")]
    public static void ValidateAll()
    {
        bool allOk = true;
        foreach (LevelDefinition level in LevelLibrary.Levels)
        {
            ValidationReport report = LevelValidator.Validate(level);
            allOk &= report.Ok;
            if (report.Ok) Debug.Log(report.ToText());
            else Debug.LogError(report.ToText());
        }
        if (allOk) Debug.Log("Entre Dos Mons: todos los niveles son correctos.");
    }
}
