using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EntreDosMons.Levels;
using EntreDosMons.Mechanics;

namespace EntreDosMons.Tests
{
    // Pruebas con físicas de verdad (Window > General > Test Runner > PlayMode). Cada prueba monta un trocito de
    // nivel por código, pone a Nil o a Volt y los mueve con ScriptedInput, como si alguien pulsara las teclas.
    public class MechanicsPlayTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Prueba");
            WorldMap.Clear();
            SignalNetwork.Reset(new[] { new ChannelInfo('a', "prueba") });
            // Suelo de 20 casillas en y = 0 (capa Suelo).
            Solid(new Vector2(10f, -0.5f), new Vector2(20f, 1f));
        }

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        GameObject Spawn(string id, Vector3 position)
        {
            GameObject go = PrefabRecipes.Create(id, _ => null);
            go.transform.SetParent(root.transform, false);
            go.transform.position = position;
            return go;
        }

        void Solid(Vector2 center, Vector2 size)
        {
            var go = new GameObject("Suelo");
            go.layer = LayerMask.NameToLayer("Suelo");
            go.transform.SetParent(root.transform, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        PlayerCharacter Character(string id, float x, out ScriptedInput input)
        {
            var p = Spawn(id, new Vector3(x, 0.5f, 0f)).GetComponent<PlayerCharacter>();
            input = new ScriptedInput();
            p.Input = input;
            return p;
        }

        [UnityTest]
        public IEnumerator NilEmpujaLaCajaYSeQuedaCentradaEnSuCasilla()
        {
            Character(PrefabIds.Nil, 3.5f, out ScriptedInput input);
            Transform box = Spawn(PrefabIds.Box, new Vector3(5.5f, 0.5f, 0f)).transform;
            yield return new WaitForSeconds(0.3f);

            input.Horizontal = 1f;
            yield return new WaitForSeconds(1.2f);
            input.Horizontal = 0f;
            yield return new WaitForSeconds(0.6f);

            Assert.Greater(box.position.x, 6.4f, "La caja no se ha movido.");
            float offset = box.position.x - Mathf.Floor(box.position.x);
            Assert.AreEqual(0.5f, offset, 0.02f, "La caja no ha quedado en el centro de una casilla.");
        }

        [UnityTest]
        public IEnumerator VoltNoPuedeEmpujarLaCaja()
        {
            PlayerCharacter volt = Character(PrefabIds.Volt, 3.5f, out ScriptedInput input);
            Transform box = Spawn(PrefabIds.Box, new Vector3(5.5f, 0.5f, 0f)).transform;
            yield return new WaitForSeconds(0.3f);

            input.Horizontal = 1f;
            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(5.5f, box.position.x, 0.01f, "Volt ha movido la caja y no tiene fuerza para ello.");
            Assert.Less(volt.transform.position.x, 5f);
        }

        [UnityTest]
        public IEnumerator UnaCajaEncimaDeLaPlacaAbreLaCompuerta()
        {
            Spawn(PrefabIds.Plate, new Vector3(8.5f, 0.5f, 0f)).GetComponent<SignalSource>().Connect('a');
            var door = Spawn(PrefabIds.Door, new Vector3(15.5f, 0.5f, 0f)).GetComponent<Door>();
            door.Build(3, false);
            door.Connect('a', Vector3.up * 3f);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(door.IsOpen);

            Spawn(PrefabIds.Box, new Vector3(8.5f, 2f, 0f));   // cae encima de la placa
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(door.IsOpen, "La placa con la caja encima no ha abierto la compuerta.");
        }

        [UnityTest]
        public IEnumerator NilAbreLaValvulaYVoltNo()
        {
            var lever = Spawn(PrefabIds.Lever, new Vector3(6.5f, 0.5f, 0f)).GetComponent<Lever>();
            lever.Connect('a');
            PlayerCharacter volt = Character(PrefabIds.Volt, 6f, out ScriptedInput voltInput);
            yield return new WaitForSeconds(0.2f);
            voltInput.PressInteract();
            yield return null;
            yield return null;
            Assert.IsFalse(SignalNetwork.IsActive('a'), "Volt ha podido girar la válvula.");

            volt.gameObject.SetActive(false);
            Character(PrefabIds.Nil, 6f, out ScriptedInput nilInput);
            yield return new WaitForSeconds(0.2f);
            nilInput.PressInteract();
            yield return null;
            yield return null;
            Assert.IsTrue(SignalNetwork.IsActive('a'), "Nil no ha podido girar la válvula.");
        }

        [UnityTest]
        public IEnumerator ElTerminalDeVoltSubeElMontacargasConNilEncima()
        {
            var terminal = Spawn(PrefabIds.Terminal, new Vector3(2.5f, 0.5f, 0f)).GetComponent<Terminal>();
            terminal.Connect('a');
            var lift = Spawn(PrefabIds.Lift, new Vector3(10f, 0.5f, 0f)).GetComponent<Lift>();
            lift.Build(2, 3);
            lift.Connect('a', Vector3.up);
            PlayerCharacter nil = Character(PrefabIds.Nil, 10f, out _);
            nil.transform.position = new Vector3(10f, 1.6f, 0f);
            Character(PrefabIds.Volt, 2.4f, out ScriptedInput voltInput);
            yield return new WaitForSeconds(0.3f);

            voltInput.PressInteract();
            yield return new WaitForSeconds(2.5f);

            Assert.IsTrue(lift.IsUp);
            Assert.Greater(lift.transform.position.y, 3.3f, "El montacargas no ha subido.");
            Assert.Greater(nil.transform.position.y, 4f, "Nil no ha subido con el montacargas.");
        }
    }
}
