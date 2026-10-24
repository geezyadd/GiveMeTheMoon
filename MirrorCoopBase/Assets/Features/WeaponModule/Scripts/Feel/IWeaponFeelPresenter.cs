using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Feel;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // A weapon's feel as a view on a weapon model: the holder binds it to its profile and floor once, drives it every
    // frame and says whether the local player looks through it. Any weapon model can bring its own.
    public interface IWeaponFeelPresenter : IWeaponFeel {
        public Transform Muzzle { get; }

        public void Bind(WeaponFeelProfile profile, IWeaponFeelFloor floor);

        public void SetViewOwned(bool isViewOwned);

        public void Advance(float deltaTime);
    }
}
