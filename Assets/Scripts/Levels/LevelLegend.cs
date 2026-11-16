using System.Collections.Generic;

namespace EntreDosMons.Levels
{
    public enum LevelLayer { Suelo, Decoracion, Fondo }

    // Qué significa cada carácter de los mapas de LevelLibrary.
    // Hay tres tipos de casilla:
    //   1) Terreno y decoración: solo ponen un tile (no tienen comportamiento).
    //   2) Recorrido: agua, cuerdas, escaleras, pinchos, plataformas... (el LevelBuilder crea su zona).
    //   3) Objetos con función: cada uno es un PREFAB (Assets/Resources/Prefabs) que el LevelBuilder instancia.
    //      Los mecanismos (palancas, terminales, placas, cristales → compuertas, montacargas, agua) se conectan
    //      con la "capa de cables" del nivel (LevelDefinition.Wiring).
    public static class LevelLegend
    {
        public const char Empty = '.';

        // ---------- Personajes y flujo ----------
        public const char NilSpawn = 'N';
        public const char VoltSpawn = 'V';
        public const char Checkpoint = 'C';    // hoguera de Nil (en las cuevas el checkpoint de Volt es el cargador)
        public const char Exit = 'E';          // salida: hace falta que lleguen los dos personajes, cada uno a la suya

        // ---------- Recorrido ----------
        public const char Spikes = '^';        // pinchos: quitan una vida (cuelgan del techo si hay suelo encima)
        public const char WaterSurface = '~';  // agua (superficie)
        public const char WaterFill = 'w';     // agua (fondo)
        public const char Rope = '|';          // cuerda vertical para trepar (solo Nil)
        public const char RopeAnchor = 'T';    // anclaje de la cuerda vertical (también se trepa)
        public const char SwingAnchor = 'P';   // anclaje de una cuerda-péndulo (solo Nil)
        public const char Ladder = 'e';        // escalera de madera (los dos)
        public const char OneWay = '-';        // plataforma de madera: se atraviesa desde abajo, abajo + saltar para bajar

        // ---------- Objetos con función (prefabs) ----------
        public const char Box = 'X';           // caja de madera: solo Nil tiene fuerza para empujarla
        public const char Charger = 'c';       // cargador de batería de Volt (y su checkpoint)
        // Fuentes de señal (encienden un canal de la capa de cables)
        public const char Lever = 'p';         // palanca/válvula manual oxidada: solo Nil (fuerza)
        public const char Terminal = 'G';      // terminal eléctrico de la estación: solo Volt (conexión de datos)
        public const char Plate = 'o';         // placa de presión: se activa con peso (caja o personaje)
        public const char Crystal = 'k';       // cristal: se carga con la linterna de Volt y se queda encendido
        // Receptores de señal
        public const char Door = 'D';          // compuerta: columna de 'D' (sube) o fila de 'D' (trampilla que se abre)
        public const char Lift = 'H';          // montacargas: posición de abajo; ':' encima marca el recorrido hasta arriba
        public const char LiftPath = ':';      // recorrido del montacargas (vacío)
        // El agua ('~' 'w') también puede ser receptor: si tiene cable, se vacía cuando el canal se activa.

        // ---------- Tiles de los objetos (los busca el LevelBuilder por nombre) ----------
        public const string SpikesExterior = "ext_punxes";
        public const string SpikesCave = "cova_punxes";

        // Tiles con bordes automáticos: el constructor elige la pieza izquierda, central o derecha mirando a los vecinos.
        public static readonly Dictionary<char, string> EdgeTiles = new Dictionary<char, string>
        {
            { '=', "ext_herba_" },              // hierba (superficie del bosque)
            { '-', "ext_plataforma_fusta_" },   // plataforma de madera
            { 'r', "cova_roca_" },              // roca (superficie de la cueva)
        };

        // Terreno y decoración (solo tile).
        public static readonly Dictionary<char, (string tile, LevelLayer layer)> Tiles = new Dictionary<char, (string, LevelLayer)>
        {
            // Exterior (mundo de Nil)
            { '#', ("ext_terra_ple", LevelLayer.Suelo) },
            { 'B', ("ext_bloc_pedra", LevelLayer.Suelo) },
            { '~', ("ext_aigua_superficie", LevelLayer.Decoracion) },
            { 'w', ("ext_aigua_ple", LevelLayer.Decoracion) },
            { '|', ("ext_corda", LevelLayer.Decoracion) },
            { 'T', ("ext_corda_ancoratge", LevelLayer.Decoracion) },
            { 'L', ("ext_fanal", LevelLayer.Decoracion) },
            { 'a', ("ext_arbust", LevelLayer.Decoracion) },
            { 'f', ("ext_herbes_flors", LevelLayer.Decoracion) },
            // Coves (mundo de Volt)
            { 'R', ("cova_roca_ple", LevelLayer.Suelo) },
            { 'm', ("cova_roca_molsa", LevelLayer.Suelo) },
            { 'M', ("cova_plataforma_metall", LevelLayer.Suelo) },
            { 'b', ("cova_bolets_brillants", LevelLayer.Decoracion) },
            { 'e', ("cova_escala_fusta", LevelLayer.Decoracion) },
            { 'v', ("cova_estalactita", LevelLayer.Decoracion) },
            { 'h', ("cova_tub_horitzontal", LevelLayer.Fondo) },
            { 't', ("cova_tub_vertical", LevelLayer.Fondo) },
            { 'g', ("cova_engranatge_fons", LevelLayer.Fondo) },
        };

        // Decoración pura: no tiene función de juego, pero el validador comprueba que esté en un sitio coherente.
        public static bool IsDecoration(char c) => "afLbvhtg".IndexOf(c) >= 0;

        public static bool IsSolidTerrain(char c)
        {
            if (c == '=' || c == 'r') return true;
            return Tiles.TryGetValue(c, out var e) && e.layer == LevelLayer.Suelo;
        }

        public static bool IsWater(char c) => c == WaterSurface || c == WaterFill;
        public static bool IsSignalSource(char c) => c == Lever || c == Terminal || c == Plate || c == Crystal;
        public static bool IsSignalReceiver(char c) => c == Door || c == Lift || IsWater(c);
        public static bool IsDialogue(char c) => c >= '1' && c <= '9';
    }
}
