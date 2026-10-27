using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Shooting;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class WeaponCatalogIndexServiceTests {
        private const string FIRST_ID = "first";
        private const string SECOND_ID = "second";
        private const string UNKNOWN_ID = "unknown";

        private readonly List<Object> _created = new List<Object>();

        private WeaponCatalogIndexService _service;

        [SetUp]
        public void SetUp() {
            WeaponCatalog catalog = Create<WeaponCatalog>();
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty weapons = serialized.FindProperty("_weapons");
            weapons.arraySize = 2;
            weapons.GetArrayElementAtIndex(0).objectReferenceValue = CreateWeapon(FIRST_ID);
            weapons.GetArrayElementAtIndex(1).objectReferenceValue = CreateWeapon(SECOND_ID);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _service = new WeaponCatalogIndexService(catalog);
        }

        [TearDown]
        public void TearDown() {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);

            _created.Clear();
        }

        [Test]
        public void WhenAnotherCopyOfACatalogWeaponIsAsked_ThenItGetsThatWeaponsIndex() =>
            Assert.AreEqual(1, _service.GetIndex(CreateWeapon(SECOND_ID)), "A copy of the definition (a player build's scene item) did not find its catalog index.");

        [Test]
        public void WhenAWeaponOutsideTheCatalogIsAsked_ThenItThrows() =>
            Assert.Throws<KeyNotFoundException>(() => _service.GetIndex(CreateWeapon(UNKNOWN_ID)));

        private WeaponDefinition CreateWeapon(string id) {
            WeaponDefinition weapon = Create<WeaponDefinition>();
            SerializedObject serialized = new SerializedObject(weapon);
            serialized.FindProperty("_id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        private T Create<T>() where T : ScriptableObject {
            T created = ScriptableObject.CreateInstance<T>();
            _created.Add(created);
            return created;
        }
    }
}
