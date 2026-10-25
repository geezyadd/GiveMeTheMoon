using System;
using System.Collections.Generic;

namespace Features.CameraModule.Scripts.Models {
    // While any owner holds the lock the mouse does not turn the view: the hand uses it (a held lever).
    public sealed class CameraLookLockModel {
        private readonly HashSet<object> _owners = new();

        public bool IsLocked =>
            _owners.Count > 0;

        public void Request(object owner) {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            _owners.Add(owner);
        }

        public void Release(object owner) {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            _owners.Remove(owner);
        }
    }
}
