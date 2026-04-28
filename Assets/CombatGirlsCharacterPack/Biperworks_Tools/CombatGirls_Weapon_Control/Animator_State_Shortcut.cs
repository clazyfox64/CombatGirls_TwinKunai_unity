using UnityEngine;

public class Animator_State_Shortcut : StateMachineBehaviour
{
    [Tooltip("Comma-separated commands to pass to Character_Weapon_Controller (e.g., To_Hand, IK_ON_CustomPart)")]
    public string enterCommand;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (string.IsNullOrEmpty(enterCommand)) return;

        Character_Weapon_Controller controller = animator.GetComponent<Character_Weapon_Controller>();
        if (controller != null)
        {
            controller.SwitchSocketByString(enterCommand);
        }
    }
}