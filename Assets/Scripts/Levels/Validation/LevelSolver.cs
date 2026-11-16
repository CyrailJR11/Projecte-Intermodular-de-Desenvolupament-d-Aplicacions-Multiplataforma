using System;
using System.Collections.Generic;
using System.Text;

namespace EntreDosMons.Levels.Validation
{
    // Simulador de flujo de un nivel: busca, como lo haría un jugador, una forma de que Nil y Volt lleguen a sus
    // salidas. Es una búsqueda en anchura (BFS) sobre "estados del puzle":
    //   estado = dónde está Nil + dónde está Volt (y si va por el techo) + dónde están las cajas
    //            + qué palancas/terminales están encendidos y qué cristales se han cargado + quién ha llegado ya.
    // Para cada estado calcula hasta dónde puede llegar cada personaje andando, saltando, trepando, nadando o
    // invirtiendo la gravedad (sin cambiar nada del mapa) y, desde ahí, qué acciones puede hacer:
    // accionar una palanca o un terminal, pisar una placa, empujar una caja, cargar un cristal, subirse a un
    // montacargas o llegar a la salida. Cada acción da un estado nuevo.
    //
    // El movimiento está simplificado a casillas con las medidas reales de los personajes:
    //   salto de 12 u/s con gravedad 3 → sube 2,4 casillas: puede subir 2 casillas y cruzar huecos de 3.
    // No simula batería ni respiración (eso lo prueban los tests PlayMode y el juego).
    public sealed class LevelSolver
    {
        public sealed class Options
        {
            public int JumpHeight = 2;          // casillas que sube un salto normal
            public int ClimbJumpHeight = 1;     // saltando desde una cuerda o escalera
            public int MaxHorizontalJump = 4;   // distancia para caer a la misma altura (hueco de 3)
            public int MaxStates = 60000;
            public readonly HashSet<int> DisabledSources = new HashSet<int>();     // nunca se activan
            public readonly HashSet<int> DisabledReceivers = new HashSet<int>();   // nunca reaccionan
            public readonly HashSet<int> RemovedBoxes = new HashSet<int>();        // casillas iniciales de cajas quitadas
            public bool AnalyseSoftlocks = true;
        }

        public sealed class Step
        {
            public string Actor;     // "Nil" o "Volt"
            public string Text;
            public int X, Row;       // dónde pasa (para dibujarlo en la vista previa)
            public override string ToString() => Actor + ": " + Text;
        }

        public sealed class Result
        {
            public bool Solved;
            public bool Truncated;
            public int States;
            public readonly List<Step> Solution = new List<Step>();
            public readonly HashSet<int> UsedSources = new HashSet<int>();
            public readonly HashSet<int> MovedReceivers = new HashSet<int>();
            public readonly HashSet<int> PushedBoxes = new HashSet<int>();   // casilla inicial de cada caja que se ha movido
            public readonly HashSet<int> NilCells = new HashSet<int>();      // casillas donde ha podido estar Nil
            public readonly HashSet<int> VoltCells = new HashSet<int>();
            public readonly HashSet<int> SwingsUsed = new HashSet<int>();
            public int SoftlockStates;
            public string SoftlockExample;
        }

        // ---------- Estado ----------

        sealed class State
        {
            public int Nil;           // casilla
            public int Volt;          // casilla * 2 + (1 si va boca abajo)
            public int[] Boxes;       // casilla de cada caja (en el orden de las cajas iniciales)
            public ulong Toggles;     // bit por fuente: palanca/terminal encendido o cristal cargado
            public int Done;          // bit 0 = Nil ha llegado, bit 1 = Volt ha llegado
            public string Key;
            public State Parent;
            public Step How;
            public int Index;

            public State Clone()
            {
                return new State { Nil = Nil, Volt = Volt, Boxes = (int[])Boxes.Clone(), Toggles = Toggles, Done = Done };
            }

