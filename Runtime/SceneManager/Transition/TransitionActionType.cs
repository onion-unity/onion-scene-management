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

        Callback,
        CallbackAsync,
        
        Yield,
        Delay,
        DelayFrame,
        WaitForEndOfFrame,
        WaitForFixedUpdate,
    }
}