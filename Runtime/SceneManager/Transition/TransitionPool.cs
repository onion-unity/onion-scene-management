using System.Collections.Generic;

namespace Onion.SceneManagement.Transition {
    internal static class TransitionPool {
        private static readonly Stack<Transition> _pool = new();

        public static Transition Get() {
            if (_pool.Count > 0) {
                return _pool.Pop();
            }

            return new Transition();
        }

        public static void Release(Transition transition) {
            _pool.Push(transition);
        }
    }
}