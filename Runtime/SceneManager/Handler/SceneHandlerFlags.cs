using System;

namespace Onion.SceneManagement {
    // Which notifications a handler receives. Per kind a handler has either the plain flag (its own scene only)
    // or the Global one ([ExecuteByAnyScene]: every scene), never both.
    [Flags]
    internal enum SceneHandlerFlags {
        None = 0,

        Enter      = 1 << 0,
        Exit       = 1 << 1,
        AsyncEnter = 1 << 2,
        AsyncExit  = 1 << 3,

        GlobalEnter      = 1 << 4,
        GlobalExit       = 1 << 5,
        GlobalAsyncEnter = 1 << 6,
        GlobalAsyncExit  = 1 << 7,

        // ISceneEnterHandler<T> / IAsyncSceneEnterHandler<T>; never global.
        ArgsEnter      = 1 << 8,
        ArgsAsyncEnter = 1 << 9,
    }
}
