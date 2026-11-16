namespace EntreDosMons.Levels
{
    // Nombres de los prefabs de Assets/Resources/Prefabs. El LevelBuilder instancia uno por cada objeto del mapa.
    public static class PrefabIds
    {
        public const string Folder = "Prefabs";   // dentro de Resources

        public const string Nil = "Nil";
        public const string Volt = "Volt";
        public const string Box = "Caja";
        public const string Lever = "Valvula";
        public const string Terminal = "Terminal";
        public const string Plate = "PlacaPresion";
        public const string Crystal = "Cristal";
        public const string Door = "Compuerta";
        public const string Lift = "Montacargas";
        public const string Charger = "Cargador";
        public const string Checkpoint = "Hoguera";
        public const string Exit = "Salida";
        public const string Swing = "CuerdaPendulo";
        public const string Spikes = "Pinchos";

        public static readonly string[] All =
        {
            Nil, Volt, Box, Lever, Terminal, Plate, Crystal, Door, Lift, Charger, Checkpoint, Exit, Swing, Spikes,
        };
    }
}
