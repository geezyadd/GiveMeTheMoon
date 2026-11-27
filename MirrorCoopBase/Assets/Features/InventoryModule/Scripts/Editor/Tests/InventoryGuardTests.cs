using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.InventoryModule.Scripts;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using NUnit.Framework;

namespace Features.InventoryModule.Editor.Tests {
    // Guards the inventory as data: every hand weapon and tool the shop sells can be stored and shows in a slot, no
    // ship module can, and the configuration loads with at least one slot and a slot view.
    public sealed class InventoryGuardTests {
        private ShopCatalog _shopCatalog;

        [SetUp]
        public void SetUp() =>
            _shopCatalog = AddressableConfigurationLoader.Load<ShopCatalog>(Address.Configurations.ShopCatalog_Default);

        [Test]
        public void WhenShopCatalogIsRead_ThenEveryHandWeaponItemCanBeStored() {
            Assert.IsNotEmpty(_shopCatalog.HandWeapons, $"{_shopCatalog.name} sells no hand weapon.");
            foreach (HandWeaponDefinition handWeapon in _shopCatalog.HandWeapons)
                AssertStorable(handWeapon.ItemPrefab.GetComponent<Grabbable>(), handWeapon.name);
        }

        [Test]
        public void WhenShopCatalogIsRead_ThenEveryToolItemCanBeStored() {
            Assert.IsNotEmpty(_shopCatalog.Tools, $"{_shopCatalog.name} sells no tool.");
            foreach (RepairToolDefinition tool in _shopCatalog.Tools)
                AssertStorable(tool.ItemPrefab.GetComponent<Grabbable>(), tool.name);
        }

        [Test]
        public void WhenModuleCatalogIsRead_ThenNoModuleItemCanBeStored() {
            ModuleCatalog moduleCatalog = AddressableConfigurationLoader.Load<ModuleCatalog>(Address.Configurations.ModuleCatalog_Default);
            foreach (ModuleDefinition module in moduleCatalog.Modules) {
                Grabbable item = module.ItemPrefab.GetComponent<Grabbable>();
                Assert.IsFalse(item.CanStoreInInventory, $"The item of the module {module.name} can be stored in the inventory; modules are carried in the hand only.");
            }
        }

        [Test]
        public void WhenInventoryConfigurationIsLoaded_ThenItHasSlotsAndASlotView() {
            InventoryConfiguration configuration = AddressableConfigurationLoader.Load<InventoryConfiguration>(Address.Configurations.InventoryConfiguration_Default);

            Assert.IsNotNull(configuration, "The inventory configuration does not load from its address.");
            Assert.GreaterOrEqual(configuration.SlotCount, 1, $"{configuration.name} has no slot.");
            Assert.IsNotNull(configuration.SlotViewPrefab, $"{configuration.name} has no slot view prefab.");
            Assert.Greater(configuration.ThrowSpeed, 0f, $"{configuration.name} throws nothing.");
        }

        private static void AssertStorable(Grabbable item, string definitionName) {
            Assert.IsNotNull(item, $"The item of {definitionName} has no {nameof(Grabbable)}.");
            Assert.IsTrue(item.CanStoreInInventory, $"The item of {definitionName} cannot be stored in the inventory.");
            Assert.IsNotNull(item.GetComponent<IInventoryItemDisplay>(), $"The item of {definitionName} has no inventory display, its slot would show its object name.");
        }
    }
}
