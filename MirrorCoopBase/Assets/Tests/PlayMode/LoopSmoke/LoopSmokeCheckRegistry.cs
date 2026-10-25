#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The checks of LoopSmokeTest, read from the [LoopSmokeStep] methods of this assembly: a feature adds its check as
    // a class with its steps and never edits the test body. A check class is made at its first step with the test's
    // LoopSmokeContext and lives for the whole test, so its steps share state (the first run's objects, a joined bot).
    // The steps of one slot run one after the other by order.
    public sealed class LoopSmokeCheckRegistry {
        private const BindingFlags STEP_METHODS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly LoopSmokeContext _context;
        private readonly Dictionary<LoopSmokeSlot, List<LoopSmokeStep>> _stepsBySlot;
        private readonly Dictionary<Type, object> _checks = new Dictionary<Type, object>();
        private readonly HashSet<LoopSmokeStep> _ranSteps = new HashSet<LoopSmokeStep>();

        private LoopSmokeCheckRegistry(LoopSmokeContext context, Dictionary<LoopSmokeSlot, List<LoopSmokeStep>> stepsBySlot) {
            _context = context;
            _stepsBySlot = stepsBySlot;
        }

        public static LoopSmokeCheckRegistry CreateFromAssembly(LoopSmokeContext context) {
            Dictionary<LoopSmokeSlot, List<LoopSmokeStep>> stepsBySlot = new Dictionary<LoopSmokeSlot, List<LoopSmokeStep>>();
            foreach (Type type in typeof(LoopSmokeCheckRegistry).Assembly.GetTypes()) {
                foreach (MethodInfo method in type.GetMethods(STEP_METHODS))
                    AddSteps(method, stepsBySlot);
            }

            foreach (List<LoopSmokeStep> steps in stepsBySlot.Values) {
                steps.Sort((first, second) => first.Order.CompareTo(second.Order));
                AssertOrdersAreUnique(steps);
            }

            return new LoopSmokeCheckRegistry(context, stepsBySlot);
        }

        public IEnumerator RunSlotCoroutine(LoopSmokeSlot slot) {
            if (_stepsBySlot.TryGetValue(slot, out List<LoopSmokeStep> steps) == false)
                yield break;

            foreach (LoopSmokeStep step in steps) {
                Debug.Log($"[LoopSmoke] {slot}: {step.Name}");
                _ranSteps.Add(step);
                IEnumerator coroutine = Invoke(step);
                if (coroutine != null)
                    yield return coroutine;
            }
        }

        // A step whose slot the test never reached (a run or flight it does not play) would silently check nothing.
        public void AssertEveryStepRan() {
            List<string> missed = _stepsBySlot.Values.SelectMany(steps => steps)
                .Where(step => _ranSteps.Contains(step) == false)
                .Select(step => $"{step.Name} ({step.Slot})")
                .ToList();
            Assert.IsEmpty(missed, $"Registered loop checks never ran: {string.Join(", ", missed)}");
        }

        private IEnumerator Invoke(LoopSmokeStep step) {
            object check = GetOrCreateCheck(step.Method.DeclaringType);
            if (step.Method.ReturnType == typeof(void)) {
                ((Action)Delegate.CreateDelegate(typeof(Action), check, step.Method))();
                return null;
            }

            return ((Func<IEnumerator>)Delegate.CreateDelegate(typeof(Func<IEnumerator>), check, step.Method))();
        }

        private object GetOrCreateCheck(Type type) {
            if (_checks.TryGetValue(type, out object check))
                return check;

            try {
                check = CreateCheck(type);
            }
            catch (TargetInvocationException exception) {
                // The check's own failure, not the reflection wrapper, is what the test reports.
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }

            _checks.Add(type, check);
            return check;
        }

        private object CreateCheck(Type type) {
            ConstructorInfo withContext = type.GetConstructor(new[] { typeof(LoopSmokeContext) });
            if (withContext != null)
                return withContext.Invoke(new object[] { _context });

            ConstructorInfo empty = type.GetConstructor(Type.EmptyTypes);
            Assert.IsNotNull(empty, $"Loop check {type.Name} has neither a public constructor taking {nameof(LoopSmokeContext)} nor an empty one.");
            return empty.Invoke(null);
        }

        private static void AddSteps(MethodInfo method, Dictionary<LoopSmokeSlot, List<LoopSmokeStep>> stepsBySlot) {
            foreach (LoopSmokeStepAttribute attribute in method.GetCustomAttributes<LoopSmokeStepAttribute>()) {
                AssertStepMethod(method);
                LoopSmokeSlot slot = new LoopSmokeSlot(attribute.Phase, attribute.Run, attribute.Flight);
                AssertSlot(method, slot);
                if (stepsBySlot.TryGetValue(slot, out List<LoopSmokeStep> steps) == false) {
                    steps = new List<LoopSmokeStep>();
                    stepsBySlot.Add(slot, steps);
                }

                steps.Add(new LoopSmokeStep(slot, attribute.Order, method));
            }
        }

        private static void AssertStepMethod(MethodInfo method) {
            string name = StepName(method);
            Assert.IsEmpty(method.GetParameters(), $"Loop step {name} must take no arguments.");
            bool isCoroutineOrAction = method.ReturnType == typeof(IEnumerator) || method.ReturnType == typeof(void);
            Assert.IsTrue(isCoroutineOrAction, $"Loop step {name} must return IEnumerator or void.");
        }

        private static void AssertSlot(MethodInfo method, LoopSmokeSlot slot) {
            string name = StepName(method);
            switch (slot.Phase) {
                case LoopSmokePhase.BeforeTakeoff:
                case LoopSmokePhase.InCruise:
                case LoopSmokePhase.AfterLanding:
                    Assert.IsTrue(slot.Run > 0 && slot.Flight > 0, $"Loop step {name}: {slot.Phase} needs a run and a flight from 1.");
                    return;
                case LoopSmokePhase.InLobby:
                case LoopSmokePhase.AfterRun:
                    Assert.IsTrue(slot.Run > 0 && slot.Flight == 0, $"Loop step {name}: {slot.Phase} needs a run from 1 and no flight.");
                    return;
                case LoopSmokePhase.InMenu:
                    Assert.IsTrue(slot.Run == 0 && slot.Flight == 0, $"Loop step {name}: {slot.Phase} takes no run and no flight.");
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot.Phase, $"Loop step {name} has no phase.");
            }
        }

        private static void AssertOrdersAreUnique(List<LoopSmokeStep> sorted) {
            for (int i = 1; i < sorted.Count; i++) {
                bool isTie = sorted[i].Order == sorted[i - 1].Order;
                Assert.IsFalse(isTie, $"Loop steps {sorted[i - 1].Name} and {sorted[i].Name} share order {sorted[i].Order} in {sorted[i].Slot}: give each its own.");
            }
        }

        private static string StepName(MethodInfo method) =>
            $"{method.DeclaringType.Name}.{method.Name}";
    }
}
#endif
