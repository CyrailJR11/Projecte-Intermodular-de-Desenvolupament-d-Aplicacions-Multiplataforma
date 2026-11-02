# Paquete de arranque · Entre Dos Mons (Unity 6, URP 2D)

Copia la carpeta `Assets/` dentro de tu proyecto (se mezcla con la tuya) y usa el menú **Entre Dos Mons**:

1. **Entre Dos Mons > 1. Crear tiles y paletas**: crea un Tile por cada PNG de `Assets/Art/Tiles` y dos paletas
   (`Assets/Art/Palettes/Exterior` y `Coves`). Ábrelas en *Window > 2D > Tile Palette*.
2. **Entre Dos Mons > 2. Preparar escena de prueba**: crea Grid con 3 Tilemaps (Fondo, Suelo con colisión, Decoracion),
   un suelo de prueba, la cámara pixel perfect (32 PPU, 640×360), Nil, Volt, el cambio de personaje y el gestor de diálogos.
   Úsalo una vez por escena (si lo repites, duplica los objetos).

Controles: A/D o flechas · Espacio/W saltar · Q cambiar Nil ↔ Volt · F invertir la gravedad (solo Volt) · E interactuar.

## Contenido
- `Art/Tiles/`: 32 tiles de 32×32 (ext_* bosque, cova_* cuevas). Los sólidos chocan; los demás son decoración.
- `Art/Characters/Nil` (4 fotogramas) y `Volt` (6), sacados de vuestros sprites a su tamaño real (32×32, pivot centro).
- `Editor/EntreDosMonsTileImporter.cs`: importa todo como pixel art (PPU 32, Point, sin compresión).
- `Editor/EntreDosMonsSetup.cs`: el menú de arriba.
- `Scripts/Player/`: `PlayerController2D`, `SimpleSpriteAnimator`, `CharacterSwitcher`, `CameraFollow2D`, `Checkpoint`, `GameInput`.
- `Scripts/Dialogue/` + `Resources/Dialogues/`: sistema de diálogos (ver su LEEME en `unity/dialogos`).

Los scripts no se han podido abrir en Unity desde aquí (los de jugador sí se han compilado con un compilador de C#);
si sale algún error en la consola, pásalo tal cual.
