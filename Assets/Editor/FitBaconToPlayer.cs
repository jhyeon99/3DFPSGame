#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Fit Bacon To Player 로 실행한다.
// 모델의 발바닥이 CharacterController 콜라이더 바닥에 오도록 Y 위치를 맞추고,
// 이동은 CharacterController가 담당하므로 Animator의 루트 모션을 끈다.
public static class FitBaconToPlayer
{
    private const string PlayerName = "Player";
    private const string ModelName = "Roblox Bacon Hair";

    [MenuItem("Tools/Fit Bacon To Player")]
    public static void Run()
    {
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            Debug.LogError("[FitBacon] 씬에서 Player를 찾지 못했습니다.");
            return;
        }

        Transform model = player.transform.Find(ModelName);
        var controller = player.GetComponent<CharacterController>();
        if (model == null || controller == null)
        {
            Debug.LogError("[FitBacon] Player 아래 '" + ModelName + "' 또는 CharacterController가 없습니다.");
            return;
        }

        Undo.RecordObject(model, "Fit Bacon To Player");

        // 1. 루트 모션 끄기: 클립의 Y 이동이 모델을 땅속으로 끌어내리는 것을 방지한다.
        var animator = model.GetComponent<Animator>();
        if (animator != null)
        {
            Undo.RecordObject(animator, "Fit Bacon To Player");
            animator.applyRootMotion = false;
        }

        // 2. 모델을 로컬 원점으로 두고 렌더러 바운드를 잰다.
        model.localPosition = Vector3.zero;
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("[FitBacon] 모델에 Renderer가 없습니다.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // 3. 콜라이더 바닥(월드 Y)과 모델 발바닥 높이의 차이만큼 올린다.
        float colliderBottom = player.transform.position.y + controller.center.y - controller.height * 0.5f;
        float offsetY = colliderBottom - bounds.min.y;
        model.position += new Vector3(0f, offsetY, 0f);

        EditorUtility.SetDirty(model);
        EditorSceneManager.MarkSceneDirty(model.gameObject.scene);
        Debug.Log($"[FitBacon] Y 보정 {offsetY:F3} 적용. 모델 높이 {bounds.size.y:F2} / 콜라이더 높이 {controller.height:F2}");
    }
}
#endif
