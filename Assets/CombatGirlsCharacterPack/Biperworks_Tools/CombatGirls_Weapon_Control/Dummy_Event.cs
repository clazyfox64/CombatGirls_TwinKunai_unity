using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A dummy script to catch Animation Events and prevent "no receiver" errors.
/// Use this if an animation triggers events that are not handled on this specific GameObject.
/// </summary>
public class Dummy_Event : MonoBehaviour
{
    // Empty function to match the Animation Event entry point.
    // This prevents Unity's "AnimationEvent ... has no receiver" error.
    public void SwitchSocket()
    {
        // Intentionally left empty to ignore the event.
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Dummy_Event))]
[CanEditMultipleObjects]
public class Dummy_EventEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Description");
        GUI.enabled = false;
        EditorGUILayout.TextArea(
            "This script is for preventing Animation Event error messages.",
            GUILayout.MinHeight(36)
        );
        GUI.enabled = true;
    }
}
#endif