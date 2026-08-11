using UnityEngine;
using UnityEngine.Animations;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

[System.Serializable]
public class DefaultEventConfig
{
    [Tooltip("Enable or disable the Default Event String feature entirely.")]
    public bool enabled = false;

    [Tooltip("Target socket applied automatically on Start() and each animation state change.")]
    public string eventName = "";

    [Tooltip("Per-layer toggle for which Animator layers to monitor.")]
    public List<bool> monitoredLayers = new List<bool>();
}

public class ReadOnlyAttribute : PropertyAttribute { }

public class RenameAttribute : PropertyAttribute
{
    public string NewName { get; private set; }
    public RenameAttribute(string name) { NewName = name; }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        
        // Inherit [Rename] attribute text if present to prevent drawer conflicts
        object[] renameAttributes = fieldInfo.GetCustomAttributes(typeof(RenameAttribute), true);
        if (renameAttributes.Length > 0)
        {
            label.text = ((RenameAttribute)renameAttributes[0]).NewName;
        }
        else if (property.name == "name")
        {
            label.text = "Source Name";
        }
        
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

[CustomPropertyDrawer(typeof(RenameAttribute))]
public class RenameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text = (attribute as RenameAttribute).NewName;
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

[CustomPropertyDrawer(typeof(DefaultEventConfig))]
public class DefaultEventConfigDrawer : PropertyDrawer
{
    private bool layerFoldout = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty enabledProp = property.FindPropertyRelative("enabled");
        SerializedProperty eventNameProp = property.FindPropertyRelative("eventName");
        SerializedProperty monitoredLayersProp = property.FindPropertyRelative("monitoredLayers");

        // Resolve label
        object[] renameAttrs = fieldInfo.GetCustomAttributes(typeof(RenameAttribute), true);
        if (renameAttrs.Length > 0)
            label.text = ((RenameAttribute)renameAttrs[0]).NewName;
        else
            label.text = "Default Event String";

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Get Animator for layer names
        Animator animator = null;
        var targetObj = property.serializedObject.targetObject as Component;
        if (targetObj != null) animator = targetObj.GetComponent<Animator>();

        int animLayerCount = 0;
        if (animator != null)
        {
            var animSO = new UnityEditor.SerializedObject(animator);
            var ctrlProp = animSO.FindProperty("m_Controller");
            if (ctrlProp != null && ctrlProp.objectReferenceValue is AnimatorController controller)
            {
                animLayerCount = controller.layers.Length;
            }
        }

        // Sync monitoredLayers list size with actual Animator layer count
        if (animLayerCount > 0)
        {
            while (monitoredLayersProp.arraySize < animLayerCount)
            {
                monitoredLayersProp.InsertArrayElementAtIndex(monitoredLayersProp.arraySize);
                monitoredLayersProp.GetArrayElementAtIndex(monitoredLayersProp.arraySize - 1).boolValue = (monitoredLayersProp.arraySize == 1);
            }
            while (monitoredLayersProp.arraySize > animLayerCount)
                monitoredLayersProp.DeleteArrayElementAtIndex(monitoredLayersProp.arraySize - 1);
        }

        // Main Foldout header
        Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + lineHeight + spacing;

            // 1st: Enable toggle (always interactable)
            Rect enableRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(enableRect, enabledProp, new GUIContent("Enable"));
            y += lineHeight + spacing;

            // Gray out when disabled
            bool prevEnabled = GUI.enabled;
            GUI.enabled = enabledProp.boolValue;

            // 2nd: Event String Name
            Rect eventNameRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(eventNameRect, eventNameProp, new GUIContent("Event String Name"));
            y += lineHeight + spacing;

            // 3rd: Target Layer (sub-foldout)
            Rect layerFoldRect = new Rect(position.x, y, position.width, lineHeight);
            layerFoldout = EditorGUI.Foldout(layerFoldRect, layerFoldout, "Target Layer", true);
            y += lineHeight + spacing;

