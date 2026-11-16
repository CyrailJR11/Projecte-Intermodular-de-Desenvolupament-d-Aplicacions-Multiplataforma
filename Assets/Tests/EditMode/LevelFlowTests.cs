using System.Linq;
using NUnit.Framework;
using EntreDosMons.Levels;
using EntreDosMons.Levels.Validation;

namespace EntreDosMons.Tests
{
    // Pruebas de flujo de los niveles (Window > General > Test Runner > EditMode).
    // El solucionador simula a Nil y a Volt casilla a casilla (andar, saltar, nadar, trepar, empujar cajas,
    // accionar válvulas y terminales, pisar placas, invertir la gravedad...) y busca una forma de terminar el nivel.
    public class LevelFlowTests
    {
        static LevelDefinition[] Levels => LevelLibrary.Levels;

        [TestCaseSource(nameof(Levels))]
        public void ElNivelEsCorrectoYSePuedeTerminar(LevelDefinition level)
        {
            ValidationReport report = LevelValidator.Validate(level);
            Assert.IsTrue(report.Ok, report.ToText());
            Assert.IsTrue(report.Result.Solved, "El simulador no encuentra cómo terminar " + level.Name);
        }

        // Si se quita cualquier fuente (válvula, terminal, placa, cristal), cualquier receptor o cualquier caja,
        // el nivel ya NO se puede terminar: no sobra ningún objeto.
        [TestCaseSource(nameof(Levels))]
        public void CadaObjetoEsImprescindible(LevelDefinition level)
        {
            var model = new LevelModel(level);
            foreach (LevelModel.Source s in model.Sources)
            {
                var options = new LevelSolver.Options { AnalyseSoftlocks = false };
                options.DisabledSources.Add(s.Id);
                Assert.IsFalse(new LevelSolver(model, options).Run().Solved, $"Sin {s.Label} el nivel se puede terminar igual: sobra.");
            }
            foreach (int box in model.Boxes)
            {
                var options = new LevelSolver.Options { AnalyseSoftlocks = false };
                options.RemovedBoxes.Add(box);
                Assert.IsFalse(new LevelSolver(model, options).Run().Solved, $"Sin la caja de ({model.XOf(box)},{model.RowOf(box)}) el nivel se puede terminar igual.");
            }
        }

        // Los dos personajes se necesitan: en la solución hay acciones de Nil que abren camino a Volt y al revés.
        [TestCaseSource(nameof(Levels))]
        public void NilYVoltSeNecesitan(LevelDefinition level)
        {
            LevelSolver.Result result = LevelSolver.Solve(level, new LevelSolver.Options { AnalyseSoftlocks = false });
            Assert.IsTrue(result.Solved);
            var model = new LevelModel(level);
            bool nilHelpsVolt = false, voltHelpsNil = false;
            foreach (int id in result.UsedSources)
            {
                LevelModel.Source s = model.Sources[id];
                World from = model.WorldOf(s.Row);
                foreach (LevelModel.Receiver r in model.Receivers.Where(r => r.Channel == s.Channel))
                {
                    World to = model.WorldOf(model.RowOf(r.Cells[0]));
                    nilHelpsVolt |= from == World.Exterior && to == World.Cueva;
                    voltHelpsNil |= from == World.Cueva && to == World.Exterior;
                }
            }
            Assert.IsTrue(nilHelpsVolt, "Nil no abre nada en la cueva de Volt.");
            Assert.IsTrue(voltHelpsNil, "Volt no abre nada en el bosque de Nil.");
        }

        // Volt sin terminales o Nil sin válvulas: imposible. Comprueba que el solucionador no hace trampas.
        [Test]
        public void SinLosTerminalesDeVoltElPrologoEsImposible()
        {
            var model = new LevelModel(LevelLibrary.Levels[0]);
            var options = new LevelSolver.Options { AnalyseSoftlocks = false };
            foreach (LevelModel.Source s in model.Sources.Where(s => s.Kind == LevelLegend.Terminal)) options.DisabledSources.Add(s.Id);
            Assert.IsFalse(new LevelSolver(model, options).Run().Solved);
        }
    }
}
