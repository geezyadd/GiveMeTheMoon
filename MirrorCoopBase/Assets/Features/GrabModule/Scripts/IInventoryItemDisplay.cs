using UnityEngine;

namespace Features.GrabModule.Scripts {
    // How a storable item shows in an inventory slot: an icon where it has one, its name otherwise.
    public interface IInventoryItemDisplay {
        public Sprite InventoryIcon { get; }
        public string InventoryName { get; }
    }
}
