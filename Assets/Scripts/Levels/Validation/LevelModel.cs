using System.Collections.Generic;

namespace EntreDosMons.Levels.Validation
{
    // Lectura de un LevelDefinition en estructuras fáciles de recorrer (sin nada de Unity: se puede usar en los tests,
    // en el menú del editor y fuera de Unity). La usa LevelSolver.
    // Coordenadas: x = columna, row = fila del texto (0 = arriba). "Abajo" es row + 1.
    public sealed class LevelModel
    {
        public sealed class Source
        {
            public int Id;            // índice en Sources
            public char Kind;         // 'p' palanca, 'G' terminal, 'o' placa, 'k' cristal
            public int X, Row;
            public char Channel;      // '\0' = sin cable
            public string Label => LabelOf(Kind) + " (" + X + "," + Row + ")";
        }

        public sealed class Receiver
        {
            public int Id;
            public char Kind;         // 'D' compuerta, 'H' montacargas, '~' agua
            public readonly List<int> Cells = new List<int>();   // casillas que ocupa (cerrada / abajo / con agua)
            public char Channel;
            // Montacargas: columnas, fila de abajo y fila de arriba.
            public int LiftLeft, LiftRight, LiftBottom, LiftTop;
            public bool Horizontal;   // compuerta horizontal = trampilla
            public string Label;
        }

        public readonly LevelDefinition Level;
        public readonly int W, H;
        public readonly int CaveTop;          // primera fila de la cueva (-1 si no hay)
        public readonly char[] Grid;          // mapa tal cual
        public readonly List<Source> Sources = new List<Source>();
        public readonly List<Receiver> Receivers = new List<Receiver>();
        public readonly List<int> Boxes = new List<int>();
        public readonly List<int> Exits = new List<int>();
        public readonly List<int> Chargers = new List<int>();
        public readonly List<int> Checkpoints = new List<int>();
        public readonly List<int> Swings = new List<int>();
        public readonly List<string> Errors = new List<string>();
        public int NilSpawn = -1, VoltSpawn = -1;

        public LevelModel(LevelDefinition level)
        {
            Level = level;
            H = level.Height;
            W = level.Width;
            CaveTop = level.CaveTopRow;
            Grid = new char[W * H];
            for (int row = 0; row < H; row++)
            {
                string line = level.Map[row];
                if (line.Length != W) Errors.Add($"La fila {row} mide {line.Length} y debería medir {W}.");
                for (int x = 0; x < W; x++) Grid[Index(x, row)] = x < line.Length ? line[x] : '.';
            }
            if (level.Wiring.Length > 0 && level.Wiring.Length != H)
                Errors.Add($"La capa de cables tiene {level.Wiring.Length} filas y el mapa {H}.");
            for (int row = 0; row < level.Wiring.Length; row++)
                if (level.Wiring[row].Length != W) Errors.Add($"La fila {row} de la capa de cables mide {level.Wiring[row].Length} y debería medir {W}.");
            Parse();
        }

        public int Index(int x, int row) => x + row * W;
        public int XOf(int cell) => cell % W;
        public int RowOf(int cell) => cell / W;
        public bool Inside(int x, int row) => x >= 0 && x < W && row >= 0 && row < H;
        public char At(int x, int row) => Inside(x, row) ? Grid[Index(x, row)] : '.';
        public World WorldOf(int row) => CaveTop >= 0 && row >= CaveTop ? World.Cueva : World.Exterior;

        public static string LabelOf(char kind)
        {
            switch (kind)
            {
                case LevelLegend.Lever: return "palanca";
                case LevelLegend.Terminal: return "terminal";
                case LevelLegend.Plate: return "placa de presión";
                case LevelLegend.Crystal: return "cristal";
                case LevelLegend.Door: return "compuerta";
                case LevelLegend.Lift: return "montacargas";
                case LevelLegend.Box: return "caja";
                case LevelLegend.Charger: return "cargador";
                default: return LevelLegend.IsWater(kind) ? "agua" : kind.ToString();
            }
        }

        void Parse()
        {
            var seen = new bool[W * H];
            for (int row = 0; row < H; row++)
            {
                for (int x = 0; x < W; x++)
                {
                    char c = At(x, row);
                    int i = Index(x, row);
                    char wire = Level.WireAt(x, row);
                    switch (c)
                    {
                        case LevelLegend.NilSpawn: NilSpawn = i; break;
                        case LevelLegend.VoltSpawn: VoltSpawn = i; break;
                        case LevelLegend.Exit: Exits.Add(i); break;
                        case LevelLegend.Box: Boxes.Add(i); break;
                        case LevelLegend.Charger: Chargers.Add(i); break;
                        case LevelLegend.Checkpoint: Checkpoints.Add(i); break;
                        case LevelLegend.SwingAnchor: Swings.Add(i); break;
                    }
                    if (LevelLegend.IsSignalSource(c))
                        Sources.Add(new Source { Id = Sources.Count, Kind = c, X = x, Row = row, Channel = wire });
                    else if (c == LevelLegend.Door && !seen[i]) ParseDoor(x, row, seen);
                    else if (c == LevelLegend.Lift && !seen[i]) ParseLift(x, row, seen);
                    else if (LevelLegend.IsWater(c) && !seen[i]) ParseWater(x, row, seen);
                    else if (wire != '\0' && !LevelLegend.IsSignalReceiver(c))
                        Errors.Add($"Hay un cable '{wire}' en ({x},{row}) encima de '{c}', que no es ni fuente ni receptor.");
                }
            }
        }

