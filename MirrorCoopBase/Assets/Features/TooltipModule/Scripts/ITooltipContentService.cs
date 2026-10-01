namespace Features.TooltipModule.Scripts {
    public interface ITooltipContentService {
        public TooltipContent Build(InteractionTooltip tooltip, bool isReady);
    }
}
