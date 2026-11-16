using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using EntreDosMons.Mechanics;

namespace EntreDosMons.Levels
{
    // "Recetas" de los prefabs: cómo se monta cada objeto del juego (sprite, físicas, componentes).
    // Las usa la fábrica del editor (Entre Dos Mons > 4. Crear prefabs) para guardar los .prefab en
    // Assets/Resources/Prefabs, y el LevelBuilder como plan B si algún prefab todavía no existe.
    // Una vez creados, los prefabs se pueden retocar en Unity (velocidades, sprites, luces...) y el juego usa esos.
    public static class PrefabRecipes
    {
        // sprite("ext_caixa_fusta") → Sprite. Los fotogramas de los personajes: "Nil/nil_0", "Volt/volt_3"...
        public static GameObject Create(string id, Func<string, Sprite> sprite)
        {
            switch (id)
            {
                case PrefabIds.Nil: return Character<NilCharacter>("Nil", sprite, F(sprite, "Nil/nil", 0), F(sprite, "Nil/nil", 0, 1), F(sprite, "Nil/nil", 2));
                case PrefabIds.Volt: return Character<VoltCharacter>("Volt", sprite, F(sprite, "Volt/volt", 3), F(sprite, "Volt/volt", 0, 1, 2), F(sprite, "Volt/volt", 4));
                case PrefabIds.Box: return Box(sprite);
                case PrefabIds.Lever: return WithSprite<Lever>(id, sprite("ext_valvula"), 2).gameObject;
                case PrefabIds.Terminal:
                {
                    var t = WithSprite<Terminal>(id, sprite("cova_terminal"), 2);
                    AddLight(t.transform, "Pantalla", new Vector3(0f, 0.2f, 0f), new Color32(0xEE, 0x6C, 0x2F, 0xFF), 1.4f, 0.7f);
                    return t.gameObject;
                }
                case PrefabIds.Plate:
                {
                    var plate = WithSprite<PressurePlate>(id, sprite("placa_pressio"), 1, new Vector2(0.9f, 0.5f), new Vector2(0f, -0.25f));
                    return plate.gameObject;
                }
                case PrefabIds.Crystal:
                {
                    var c = WithSprite<Crystal>(id, sprite("cova_cristall_blau"), 2, new Vector2(0.8f, 0.8f), Vector2.zero);
                    AddLight(c.transform, "Brillo", Vector3.zero, new Color32(0x4B, 0x89, 0xA6, 0xFF), 2.5f, 0.3f);
                    return c.gameObject;
                }
                case PrefabIds.Door:
                {
                    var go = Root(id, "Suelo");
                    go.AddComponent<BoxCollider2D>();
                    go.AddComponent<Door>().SetSprites(sprite("cova_comporta"), sprite("cova_trampilla"));
                    return go;
                }
                case PrefabIds.Lift:
                {
                    var go = Root(id, "Suelo");
                    var body = go.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    go.AddComponent<BoxCollider2D>();
                    go.AddComponent<Lift>().SetSprite(sprite("ext_montacarregues"));
                    return go;
                }
                case PrefabIds.Charger:
                {
                    var ch = WithSprite<BatteryCharger>(id, sprite("cova_carregador_bateria"), 1, new Vector2(1.4f, 1.4f), Vector2.zero);
                    AddLight(ch.transform, "Luz", Vector3.zero, new Color32(0x62, 0xB5, 0x6B, 0xFF), 2.5f, 0.8f);
                    return ch.gameObject;
                }
                case PrefabIds.Checkpoint:
                {
                    var go = Root(id, null);
                    Trigger(go, new Vector2(1f, 4f), new Vector2(0f, 1.5f));
                    go.AddComponent<Checkpoint>();
                    AddLight(go.transform, "Brasa", Vector3.zero, new Color32(0xFF, 0x9E, 0x3D, 0xFF), 1.8f, 0.6f);
                    return go;
                }
                case PrefabIds.Exit:
                {
                    var go = Root(id, null);
                    Trigger(go, new Vector2(1f, 4f), new Vector2(0f, 1.5f));
                    go.AddComponent<LevelExit>();
                    AddLight(go.transform, "Luz salida", Vector3.zero, new Color32(0xF5, 0xF0, 0xD8, 0xFF), 2.5f, 0.6f);
                    return go;
                }
                case PrefabIds.Swing:
                {
                    var go = Root(id, null);
                    go.AddComponent<SwingRope>();
                    return go;
                }
                case PrefabIds.Spikes:
                {
                    var go = Root(id, null);
                    Trigger(go, new Vector2(0.8f, 0.45f), Vector2.zero);
                    go.AddComponent<Hazard>();
                    return go;
                }
            }
            Debug.LogError("Entre Dos Mons: no hay receta para el prefab " + id);
            return new GameObject(id);
        }

        // ---------- Recetas ----------

        static GameObject Character<T>(string name, Func<string, Sprite> sprite, Sprite[] idle, Sprite[] walk, Sprite[] air) where T : PlayerCharacter
        {
            var go = Root(name, "Jugador");
            go.tag = name;   // los diálogos distinguen a Nil y Volt por la etiqueta
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = idle.Length > 0 ? idle[0] : null;
            sr.sortingOrder = 10;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.5f, 0.9f);
            box.offset = new Vector2(0f, -0.05f);
            // [RequireComponent] añade solo lo demás: vidas, y respiración (Nil) o batería + linterna (Volt).
            go.AddComponent<T>();
            go.AddComponent<SimpleSpriteAnimator>().SetFrames(idle, walk, air);
            return go;
        }

        static GameObject Box(Func<string, Sprite> sprite)
        {
            var go = Root(PrefabIds.Box, "Suelo");   // se puede pisar
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite("ext_caixa_fusta");
            sr.sortingOrder = 1;
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 2f;
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.94f, 0.98f);
            col.sharedMaterial = new PhysicsMaterial2D("Caja sin rozamiento") { friction = 0f, bounciness = 0f };
            go.AddComponent<PushableBox>();
            return go;
        }

        // Objeto con el sprite en un hijo "Sprite" (así se puede mover el dibujo sin mover el objeto).
        // Si se pasa triggerSize, el collider (trigger) se pone ANTES del componente: los que llevan
        // [RequireComponent(typeof(Collider2D))] (placa, cristal, cargador) no se pueden añadir sin él,
        // porque Collider2D es abstracta y Unity no sabe cuál crear.
        static T WithSprite<T>(string id, Sprite s, int order, Vector2? triggerSize = null, Vector2 triggerOffset = default) where T : Component
        {
            var go = Root(id, null);
            var child = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            child.transform.SetParent(go.transform, false);
            child.sprite = s;
            child.sortingOrder = order;
            if (triggerSize.HasValue) Trigger(go, triggerSize.Value, triggerOffset);
            return go.AddComponent<T>();
        }

        static GameObject Root(string name, string layer)
        {
            var go = new GameObject(name);
            if (layer != null)
            {
                int l = LayerMask.NameToLayer(layer);
                if (l >= 0) go.layer = l;
            }
            return go;
        }

        static void Trigger(GameObject go, Vector2 size, Vector2 offset)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            box.offset = offset;
        }

        public static Light2D AddLight(Transform parent, string name, Vector3 localPosition, Color color, float radius, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.7f;
            return light;
        }

        static Sprite[] F(Func<string, Sprite> sprite, string prefix, params int[] frames)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (int i in frames)
            {
                Sprite s = sprite(prefix + "_" + i);
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }
    }
}
