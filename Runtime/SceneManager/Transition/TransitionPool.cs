using System.Collections.Generic;

namespace Onion.SceneManagement.Transition {
    internal static class TransitionPool {
        private static readonly Stack<Transition> _pool = new();

        public static Transition Get() {
            Transition transition;
            if (_pool.Count > 0) {
                transition = _pool.Pop();
            }
            else {
                transition = new Transition();
            }

            transition.Init();
            return transition;
        }

        public static void Release(Transition transition) {
            _pool.Push(transition);
        }
    }
}