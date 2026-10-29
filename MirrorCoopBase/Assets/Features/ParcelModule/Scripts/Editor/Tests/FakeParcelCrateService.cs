using System;
using System.Collections.Generic;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // Takes a crate out of the server model and the entries, as the real one does, without spawning anything.
    public sealed class FakeParcelCrateService : IParcelCrateService {
        private readonly ParcelServerModel _parcelServerModel;
        private readonly FakeParcelEntryService _entries;

        public List<uint> Destroyed { get; } = new();

        public FakeParcelCrateService(ParcelServerModel parcelServerModel, FakeParcelEntryService entries) {
            _parcelServerModel = parcelServerModel;
            _entries = entries;
        }

        public ParcelServerRecord ServerSpawn(Vector3 position, Quaternion rotation, long reward, int destinationLoop) =>
            throw new NotSupportedException();

        public void ServerDestroy(uint netId) {
            Destroyed.Add(netId);
            _parcelServerModel.Remove(netId);
            _entries.ServerRemove(netId);
        }
    }
}
