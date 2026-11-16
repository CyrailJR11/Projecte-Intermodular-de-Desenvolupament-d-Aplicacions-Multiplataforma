using NUnit.Framework;
using UnityEngine;
using EntreDosMons.Levels;
using EntreDosMons.Mechanics;

namespace EntreDosMons.Tests
{
    // Pruebas de la arquitectura: prefabs, herencia y red de señales.
    public class ArchitectureTests
    {
        [TearDown]
        public void Clean()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.transform.parent == null && go.name.StartsWith("T_")) Object.DestroyImmediate(go);
        }

        // Cada prefab (o su receta, si aún no se ha guardado) tiene los componentes que el LevelBuilder espera.
        [TestCase(PrefabIds.Nil, typeof(NilCharacter))]
        [TestCase(PrefabIds.Volt, typeof(VoltCharacter))]
        [TestCase(PrefabIds.Box, typeof(PushableBox))]
        [TestCase(PrefabIds.Lever, typeof(Lever))]
        [TestCase(PrefabIds.Terminal, typeof(Terminal))]
        [TestCase(PrefabIds.Plate, typeof(PressurePlate))]
        [TestCase(PrefabIds.Crystal, typeof(Crystal))]
        [TestCase(PrefabIds.Door, typeof(Door))]
        [TestCase(PrefabIds.Lift, typeof(Lift))]
        [TestCase(PrefabIds.Charger, typeof(BatteryCharger))]
        public void ElPrefabTieneSuComponente(string id, System.Type component)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabIds.Folder + "/" + id);
            GameObject go = prefab != null ? Object.Instantiate(prefab) : PrefabRecipes.Create(id, _ => null);
            go.name = "T_" + id;
            Assert.IsNotNull(go.GetComponent(component), $"{id} no tiene {component.Name}");
        }

        // [Herencia] RequireComponent monta solos los componentes de cada personaje.
        [Test]
        public void CadaPersonajeTraeSusPiezas()
        {
            GameObject nil = PrefabRecipes.Create(PrefabIds.Nil, _ => null); nil.name = "T_nil";
            GameObject volt = PrefabRecipes.Create(PrefabIds.Volt, _ => null); volt.name = "T_volt";
            Assert.IsNotNull(nil.GetComponent<CharacterHealth>());
            Assert.IsNotNull(nil.GetComponent<NilBreath>());
            Assert.IsNull(nil.GetComponent<VoltBattery>());
            Assert.IsNotNull(volt.GetComponent<VoltBattery>());
            Assert.IsNotNull(volt.GetComponent<VoltFlashlight>());
            // [Polimorfismo] Los dos son PlayerCharacter y CharacterResource, cada uno a su manera.
            Assert.IsInstanceOf<PlayerCharacter>(nil.GetComponent<NilCharacter>());
            Assert.IsInstanceOf<CharacterResource>(volt.GetComponent<VoltBattery>());
        }

        [Test]
        public void LasHabilidadesSonComplementarias()
        {
            GameObject nilGo = PrefabRecipes.Create(PrefabIds.Nil, _ => null); nilGo.name = "T_nil";
            GameObject voltGo = PrefabRecipes.Create(PrefabIds.Volt, _ => null); voltGo.name = "T_volt";
            PlayerCharacter nil = nilGo.GetComponent<PlayerCharacter>(), volt = voltGo.GetComponent<PlayerCharacter>();
            Assert.IsTrue(nil.Has(CharacterAbility.FuerzaManual) && !volt.Has(CharacterAbility.FuerzaManual));
            Assert.IsTrue(nil.Has(CharacterAbility.Nadar) && !volt.Has(CharacterAbility.Nadar));
            Assert.IsTrue(volt.Has(CharacterAbility.ConexionDatos) && !nil.Has(CharacterAbility.ConexionDatos));
            Assert.IsTrue(volt.Has(CharacterAbility.Linterna) && !nil.Has(CharacterAbility.Linterna));
            Assert.AreNotEqual(nil.HomeWorld, volt.HomeWorld);
        }

        // Canal normal (O): basta una fuente. Canal con RequireAll (Y): hacen falta todas.
        [Test]
        public void LaRedDeSenalesHaceOyY()
        {
            SignalNetwork.Reset(new[] { new ChannelInfo('a', "O"), new ChannelInfo('b', "Y", requireAll: true) });
            var a1 = NewSource('a'); var a2 = NewSource('a');
            var b1 = NewSource('b'); var b2 = NewSource('b');
            var ra = new FakeReceiver('a'); var rb = new FakeReceiver('b');
            SignalNetwork.AddReceiver(ra);
            SignalNetwork.AddReceiver(rb);

            a1.Set(true);
            Assert.IsTrue(ra.Active);
            a2.Set(true); a1.Set(false);
            Assert.IsTrue(ra.Active);
            a2.Set(false);
            Assert.IsFalse(ra.Active);

            b1.Set(true);
            Assert.IsFalse(rb.Active, "Con una sola fuente un canal Y no se enciende.");
            b2.Set(true);
            Assert.IsTrue(rb.Active);
        }

        static TestSource NewSource(char channel)
        {
            var s = new GameObject("T_source").AddComponent<TestSource>();
            s.Connect(channel);
            return s;
        }

        class TestSource : SignalSource
        {
            public void Set(bool on) => SetActive(on);
            protected override void OnActiveChanged(bool on) { }
        }

        class FakeReceiver : ISignalReceiver
        {
            public FakeReceiver(char ch) { Channel = ch; }
            public char Channel { get; }
            public bool Active;
            public void OnSignal(bool active) => Active = active;
        }
    }
}
