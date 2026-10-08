using UnityEngine;

public static class WebKeyboardInputFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureWebKeyboardInput()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Unity Web captures all keyboard events by default, even when an
        // HTML input field has focus. Disabling this lets the website's
        // number fields receive normal typing/backspace/delete input.
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }
}