            public void MakeKey()
            {
                var sb = new StringBuilder();
                sb.Append(Nil).Append('|').Append(Volt).Append('|').Append(Toggles).Append('|').Append(Done);
                // Las cajas son iguales entre sí: se ordenan para no repetir estados.
                var sorted = (int[])Boxes.Clone();
                Array.Sort(sorted);
                foreach (int b in sorted) sb.Append('|').Append(b);
                Key = sb.ToString();
            }
        }

        // Lo que hay en el mapa en un estado concreto.
        sealed class Env
        {
            public bool[] Solid, Water;
            public bool[] ChannelOn = new bool[26];
            public bool[] ReceiverOn;
            public int[] LiftRow;
        }

        enum Who { Nil, Volt }

        readonly LevelModel m;
        readonly Options opt;
        readonly bool[] terrain, oneWay, spikes, ladder, rope, waterStatic;
        readonly int[] boxStart;

        public LevelSolver(LevelModel model, Options options = null)
        {
            m = model;
            opt = options ?? new Options();
            int n = m.W * m.H;
            terrain = new bool[n]; oneWay = new bool[n]; spikes = new bool[n];
            ladder = new bool[n]; rope = new bool[n]; waterStatic = new bool[n];
            for (int i = 0; i < n; i++)
            {
                char c = m.Grid[i];
                terrain[i] = LevelLegend.IsSolidTerrain(c);
                oneWay[i] = c == LevelLegend.OneWay;
                spikes[i] = c == LevelLegend.Spikes;
                ladder[i] = c == LevelLegend.Ladder;
                rope[i] = c == LevelLegend.Rope || c == LevelLegend.RopeAnchor;
                waterStatic[i] = LevelLegend.IsWater(c);
            }
            var boxes = new List<int>();
            foreach (int b in m.Boxes) if (!opt.RemovedBoxes.Contains(b)) boxes.Add(b);
            boxStart = boxes.ToArray();
        }

        public static Result Solve(LevelDefinition level, Options options = null) => new LevelSolver(new LevelModel(level), options).Run();

        // ---------- Búsqueda ----------

        public Result Run()
        {
            var result = new Result();
            if (m.NilSpawn < 0 || m.VoltSpawn < 0) return result;

            var start = new State { Nil = m.NilSpawn, Volt = m.VoltSpawn * 2, Boxes = (int[])boxStart.Clone() };
            if (!Normalize(start, null)) return result;
            start.MakeKey();

            var all = new List<State>();
            var byKey = new Dictionary<string, State>();
            var edges = new List<List<int>>();
            var queue = new Queue<State>();
            start.Index = 0;
            all.Add(start); edges.Add(new List<int>());
            byKey[start.Key] = start;
            queue.Enqueue(start);
            State goal = null;
            var goals = new List<int>();

            while (queue.Count > 0)
            {
                State s = queue.Dequeue();
                if (s.Done == 3) { goals.Add(s.Index); if (goal == null) goal = s; continue; }
                if (all.Count >= opt.MaxStates) { result.Truncated = true; break; }

                // Cada personaje se mueve con el mapa tal como queda si ÉL se baja de una placa
                // (si no, podría "aguantar" una compuerta abierta mientras la cruza).
                Env env = BuildEnv(s);
                Env nilEnv = BuildEnv(s, Who.Nil), voltEnv = BuildEnv(s, Who.Volt);
                var nilReach = Reach(Who.Nil, s.Nil * 2, nilEnv, out var nilSwings);
                var voltReach = Reach(Who.Volt, s.Volt, voltEnv, out _);
                foreach (int node in nilReach) result.NilCells.Add(node / 2);
                foreach (int node in voltReach) result.VoltCells.Add(node / 2);
                foreach (int sw in nilSwings) result.SwingsUsed.Add(sw);

                foreach (var next in Actions(s, nilEnv, voltEnv, nilReach, voltReach, result))
                {
                    if (!Normalize(next, env)) continue;
                    next.MakeKey();
                    if (byKey.TryGetValue(next.Key, out State known))
                    {
                        edges[s.Index].Add(known.Index);
                        continue;
                    }
                    next.Index = all.Count;
                    all.Add(next); edges.Add(new List<int>());
                    byKey[next.Key] = next;
                    edges[s.Index].Add(next.Index);
                    queue.Enqueue(next);
                    NoteChanges(s, next, env, result);
                }
            }

            result.States = all.Count;
            result.Solved = goal != null;
            if (goal != null)
            {
                var path = new List<Step>();
                for (State s = goal; s.Parent != null; s = s.Parent) path.Add(s.How);
                path.Reverse();
                result.Solution.AddRange(path);
            }
            if (opt.AnalyseSoftlocks && !result.Truncated && goal != null) FindSoftlocks(all, edges, goals, result);
            return result;
        }

