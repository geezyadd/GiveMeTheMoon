#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.FieldAgentModule.Scripts.Client;
using Tests.Coop.Shared;
using Tests.Shared;

namespace Tests.Coop.Client {
    // The client watches the host's field agents (scenario field-agents): in the cruise, on the host's command, it
    // waits until it shows the host's flock and reports every agent it shows with its position and network time,
    // capturing the screen.
    public sealed class CoopFieldAgentsClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string SCREENSHOT = "field-agents";

        private string _screenshot;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ReportAgentsCoroutine();
            yield return WaitForLandingCoroutine(LOOP);
        }

        private IEnumerator ReportAgentsCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_AGENTS);
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            yield return WaitForCoroutine(() => shown.ShownCount >= CoopFlightSteps.FIELD_AGENT_COUNT, $"{CoopFlightSteps.FIELD_AGENT_COUNT} field agents shown");
            yield return CaptureScreenCoroutine(SCREENSHOT, path => _screenshot = path);
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.AGENTS_SEEN);
            observation.Message = CoopFieldAgentView.Capture().Format();
            observation.Screenshot = _screenshot;
            Report(observation);
        }
    }
}
#endif
