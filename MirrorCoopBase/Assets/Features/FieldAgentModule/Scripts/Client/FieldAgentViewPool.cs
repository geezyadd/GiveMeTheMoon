using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Features.FieldAgentModule.Scripts.Client {
    // Agent looks from each kind's view prefab under one root, made on first need (injected, so a kind's own parts
    // get their dependencies) and kept for reuse; the root and all looks go with the session.
    public sealed class FieldAgentViewPool : IFieldAgentViewPool, IDisposable {
        private const string ROOT_NAME = "FieldAgentViews";

        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly IInstantiator _instantiator;
        private readonly Dictionary<byte, Stack<FieldAgentView>> _free = new();
        private Transform _root;

        public FieldAgentViewPool(FieldAgentConfiguration fieldAgentConfiguration, IInstantiator instantiator) {
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _instantiator = instantiator;
        }

        public void Dispose() {
            if (_root != null)
                Object.Destroy(_root.gameObject);

            _free.Clear();
        }

        public FieldAgentView Take(byte kindIndex, int agentId) {
            FieldAgentProfile profile = _fieldAgentConfiguration.Profiles[kindIndex];
            FieldAgentView view = TryTakeFree(kindIndex, out FieldAgentView free) ? free : _instantiator.InstantiatePrefabForComponent<FieldAgentView>(profile.ViewPrefab, GetOrCreateRoot());
            view.Begin(profile, agentId);
            return view;
        }

        public void Return(byte kindIndex, FieldAgentView view) {
            if (view == null)
                return;

            view.End();
            if (_free.TryGetValue(kindIndex, out Stack<FieldAgentView> stack) == false) {
                stack = new Stack<FieldAgentView>();
                _free.Add(kindIndex, stack);
            }

            stack.Push(view);
        }

        // A pooled look can be gone with its scene; such entries are skipped.
        private bool TryTakeFree(byte kindIndex, out FieldAgentView view) {
            view = null;
            if (_free.TryGetValue(kindIndex, out Stack<FieldAgentView> stack) == false)
                return false;

            while (stack.Count > 0) {
                view = stack.Pop();
                if (view != null)
                    return true;
            }

            return false;
        }

        private Transform GetOrCreateRoot() {
            if (_root == null)
                _root = new GameObject(ROOT_NAME).transform;

            return _root;
        }
    }
}