        void NoteChanges(State from, State to, Env envFrom, Result r)
        {
            Env envTo = BuildEnv(to);
            foreach (var rc in m.Receivers)
                if (envFrom.ReceiverOn[rc.Id] != envTo.ReceiverOn[rc.Id]) r.MovedReceivers.Add(rc.Id);
            for (int i = 0; i < from.Boxes.Length; i++)
                if (from.Boxes[i] != to.Boxes[i]) r.PushedBoxes.Add(boxStart[i]);
        }

        // Estados desde los que ya no se puede terminar el nivel (habría que reiniciar con R).
        void FindSoftlocks(List<State> all, List<List<int>> edges, List<int> goals, Result r)
        {
            var reverse = new List<int>[all.Count];
            for (int i = 0; i < all.Count; i++) reverse[i] = new List<int>();
            for (int i = 0; i < all.Count; i++) foreach (int j in edges[i]) reverse[j].Add(i);
            var good = new bool[all.Count];
            var q = new Queue<int>();
            foreach (int g in goals) { good[g] = true; q.Enqueue(g); }
            while (q.Count > 0)
            {
                int j = q.Dequeue();
                foreach (int i in reverse[j]) if (!good[i]) { good[i] = true; q.Enqueue(i); }
            }
            for (int i = 0; i < all.Count; i++)
            {
                if (good[i]) continue;
                r.SoftlockStates++;
                if (r.SoftlockExample == null && all[i].How != null)
                {
                    var path = new List<string>();
                    for (State s = all[i]; s.Parent != null; s = s.Parent) path.Add(s.How.ToString());
                    path.Reverse();
                    r.SoftlockExample = string.Join(" → ", path);
                }
            }
        }

        // ---------- Entorno ----------

        Env BuildEnv(State s) => BuildEnv(s, null);

        // ignore = personaje cuyo peso no cuenta en las placas (el que se está moviendo).
        Env BuildEnv(State s, Who? ignore)
        {
            var env = new Env
            {
                Solid = (bool[])terrain.Clone(),
                Water = (bool[])waterStatic.Clone(),
                ReceiverOn = new bool[m.Receivers.Count],
                LiftRow = new int[m.Receivers.Count],
            };

            // Fuentes activas.
            var active = new bool[m.Sources.Count];
            int voltCell = s.Volt / 2;
            bool voltStanding = (s.Volt & 1) == 0;
            foreach (var src in m.Sources)
            {
                if (opt.DisabledSources.Contains(src.Id)) continue;
                if (src.Kind == LevelLegend.Plate)
                {
                    int cell = m.Index(src.X, src.Row);
                    active[src.Id] = (ignore != Who.Nil && s.Nil == cell) || (ignore != Who.Volt && voltStanding && voltCell == cell)
                        || Array.IndexOf(s.Boxes, cell) >= 0;
                }
                else active[src.Id] = (s.Toggles & (1UL << src.Id)) != 0;
            }
            // Canales (O / Y).
            for (int c = 0; c < 26; c++)
            {
                char id = (char)('a' + c);
                bool any = false, all = true, has = false;
                foreach (var src in m.Sources)
                {
                    if (src.Channel != id) continue;
                    has = true;
                    any |= active[src.Id];
                    all &= active[src.Id];
                }
                env.ChannelOn[c] = has && (m.Level.Channel(id).RequireAll ? all : any);
            }
            // Receptores.
            foreach (var rc in m.Receivers)
            {
                bool on = rc.Channel != '\0' && env.ChannelOn[rc.Channel - 'a'] && !opt.DisabledReceivers.Contains(rc.Id);
                env.ReceiverOn[rc.Id] = on;
                switch (rc.Kind)
                {
                    case LevelLegend.Door:
                        foreach (int cell in rc.Cells) env.Solid[cell] = !on;
                        break;
                    case LevelLegend.Lift:
                        int row = on ? rc.LiftTop : rc.LiftBottom;
                        env.LiftRow[rc.Id] = row;
                        for (int x = rc.LiftLeft; x <= rc.LiftRight; x++) env.Solid[m.Index(x, row)] = true;
                        break;
                    default:   // agua
                        if (on) foreach (int cell in rc.Cells) env.Water[cell] = false;
                        break;
                }
            }
            foreach (int b in s.Boxes) env.Solid[b] = true;
            return env;
        }

