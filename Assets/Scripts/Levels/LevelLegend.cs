using System.Collections.Generic;

namespace EntreDosMons.Levels
{
    public enum LevelLayer { Suelo, Decoracion, Fondo }

    // Qué significa cada carácter de los mapas de LevelLibrary.
    public static class LevelLegend
    {
        public const char Empty = '.';
        public const char NilSpawn = 'N';
        public const char VoltSpawn = 'V';
        public const char Checkpoint = 'C';
        public const char Exit = 'E';

        // Tiles con bordes automáticos: el constructor elige la pieza izquierda, central o derecha mirando a los vecinos.
        public static readonly Dictionary<char, string> EdgeTiles = new Dictionary<char, string>
        {
            { '=', "ext_herba_" },              // hierba (superficie del bosque)
            { '-', "ext_plataforma_fusta_" },   // plataforma de madera
            { 'r', "cova_roca_" },              // roca (superficie de la cueva)
        };

        public static readonly Dictionary<char, (string tile, LevelLayer layer)> Tiles = new Dictionary<char, (string, LevelLayer)>
        {
            // Exterior (Nil)
            { '#', ("ext_terra_ple", LevelLayer.Suelo) },
            { 'B', ("ext_bloc_pedra", LevelLayer.Suelo) },
            { 'X', ("ext_caixa_fusta", LevelLayer.Suelo) },
            { '~', ("ext_aigua_superficie", LevelLayer.Decoracion) },
            { 'w', ("ext_aigua_ple", LevelLayer.Decoracion) },
            { '|', ("ext_corda", LevelLayer.Decoracion) },
            { 'T', ("ext_corda_ancoratge", LevelLayer.Decoracion) },
            { 'L', ("ext_fanal", LevelLayer.Decoracion) },
            { 'a', ("ext_arbust", LevelLayer.Decoracion) },
            { 'f', ("ext_herbes_flors", LevelLayer.Decoracion) },
            // Coves (Volt)
            { 'R', ("cova_roca_ple", LevelLayer.Suelo) },
            { 'm', ("cova_roca_molsa", LevelLayer.Suelo) },
            { 'M', ("cova_plataforma_metall", LevelLayer.Suelo) },
            { 'k', ("cova_cristall_blau", LevelLayer.Decoracion) },
            { 'b', ("cova_bolets_brillants", LevelLayer.Decoracion) },
            { 'e', ("cova_escala_fusta", LevelLayer.Decoracion) },
            { 'v', ("cova_estalactita", LevelLayer.Decoracion) },
            { 'p', ("cova_palanca", LevelLayer.Decoracion) },
            { 'c', ("cova_carregador_bateria", LevelLayer.Decoracion) },
            { 'h', ("cova_tub_horitzontal", LevelLayer.Decoracion) },
            { 't', ("cova_tub_vertical", LevelLayer.Decoracion) },
            { '_', ("cova_via_vagoneta", LevelLayer.Decoracion) },
            { 'g', ("cova_engranatge_fons", LevelLayer.Fondo) },
        };
    }
}
