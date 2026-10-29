#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.Parcel {
    // The steps the parcel tests share: the synced parcel model, the crate a parcel's entry names, waits for a parcel
    // state, every rise of the crew wallet, and the screenshots in the workspace's parcel folder.
    public sealed class ParcelTestSteps {
        public const string LOG_PREFIX = "[Parcel] ";

        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/parcel";
        private const float PROGRESS_LOG_SECONDS = 5f;

        private readonly List<long> _walletRises = new();
        private long _lastBalance;

        public IReadOnlyParcelsModel Parcels => SessionQueries.Resolve<IReadOnlyParcelsModel>();
        public IReadOnlyList<long> WalletRises => _walletRises;

        public static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }

        public bool TryGetOnlyEntry(out ParcelEntry entry) {
            entry = default;
            if (Parcels.Entries.Count != 1)
                return false;

            entry = Parcels.Entries[0];
            return true;
        }

        public ParcelEntry GetOnlyEntry() {
            Assert.IsTrue(TryGetOnlyEntry(out ParcelEntry entry), $"Expected one parcel, the model has {Parcels.Entries.Count}.");
            return entry;
        }

        public ParcelCrate GetCrate(in ParcelEntry entry) {
            Assert.IsTrue(NetworkClient.spawned.TryGetValue(entry.NetId, out NetworkIdentity identity), $"Parcel crate {entry.NetId} is not spawned on the host.");
            return identity.GetComponent<ParcelCrate>();
        }

        public bool IsAboard(ParcelCrate crate, ShipBase ship) =>
            SessionQueries.Resolve<IParcelAboardService>().IsAboard(crate.Position, ship);

        public IEnumerator WaitForStateCoroutine(ParcelState state, float timeoutSeconds, IEnumerator whileWaiting = null) {
            float deadline = Time.time + timeoutSeconds;
            float nextLog = Time.time;
            while (TryGetOnlyEntry(out ParcelEntry entry) == false || entry.State != state) {
                if (Time.time >= nextLog) {
                    nextLog = Time.time + PROGRESS_LOG_SECONDS;
                    Debug.Log($"{LOG_PREFIX}waiting for {state}: {DescribeParcels()}");
                }

                Assert.IsTrue(Time.time <= deadline, $"The parcel did not reach {state} in {timeoutSeconds} s ({DescribeParcels()}).");
                if (whileWaiting != null && whileWaiting.MoveNext())
                    yield return whileWaiting.Current;
                else
                    yield return null;
            }
        }

        // Every rise of the crew wallet from now on, one by one (ore the ship flies into pays too).
        public void StartRecordingWallet() {
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            _lastBalance = wallet.Balance;
            _walletRises.Clear();
            wallet.OnBalanceChanged += RecordBalance;
        }

        public void StopRecordingWallet() =>
            SessionQueries.Resolve<IReadOnlyWalletModel>().OnBalanceChanged -= RecordBalance;

        private void RecordBalance() {
            long balance = SessionQueries.Resolve<IReadOnlyWalletModel>().Balance;
            if (balance > _lastBalance)
                _walletRises.Add(balance - _lastBalance);

            _lastBalance = balance;
        }

        private string DescribeParcels() {
            string parcels = string.Empty;
            foreach (ParcelEntry entry in Parcels.Entries)
                parcels += $"{entry.NetId}:{entry.State} ";

            return parcels.Length == 0 ? "no parcels" : parcels.TrimEnd();
        }
    }
}
#endif