            if (layerFoldout && animLayerCount > 0)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < animLayerCount; i++)
                {
                    string layerName = "Layer " + i;
                    var animSO = new UnityEditor.SerializedObject(animator);
                    var ctrlProp = animSO.FindProperty("m_Controller");
                    if (ctrlProp != null && ctrlProp.objectReferenceValue is AnimatorController controller)
                    {
                        if (i < controller.layers.Length) layerName = controller.layers[i].name;
                    }
                    
                    Rect toggleRect = new Rect(position.x, y, position.width, lineHeight);
                    SerializedProperty elem = monitoredLayersProp.GetArrayElementAtIndex(i);
                    elem.boolValue = EditorGUI.Toggle(toggleRect, layerName, elem.boolValue);
                    y += lineHeight + spacing;
                }
                EditorGUI.indentLevel--;
            }
            else if (layerFoldout && animLayerCount == 0)
            {
                Rect warnRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.LabelField(warnRect, "  No Animator found on this GameObject.");
                y += lineHeight + spacing;
            }

            GUI.enabled = prevEnabled;
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        // Header + Enable + EventName + TargetLayer foldout
        float height = lineHeight + (lineHeight + spacing) * 3;

        if (layerFoldout)
        {
            Animator animator = null;
            var targetObj = property.serializedObject.targetObject as Component;
            if (targetObj != null) animator = targetObj.GetComponent<Animator>();
            int count = 1;
            if (animator != null)
            {
                var animSO = new UnityEditor.SerializedObject(animator);
                var ctrlProp = animSO.FindProperty("m_Controller");
                if (ctrlProp != null && ctrlProp.objectReferenceValue is AnimatorController controller)
                {
                    count = controller.layers.Length;
                }
            }
            height += (lineHeight + spacing) * count;
        }

        return height;
    }
}

