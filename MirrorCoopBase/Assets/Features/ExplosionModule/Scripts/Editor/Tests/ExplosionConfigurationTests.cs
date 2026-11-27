using System.Collections.Generic;
using Features.ExplosionModule.Configuration;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ExplosionModule.Editor.Tests {
    // A preset is found in the configuration by itself or by a copy of it (a player build loads one copy per bundle),
    // and an unknown one is not.
    public sealed class ExplosionConfigurationTests {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown() {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void WhenACopyOfAListedPresetIsLookedUp_ThenItsIndexIsFound() {
            ExplosionPreset small = CreatePreset("ExplosionSmall");
            ExplosionPreset large = CreatePreset("ExplosionLarge");
            ExplosionConfiguration configuration = CreateConfiguration(small, large);
            ExplosionPreset copy = Object.Instantiate(large);
            copy.name = large.name;
            _created.Add(copy);

            Assert.AreEqual(1, configuration.IndexOf(copy));
            Assert.AreEqual(0, configuration.IndexOf(small));
        }

        [Test]
        public void WhenAnUnlistedPresetIsLookedUp_ThenNoIndexIsFound() {
            ExplosionConfiguration configuration = CreateConfiguration(CreatePreset("ExplosionSmall"));

            Assert.AreEqual(-1, configuration.IndexOf(CreatePreset("ExplosionHuge")));
            Assert.AreEqual(-1, configuration.IndexOf(null));
        }

        private ExplosionPreset CreatePreset(string name) {
            ExplosionPreset preset = ScriptableObject.CreateInstance<ExplosionPreset>();
            preset.name = name;
            _created.Add(preset);
            return preset;
        }

        private ExplosionConfiguration CreateConfiguration(params ExplosionPreset[] presets) {
            ExplosionConfiguration configuration = ScriptableObject.CreateInstance<ExplosionConfiguration>();
            _created.Add(configuration);
            SerializedObject serialized = new SerializedObject(configuration);
            SerializedProperty list = serialized.FindProperty("_presets");
            list.arraySize = presets.Length;
            for (int i = 0; i < presets.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = presets[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return configuration;
        }
    }
}
