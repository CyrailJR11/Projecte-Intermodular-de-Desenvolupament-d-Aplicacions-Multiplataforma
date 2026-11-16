using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels.Validation
{
    // Resultado de validar un nivel: errores (el nivel está mal), avisos (conviene mirarlo) e información.
    public sealed class ValidationReport
    {
        public string LevelName;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Info = new List<string>();
        public LevelSolver.Result Result;
        public LevelModel Model;
        public bool Ok => Errors.Count == 0;

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"== {LevelName} ==  {(Ok ? "OK" : "CON ERRORES")}");
            foreach (string e in Errors) sb.AppendLine("  ERROR: " + e);
            foreach (string w in Warnings) sb.AppendLine("  Aviso: " + w);
            foreach (string i in Info) sb.AppendLine("  " + i);
            if (Result != null && Result.Solved)
            {
                sb.AppendLine("  Solución encontrada por el simulador:");
                int n = 1;
                foreach (var step in Result.Solution) sb.AppendLine($"    {n++,2}. {step}");
            }
            return sb.ToString();
        }
    }

    // Validador de niveles: comprueba que cada nivel se puede terminar, que todos los objetos tienen una función
    // (y que sin ellos el nivel NO se podría terminar), que Nil y Volt se necesitan el uno al otro, y que la
    // decoración está en un sitio coherente. Lo usan los tests EditMode y el menú Entre Dos Mons > Validar niveles.
    public static class LevelValidator
    {
        const string Known = ".NVCE^~w|TPe-XcpGokDH:=#BLafRmMbvhtgr";

        public static List<ValidationReport> ValidateAll()
        {
            var list = new List<ValidationReport>();
            foreach (LevelDefinition level in LevelLibrary.Levels) list.Add(Validate(level));
            return list;
        }

        public static ValidationReport Validate(LevelDefinition level, bool checkNecessity = true)
        {
            var report = new ValidationReport { LevelName = level.Name };
            var m = new LevelModel(level);
            report.Model = m;
            report.Errors.AddRange(m.Errors);
            if (report.Errors.Count > 0) return report;

            CheckCharacters(m, report);
            CheckLayout(m, report);
            CheckWiring(m, report);
            CheckDecoration(m, report);
            CheckDialogues(m, report);
            if (report.Errors.Count > 0) return report;

            var result = new LevelSolver(m).Run();
            report.Result = result;
            report.Info.Add($"Estados del puzle explorados: {result.States}{(result.Truncated ? " (búsqueda cortada)" : "")}.");
            if (!result.Solved)
            {
                report.Errors.Add("El simulador NO encuentra ninguna forma de que Nil y Volt lleguen a la salida.");
                return report;
            }

            CheckUsage(m, result, report);
            if (checkNecessity) CheckNecessity(m, level, report);
            CheckCooperation(m, result, report);
            CheckReachables(m, result, report);

            if (result.SoftlockStates > 0)
                report.Warnings.Add($"Hay {result.SoftlockStates} estados sin salida (habría que reiniciar con R). Ejemplo: {result.SoftlockExample}");
            return report;
        }

        // ---------- Comprobaciones estáticas ----------

        static void CheckCharacters(LevelModel m, ValidationReport r)
        {
            int nils = 0, volts = 0;
            for (int i = 0; i < m.Grid.Length; i++)
            {
                char c = m.Grid[i];
                if (c == LevelLegend.NilSpawn) nils++;
                if (c == LevelLegend.VoltSpawn) volts++;
                if (Known.IndexOf(c) < 0 && !LevelLegend.IsDialogue(c))
                    r.Errors.Add($"Carácter desconocido '{c}' en ({m.XOf(i)},{m.RowOf(i)}).");
            }
            if (nils != 1) r.Errors.Add($"Tiene que haber exactamente una 'N' (hay {nils}).");
            if (volts != 1) r.Errors.Add($"Tiene que haber exactamente una 'V' (hay {volts}).");
            if (m.CaveTop < 0) r.Errors.Add("El nivel no tiene cueva (una fila entera de 'R' marca el techo de la cueva).");
            if (m.NilSpawn >= 0 && m.WorldOf(m.RowOf(m.NilSpawn)) != World.Exterior) r.Errors.Add("Nil tiene que empezar en el exterior.");
            if (m.VoltSpawn >= 0 && m.WorldOf(m.RowOf(m.VoltSpawn)) != World.Cueva) r.Errors.Add("Volt tiene que empezar en la cueva.");
            if (!m.Exits.Any(e => m.WorldOf(m.RowOf(e)) == World.Exterior)) r.Errors.Add("Falta la salida de Nil ('E' en el exterior).");
            if (!m.Exits.Any(e => m.WorldOf(m.RowOf(e)) == World.Cueva)) r.Errors.Add("Falta la salida de Volt ('E' en la cueva).");
        }

        static void CheckLayout(LevelModel m, ValidationReport r)
        {
            foreach (int c in m.Checkpoints)
                if (m.WorldOf(m.RowOf(c)) != World.Exterior)
                    r.Errors.Add($"Hoguera 'C' en la cueva ({m.XOf(c)},{m.RowOf(c)}): el punto de control de Volt es el cargador 'c'.");
            foreach (int c in m.Chargers)
                if (m.WorldOf(m.RowOf(c)) != World.Cueva)
                    r.Errors.Add($"Cargador 'c' en el exterior ({m.XOf(c)},{m.RowOf(c)}): Volt no puede llegar.");
            // Cajas separadas: dos cajas juntas pesan el doble y no hay forma clara de empujarlas.
            foreach (int a in m.Boxes)
                foreach (int b in m.Boxes)
                {
                    if (a >= b) continue;
                    int dx = System.Math.Abs(m.XOf(a) - m.XOf(b)), dr = System.Math.Abs(m.RowOf(a) - m.RowOf(b));
                    if (dx + dr == 1) r.Errors.Add($"Dos cajas pegadas en ({m.XOf(a)},{m.RowOf(a)}) y ({m.XOf(b)},{m.RowOf(b)}): sepáralas.");
                }
            foreach (var src in m.Sources)
            {
                World w = m.WorldOf(src.Row);
                if (src.Kind == LevelLegend.Lever && w != World.Exterior) r.Errors.Add($"Palanca en la cueva ({src.X},{src.Row}): solo Nil puede moverlas.");
                if ((src.Kind == LevelLegend.Terminal || src.Kind == LevelLegend.Crystal) && w != World.Cueva)
                    r.Errors.Add($"{LevelModel.LabelOf(src.Kind)} en el exterior ({src.X},{src.Row}): solo Volt puede usarlo.");
            }
        }

        static void CheckWiring(LevelModel m, ValidationReport r)
        {
            var used = new HashSet<char>();
            foreach (var s in m.Sources)
            {
                if (s.Channel == '\0') r.Errors.Add($"La {s.Label} no tiene cable: no serviría para nada.");
                else used.Add(s.Channel);
            }
            foreach (var rc in m.Receivers)
                if (rc.Channel == '\0') r.Errors.Add($"La {rc.Label} no tiene cable: no se podría abrir/mover nunca.");
            foreach (char ch in used)
            {
                if (!m.Receivers.Any(x => x.Channel == ch)) r.Errors.Add($"El canal '{ch}' tiene fuentes pero ningún receptor.");
                if (!m.Level.Channels.Any(x => x.Id == ch)) r.Errors.Add($"El canal '{ch}' no está descrito en Channels del nivel.");
            }
            foreach (var rc in m.Receivers)
                if (rc.Channel != '\0' && !used.Contains(rc.Channel)) r.Errors.Add($"La {rc.Label} está en el canal '{rc.Channel}', que no tiene fuentes.");
            foreach (var ch in m.Level.Channels)
                if (!used.Contains(ch.Id)) r.Errors.Add($"El canal '{ch.Id}' está descrito pero no se usa en el mapa.");
        }

        // Cada decoración tiene que tener sentido donde está.
        static void CheckDecoration(LevelModel m, ValidationReport r)
        {
            for (int row = 0; row < m.H; row++)
                for (int x = 0; x < m.W; x++)
                {
                    char c = m.At(x, row);
                    if (!LevelLegend.IsDecoration(c)) continue;
                    bool below = LevelLegend.IsSolidTerrain(m.At(x, row + 1));
                    bool above = LevelLegend.IsSolidTerrain(m.At(x, row - 1));
                    World w = m.WorldOf(row);
                    string where = $"({x},{row})";
                    switch (c)
                    {
                        case 'a': case 'f': case 'L':
                            if (w != World.Exterior) r.Errors.Add($"Decoración de bosque '{c}' dentro de la cueva {where}.");
                            if (!below) r.Errors.Add($"Decoración '{c}' flotando en {where}: necesita suelo debajo.");
                            break;
                        case 'b':
                            if (w != World.Cueva) r.Errors.Add($"Setas brillantes fuera de la cueva {where}.");
                            if (!below) r.Errors.Add($"Setas flotando en {where}.");
                            break;
                        case 'v':
                            if (w != World.Cueva) r.Errors.Add($"Estalactita fuera de la cueva {where}.");
                            if (!above) r.Errors.Add($"Estalactita en {where} sin techo encima.");
                            break;
                        default:   // engranajes y tuberías de la estación
                            if (w != World.Cueva) r.Errors.Add($"Pieza de la estación '{c}' fuera de la cueva {where}.");
                            break;
                    }
                }
        }

        static void CheckDialogues(LevelModel m, ValidationReport r)
        {
            var symbols = new HashSet<char>();
            foreach (char c in m.Grid) if (LevelLegend.IsDialogue(c)) symbols.Add(c);
            foreach (var d in m.Level.Dialogues)
            {
                if (!symbols.Contains(d.Symbol)) r.Errors.Add($"El diálogo {d.Id} usa el número '{d.Symbol}', que no está en el mapa.");
                symbols.Remove(d.Symbol);
            }
            foreach (char c in symbols) r.Errors.Add($"El número '{c}' del mapa no tiene diálogo asignado.");
        }

        // ---------- Comprobaciones con el simulador ----------

        static void CheckUsage(LevelModel m, LevelSolver.Result res, ValidationReport r)
        {
            foreach (var s in m.Sources)
                if (!res.UsedSources.Contains(s.Id)) r.Errors.Add($"Nadie puede usar la {s.Label} en ninguna partida.");
            foreach (var rc in m.Receivers)
                if (!res.MovedReceivers.Contains(rc.Id)) r.Errors.Add($"La {rc.Label} no cambia nunca.");
            foreach (int b in m.Boxes)
                if (!res.PushedBoxes.Contains(b)) r.Errors.Add($"La caja de ({m.XOf(b)},{m.RowOf(b)}) no se puede mover nunca.");
        }

        // Quita cada objeto (o lo deja siempre apagado) y vuelve a simular: si el nivel aún se puede terminar,
        // ese objeto sobra.
        static void CheckNecessity(LevelModel m, LevelDefinition level, ValidationReport r)
        {
            var needed = new List<string>();
            foreach (var s in m.Sources)
            {
                var o = new LevelSolver.Options { AnalyseSoftlocks = false };
                o.DisabledSources.Add(s.Id);
                if (new LevelSolver(m, o).Run().Solved) r.Errors.Add($"La {s.Label} sobra: sin ella el nivel también se termina.");
                else needed.Add(s.Label);
            }
            foreach (var rc in m.Receivers)
            {
                var o = new LevelSolver.Options { AnalyseSoftlocks = false };
                o.DisabledReceivers.Add(rc.Id);
                if (new LevelSolver(m, o).Run().Solved) r.Errors.Add($"La {rc.Label} sobra: aunque no se abra/mueva nunca, el nivel se termina.");
                else needed.Add(rc.Label);
            }
            foreach (int b in m.Boxes)
            {
                var o = new LevelSolver.Options { AnalyseSoftlocks = false };
                o.RemovedBoxes.Add(b);
                if (new LevelSolver(m, o).Run().Solved) r.Errors.Add($"La caja de ({m.XOf(b)},{m.RowOf(b)}) sobra: sin ella el nivel también se termina.");
                else needed.Add($"caja ({m.XOf(b)},{m.RowOf(b)})");
            }
            r.Info.Add("Objetos imprescindibles (sin cada uno el nivel no se puede terminar): " + string.Join(", ", needed) + ".");
        }

        // Nil y Volt tienen que depender el uno del otro: al menos un canal que va del exterior a la cueva
        // y otro de la cueva al exterior.
        static void CheckCooperation(LevelModel m, LevelSolver.Result res, ValidationReport r)
        {
            int nilToVolt = 0, voltToNil = 0;
            foreach (var ch in m.Level.Channels)
            {
                bool fromOut = m.Sources.Any(s => s.Channel == ch.Id && m.WorldOf(s.Row) == World.Exterior);
                bool fromIn = m.Sources.Any(s => s.Channel == ch.Id && m.WorldOf(s.Row) == World.Cueva);
                bool toOut = m.Receivers.Any(x => x.Channel == ch.Id && m.WorldOf(m.RowOf(x.Cells[0])) == World.Exterior);
                bool toIn = m.Receivers.Any(x => x.Channel == ch.Id && m.WorldOf(m.RowOf(x.Cells[0])) == World.Cueva);
                if (fromOut && toIn) { nilToVolt++; r.Info.Add($"Nil → Volt: canal '{ch.Id}': {ch.Description}"); }
                if (fromIn && toOut) { voltToNil++; r.Info.Add($"Volt → Nil: canal '{ch.Id}': {ch.Description}"); }
            }
            if (nilToVolt == 0) r.Errors.Add("Volt no necesita a Nil en este nivel (ningún mecanismo del exterior cambia la cueva).");
            if (voltToNil == 0) r.Errors.Add("Nil no necesita a Volt en este nivel (ningún mecanismo de la cueva cambia el exterior).");

            int switches = 0;
            for (int i = 1; i < res.Solution.Count; i++) if (res.Solution[i].Actor != res.Solution[i - 1].Actor) switches++;
            r.Info.Add($"Pasos de la solución: {res.Solution.Count}. Cambios de personaje necesarios: {switches}.");
        }

        static void CheckReachables(LevelModel m, LevelSolver.Result res, ValidationReport r)
        {
            foreach (int c in m.Chargers)
                if (!res.VoltCells.Contains(c)) r.Errors.Add($"Volt no puede llegar al cargador de ({m.XOf(c)},{m.RowOf(c)}).");
            foreach (int c in m.Checkpoints)
                if (!res.NilCells.Contains(c)) r.Errors.Add($"Nil no puede llegar a la hoguera de ({m.XOf(c)},{m.RowOf(c)}).");
            foreach (int s in m.Swings)
                if (!res.SwingsUsed.Contains(s)) r.Errors.Add($"Nil no puede agarrarse nunca a la cuerda-péndulo de ({m.XOf(s)},{m.RowOf(s)}).");
            for (int i = 0; i < m.Grid.Length; i++)
            {
                char c = m.Grid[i];
                if (c == LevelLegend.Ladder && !res.NilCells.Contains(i) && !res.VoltCells.Contains(i))
                    r.Errors.Add($"Nadie llega a la escalera de ({m.XOf(i)},{m.RowOf(i)}).");
                if ((c == LevelLegend.Rope || c == LevelLegend.RopeAnchor) && !res.NilCells.Contains(i))
                    r.Errors.Add($"Nil no llega a la cuerda de ({m.XOf(i)},{m.RowOf(i)}).");
                if (LevelLegend.IsDialogue(c))
                {
                    var spot = m.Level.Dialogues.FirstOrDefault(d => d.Symbol == c);
                    bool ok = spot.Who == DialogueTrigger.Who.Nil ? res.NilCells.Contains(i)
                            : spot.Who == DialogueTrigger.Who.Volt ? res.VoltCells.Contains(i)
                            : res.NilCells.Contains(i) || res.VoltCells.Contains(i);
                    // Las zonas "al entrar" son una franja de 6 casillas de alto: basta con pasar por la columna.
                    if (!ok && spot.Activation == DialogueTrigger.Activation.AlEntrar)
                    {
                        var cells = spot.Who == DialogueTrigger.Who.Volt ? res.VoltCells : res.NilCells;
                        int x = m.XOf(i), row = m.RowOf(i);
                        for (int k = 0; k < 6 && !ok; k++) ok = cells.Contains(m.Index(x, System.Math.Max(0, row - k)));
                    }
                    if (!ok) r.Errors.Add($"El diálogo '{c}' ({spot.Id}) está donde {spot.Who} no puede llegar.");
                }
            }
        }
    }
}