[CustomPropertyDrawer(typeof(WeaponSlot))]
public class WeaponSlotDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty nameProp = property.FindPropertyRelative("name");
        
        // Use the name field value as the foldout header
        string displayName = (!string.IsNullOrEmpty(nameProp.stringValue)) ? nameProp.stringValue : label.text;
        
        EditorGUI.PropertyField(position, property, new GUIContent(displayName), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif

[System.Serializable]
public class SocketSetting
{
    [ReadOnly]
    public string name; 

    [Rename("Animation Event String Name")]
    [Tooltip("The matching string used in the Animation Event parameter.")]
    public string eventStringName;
    
    [HideInInspector]
    public int sourceIndex;
}

[System.Serializable]
public class WeaponSlot
{
    [Rename("Name & Debug")]
    [Tooltip("Identifier for inspector organization (e.g., Main Hand, Off Hand).")]
    public string name = "New Weapon";

    [Space(5)]
    [Rename("Parent Constraint Target")]
    [Tooltip("The Parent Constraint component driving this weapon's socket connections.")]
    public ParentConstraint weaponConstraint;

    [Space(5)]
    [Rename("Constraint Cut OFF Event String")]
    [Tooltip("Animation Event string to disconnect this specific weapon constraint.")]
    public string dropCommand = "Drop_Weapon";

    [Space(5)]
    [Rename("Socket Settings(Animation Event)")]
    public List<SocketSetting> socketSettings = new List<SocketSetting>();

    // Internal physics cache
    [HideInInspector] public Rigidbody weaponRigidbody;
}

[System.Serializable]
public class SupportIKConfig
{
    [Rename("Use Support IK")]
    [Tooltip("Enables IK Pass targeting for this limb.")]
    public bool useIK = false;

    [Rename("IK Avatar Goal")]
    [Tooltip("The AvatarIKGoal (Hand/Foot) to snap to the target.")]
    public AvatarIKGoal ikHand = AvatarIKGoal.LeftHand;

    [Rename("Target Transform")]
    [Tooltip("The actual Transform (e.g., barrel or grip) to attach the IK limb to.")]
    public Transform ikTarget;

    [Space(5)]
    [Range(0f, 1f)]
    [Rename("IK Max Weight")]
    [Tooltip("1.0 snaps perfectly to target. Lower values blend with the base animation.")]
    public float ikWeight = 1f;

    [Space(10)]
    [Rename("IK Command ID")]
    [Tooltip("A custom ID (e.g., Mag, Barrel) to autogenerate Animation Event strings.")]
    public string commandID = "CustomPart";

    [Space(5)]
    [ReadOnly]
    [Rename("Event String Name(OFF)")]
    [Tooltip("Autogenerated. Use this string in an Animation Event to detach IK.")]
    public string offCommand;

    [Space(2)]
    [ReadOnly]
    [Rename("Event String Name(ON)")]
    [Tooltip("Autogenerated. Use this string in an Animation Event to attach IK.")]
    public string onCommand;

    // Internal Variables
    [HideInInspector] public float currentWeight = 0f;
    [HideInInspector] public float targetWeight = 0f;
}

[System.Serializable]
public class PropEvent
{
    [Tooltip("The GameObject to toggle active/inactive.")]
    public GameObject targetObject;

    [Rename("Active ON String")]
    [Tooltip("Animation Event string to activate this prop. Auto-filled if empty.")]
    public string active_on;

    [Rename("Active OFF String")]
    [Tooltip("Animation Event string to deactivate this prop. Auto-filled if empty.")]
    public string active_off;
}

public class Character_Weapon_Controller : MonoBehaviour
{
    [ReadOnly]
    [Rename("Animation Event Function Name")]
    public string animEventFunctionName = "SwitchSocket";

    [Space(5)]
    public DefaultEventConfig defaultEventSetting;

    [Space(5)]
    [Rename("Show Debug Log")]
    [Tooltip("Enable tracking of incoming Socket and IK events in the console log.")]
    public bool showDebugLog = false;

    [Space(10)]
    [Tooltip("Manage multiple weapons and their respective constraints and sockets.")]
    public List<WeaponSlot> weaponSlots = new List<WeaponSlot>();

    [Space(10)]
    [Rename("IK Settings(Humanoid)")]
    [Tooltip("Add multiple Support IK targets (Hands/Feet) for granular IK control via animation events.")]
    public List<SupportIKConfig> supportIKSettings = new List<SupportIKConfig>();

    [Space(10)]
    [Rename("Prop Active Settings")]
    [Tooltip("Manage additional props that need to be turned on/off via animation events.")]
    public List<PropEvent> propEvents = new List<PropEvent>();

    // Internal Variables
    private Animator animator;
    private int[] lastStateHashes;
    private Dictionary<string, GameObject> propObjectMap;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (weaponSlots != null)
        {
            foreach (var slot in weaponSlots)
            {
                if (slot.weaponConstraint != null)
                    slot.weaponRigidbody = slot.weaponConstraint.GetComponent<Rigidbody>();
            }
        }
        
        // Initialize Prop Map
        propObjectMap = new Dictionary<string, GameObject>();
        if (propEvents != null)
        {
            foreach (var prop in propEvents)
            {
                if (prop.targetObject != null && !propObjectMap.ContainsKey(prop.targetObject.name))
                {
                    propObjectMap[prop.targetObject.name] = prop.targetObject;
                }
            }
        }

        if (animator != null)
        {
            lastStateHashes = new int[animator.layerCount];
            for (int i = 0; i < lastStateHashes.Length; i++)
            {
                lastStateHashes[i] = animator.GetCurrentAnimatorStateInfo(i).fullPathHash;
            }
        }
    }

    /// <summary>
    /// Initializes default socket equip state on Start.
    /// </summary>
    private void Start()
    {
        if (defaultEventSetting != null && defaultEventSetting.enabled && !string.IsNullOrEmpty(defaultEventSetting.eventName))
        {
            SwitchSocketByString(defaultEventSetting.eventName);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (weaponSlots != null)
        {
            foreach (var slot in weaponSlots)
            {
                if (slot.weaponConstraint == null) continue;

                int currentSourceCount = slot.weaponConstraint.sourceCount;

                if (slot.socketSettings.Count != currentSourceCount)
                {
                    while (slot.socketSettings.Count < currentSourceCount) slot.socketSettings.Add(new SocketSetting());
                    while (slot.socketSettings.Count > currentSourceCount) slot.socketSettings.RemoveAt(slot.socketSettings.Count - 1);
                }

                for (int i = 0; i < currentSourceCount; i++)
                {
                    ConstraintSource source = slot.weaponConstraint.GetSource(i);
                    string tName = source.sourceTransform != null ? source.sourceTransform.name : "None";

                    slot.socketSettings[i].name = tName;
                    slot.socketSettings[i].sourceIndex = i;

                    if (string.IsNullOrEmpty(slot.socketSettings[i].eventStringName))
                    {
                        slot.socketSettings[i].eventStringName = "To_" + tName;
                    }
                }
            }
        }

        // Auto-generate IK command prefixes for Inspector visualization
        if (supportIKSettings != null)
        {
            foreach (var ik in supportIKSettings)
            {
                if (!string.IsNullOrEmpty(ik.commandID))
                {
                    ik.offCommand = "IK_OFF_" + ik.commandID;
                    ik.onCommand = "IK_ON_" + ik.commandID;
                }
            }
        }

        // Auto-generate Prop activation strings only if they are empty
        if (propEvents != null)
        {
            foreach (var prop in propEvents)
            {
                if (prop.targetObject != null)
                {
                    if (string.IsNullOrEmpty(prop.active_on))
                        prop.active_on = "Active_on_" + prop.targetObject.name;
                    if (string.IsNullOrEmpty(prop.active_off))
                        prop.active_off = "Active_off_" + prop.targetObject.name;
                }
                else
                {
                    prop.active_on = "";
                    prop.active_off = "";
                }
            }
        }
    }

    private void Update()
    {
        if (defaultEventSetting == null || !defaultEventSetting.enabled || string.IsNullOrEmpty(defaultEventSetting.eventName) || animator == null) return;

        int layerCount = animator.layerCount;
        if (lastStateHashes == null || lastStateHashes.Length != layerCount)
            lastStateHashes = new int[layerCount];

        bool stateChanged = false;

        for (int i = 0; i < layerCount; i++)
        {
            // Skip layers that are not monitored
            if (i >= defaultEventSetting.monitoredLayers.Count || !defaultEventSetting.monitoredLayers[i])
                continue;

            int currentHash = animator.GetCurrentAnimatorStateInfo(i).fullPathHash;
            if (currentHash != lastStateHashes[i])
            {
                lastStateHashes[i] = currentHash;
                stateChanged = true;
            }
        }

        // Always update non-monitored layer hashes to prevent false triggers if toggled on later
        for (int i = 0; i < layerCount; i++)
        {
            if (i < defaultEventSetting.monitoredLayers.Count && defaultEventSetting.monitoredLayers[i])
                continue;
            lastStateHashes[i] = animator.GetCurrentAnimatorStateInfo(i).fullPathHash;
        }

        if (stateChanged)
        {
            if (CurrentClipHasSwitchSocketEvent())
            {
                if (showDebugLog) Debug.Log($"[DefaultEvent] State Change Detected, but clip has SwitchSocket events. Delegating to Animation Events.");
                return;
            }

            if (showDebugLog) Debug.Log($"[DefaultEvent] State Change Detected. Resetting to '{defaultEventSetting.eventName}'");
            SwitchSocketByString(defaultEventSetting.eventName);
        }
    }

    /// <summary>
    /// Scans all active clips on monitored layers for any AnimationEvent that calls 'SwitchSocket'.
    /// Used to determine whether the default reset should yield priority to the clip's own events.
    /// </summary>
    private bool CurrentClipHasSwitchSocketEvent()
    {
        int layerCount = animator.layerCount;

        for (int layer = 0; layer < layerCount; layer++)
        {
            // Only check monitored layers
            if (layer >= defaultEventSetting.monitoredLayers.Count || !defaultEventSetting.monitoredLayers[layer])
                continue;

            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(layer);
            foreach (var clipInfo in clipInfos)
            {
                if (clipInfo.clip == null) continue;

                AnimationEvent[] events = clipInfo.clip.events;
                foreach (var evt in events)
                {
                    if (evt.functionName == "SwitchSocket")
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Main entry point for Unity Animation Events. Receives AnimationEvent to track clip info.
    /// </summary>
    public void SwitchSocket(AnimationEvent animEvent)
    {
        string triggerName = animEvent.stringParameter;
        string clipName = "알 수 없는 애니메이션";
        if (animEvent.animatorClipInfo.clip != null)
        {
            clipName = animEvent.animatorClipInfo.clip.name;
        }
        
        PerformSocketSwitch(triggerName, clipName);
    }

    /// <summary>
    /// String-based entry point for direct script calls or UI buttons.
    /// </summary>
    public void SwitchSocketByString(string triggerEventName)
    {
        PerformSocketSwitch(triggerEventName, "ScriptCall");
    }

    // --- New Animation Event Functions ---

    /// <summary>
    /// Function: Active_on, String: ObjectName or custom string.
    /// Directly toggles a registered prop to active.
    /// </summary>
    public void Active_on(string targetName)
    {
        SetPropActive(targetName, true);
    }

    /// <summary>
    /// Function: Active_off, String: ObjectName or custom string.
    /// Directly toggles a registered prop to inactive.
    /// </summary>
    public void Active_off(string targetName)
    {
        SetPropActive(targetName, false);
    }

    private void SetPropActive(string targetName, bool active)
    {
        // 1. Try to match by custom string defined in propEvents
        if (propEvents != null)
        {
            foreach (var prop in propEvents)
            {
                if (prop.targetObject == null) continue;

                // Compare with the strings in the inspector
                if (active && targetName.Equals(prop.active_on, System.StringComparison.OrdinalIgnoreCase))
                {
                    prop.targetObject.SetActive(true);
                    if (showDebugLog) Debug.Log($"[PropActive] '{targetName}' (Custom ON) matched '{prop.targetObject.name}'");
                    return;
                }
                if (!active && targetName.Equals(prop.active_off, System.StringComparison.OrdinalIgnoreCase))
                {
                    prop.targetObject.SetActive(false);
                    if (showDebugLog) Debug.Log($"[PropActive] '{targetName}' (Custom OFF) matched '{prop.targetObject.name}'");
                    return;
                }
            }
        }

        // 2. Fallback: match by object name directly (for standard Active_on_ pattern or direct name)
        if (propObjectMap != null && propObjectMap.TryGetValue(targetName, out GameObject obj))
        {
            obj.SetActive(active);
            if (showDebugLog) Debug.Log($"[PropActive] '{targetName}' -> {(active ? "ON" : "OFF")}");
        }
    }

    private void PerformSocketSwitch(string triggerEventName, string sourceInfo)
    {
        if (string.IsNullOrEmpty(triggerEventName)) return;

        // Split incoming string by commas to process sequential multi-commands on the same frame.
        string[] commands = triggerEventName.Split(',');

        foreach (string rawCmd in commands)
        {
            string cmd = rawCmd.Trim();
            if (string.IsNullOrEmpty(cmd)) continue;

            ProcessSingleCommand(cmd, sourceInfo);
        }
    }

    private void ProcessSingleCommand(string cmd, string sourceInfo)
    {
        // Sub-routine 0a: Match exact custom ON/OFF strings from PropEvents
        if (propEvents != null)
        {
            foreach (var prop in propEvents)
            {
                if (prop.targetObject == null) continue;

                if (cmd.Equals(prop.active_on, System.StringComparison.OrdinalIgnoreCase))
                {
                    prop.targetObject.SetActive(true);
                    if (showDebugLog) Debug.Log($"[PropEvent] '{sourceInfo}' matched custom ON: {cmd}");
                    return;
                }
                if (cmd.Equals(prop.active_off, System.StringComparison.OrdinalIgnoreCase))
                {
                    prop.targetObject.SetActive(false);
                    if (showDebugLog) Debug.Log($"[PropEvent] '{sourceInfo}' matched custom OFF: {cmd}");
                    return;
                }
            }
        }

        // Sub-routine 0b: Fallback pattern recognition for unified SwitchSocket calls (Active_on_/off_)
        if (cmd.StartsWith("Active_on_"))
        {
            Active_on(cmd.Substring(10));
            return;
        }
        if (cmd.StartsWith("Active_off_"))
        {
            Active_off(cmd.Substring(11));
            return;
        }

        // Sub-routine 1: Scan and process multi-target IK activation/deactivation.
        bool isIKCommand = false;
        foreach (var ik in supportIKSettings)
        {
            if (!string.IsNullOrEmpty(ik.offCommand) && cmd.Equals(ik.offCommand, System.StringComparison.OrdinalIgnoreCase))
            {
                ik.targetWeight = 0f;
                if (showDebugLog) Debug.Log($"[SupportIK] '{sourceInfo}' -> '{ik.ikHand}' IK Constraint OFF");
                isIKCommand = true;
            }
            else if (!string.IsNullOrEmpty(ik.onCommand) && cmd.Equals(ik.onCommand, System.StringComparison.OrdinalIgnoreCase))
            {
                ik.targetWeight = ik.ikWeight;
                if (showDebugLog) Debug.Log($"[SupportIK] '{sourceInfo}' -> '{ik.ikHand}' IK Constraint ON (Weight: {ik.targetWeight})");
                isIKCommand = true;
            }
        }

        // Return early if the command was strictly for targeting IK.
        if (isIKCommand) return; 

        bool handled = false;

        // Sequence through all configured weapon slots
        if (weaponSlots != null)
        {
            foreach (var slot in weaponSlots)
            {
                if (slot.weaponConstraint == null) continue;

                // Sub-routine 2: Check for slot-specific drop command.
                if (!string.IsNullOrEmpty(slot.dropCommand) && cmd.Equals(slot.dropCommand, System.StringComparison.OrdinalIgnoreCase))
                {
                    slot.weaponConstraint.constraintActive = false;
                    
                    // Re-enable physics gravity if Rigidbody is present.
                    if (slot.weaponRigidbody != null) slot.weaponRigidbody.isKinematic = false;

                    // Release all active IK limb trackers simultaneously for safety.
                    foreach (var ik in supportIKSettings)
                    {
                        ik.targetWeight = 0f;
                    }

                    if (showDebugLog)
                        Debug.Log($"[SocketSwitch] DROP='{sourceInfo}', Weapon '{slot.name}' dropped to ground!");
                    
                    handled = true;
                    continue; // Command handled for this slot, move to next slot in case of identical socket names.
                }

                // Sub-routine 3: Standard socket switch handling.
                int targetIndex = -1; 
                foreach (var setting in slot.socketSettings)
                {
                    // Case-insensitive iteration matching event string names.
                    if (!string.IsNullOrEmpty(setting.eventStringName) && 
                        setting.eventStringName.Trim().Equals(cmd, System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetIndex = setting.sourceIndex;
                        break;
                    }
                }

                if (targetIndex != -1)
                {
                    slot.weaponConstraint.constraintActive = true;
                    if (slot.weaponRigidbody != null) slot.weaponRigidbody.isKinematic = true;

                    int totalSources = slot.weaponConstraint.sourceCount;
                    for (int i = 0; i < totalSources; i++)
                    {
                        ConstraintSource source = slot.weaponConstraint.GetSource(i);
                        source.weight = (i == targetIndex) ? 1f : 0f; 
                        slot.weaponConstraint.SetSource(i, source);
                    }

                    if (showDebugLog)
                    {
                        Debug.Log($"[SocketSwitch] OK='{sourceInfo}', Applied Socket Trigger : '{cmd}' on '{slot.name}'");
                    }
                    handled = true;
                }
            }
        }

        if (!handled && showDebugLog)
        {
            Debug.LogWarning($"[SocketSwitch] Error='{sourceInfo}', Unregistered Command Executed : {cmd}");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Iterate through all tracked IK modules for individual smooth lerp transitioning.
        foreach (var ik in supportIKSettings)
        {
            if (!ik.useIK || ik.ikTarget == null) continue;

            ik.currentWeight = Mathf.Lerp(ik.currentWeight, ik.targetWeight, Time.deltaTime * 15f);

            animator.SetIKPositionWeight(ik.ikHand, ik.currentWeight);
            animator.SetIKRotationWeight(ik.ikHand, ik.currentWeight);
            
            animator.SetIKPosition(ik.ikHand, ik.ikTarget.position);
            animator.SetIKRotation(ik.ikHand, ik.ikTarget.rotation);
        }
    }
}
