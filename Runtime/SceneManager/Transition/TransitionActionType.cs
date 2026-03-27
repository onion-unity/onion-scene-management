namespace Onion.SceneManagement.Transition {
    internal enum TransitionActionType {
        None,
        Load,
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
        
        NextFrame,
        Delay,
        DelayFrame,
        WaitForEndOfFrame,
        WaitForFixedUpdate,
        WaitForParallels,
    }
}