using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts.MatrixFactory;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Frame: a square field on the flight plane, side = A -> B in cells + margins behind A and B (at least the side
    // margins on each side of the axis), the A -> B axis along the middle row.
    public sealed class FieldFrameStep : IFieldGenerationStep {
        private const float MIN_AXIS_LENGTH = 0.01f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IMatrixFactory _matrixFactory;

        public FieldFrameStep(FlightFieldConfiguration flightFieldConfiguration, IMatrixFactory matrixFactory) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _matrixFactory = matrixFactory;
        }

        public void Execute(FieldGenerationContext context) {
            FieldRequest request = context.Request;
            FieldFrame frame = CreateFrame(request);
            context.Frame = frame;
            context.Cells = _matrixFactory.Create(frame.SideCells, frame.SideCells, default(FieldCell));
            context.StationACell = frame.WorldToCell(request.StationA);
            context.StationBCell = frame.WorldToCell(request.StationB);
            context.CruiseStartCell = frame.WorldToCell(request.CruiseStart);
        }

        private FieldFrame CreateFrame(FieldRequest request) {
            float cellSize = _flightFieldConfiguration.CellSize;
            Vector3 axis = request.StationB - request.StationA;
            axis.y = 0f;
            float distance = axis.magnitude;
            Vector3 forward = distance > MIN_AXIS_LENGTH ? axis / distance : Vector3.forward;
            int fittedSide = Mathf.Max(1, Mathf.CeilToInt(distance / cellSize)) + _flightFieldConfiguration.MarginBehindACells + _flightFieldConfiguration.MarginBehindBCells;
            int side = Mathf.Max(fittedSide, _flightFieldConfiguration.SideMarginCells * 2 + 1);
            int cellsBehindA = _flightFieldConfiguration.MarginBehindACells + (side - fittedSide) / 2;
            Vector3 stationAOnPlane = new(request.StationA.x, request.CruiseStart.y, request.StationA.z);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 origin = stationAOnPlane - forward * (cellsBehindA * cellSize + cellSize / 2f) - right * (side / 2 * cellSize + cellSize / 2f);
            return new FieldFrame(origin, forward, cellSize, side);
        }
    }
}
