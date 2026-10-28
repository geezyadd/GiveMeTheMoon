#if UNITY_INCLUDE_TESTS
using System;
using Mirror;
using Mirror.RemoteCalls;

namespace Tests.Coop.Host {
    // Counts on the server the commands a player's weapon sends while it fires (Mirror's diagnostics of every received
    // message): the hit reports (WeaponHolderSync.CmdReportHits) and the trigger / aim updates (CmdSetAim). The weapon
    // path batches the hits at most once per network tick and sends no message per bullet.
    public sealed class CoopWeaponCommandCounter : IDisposable {
        private const string HIT_REPORT_COMMAND = "CmdReportHits";
        private const string AIM_COMMAND = "CmdSetAim";

        private readonly uint _netId;

        public int HitReports { get; private set; }
        public int AimUpdates { get; private set; }

        public CoopWeaponCommandCounter(uint playerNetId) {
            _netId = playerNetId;
            NetworkDiagnostics.InMessageEvent += OnMessageReceived;
        }

        public void Dispose() =>
            NetworkDiagnostics.InMessageEvent -= OnMessageReceived;

        private void OnMessageReceived(NetworkDiagnostics.MessageInfo info) {
            if (info.message is not CommandMessage command || command.netId != _netId)
                return;

            RemoteCallDelegate function = RemoteProcedureCalls.GetDelegate(command.functionHash);
            string name = function?.Method.Name ?? string.Empty;
            if (name.Contains(HIT_REPORT_COMMAND))
                HitReports++;
            else if (name.Contains(AIM_COMMAND))
                AimUpdates++;
        }
    }
}
#endif