        // Aplica la gravedad a cajas y personajes y comprueba que nadie haya quedado atrapado o muerto.
        // prev = entorno anterior a la acción (para que los montacargas lleven encima lo que tienen).
        bool Normalize(State s, Env prev)
        {
            for (int iteration = 0; iteration < 6; iteration++)
            {
                Env env = BuildEnv(s);
                bool changed = false;

                // Montacargas que se han movido: suben o bajan lo que tienen encima.
                if (prev != null)
                {
                    foreach (var rc in m.Receivers)
                    {
                        if (rc.Kind != LevelLegend.Lift) continue;
                        int before = prev.LiftRow[rc.Id], after = env.LiftRow[rc.Id];
                        if (before == after) continue;
                        int delta = after - before;
                        for (int x = rc.LiftLeft; x <= rc.LiftRight; x++)
                        {
                            int on = m.Index(x, before - 1);
                            if (s.Nil == on) s.Nil = m.Index(x, after - 1);
                            if (s.Volt == on * 2) s.Volt = m.Index(x, after - 1) * 2;
                            for (int b = 0; b < s.Boxes.Length; b++) if (s.Boxes[b] == on) s.Boxes[b] = m.Index(x, after - 1);
                        }
                        if (delta != 0) changed = true;
                    }
                    prev = null;
                    if (changed) continue;
                }

                // Nadie puede quedar dentro de algo sólido (una compuerta que se cierra encima, etc.).
                if (Overlaps(env, s.Nil, s, -1) || Overlaps(env, s.Volt / 2, s, -1)) return false;

                // Cajas: caen hasta apoyarse.
                for (int b = 0; b < s.Boxes.Length; b++)
                {
                    int cell = s.Boxes[b];
                    int x = m.XOf(cell), row = m.RowOf(cell);
                    env.Solid[cell] = false;
                    while (true)
                    {
                        int below = row + 1;
                        if (below >= m.H) return false;   // se ha caído del mapa
                        int bi = m.Index(x, below);
                        if (env.Solid[bi] || oneWay[bi]) break;
                        row = below;
                    }
                    int landed = m.Index(x, row);
                    env.Solid[landed] = true;
                    if (landed != cell) { s.Boxes[b] = landed; changed = true; }
                }
                if (changed) continue;

                // Personajes: si ya no están en un sitio estable, caen.
                int nil = SettleTo(Who.Nil, s.Nil * 2, env);
                if (nil < 0) return false;
                if (nil / 2 != s.Nil) { s.Nil = nil / 2; changed = true; }
                int volt = SettleTo(Who.Volt, s.Volt, env);
                if (volt < 0) return false;
                if (volt != s.Volt) { s.Volt = volt; changed = true; }
                if (!changed) return true;
            }
            return true;
        }

        bool Overlaps(Env env, int cell, State s, int ignore) => env.Solid[cell] && Array.IndexOf(s.Boxes, cell) != ignore;