        // Compuerta: columna vertical de 'D' (o fila horizontal = trampilla).
        void ParseDoor(int x, int row, bool[] seen)
        {
            var r = new Receiver { Id = Receivers.Count, Kind = LevelLegend.Door };
            bool vertical = At(x, row + 1) == LevelLegend.Door;
            // Horizontal (trampilla): fila de 'D', o una sola 'D' metida en el suelo (con suelo a los dos lados).
            r.Horizontal = !vertical && (At(x + 1, row) == LevelLegend.Door
                || (LevelLegend.IsSolidTerrain(At(x - 1, row)) && LevelLegend.IsSolidTerrain(At(x + 1, row))));
            int cx = x, cr = row;
            while (At(cx, cr) == LevelLegend.Door)
            {
                int i = Index(cx, cr);
                seen[i] = true;
                r.Cells.Add(i);
                r.Channel = Merge(r.Channel, Level.WireAt(cx, cr), "compuerta", cx, cr);
                if (r.Horizontal) cx++; else cr++;
            }
            r.Label = (r.Horizontal ? "trampilla" : "compuerta") + $" ({x},{row})";
            Receivers.Add(r);
        }

        // Montacargas: fila de 'H' (su posición de abajo) y ':' encima de cada 'H' hasta la posición de arriba.
        void ParseLift(int x, int row, bool[] seen)
        {
            var r = new Receiver { Id = Receivers.Count, Kind = LevelLegend.Lift, LiftLeft = x, LiftBottom = row };
            int cx = x;
            while (At(cx, row) == LevelLegend.Lift)
            {
                int i = Index(cx, row);
                seen[i] = true;
                r.Cells.Add(i);
                r.Channel = Merge(r.Channel, Level.WireAt(cx, row), "montacargas", cx, row);
                cx++;
            }
            r.LiftRight = cx - 1;
            int top = row;
            while (At(x, top - 1) == LevelLegend.LiftPath) top--;
            r.LiftTop = top;
            if (top == row) Errors.Add($"El montacargas de ({x},{row}) no tiene recorrido: pon ':' encima.");
            for (int lx = r.LiftLeft; lx <= r.LiftRight; lx++)
                for (int lr = top; lr < row; lr++)
                {
                    if (At(lx, lr) != LevelLegend.LiftPath)
                        Errors.Add($"El recorrido del montacargas de ({x},{row}) está tapado en ({lx},{lr}).");
                    r.Channel = Merge(r.Channel, Level.WireAt(lx, lr), "montacargas", lx, lr);
                }
            r.Label = $"montacargas ({x},{row})";
            Receivers.Add(r);
        }

        // Agua: todas las casillas de agua conectadas. Solo cuenta como receptor si tiene cable.
        void ParseWater(int x, int row, bool[] seen)
        {
            var r = new Receiver { Id = -1, Kind = LevelLegend.WaterSurface };
            var stack = new Stack<int>();
            stack.Push(Index(x, row));
            seen[Index(x, row)] = true;
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                r.Cells.Add(i);
                int cx = XOf(i), cr = RowOf(i);
                r.Channel = Merge(r.Channel, Level.WireAt(cx, cr), "agua", cx, cr);
                int[] nx = { cx + 1, cx - 1, cx, cx };
                int[] nr = { cr, cr, cr + 1, cr - 1 };
                for (int k = 0; k < 4; k++)
                {
                    if (!Inside(nx[k], nr[k])) continue;
                    int j = Index(nx[k], nr[k]);
                    if (seen[j] || !LevelLegend.IsWater(Grid[j])) continue;
                    seen[j] = true;
                    stack.Push(j);
                }
            }
            if (r.Channel == '\0') return;   // agua normal
            r.Id = Receivers.Count;
            r.Label = $"agua drenable ({x},{row})";
            Receivers.Add(r);
        }

        char Merge(char current, char wire, string what, int x, int row)
        {
            if (wire == '\0') return current;
            if (current != '\0' && current != wire)
                Errors.Add($"La {what} de ({x},{row}) tiene dos cables distintos: '{current}' y '{wire}'.");
            return wire;
        }
    }
}
