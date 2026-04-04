namespace Onion.SceneManagement.Transition {
    internal enum TransitionActionType {
        None,
        Load,
        Activate,
        Unload,
        
        Enter,
        EnterAsync,
        Exit,
        ExitAsync,

        GlobalEnter,
        GlobalEnterAsync,
        GlobalExit,
        GlobalExitAsync,

        BatchEnter,
        BatchExit,

        Callback,
        CallbackAsync,
        
        Delay,
        DelayFrame,
        NextFrame,

        WaitWhile,
        WaitUntil,
        
        WaitForEndOfFrame,
        WaitForFixedUpdate,
        WaitForParallels,
    }
}