        // Posición estable final después de caer (o -1 si muere).
        int SettleTo(Who who, int node, Env env)
        {
            int cell = node / 2, g = (node & 1) == 1 ? -1 : 1;
            if (Deadly(who, cell, env)) return -1;
            if (IsRest(who, cell, g, env)) return node;
            var list = new List<int>();
            Settle(who, cell, g, env, list, null);
            return list.Count > 0 ? list[list.Count - 1] : -1;
        }

        // ---------- Reglas de movimiento ----------

        int Home(Who who) => who == Who.Nil ? 0 : 1;

        bool Free(Env env, int x, int row) => m.Inside(x, row) && !env.Solid[m.Index(x, row)];

        bool Deadly(Who who, int cell, Env env)
        {
            if (spikes[cell]) return true;
            int row = m.RowOf(cell);
            if ((int)m.WorldOf(row) != Home(who)) return true;   // cada uno en su mundo
            if (who == Who.Volt && env.Water[cell]) return true;  // Volt no flota: cortocircuito
            return false;
        }

        bool Supported(int cell, int g, Env env)
        {
            int x = m.XOf(cell), below = m.RowOf(cell) + g;
            if (!m.Inside(x, below)) return false;
            int bi = m.Index(x, below);
            return env.Solid[bi] || (g == 1 && oneWay[bi]);
        }

        bool Climbable(Who who, int cell, int g) => g == 1 && (ladder[cell] || (who == Who.Nil && rope[cell]));
        bool Swims(Who who, int cell, Env env) => who == Who.Nil && env.Water[cell];
        bool IsRest(Who who, int cell, int g, Env env) => Supported(cell, g, env) || Climbable(who, cell, g) || Swims(who, cell, env);

        // Caer desde una casilla libre: añade a "rests" cada sitio donde puede quedarse (agarrarse a una escalera
        // al pasar, el agua o el suelo donde aterriza). "air" recoge las casillas por las que pasa en el aire.
        void Settle(Who who, int cell, int g, Env env, List<int> rests, List<int> air)
        {
            int x = m.XOf(cell), row = m.RowOf(cell);
            for (int guard = 0; guard < m.H + 2; guard++)
            {
                if (!m.Inside(x, row)) return;          // se cae del mapa
                int i = m.Index(x, row);
                if (env.Solid[i] || Deadly(who, i, env)) return;
                if (Swims(who, i, env)) { rests.Add(i * 2); return; }
                if (Supported(i, g, env)) { rests.Add(i * 2 + (g == -1 ? 1 : 0)); return; }
                if (Climbable(who, i, g)) rests.Add(i * 2);
                else if (air != null) air.Add(i);
                row += g;
            }
        }

        // Todas las posiciones a las que puede llegar un personaje sin cambiar nada del mapa.
        HashSet<int> Reach(Who who, int startNode, Env env, out HashSet<int> swings)
        {
            var seen = new HashSet<int>();
            swings = new HashSet<int>();
            var queue = new Queue<int>();
            seen.Add(startNode);
            queue.Enqueue(startNode);
            var rests = new List<int>();
            var air = new List<int>();

            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                rests.Clear();
                air.Clear();
                Moves(who, node, env, rests, air);
                if (who == Who.Nil) CheckSwings(air, env, rests, swings);
                foreach (int r in rests)
                    if (seen.Add(r)) queue.Enqueue(r);
            }
            return seen;
        }

