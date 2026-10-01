namespace Features.TooltipModule.Scripts {
    public readonly struct TooltipContent {
        public string Title { get; }
        public string Description { get; }
        public string ActionHint { get; }

        public TooltipContent(string title, string description, string actionHint) {
            Title = title;
            Description = description;
            ActionHint = actionHint;
        }
    }
}
