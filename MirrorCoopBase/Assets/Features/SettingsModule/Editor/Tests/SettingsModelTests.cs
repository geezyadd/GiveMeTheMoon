using System;
using System.Collections.Generic;
using Features.SettingsModule.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.SettingsModule.Editor.Tests {
    public sealed class SettingsModelTests {
        private const string TOGGLE_KEY = "test.toggle";
        private const string SLIDER_KEY = "test.slider";
        private const string CHOICE_KEY = "test.choice";
        private const string RESOLUTION_KEY = "test.resolution";

        private readonly List<Object> _createdObjects = new();
        private SettingsCatalog _catalog;

        [SetUp]
        public void SetUp() {
            _catalog = CreateCatalog(
                CreateToggle(TOGGLE_KEY, true),
                CreateSlider(SLIDER_KEY, 0f, 100f, 5f, 50f),
                CreateChoice(CHOICE_KEY, "medium", "low", "medium", "high"),
                CreateResolution(RESOLUTION_KEY));
        }

        [TearDown]
        public void TearDown() {
            foreach (Object createdObject in _createdObjects)
                Object.DestroyImmediate(createdObject);

            _createdObjects.Clear();
        }

        [Test]
        public void Model_StartsFromDefinitionDefaults() {
            SettingsModel model = new(_catalog);

            Assert.That(model.GetToggle(TOGGLE_KEY), Is.True);
            Assert.That(model.GetSlider(SLIDER_KEY), Is.EqualTo(50f));
            Assert.That(model.GetChoice(CHOICE_KEY), Is.EqualTo("medium"));
            Assert.That(model.GetChoice(RESOLUTION_KEY), Is.Empty);
            Assert.That(model.Definitions, Has.Count.EqualTo(4));
        }

        [Test]
        public void Set_RaisesEventOnlyOnRealChange() {
            SettingsModel model = new(_catalog);
            List<string> changedKeys = new();
            model.OnValueChanged += changedKeys.Add;

            model.SetToggle(TOGGLE_KEY, true);
            model.SetSlider(SLIDER_KEY, 50f);
            model.SetChoice(CHOICE_KEY, "medium");
            Assert.That(changedKeys, Is.Empty);

            model.SetToggle(TOGGLE_KEY, false);
            model.SetSlider(SLIDER_KEY, 75f);
            model.SetChoice(CHOICE_KEY, "high");
            Assert.That(changedKeys, Is.EqualTo(new[] { TOGGLE_KEY, SLIDER_KEY, CHOICE_KEY }));
        }

        [Test]
        public void SetSlider_ClampsAndSnapsToStep() {
            SettingsModel model = new(_catalog);

            model.SetSlider(SLIDER_KEY, 63f);
            Assert.That(model.GetSlider(SLIDER_KEY), Is.EqualTo(65f));

            model.SetSlider(SLIDER_KEY, 140f);
            Assert.That(model.GetSlider(SLIDER_KEY), Is.EqualTo(100f));

            model.SetSlider(SLIDER_KEY, -10f);
            Assert.That(model.GetSlider(SLIDER_KEY), Is.EqualTo(0f));
        }

        [TestCase(0f, 0f)]
        [TestCase(50f, 1f)]
        [TestCase(100f, 2f)]
        public void GetSliderScaleToDefault_IsValueOverDefault(float value, float expected) {
            SettingsModel model = new(_catalog);

            model.SetSlider(SLIDER_KEY, value);

            Assert.That(model.GetSliderScaleToDefault(SLIDER_KEY), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ResolutionOptionId_FormatsAndParses() {
            Assert.That(ResolutionOptionId.Format(1920, 1080), Is.EqualTo("1920x1080"));
            Assert.That(ResolutionOptionId.TryParse("2560x1440", out int width, out int height), Is.True);
            Assert.That((width, height), Is.EqualTo((2560, 1440)));
            Assert.That(ResolutionOptionId.TryParse(string.Empty, out _, out _), Is.False);
            Assert.That(ResolutionOptionId.TryParse("1920", out _, out _), Is.False);
            Assert.That(ResolutionOptionId.TryParse("ax1080", out _, out _), Is.False);
        }

        [Test]
        public void SetChoice_RejectsUnknownOption() {
            SettingsModel model = new(_catalog);

            Assert.Throws<ArgumentException>(() => model.SetChoice(CHOICE_KEY, "ultra"));
            Assert.That(model.GetChoice(CHOICE_KEY), Is.EqualTo("medium"));
        }

        [Test]
        public void SetChoice_ResolutionAcceptsAnyId() {
            SettingsModel model = new(_catalog);

            model.SetChoice(RESOLUTION_KEY, "1920x1080");

            Assert.That(model.GetChoice(RESOLUTION_KEY), Is.EqualTo("1920x1080"));
        }

        [Test]
        public void ResetToDefault_RestoresOnlyThatSetting() {
            SettingsModel model = new(_catalog);
            model.SetToggle(TOGGLE_KEY, false);
            model.SetSlider(SLIDER_KEY, 10f);
            List<string> changedKeys = new();
            model.OnValueChanged += changedKeys.Add;

            model.ResetToDefault(SLIDER_KEY);

            Assert.That(model.GetSlider(SLIDER_KEY), Is.EqualTo(50f));
            Assert.That(model.GetToggle(TOGGLE_KEY), Is.False);
            Assert.That(changedKeys, Is.EqualTo(new[] { SLIDER_KEY }));
        }

        [Test]
        public void CopyValuesFrom_TakesEveryValue_AndHasSameValuesSeesIt() {
            SettingsModel live = new(_catalog);
            SettingsModel draft = new(_catalog);
            draft.SetToggle(TOGGLE_KEY, false);
            draft.SetSlider(SLIDER_KEY, 80f);
            draft.SetChoice(CHOICE_KEY, "low");
            Assert.That(live.HasSameValues(draft), Is.False);
            Assert.That(live.HasSameValue(draft, RESOLUTION_KEY), Is.True);

            live.CopyValuesFrom(draft);

            Assert.That(live.HasSameValues(draft), Is.True);
            Assert.That(live.GetSlider(SLIDER_KEY), Is.EqualTo(80f));
            Assert.That(live.GetChoice(CHOICE_KEY), Is.EqualTo("low"));
        }

        [Test]
        public void Access_WithWrongKindOrUnknownKey_Throws() {
            SettingsModel model = new(_catalog);

            Assert.Throws<InvalidOperationException>(() => model.GetSlider(TOGGLE_KEY));
            Assert.Throws<InvalidOperationException>(() => model.SetToggle(CHOICE_KEY, true));
            Assert.Throws<KeyNotFoundException>(() => model.GetToggle("test.missing"));
            Assert.That(model.Contains("test.missing"), Is.False);
            Assert.That(model.Contains(TOGGLE_KEY), Is.True);
        }

        [Test]
        public void Model_WithDuplicateKey_Throws() {
            SettingsCatalog catalog = CreateCatalog(CreateToggle(TOGGLE_KEY, true), CreateToggle(TOGGLE_KEY, false));

            Assert.Throws<ArgumentException>(() => _ = new SettingsModel(catalog));
        }

        private SettingsCatalog CreateCatalog(params SettingDefinition[] definitions) {
            SettingsCatalog catalog = Track(ScriptableObject.CreateInstance<SettingsCatalog>());
            SerializedObject serialized = new(catalog);
            SerializedProperty settings = serialized.FindProperty("_settings");
            settings.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
                settings.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private ToggleSettingDefinition CreateToggle(string key, bool defaultValue) {
            ToggleSettingDefinition definition = CreateDefinition<ToggleSettingDefinition>(key, out SerializedObject serialized);
            serialized.FindProperty("_defaultValue").boolValue = defaultValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private SliderSettingDefinition CreateSlider(string key, float minimum, float maximum, float step, float defaultValue) {
            SliderSettingDefinition definition = CreateDefinition<SliderSettingDefinition>(key, out SerializedObject serialized);
            serialized.FindProperty("_minimum").floatValue = minimum;
            serialized.FindProperty("_maximum").floatValue = maximum;
            serialized.FindProperty("_step").floatValue = step;
            serialized.FindProperty("_defaultValue").floatValue = defaultValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private ChoiceSettingDefinition CreateChoice(string key, string defaultOptionId, params string[] optionIds) {
            ChoiceSettingDefinition definition = CreateDefinition<ChoiceSettingDefinition>(key, out SerializedObject serialized);
            serialized.FindProperty("_defaultOptionId").stringValue = defaultOptionId;
            SerializedProperty options = serialized.FindProperty("_options");
            options.arraySize = optionIds.Length;
            for (int index = 0; index < optionIds.Length; index++) {
                SerializedProperty option = options.GetArrayElementAtIndex(index);
                option.FindPropertyRelative("_id").stringValue = optionIds[index];
                option.FindPropertyRelative("_label").stringValue = optionIds[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private ResolutionSettingDefinition CreateResolution(string key) {
            ResolutionSettingDefinition definition = CreateDefinition<ResolutionSettingDefinition>(key, out SerializedObject serialized);
            serialized.FindProperty("_defaultOptionId").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private TDefinition CreateDefinition<TDefinition>(string key, out SerializedObject serialized)
            where TDefinition : SettingDefinition {
            TDefinition definition = Track(ScriptableObject.CreateInstance<TDefinition>());
            serialized = new SerializedObject(definition);
            serialized.FindProperty("_key").stringValue = key;
            serialized.FindProperty("_label").stringValue = key;
            return definition;
        }

        private T Track<T>(T createdObject) where T : Object {
            _createdObjects.Add(createdObject);
            return createdObject;
        }
    }
}
