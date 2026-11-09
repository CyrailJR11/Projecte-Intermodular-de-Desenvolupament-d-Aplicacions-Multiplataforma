using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels
{
    // Los niveles del juego, escritos como texto. Cada carácter es una casilla de 32×32 (ver LevelLegend).
    // La fila de arriba del texto es la de arriba del nivel. Todas las filas de un nivel deben medir lo mismo.
    // Para añadir un nivel: copia uno, cambia el mapa y sus diálogos, y añádelo a la lista Levels.
    public static class LevelLibrary
    {
        public static readonly LevelDefinition[] Levels =
        {
            new LevelDefinition("Prólogo · El refugio y la estación", new[]
            {
                "............................................................",
                "............................................................",
                "............................................................",
                "..........................................---...............",
                "............................................................",
                "...................................---..........L...........",
                "....L...............................T.....................E.",
                "....................---.............|...........======....C.",
                ".N.1.2.3..f..a...6...XX....a..f.....|......=====######.a....",
                "=====================BB=====~~~~============##########======",
                "#####################BB#####wwww############################",
                "############################################################",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
                "R..v......v....v...........v.....v.........v.........v.....R",
                "R..........................................................R",
                "R..........................................................R",
                "R...........................................C.........7....R",
                "R.......................RRR.............MMM................R",
                "R..ggg..5...............RRR.....................k..........R",
                "R..ggg.c.e..............RRR.........b...........k..........R",
                "R.V....p.e..........b...RRR.........b.........C.k..........R",
                "rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr.....rrrrrrrrrrrrrrrrrrrrr",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.....RRRRRRRRRRRRRRRRRRRRR",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.....RRRRRRRRRRRRRRRRRRRRR",
            },
            new[]
            {
                new DialogueSpot('1', "pro_01_refugio", DialogueTrigger.Who.Nil),
                new DialogueSpot('2', "pro_02_cuaderno", DialogueTrigger.Who.Nil, DialogueTrigger.Activation.AlInteractuar),
                new DialogueSpot('3', "pro_03_avi_roc", DialogueTrigger.Who.Nil, DialogueTrigger.Activation.AlInteractuar),
                new DialogueSpot('6', "pro_06_ruido_radio", DialogueTrigger.Who.Nil),
                new DialogueSpot('5', "pro_05_terminal", DialogueTrigger.Who.Volt, DialogueTrigger.Activation.AlInteractuar),
                new DialogueSpot('7', "pro_07_primer_contacto", DialogueTrigger.Who.Volt),
            }),

            new LevelDefinition("Capítulo 1 · Bosc de les Arrels / Coves Humides", new[]
            {
                "......................................................................",
                "......................................................................",
                "..................................................---.................",
                "......................................................................",
                ".............................---...........---........................",
                "...............T......................................L...............",
                ".......L.......|.......................BB.............................",
                "...............|.......3..............BBBB......C...........9.......E.",
                ".N.1......f....|....a.............a..BBBBB......a....f......C...a.....",
                "=========.......===============.....======.....=======================",
                "#########.......###############.....######.....#######################",
                "######################################################################",
                "######################################################################",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
                "R....v.........v.........hhhhhhhh........v..........v...........ggg..R",
                "R........................t......t...................................gR",
                "R........................t......t.........................7.....ggg..R",
                "R.......................................MMM......................g...R",
                "R..........2..........b...........C...............kk.................R",
                "R..ggg..............RR..........................m.kk.........e.......R",
                "R.V.ggg........~~~~~RR........5.c......~~~~~~~..m............e.......R",
                "rrrrrrrrrrrrrrrwwwwwrrrrrrrrrrrrrrrrrrrwwwwwwwrrrrr.....rrrrrrrrrrrrrr",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.....RRRRRRRRRRRRRR",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.....RRRRRRRRRRRRRR",
            },
            new[]
            {
                new DialogueSpot('1', "c1_01_bosque_seco", DialogueTrigger.Who.Nil),
                new DialogueSpot('2', "c1_02_cueva_inundada", DialogueTrigger.Who.Volt),
                new DialogueSpot('3', "c1_03_desprendimiento", DialogueTrigger.Who.Nil),
                new DialogueSpot('5', "c1_05_primer_cargador", DialogueTrigger.Who.Volt, DialogueTrigger.Activation.AlInteractuar),
                new DialogueSpot('7', "c1_07_invernadero", DialogueTrigger.Who.Volt),
                new DialogueSpot('9', "c1_10_hoguera", DialogueTrigger.Who.Nil),
            }),
        };
    }
}
