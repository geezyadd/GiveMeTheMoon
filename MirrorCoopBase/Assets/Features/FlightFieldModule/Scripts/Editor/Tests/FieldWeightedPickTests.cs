using System.Collections.Generic;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The shared weighted roll replaced the structure picker's and the cluster layout's own loops: for the same seed
    // they must still pick the same structures and rocks, or every field and its look would change
    // (FieldClusterLayout.PickPrefab is internal and now only returns the shared pick's prefab).
    public sealed class FieldWeightedPickTests {
        private const int SEEDS = 500;
        private const int PICKS_PER_SEED = 20;

        private FieldStructureCatalog _catalog;
        private FieldRandomFactory _randomFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _catalog = FlightFieldEditorCompositionFactory.LoadConfiguration<FieldStructureCatalog>(Address.Configurations.FieldStructureCatalog_Default);
            _randomFactory = new FieldRandomFactory();
        }

        [Test]
        public void WhenStructuresArePickedByWeight_ThenTheSequenceIsTheSameAsTheFormerStructurePickers() {
            List<FieldStructureDefinition> obstacles = new();
            foreach (FieldStructureDefinition structure in _catalog.Structures) {
                if (structure.IsRandomObstacle)
                    obstacles.Add(structure);
            }

            FieldStructurePicker picker = new();
            IFieldRandom random = _randomFactory.Create(0);
            IFieldRandom formerRandom = _randomFactory.Create(0);
            for (int seed = 1; seed <= SEEDS; seed++) {
                random.Reseed(seed);
                formerRandom.Reseed(seed);
                for (int i = 0; i < PICKS_PER_SEED; i++)
                    Assert.AreSame(FormerPickByWeight(obstacles, formerRandom), picker.PickByWeight(obstacles, random), $"Seed {seed}, pick {i}: the structure differs.");
            }
        }

        [Test]
        public void WhenClusterRocksArePickedByWeight_ThenTheSequenceIsTheSameAsTheFormerClusterLayouts() {
            IFieldRandom random = _randomFactory.Create(0);
            IFieldRandom formerRandom = _randomFactory.Create(0);
            foreach (FieldStructureDefinition structure in _catalog.Structures) {
                if (!structure.HasClusterRecipe)
                    continue;

                for (int seed = 1; seed <= SEEDS; seed++) {
                    random.Reseed(seed);
                    formerRandom.Reseed(seed);
                    for (int i = 0; i < PICKS_PER_SEED; i++) {
                        Assert.AreSame(FormerPickPrefab(structure.ClusterRecipe, formerRandom), FieldWeightedPick.Pick(structure.ClusterRecipe.Rocks, random).Prefab,
                            $"{structure.Id}, seed {seed}, pick {i}: the rock differs.");
                    }
                }
            }
        }

        // The loop FieldStructurePicker had before the shared pick.
        private static FieldStructureDefinition FormerPickByWeight(IReadOnlyList<FieldStructureDefinition> structures, IFieldRandom random) {
            float totalWeight = 0f;
            foreach (FieldStructureDefinition structure in structures)
                totalWeight += structure.Weight;

            float roll = random.Value() * totalWeight;
            foreach (FieldStructureDefinition structure in structures) {
                roll -= structure.Weight;
                if (roll < 0f)
                    return structure;
            }

            return structures[structures.Count - 1];
        }

        // The loop FieldClusterLayout.PickPrefab had before the shared pick.
        private static FieldRockVisual FormerPickPrefab(FieldClusterRecipe recipe, IFieldRandom random) {
            float total = 0f;
            foreach (FieldClusterRockOption option in recipe.Rocks)
                total += option.Weight;

            float pick = random.Value() * total;
            foreach (FieldClusterRockOption option in recipe.Rocks) {
                pick -= option.Weight;
                if (pick < 0f)
                    return option.Prefab;
            }

            return recipe.Rocks[recipe.Rocks.Count - 1].Prefab;
        }
    }
}
