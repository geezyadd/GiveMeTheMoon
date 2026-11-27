using System.Threading.Tasks;

namespace Features.LoadingScreenModule.Contracts {
    // For async callers: await the fade instead of passing an action.
    public static class LoadingScreenServiceExtensions {
        public static Task FadeInAsync(this ILoadingScreenService loadingScreen) {
            var completion = new TaskCompletionSource<bool>();
            loadingScreen.FadeIn(() => completion.TrySetResult(true));
            return completion.Task;
        }
    }
}
