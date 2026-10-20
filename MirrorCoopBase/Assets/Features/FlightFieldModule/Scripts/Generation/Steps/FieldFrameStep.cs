using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.MatrixModule.Scripts.MatrixFactory;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Frame: a rectangle on the flight plane, length = A -> B in cells + margins behind A and B (at least the side
    // margins on each side of the axis), width = the configured field width, the A -> B axis along the middle row.
    // A cruise start far off the axis (the ship takes off at an angle to it) widens the field so the side margin
    // stays beside it for the turn.
    public sealed class FieldFrameStep : IFieldGenerationStep {
        private const float MIN_AXIS_LENGTH = 0.01f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IMatrixFactory _matrixFactory;

        public FieldFrameStep(FlightFieldConfiguration flightFieldConfiguration, IMatrixFactory matrixFactory) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _matrixFactory = matrixFactory;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            true;

        public void Execute(FieldGenerationContext context) {
            FieldRequest request = context.Request;
            FieldFrame frame = CreateFrame(request);
            context.Frame = frame;
            context.Cells = _matrixFactory.Create(frame.LengthCells, frame.WidthCells, default(FieldCell));
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
            int minSide = _flightFieldConfiguration.SideMarginCells * 2 + 1;
            int fittedLength = Mathf.Max(1, Mathf.CeilToInt(distance / cellSize)) + _flightFieldConfiguration.MarginBehindACells + _flightFieldConfiguration.MarginBehindBCells;
            int length = Mathf.Max(fittedLength, minSide);
            int width = MakeOdd(Mathf.Max(_flightFieldConfiguration.FieldWidthCells, minSide, CruiseStartWidth(request, forward, cellSize)));
            int cellsBehindA = _flightFieldConfiguration.MarginBehindACells + (length - fittedLength) / 2;
            Vector3 stationAOnPlane = new(request.StationA.x, request.CruiseStart.y, request.StationA.z);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 origin = stationAOnPlane - forward * (cellsBehindA * cellSize + cellSize / 2f) - right * (width / 2 * cellSize + cellSize / 2f);
            return new FieldFrame(origin, forward, cellSize, length, width);
        }

        // The width that keeps the side margin beside the cruise start on both sides of the axis.
        private int CruiseStartWidth(FieldRequest request, Vector3 forward, float cellSize) {
            Vector3 offset = request.CruiseStart - request.StationA;
            float sideways = Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(Vector3.up, forward)));
            return (Mathf.CeilToInt(sideways / cellSize) + _flightFieldConfiguration.SideMarginCells) * 2 + 1;
        }

        // An odd width puts the A -> B axis on the middle row with the same cells on each side.
        private static int MakeOdd(int cells) =>
            cells % 2 == 0 ? cells + 1 : cells;
    }
}
