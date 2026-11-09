# Paquete de arranque · Entre Dos Mons (Unity 6, URP 2D)

Copia la carpeta `Assets/` dentro de tu proyecto (se mezcla con la tuya) y usa el menú **Entre Dos Mons**:

1. **Entre Dos Mons > 1. Crear tiles y paletas**: crea un Tile por cada PNG de `Assets/Art/Tiles` y dos paletas
   (`Assets/Art/Palettes/Exterior` y `Coves`). Ábrelas en *Window > 2D > Tile Palette*.
2. **Entre Dos Mons > 2. Preparar escena de prueba**: crea Grid con 3 Tilemaps (Fondo, Suelo con colisión, Decoracion),
   un suelo de prueba, la cámara pixel perfect (32 PPU, 640×360), Nil, Volt, el cambio de personaje y el gestor de diálogos.
   Úsalo una vez por escena (si lo repites, duplica los objetos).

Controles: A/D o flechas · Espacio/W saltar · W/S trepar · Q cambiar Nil ↔ Volt · F invertir la gravedad (solo Volt, otra F para volver al suelo) · L linterna (Volt) · E interactuar · S + Espacio bajar de una plataforma de madera.

## Contenido
- `Art/Tiles/`: 32 tiles de 32×32 (ext_* bosque, cova_* cuevas). Los sólidos chocan; los demás son decoración.
- `Art/Characters/Nil` (4 fotogramas) y `Volt` (6), sacados de vuestros sprites a su tamaño real (32×32, pivot centro).
- `Editor/EntreDosMonsTileImporter.cs`: importa todo como pixel art (PPU 32, Point, sin compresión).
- `Editor/EntreDosMonsSetup.cs`: el menú de arriba.
- `Scripts/Player/`: `PlayerController2D`, `SimpleSpriteAnimator`, `CharacterSwitcher`, `CameraFollow2D`, `Checkpoint`, `GameInput`.
- `Scripts/Dialogue/` + `Resources/Dialogues/`: sistema de diálogos (ver su LEEME en `unity/dialogos`).

Los scripts no se han podido abrir en Unity desde aquí (los de jugador sí se han compilado con un compilador de C#);
si sale algún error en la consola, pásalo tal cual.

## Mecánicas (todo se genera por código desde el mapa del nivel)

Al darle a Play, el `LevelBuilder` añade a Nil y Volt los componentes que faltan y crea el HUD, así que no hay que tocar la escena.
Si añadís tiles nuevos, usad otra vez **Entre Dos Mons > 3** para refrescar la lista de tiles.

| Carácter | Qué es | Cómo funciona |
|---|---|---|
| `^` | Pinchos | Quitan una vida y devuelven al último checkpoint. Si tienen suelo encima cuelgan del techo (para Volt boca abajo). |
| `~` `w` | Agua | Nil nada (Saltar = brazada) y gasta **respiración**; sin aire pierde una vida. Volt no flota: se hunde, va lento y gasta mucha **batería**; sin batería en el agua, cortocircuito. |
| `T` `\|` | Cuerda vertical | Nil trepa con W/S y se suelta con Espacio. |
| `P` | Cuerda-péndulo | Nil se agarra al tocarla en el aire, se balancea con A/D, sube/baja con W/S y salta con Espacio. |
| `e` | Escalera | La trepan los dos. |
| `X` | Caja | Se empuja andando contra ella y sirve de escalón. |
| `p` + `D` | Palanca + compuerta | E junto a la palanca abre/cierra la compuerta más cercana (columna de `D`). |
| `c` | Cargador | Recarga la batería de Volt y guarda su checkpoint. |
| `-` | Plataforma de madera | Se atraviesa desde abajo; abajo + Espacio para bajar. |
| `k` `b` `L` | Cristal, setas, farola | Dan un poco de luz. |

- **Vidas**: 3 por personaje. Sin vidas se reinicia el nivel.
- **Gravedad de Volt**: F en el suelo lo sube al techo y F en el techo lo baja (cuesta 5 % de batería). Sin batería cae al suelo.
- **Batería y linterna**: la linterna (Light 2D de URP en cono, L para apagarla) gasta batería; con poca carga alumbra menos y parpadea. Las cuevas están casi a oscuras.
- **Sonidos**: `Assets/Resources/Audio/` (8-bit generados por código). Se pueden cambiar por otros con el mismo nombre.
