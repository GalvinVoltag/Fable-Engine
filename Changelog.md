[2026-10-21]___________________________________________
* changed ManagedRoutine.Input from void to bool.
* changed UIArea to have an UpdateState() method because getters were too confusing.
* ManagedRoutines can cancel input calls from their children, to make input masks possible.
* Some smaller tweaks to existing context actions and portals as well as adding new.

[2026-10-22]___________________________________________
* Made adjustments and changes to Context and ContextWindow; not Context can suppress specific context actions, and ContextWindow will close when mouse clicks on its parent.
* Added functionality to RoutineProviderContextActions.

[2026-10-25]___________________________________________
* Added hover option to ContextMenuProvider.
* Added ability to call or not all input methods on unfocused windows
* Hid a bug that would make all buttons of a context window hovered on creation
* Added an option to ContextMenuProvider so it spawns a window on hover
* Changed Shader class so it uses IShaderSource instead of two strings

[2026-10-26]___________________________________________
* Changed BinaryTree so it's a ManagedRoutine with a transform rather than a Routine that depends on a provider.

[2026-11-19]___________________________________________
* Made BinaryTree into a Thing, and fixed its issues.
* BinaryTrees now are created with splits, and any A or B side can be detached non-destructively.
* ... I'm still working on re-attaching them.
* Also got rid of ContextWindow's list layout inside padding. Set it to 0