using System;
using System.Collections;
using System.Collections.Generic;
using Features.CoroutineRunnerModule;
using Features.LoadingScreenModule.Contracts;
using UnityEngine;

namespace Features.LoadingScreenModule {
    public sealed class LoadingScreenService : ILoadingScreenService {
        private const float Covered = 1f;
        private const float Uncovered = 0f;

        private readonly ICoroutineRunner _coroutineRunner;
        private readonly LoadingScreenView _view;
        private readonly List<Action> _pending = new List<Action>();
        private Coroutine _fade;
        private float _target;

        public LoadingScreenService(ICoroutineRunner coroutineRunner, LoadingScreenView view) {
            _coroutineRunner = coroutineRunner;
            _view = view;
        }

        public bool IsCovered => _view.Alpha >= Covered;

        public void FadeIn(Action onComplete = null) =>
            Fade(Covered, _view.FadeInSeconds, 0f, onComplete);

        public void FadeOut(Action onComplete = null) =>
            Fade(Uncovered, _view.FadeOutSeconds, 0f, onComplete);

        public void FadeOut(float delaySeconds, Action onComplete = null) =>
            Fade(Uncovered, _view.FadeOutSeconds, delaySeconds, onComplete);

        private void Fade(float target, float seconds, float delaySeconds, Action onComplete) {
            // A fade turned around still calls its actions, so nobody waits on it forever.
            if (_fade != null && Mathf.Approximately(_target, target) == false)
                Stop();

            if (onComplete != null)
                _pending.Add(onComplete);

            _target = target;
            if (_fade == null)
                _fade = _coroutineRunner.StartCoroutine(FadeRoutine(seconds, delaySeconds));
        }

        private IEnumerator FadeRoutine(float seconds, float delaySeconds) {
            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);

            // Speed per full range, so a fade turned around halfway takes half the time back.
            float speed = seconds > 0f ? 1f / seconds : float.PositiveInfinity;
            while (Mathf.Approximately(_view.Alpha, _target) == false) {
                _view.Alpha = Mathf.MoveTowards(_view.Alpha, _target, speed * Time.unscaledDeltaTime);
                if (Mathf.Approximately(_view.Alpha, _target))
                    break;

                yield return null;
            }

            _view.Alpha = _target;
            _fade = null;
            InvokePending();
        }

        private void Stop() {
            _coroutineRunner.StopCoroutine(_fade);
            _fade = null;
            InvokePending();
        }

        private void InvokePending() {
            if (_pending.Count == 0)
                return;

            Action[] actions = _pending.ToArray();
            _pending.Clear();
            foreach (Action action in actions)
                action();
        }
    }
}
