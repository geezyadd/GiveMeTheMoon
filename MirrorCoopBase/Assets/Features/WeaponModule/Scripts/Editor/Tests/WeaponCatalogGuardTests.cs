using System;
using System.Linq;
using System.Text.RegularExpressions;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Guards weapons as data: every weapon is one definition in the WeaponCatalog with an id and all its parts, and
    // the weapon code never branches on a concrete weapon.
    public sealed class WeaponCatalogGuardTests {
        private const string WEAPON_CODE_FOLDER = "Assets/Features/WeaponModule/Scripts";
        private const string TESTS_FOLDER = "/Editor/Tests/";
        private const string CONCRETE_WEAPON_PATTERN =
            @"switch\s*\([^)]*([Ww]eapon[Tt]ype|\.Id)\)"
            + @"|\.Id\s*[!=]=\s*"""
            + @"|\bis\s+(AutoFireMode|HitscanDelivery|DamageHitEffect)";

        private WeaponCatalog _weaponCatalog;

        [SetUp]
        public void SetUp() =>
            _weaponCatalog = AddressableConfigurationLoader.Load<WeaponCatalog>(Address.Configurations.WeaponCatalog_Default);

        [Test]
        public void WhenWeaponCatalogIsRead_ThenEveryIdIsSetAndUnique() {
            string[] ids = _weaponCatalog.Weapons.Select(weapon => weapon.Id).ToArray();

            Assert.IsFalse(ids.Any(string.IsNullOrEmpty), $"A weapon in {_weaponCatalog.name} has no id.");
            string[] duplicates = ids.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
            Assert.IsEmpty(duplicates, $"Weapon ids used twice, a hit would resolve the wrong weapon: {string.Join(", ", duplicates)}");
        }

        [Test]
        public void WhenWeaponCatalogIsRead_ThenEveryWeaponHasAllItsParts() {
            foreach (WeaponDefinition weapon in _weaponCatalog.Weapons) {
                Assert.IsNotNull(weapon.FireMode, $"{weapon.name} has no fire mode.");
                Assert.IsNotNull(weapon.Delivery, $"{weapon.name} has no shot delivery.");
                Assert.IsNotEmpty(weapon.HitEffects, $"{weapon.name} has no hit effect.");
                Assert.IsFalse(weapon.HitEffects.Any(effect => effect == null), $"{weapon.name} has an empty hit effect.");
                Assert.IsNotNull(weapon.FeelProfile, $"{weapon.name} has no feel profile.");
            }
        }

        [Test]
        public void WhenWeaponCatalogIsValidatedAtStartup_ThenItPasses() =>
            Assert.DoesNotThrow(new WeaponCatalogValidator(_weaponCatalog).Initialize);

        [Test]
        public void WhenTwoWeaponsShareAnId_ThenStartupValidationFails() {
            WeaponDefinition weapon = _weaponCatalog.Weapons[0];
            WeaponCatalog brokenCatalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            SerializedObject serialized = new SerializedObject(brokenCatalog);
            SerializedProperty weapons = serialized.FindProperty("_weapons");
            weapons.arraySize = 2;
            weapons.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
            weapons.GetArrayElementAtIndex(1).objectReferenceValue = weapon;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.Throws<InvalidOperationException>(new WeaponCatalogValidator(brokenCatalog).Initialize);
            UnityEngine.Object.DestroyImmediate(brokenCatalog);
        }

        [Test]
        public void WhenWeaponSourcesAreScanned_ThenNoneBranchesOnAConcreteWeapon() {
            string[] offenders = SourceScan.FindFilesMatching(WEAPON_CODE_FOLDER, TESTS_FOLDER, BranchesOnAConcreteWeapon);

            Assert.IsEmpty(offenders, $"Weapon behaviour comes from parts in the definition, not from checks on a concrete weapon: {string.Join(", ", offenders)}");
        }

        private static bool BranchesOnAConcreteWeapon(string source) =>
            Regex.IsMatch(source, CONCRETE_WEAPON_PATTERN);
    }
}
