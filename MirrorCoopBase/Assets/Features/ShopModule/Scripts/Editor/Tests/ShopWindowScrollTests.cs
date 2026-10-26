using System.Collections.Generic;
using Features.ShopModule.Scripts.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.ShopModule.Editor.Tests {
    // The shipped shop window lists its items in a scroll view: a list longer than the panel stacks its rows without
    // overlap, scrolls with the mouse wheel and the scrollbar, and the panel fits the windows canvas (1920 x 1080 for
    // every 16:9 screen, 1280 x 720 included).
    public sealed class ShopWindowScrollTests {
        // ShopWindow.prefab.
        private const string SHOP_WINDOW_PREFAB_GUID = "005f48e17a7e79141aec3adc7c6019b1";
        private const int ITEM_COUNT = 12;
        private const float CANVAS_WIDTH = 1920f;
        private const float CANVAS_HEIGHT = 1080f;
        // One mouse wheel notch as the UI input module reports it (InputSystemUIInputModule.scrollDeltaPerTick).
        private const float WHEEL_NOTCH = 6f;
        private const float TOLERANCE = 0.5f;
        private const string PANEL_NAME = "ShopPanel";

        private GameObject _canvas;
        private ScrollRect _scroll;
        private List<ShopItemRow> _rows;

        [SetUp]
        public void SetUp() {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(SHOP_WINDOW_PREFAB_GUID));
            Assert.IsNotNull(prefab, "The shop window prefab was not found.");
            _canvas = new GameObject(nameof(ShopWindowScrollTests), typeof(RectTransform), typeof(Canvas));
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT);
            GameObject window = Object.Instantiate(prefab, _canvas.transform, false);
            ShopView view = window.GetComponentInChildren<ShopView>(true);
            view.SetItems(CreateItems());
            _scroll = window.GetComponentInChildren<ScrollRect>(true);
            Assert.IsNotNull(_scroll, "The shop window has no scroll view.");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)window.transform);
            Canvas.ForceUpdateCanvases();
            _rows = FindActiveRows(_scroll.content);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_canvas);

        [Test]
        public void WhenManyItemsAreListed_ThenEveryItemHasItsOwnRowWithoutOverlap() {
            Assert.AreEqual(ITEM_COUNT, _rows.Count, "Not every item has a row.");
            for (int i = 1; i < _rows.Count; i++)
                Assert.LessOrEqual(WorldRect(_rows[i]).yMax, WorldRect(_rows[i - 1]).yMin + TOLERANCE, $"Row {i} overlaps row {i - 1}.");

            Assert.Greater(_scroll.content.rect.height, _scroll.viewport.rect.height, "A long list does not grow the scroll content past the viewport.");
        }

        [Test]
        public void WhenTheWheelTurnsDownOverARow_ThenTheListScrollsDown() {
            float before = _scroll.content.anchoredPosition.y;

            ExecuteEvents.ExecuteHierarchy(_rows[0].gameObject, new PointerEventData(null) { scrollDelta = Vector2.down * WHEEL_NOTCH }, ExecuteEvents.scrollHandler);

            Assert.Greater(_scroll.content.anchoredPosition.y, before, "The wheel over an item row does not scroll the list.");
        }

        [Test]
        public void WhenTheScrollbarIsDraggedToTheBottom_ThenTheLastItemIsInView() {
            Assert.IsNotNull(_scroll.verticalScrollbar, "The shop's scroll view has no vertical scrollbar.");

            _scroll.verticalScrollbar.value = 0f;

            Rect last = WorldRect(_rows[^1]);
            Rect viewport = WorldRect(_scroll.viewport);
            Assert.GreaterOrEqual(last.yMin, viewport.yMin - TOLERANCE, "The last item stays below the viewport.");
            Assert.LessOrEqual(last.yMax, viewport.yMax + TOLERANCE, "The last item is scrolled past the viewport's top.");
        }

        [Test]
        public void WhenTheWindowIsOnTheWindowsCanvas_ThenThePanelFitsInside() {
            Rect canvas = WorldRect(_canvas.transform);
            Rect panel = WorldRect(_scroll.transform.parent);
            Assert.AreEqual(PANEL_NAME, _scroll.transform.parent.name, "The scroll view is not a child of the shop panel.");
            Assert.IsTrue(canvas.Contains(panel.min) && canvas.Contains(panel.max), $"The shop panel {panel} does not fit the canvas {canvas}.");
        }

        private static IReadOnlyList<ShopItemDisplay> CreateItems() {
            List<ShopItemDisplay> items = new(ITEM_COUNT);
            for (int i = 0; i < ITEM_COUNT; i++)
                items.Add(new ShopItemDisplay($"Item {i}", "Module: Engine", "Flight speed +10%", "A test item with a description.", "100", null));

            return items;
        }

        private static List<ShopItemRow> FindActiveRows(Transform content) {
            List<ShopItemRow> rows = new();
            foreach (Transform child in content) {
                if (child.gameObject.activeSelf && child.TryGetComponent(out ShopItemRow row))
                    rows.Add(row);
            }

            return rows;
        }

        private static Rect WorldRect(Component component) {
            Vector3[] corners = new Vector3[4];
            ((RectTransform)component.transform).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