        void Moves(Who who, int node, Env env, List<int> rests, List<int> air)
        {
            int cell = node / 2, g = (node & 1) == 1 ? -1 : 1;
            int x = m.XOf(cell), row = m.RowOf(cell);
            bool sup = Supported(cell, g, env);
            bool climb = Climbable(who, cell, g);
            bool swim = Swims(who, cell, env);

            // Andar (o nadar / soltarse de lado).
            for (int dir = -1; dir <= 1; dir += 2)
                if (Free(env, x + dir, row) && !Deadly(who, m.Index(x + dir, row), env))
                    Settle(who, m.Index(x + dir, row), g, env, rests, air);

            // Trepar.
            if (climb)
            {
                if (Free(env, x, row - 1) && !Deadly(who, m.Index(x, row - 1), env))
                {
                    int up = m.Index(x, row - 1);
                    if (Climbable(who, up, 1)) rests.Add(up * 2);
                    else
                    {
                        // Arriba del todo: empujón hacia la plataforma de al lado.
                        Settle(who, up, 1, env, rests, air);
                        for (int dir = -1; dir <= 1; dir += 2)
                            if (Free(env, x + dir, row - 1) && !Deadly(who, m.Index(x + dir, row - 1), env))
                                Settle(who, m.Index(x + dir, row - 1), 1, env, rests, air);
                    }
                }
                if (Free(env, x, row + 1) && !Deadly(who, m.Index(x, row + 1), env))
                    Settle(who, m.Index(x, row + 1), 1, env, rests, air);
            }

            // Nadar (Nil): arriba, abajo y a los lados dentro del agua.
            if (swim)
            {
                for (int dr = -1; dr <= 1; dr += 2)
                    if (Free(env, x, row + dr))
                    {
                        int j = m.Index(x, row + dr);
                        if (Deadly(who, j, env)) continue;
                        if (env.Water[j]) rests.Add(j * 2);
                    }
            }

            // Saltar.
            int jump = 0;
            if (sup) jump = opt.JumpHeight;
            else if (climb) jump = opt.ClimbJumpHeight;
            else if (swim && Free(env, x, row - 1) && !env.Water[m.Index(x, row - 1)]) jump = opt.JumpHeight;
            if (jump > 0) Jump(who, x, row, g, jump, env, rests, air);

            // Bajar de una plataforma de madera: abajo + saltar.
            if (g == 1 && sup && m.Inside(x, row + 1) && oneWay[m.Index(x, row + 1)] && !env.Solid[m.Index(x, row + 1)])
                Settle(who, m.Index(x, row + 1), 1, env, rests, air);

            // Volt: invertir la gravedad (botas magnéticas). Cae hacia el techo (o vuelve al suelo).
            if (who == Who.Volt && sup)
                Settle(who, cell, -g, env, rests, air);
        }

        // Salto: sube "height" casillas como mucho, avanza en horizontal y cae. Límites según la altura ganada:
        // +2 casillas → 2 de distancia, +1 → 3, misma altura → 4 (hueco de 3), bajando → más.
        void Jump(Who who, int x, int row, int g, int height, Env env, List<int> rests, List<int> air)
        {
            var land = new List<int>();
            for (int p = 1; p <= height; p++)
            {
                int apex = row - g * p;
                if (!Free(env, x, apex) || Deadly(who, m.Index(x, apex), env)) break;
                air.Add(m.Index(x, apex));
                for (int dir = -1; dir <= 1; dir += 2)
                {
                    for (int d = 0; d <= opt.MaxHorizontalJump + 2; d++)
                    {
                        int cx = x + dir * d;
                        if (d > 0)
                        {
                            if (!Free(env, cx, apex) || Deadly(who, m.Index(cx, apex), env)) break;
                            air.Add(m.Index(cx, apex));
                        }
                        land.Clear();
                        Settle(who, m.Index(cx, apex), g, env, land, air);
                        foreach (int l in land)
                        {
                            int dy = (row - m.RowOf(l / 2)) * g;   // > 0: aterriza más arriba
                            int limit = dy >= 2 ? 2 : dy == 1 ? 3 : dy == 0 ? opt.MaxHorizontalJump : Math.Min(opt.MaxHorizontalJump - dy, opt.MaxHorizontalJump + 2);
                            if (dy <= height && d <= limit) rests.Add(l);
                        }
                    }
                }
            }
        }

