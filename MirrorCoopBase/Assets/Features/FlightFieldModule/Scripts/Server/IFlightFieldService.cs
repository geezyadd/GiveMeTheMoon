using System.Threading.Tasks;
using Features.FlightFieldModule.Scripts.Model;
using Features.GameCoreModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Server {
    public interface IFlightFieldService {
        // Picks the seed on the calling (main) thread and generates on a worker thread.
        public Task<FlightField> ServerStartGeneration(FlightLaunch launch);
        public void ServerPublish(FlightField field);
        public void ServerClear();
    }
}
