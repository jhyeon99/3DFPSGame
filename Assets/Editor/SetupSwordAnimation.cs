#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Tools > Setup Sword Animation 으로 실행한다.
// PlayerAC에 Slash1~3 상태와 트리거/인덱스 파라미터를 추가하고 Player에 스윙 컴포넌트를 붙인다.
public static class SetupSwordAnimation
{
    private const string ControllerPath = "Assets/07.Animations/PlayerAC.controller";
    private const string ClipDir = "Assets/EEJANAI_Team/FreeSwordAnimations/Animations";
    private const string SlashTag = "Slash";
    private const int SlashCount = 3;
    private const float EnterBlend = 0.1f;
    private const float ExitTimeRatio = 0.85f;
    private const float ExitBlend = 0.15f;

    [MenuItem("Tools/Setup Sword Animation")]
    public static void Run()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[SwordAnim] PlayerAC를 찾지 못했습니다: " + ControllerPath);
            return;
        }

        AddParameter(controller, "Slash", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "SlashIndex", AnimatorControllerParameterType.Int);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState moveState = machine.defaultState;

        for (int i = 1; i <= SlashCount; i++)
        {
            string stateName = "Slash" + i;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipDir}/slash{i}.anim");
            if (clip == null)
            {
                Debug.LogError("[SwordAnim] 클립을 찾지 못했습니다: slash" + i);
                continue;
            }

            AnimatorState state = FindState(machine, stateName);
            if (state == null)
            {
                state = machine.AddState(stateName, new Vector3(650f, 100f + i * 60f, 0f));
            }
            state.motion = clip;
            state.tag = SlashTag;
            state.writeDefaultValues = true;

            // 다시 실행해도 중복되지 않도록 기존 전이를 지우고 새로 만든다.
            foreach (AnimatorStateTransition t in state.transitions)
            {
                state.RemoveTransition(t);
            }
            foreach (AnimatorStateTransition t in machine.anyStateTransitions)
            {
                if (t.destinationState == state)
                {
                    machine.RemoveAnyStateTransition(t);
                }
            }

            // Any State -> SlashN (Slash 트리거 + SlashIndex == N)
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Slash");
            enter.AddCondition(AnimatorConditionMode.Equals, i, "SlashIndex");
            enter.hasExitTime = false;
            enter.duration = EnterBlend;
            enter.canTransitionToSelf = true;

            // SlashN -> 이동(블렌드 트리)
            AnimatorStateTransition exit = state.AddTransition(moveState);
            exit.hasExitTime = true;
            exit.exitTime = ExitTimeRatio;
            exit.duration = ExitBlend;
        }

        EditorUtility.SetDirty(controller);
        SetupPlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("[SwordAnim] 완료: PlayerAC에 Slash1~" + SlashCount + " 추가. F키로 콤보 베기.");
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("[SwordAnim] 씬에서 Player를 찾지 못했습니다. 씬을 연 뒤 다시 실행하세요.");
            return;
        }

        if (player.GetComponent<PlayerSwordSwing>() == null)
        {
            Undo.AddComponent<PlayerSwordSwing>(player);
        }

        // 이동은 CharacterController가 담당하므로 베기 클립의 루트 모션은 쓰지 않는다.
        var animator = player.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            Undo.RecordObject(animator, "Setup Sword Animation");
            animator.applyRootMotion = false;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == name) return child.state;
        }
        return null;
    }
}
#endif
