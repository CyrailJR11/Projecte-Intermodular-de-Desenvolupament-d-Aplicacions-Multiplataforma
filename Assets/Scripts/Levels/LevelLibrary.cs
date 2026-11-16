using EntreDosMons.Dialogue;

namespace EntreDosMons.Levels
{
    // Los niveles del juego, escritos como texto. Cada carácter es una casilla de 32×32 (ver LevelLegend).
    // La fila de arriba del texto es la de arriba del nivel. Todas las filas de un nivel deben medir lo mismo.
    // Arriba está el mundo de Nil (el bosque) y, a partir de la fila toda de 'R', el de Volt (la estación).
    //
    // Cada nivel tiene tres partes:
    //   1) Map: el mapa.
    //   2) Wiring: la "capa de cables". Una letra encima de una fuente (palanca p, terminal G, placa o, cristal k)
    //      y la misma letra encima de un receptor (compuerta D, montacargas H, agua) los conecta.
    //   3) Channels: qué hace cada cable (lo enseña el validador) y Dialogues: qué diálogo va en cada número.
    //
    // Después de cambiar un mapa: menú Entre Dos Mons > Validar niveles (o los tests EditMode) comprueba que se
    // puede terminar, que cada objeto sirve para algo y que Nil y Volt se necesitan.
    public static class LevelLibrary
    {
        public static readonly LevelDefinition[] Levels =
        {
            new LevelDefinition("Prólogo · El refugio y la estación",
            new[]
            {
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                ".....................................C..L....^^...a.f....E..",
                ".................f..............::==========================",
                "................BBBB............::##########################",
                "................BBBB............::##########################",
                ".N.1.2.3.L.X....BBBB.a6.o...af..HH##########################",
                "============================================================",
                "############################################################",
                "############################################################",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
                "....v.....v......v......D.....v..v.............v.....v......",
                "........................D...................................",
                "........................D...................................",
                "........................D...................................",
                "..ggg...................D................hhh................",
                "..ggg...................D...................................",
                ".V.5..c..b.........b....D...^^^^^^^^..c...G...7..b.......E..",
                "rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
            },
            new[]   // capa de cables
            {
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "........................b.......aa..........................",
                "............................................................",
                "............................................................",
                "............................................................",
                "............................................................",
                "........................b...................................",
                "........................b...................................",
                "........................b...................................",
                "........................b...................................",
                "........................b...................................",
                "........................b...................................",
                "........................b.................a.................",
                "............................................................",
                "............................................................",
            },
            new[]
            {
                new ChannelInfo('a', "Terminal de Volt → da corriente al montacargas de Nil"),
                new ChannelInfo('b', "Placa de servicio (Nil la pisa) → abre la compuerta de la cueva de Volt"),
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

            new LevelDefinition("Capítulo 1 · Bosc de les Arrels / Coves Humides",
            new[]
            {
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "...........................P........................C.9..L....f......E..",
                "...............................................::=======================",
                "...............................................::#######################",
                "...................D..a........................::#######################",
                "...................D.BB........................::#######################",
                ".N.1f.............3DpBB............X.......a...HH#######################",
                "=====....==~~~~~~=======^^^^^^^^=======D================================",
                "#####====##wwwwww######################.################################",
                "#######################################.################################",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR.RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
                "...v...^^^^^^^^...........v....v...........D.v......v.......v.....v.....",
                "...........................................D............ggg.............",
                "...........................................D............ggg.............",
                "...........................................D.............G..............",
                "...........................................D..........eMMMM.............",
                "....hhh......................hhh...........D..........e.................",
                "...........................................D..........e.................",
                ".V.2.G..~~~~~~...c5....b......G........o...D..7.b.b...e........c.....E..",
                "rrrrrrrrwwwwwwrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr",
                "RRRRRRRRwwwwwwRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
                "RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR",
            },
            new[]   // capa de cables
            {
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "...................e....................................................",
                "...................e....................................................",
                "...................ea..........................dd.......................",
                ".......................................b................................",
                "........................................................................",
                "........................................................................",
                "........................................................................",
                "...........................................c............................",
                "...........................................c............................",
                "...........................................c............................",
                "...........................................c.............d..............",
                "...........................................c............................",
                "...........................................c............................",
                "...........................................c............................",
                ".....e..aaaaaa................b........c...c............................",
                "........aaaaaa..........................................................",
                "........aaaaaa..........................................................",
                "........................................................................",
            },
            new[]
            {
                new ChannelInfo('e', "Terminal de Volt → abre la verja eléctrica de la estación en el bosque"),
                new ChannelInfo('a', "Válvula del desvío (Nil) → el río deja de entrar en la montaña y se vacía la galería inundada"),
                new ChannelInfo('b', "Terminal de Volt → abre la trampilla del bosque (la caja cae por el pozo)"),
                new ChannelInfo('c', "Placa bajo el pozo (peso de la caja) → abre la compuerta del invernadero"),
                new ChannelInfo('d', "Generador de Volt → da corriente al montacargas de Nil"),
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
