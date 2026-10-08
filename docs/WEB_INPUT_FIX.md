# HTML keyboard input fix

Unity Web captures keyboard input globally by default. That prevents the page's
HTML number fields from receiving normal typing/backspace/delete events after
the Unity player is loaded.

For the currently bundled build, the generated Unity framework has been patched
so keyboard events whose target is an HTML input/textarea/select/contenteditable
control are ignored by Unity and remain available to the webpage.

For future Unity rebuilds, copy `unity-source-patch/Scripts/WebKeyboardInputFix.cs`
into the Unity project's `Assets/Scripts/` before building. It uses
`WebGLInput.captureAllKeyboardInput = false` in Web builds, which is the proper
source-level fix and does not require attaching the script to a GameObject.
