using System;
using System.Collections.Generic;

namespace Features.CharacterMovableModule.Scripts.Models {
    public sealed class PlayerControlBlockModel {
        private readonly HashSet<object> _owners = new();

        public bool IsBlocked => _owners.Count > 0;

        public event Action OnChanged;

        public void Request(object owner) {
            if (_owners.Add(owner) == false)
                return;

            OnChanged?.Invoke();
        }

        public void Release(object owner) {
            if (_owners.Remove(owner) == false)
                return;

            OnChanged?.Invoke();
        }
    }
}
