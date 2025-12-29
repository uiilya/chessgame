#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Toggle fullscreen game view inside the unity editor
/// 
/// * works only Unity 6.000.40 or so, some minor version fixed that the game view toolbar wasn't hideable
/// * use ctrl+shift+alt+2 for game view fullscreen
/// * use ctrl+shift+alt+3 for default layout (fallback in case sth fails)
/// 
/// Precondition
/// * when you start Unity the windows taskbar should have been removed, otherwise the area keeps on being reserved by Unity and won't be overlapped by the popup
/// </summary>
public static class FullscreenGameView
{
    private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
    private static readonly PropertyInfo ShowToolbarProperty = GameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    // display ids used to hide gameview while we render in the popup
    private static int DISPLAY_0 = 0; // typical gameview display
    private static int DISPLAY_1 = 1; // display gameview doesn't render to

    static EditorWindow instance;

    [MenuItem("Tools/Fullscreen Reset %#&3", priority = 3)]
    static void ResetUnityLayout()
    {
        EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
    }



    [MenuItem("Tools/Fullscreen Game View (Toggle) %#&2", priority = 2)]
    public static void Toggle()
    {
        if (GameViewType == null)
        {
            Debug.LogError("GameView type not found.");
            return;
        }

        if (ShowToolbarProperty == null)
        {
            Debug.LogWarning("GameView.showToolbar property not found.");
        }

        if (instance != null)
        {
            instance.Close();
            instance = null;

            // switch on display 0 again
            SetGameViewTargetDisplay(DISPLAY_0);

        }
        else
        {
            // switch off display 0, otherwise the rendering would happen to the game view and to our instance => performance
            SetGameViewTargetDisplay(DISPLAY_1);

            // gets the game window, but can't hide window title bar, ie can't be converted to popup
            // EditorWindow gameView = GetMainGameView();

            // creates a 2nd window, costs performance
            instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            // hide top toolbar
            // note: unity 6.000.40 or sth required
            ShowToolbarProperty?.SetValue(instance, false);

            // note: work area considers taskbar
            // DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            // Vector2 resolution = new Vector2(displayInfo.workArea.width, displayInfo.workArea.height);
            Vector2 position = new Vector2(0, 0); // Vector2.zero
            Vector2 resolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);

            // consider windows scale (eg 150% results in 1.5)
            resolution /= EditorGUIUtility.pixelsPerPoint;

            var fullscreenRect = new Rect(position, resolution);

            instance.ShowPopup(); // this fits screen, can't go into negative position            
            instance.position = fullscreenRect;
            instance.Focus();
        }
    }

    public static EditorWindow GetMainGameView()
    {
        // Get the type for the GameView from the UnityEditor assembly
        System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            Debug.LogError("Unable to find the UnityEditor.GameView type.");
            return null;
        }
        // Retrieve the game view window
        return EditorWindow.GetWindow(gameViewType);
    }

    /// <summary>
    /// Set target display to display index
    /// </summary>
    /// <param name="displayIndex"></param>
    private static void SetGameViewTargetDisplay( int displayIndex)
    {
        EditorWindow gameView = GetMainGameView();

        System.Type type = gameView.GetType();
        type.InvokeMember(
            "SetTargetDisplay",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            null,
            gameView,
            new object[] { displayIndex }
            );
    }
}
#endif