        // Cuerda-péndulo: si Nil pasa por el aire cerca de la cuerda, se agarra; desde ella puede soltarse
        // en cualquier punto del balanceo con un pequeño salto.
        void CheckSwings(List<int> air, Env env, List<int> rests, HashSet<int> swings)
        {
            foreach (int anchor in m.Swings)
            {
                if (swings.Contains(anchor)) continue;
                int ax = m.XOf(anchor), ar = m.RowOf(anchor);
                bool grabbed = false;
                foreach (int a in air)
                {
                    int dx = Math.Abs(m.XOf(a) - ax), dr = m.RowOf(a) - ar;
                    if (dx <= 1 && dr >= 1 && dr <= 4) { grabbed = true; break; }
                }
                if (!grabbed) continue;
                swings.Add(anchor);
                var dummyAir = new List<int>();
                for (int r = ar + 2; r <= ar + 4; r++)
                {
                    if (!Free(env, ax, r)) break;
                    for (int dir = -1; dir <= 1; dir += 2)
                        for (int d = 0; d <= 3; d++)
                        {
                            int cx = ax + dir * d;
                            if (!Free(env, cx, r) || Deadly(Who.Nil, m.Index(cx, r), env)) break;
                            Settle(Who.Nil, m.Index(cx, r), 1, env, rests, dummyAir);
                            // Al soltarse sale despedido: un salto pequeño hacia delante.
                            if (Free(env, cx, r - 1)) Jump(Who.Nil, cx, r, 1, 1, env, rests, dummyAir);
                        }
                }
            }
        }

        // ---------- Acciones ----------

        IEnumerable<State> Actions(State s, Env nilEnv, Env voltEnv, HashSet<int> nilReach, HashSet<int> voltReach, Result r)
        {
            foreach (var st in ActionsFor(Who.Nil, s, nilEnv, nilReach, r)) yield return st;
            foreach (var st in ActionsFor(Who.Volt, s, voltEnv, voltReach, r)) yield return st;
        }

