using UnityEngine;

namespace EntreDosMons.UI
{
    // Tipografía oficial del juego (la misma que la presentación):
    //   · Pixel: Press Start 2P para nombres, títulos y números. Usadla en tamaños múltiplos de 8 para que se vea nítida.
    //   · Body / Bold: Atkinson Hyperlegible para frases largas (diálogos, avisos).
    // Los TTF están en Assets/Resources/Fonts. Si faltan, se usa la fuente de Unity para que nada se rompa.
    public static class GameFonts
    {
        static Font pixel, body, bold, fallback;

        public static Font Pixel => pixel != null ? pixel : (pixel = Load("Fonts/PressStart2P-Regular"));
        public static Font Body => body != null ? body : (body = Load("Fonts/AtkinsonHyperlegible-Regular"));
        public static Font Bold => bold != null ? bold : (bold = Load("Fonts/AtkinsonHyperlegible-Bold"));

        static Font Load(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f != null) return f;
            if (fallback == null) fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fallback;
        }
    }
}
