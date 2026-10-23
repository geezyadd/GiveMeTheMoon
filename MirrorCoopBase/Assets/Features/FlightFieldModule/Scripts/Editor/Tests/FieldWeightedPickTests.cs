using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using NUnit.Framework;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The shared weighted roll replaced the giant packer's and the cluster layout's own loops: for the same seed they
    // must still pick the same obstacles and rocks, or every field and its look would change
    // (FieldClusterLayout.PickPrefab is internal and now only returns the shared pick's prefab).
    public sealed class FieldWeightedPickTests {
        private const int SEEDS = 500;
        private const int PICKS_PER_SEED = 20;

        private FieldStructureCatalog _catalog;
        private FlightFieldConfiguration _configuration;
        private FieldRandomFactory _randomFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _catalog = AddressableConfigurationLoader.Load<FieldStructureCatalog>(Address.Configurations.FieldStructureCatalog_Default);
            _configuration = AddressableConfigurationLoader.Load<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default);
            _randomFactory = new FieldRandomFactory();
        }

        [Test]
        public void WhenObstacleOptionsArePickedByWeight_ThenTheSequenceIsTheSameAsTheFormerPackers() {
            IFieldRandom random = _randomFactory.Create(0);
            IFieldRandom formerRandom = _randomFactory.Create(0);
            foreach (FieldDifficultyProfile profile in FieldTestProfiles.Corridors(_configuration)) {
                for (int seed = 1; seed <= SEEDS; seed++) {
                    random.Reseed(seed);
                    formerRandom.Reseed(seed);
                    for (int i = 0; i < PICKS_PER_SEED; i++) {
                        Assert.AreSame(FormerPickByWeight(profile.RidgeObstacles, formerRandom), FieldWeightedPick.Pick(profile.RidgeObstacles, random),
                            $"{profile.Id}, seed {seed}, pick {i}: the obstacle differs.");
                    }
                }
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

        // The loop FieldGiantPacker had before the shared pick.
        private static FieldObstacleOption FormerPickByWeight(IReadOnlyList<FieldObstacleOption> options, IFieldRandom random) {
            float total = 0f;
            foreach (FieldObstacleOption option in options)
                total += option.Weight;

            float roll = random.Value() * total;
            foreach (FieldObstacleOption option in options) {
                roll -= option.Weight;
                if (roll < 0f)
                    return option;
            }

            return options[options.Count - 1];
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