        IEnumerable<State> ActionsFor(Who who, State s, Env env, HashSet<int> reach, Result r)
        {
            string name = who == Who.Nil ? "Nil" : "Volt";
            int bit = who == Who.Nil ? 1 : 2;

            // Llegar a la salida de su mundo.
            if ((s.Done & bit) == 0)
                foreach (int exit in m.Exits)
                    if ((int)m.WorldOf(m.RowOf(exit)) == Home(who) && (reach.Contains(exit * 2) || reach.Contains(exit * 2 + 1)))
                    {
                        State n = Move(who, s, exit * 2 + (reach.Contains(exit * 2) ? 0 : 1));
                        n.Done |= bit;
                        n.How = StepAt(name, "llega a la salida", exit);
                        yield return n;
                    }

            foreach (var src in m.Sources)
            {
                if (opt.DisabledSources.Contains(src.Id)) continue;
                bool toggle = (src.Kind == LevelLegend.Lever && who == Who.Nil) || (src.Kind == LevelLegend.Terminal && who == Who.Volt);
                if (toggle)
                {
                    int spot = FindNear(reach, src.X, src.Row, who == Who.Nil);
                    if (spot < 0) continue;
                    r.UsedSources.Add(src.Id);
                    var n = Move(who, s, spot);
                    n.Toggles ^= 1UL << src.Id;
                    bool on = (n.Toggles & (1UL << src.Id)) != 0;
                    n.How = StepAt(name, $"{(src.Kind == LevelLegend.Lever ? "mueve la palanca" : "usa el terminal")} ({(on ? "ON" : "OFF")}) → {Describe(src.Channel)}", m.Index(src.X, src.Row));
                    yield return n;
                }
                else if (src.Kind == LevelLegend.Crystal && who == Who.Volt && (s.Toggles & (1UL << src.Id)) == 0)
                {
                    int spot = FindLightSpot(reach, src, env);
                    if (spot < 0) continue;
                    r.UsedSources.Add(src.Id);
                    var n = Move(who, s, spot);
                    n.Toggles |= 1UL << src.Id;
                    n.How = StepAt(name, $"ilumina el cristal con la linterna → {Describe(src.Channel)}", m.Index(src.X, src.Row));
                    yield return n;
                }
                else if (src.Kind == LevelLegend.Plate)
                {
                    int cell = m.Index(src.X, src.Row);
                    int current = who == Who.Nil ? s.Nil : s.Volt / 2;
                    if (current == cell || !reach.Contains(cell * 2)) continue;
                    r.UsedSources.Add(src.Id);
                    var n = Move(who, s, cell * 2);
                    n.How = StepAt(name, $"se queda encima de la placa → {Describe(src.Channel)}", cell);
                    yield return n;
                }
            }
            foreach (var src in m.Sources)
                if (src.Kind == LevelLegend.Plate && Array.IndexOf(s.Boxes, m.Index(src.X, src.Row)) >= 0) r.UsedSources.Add(src.Id);

            // Subirse a un montacargas (para que lo lleve el otro personaje).
            foreach (var rc in m.Receivers)
            {
                if (rc.Kind != LevelLegend.Lift) continue;
                int row = env.LiftRow[rc.Id] - 1;
                for (int x = rc.LiftLeft; x <= rc.LiftRight; x++)
                {
                    int cell = m.Index(x, row);
                    int current = who == Who.Nil ? s.Nil : s.Volt / 2;
                    if (cell == current || !reach.Contains(cell * 2)) continue;
                    var n = Move(who, s, cell * 2);
                    n.How = StepAt(name, "se sube al montacargas y espera", cell);
                    yield return n;
                    break;
                }
            }

            // Empujar cajas: solo Nil tiene fuerza.
            if (who == Who.Nil)
            {
                for (int b = 0; b < s.Boxes.Length; b++)
                {
                    int bc = s.Boxes[b];
                    int bx = m.XOf(bc), br = m.RowOf(bc);
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        if (!m.Inside(bx - dir, br)) continue;
                        int spot = m.Index(bx - dir, br);
                        if (!reach.Contains(spot * 2) || !Supported(spot, 1, env)) continue;
                        if (!Free(env, bx + dir, br)) continue;
                        var n = s.Clone();
                        n.Boxes[b] = m.Index(bx + dir, br);
                        n.Nil = bc;
                        n.Parent = s;
                        n.How = StepAt("Nil", $"empuja la caja hacia la {(dir > 0 ? "derecha" : "izquierda")}", bc);
                        yield return n;
                    }
                }
            }
        }

        State Move(Who who, State s, int node)
        {
            var n = s.Clone();
            if (who == Who.Nil) n.Nil = node / 2; else n.Volt = node;
            n.Parent = s;
            return n;
        }

        Step StepAt(string actor, string text, int cell) => new Step { Actor = actor, Text = text, X = m.XOf(cell), Row = m.RowOf(cell) };

        string Describe(char channel) => channel == '\0' ? "(sin cable)" : m.Level.Channel(channel).Description;

        // Posición desde la que se alcanza un objeto (la misma casilla o la de al lado, en la misma fila).
        int FindNear(HashSet<int> reach, int x, int row, bool normalGravityOnly)
        {
            for (int dx = 0; dx <= 1; dx++)
                for (int sign = 1; sign >= -1; sign -= 2)
                {
                    if (!m.Inside(x + dx * sign, row)) continue;
                    int cell = m.Index(x + dx * sign, row);
                    if (reach.Contains(cell * 2)) return cell * 2;
                    if (!normalGravityOnly && reach.Contains(cell * 2 + 1)) return cell * 2 + 1;
                }
            return -1;
        }

        // La linterna alcanza ~6 casillas en horizontal (una fila arriba o abajo) si no hay nada en medio.
        int FindLightSpot(HashSet<int> reach, LevelModel.Source crystal, Env env)
        {
            foreach (int node in reach)
            {
                int cell = node / 2;
                int x = m.XOf(cell), row = m.RowOf(cell);
                if (Math.Abs(row - crystal.Row) > 1 || Math.Abs(x - crystal.X) > 6) continue;
                int dir = Math.Sign(crystal.X - x);
                bool clear = true;
                for (int cx = x + dir; dir != 0 && cx != crystal.X; cx += dir)
                    if (env.Solid[m.Index(cx, row)]) { clear = false; break; }
                if (clear) return node;
            }
            return -1;
        }
    }
}
