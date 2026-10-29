#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Attach Sword To Player 로 실행한다.
// 검을 오른손 본의 자식으로 붙이고(손잡이가 손바닥에 오도록), 스윙 컴포넌트를 연결한다.
public static class AttachSword
{
    private const string SwordDir = "Assets/05.Models/Sword";
    private const string SwordModelPath = SwordDir + "/Meshy_AI_Diamond_Sword_20261029_texture.fbx";
    private const string PlayerName = "Player";
    private const string SwordName = "Sword";
    private const float TargetLength = 1.1f;   // 검 전체 길이(m)
    private const float HandGripOffset = 0.08f; // 손목에서 손바닥 방향으로 이동할 거리(m)
    private const float GripRatio = 0.5f;       // 손잡이 끝 ~ 가드 사이에서 쥘 위치 비율
    private const int SliceCount = 24;

    [MenuItem("Tools/Attach Sword To Player")]
    public static void Run()
    {
        GameObject player = GameObject.Find(PlayerName);
        var swordModel = AssetDatabase.LoadAssetAtPath<GameObject>(SwordModelPath);
        if (player == null || swordModel == null)
        {
            Debug.LogError("[AttachSword] Player 또는 검 FBX를 찾지 못했습니다: " + SwordModelPath);
            return;
        }

        Transform hand = FindDeep(player.transform, "RightHand");
        Transform foreArm = FindDeep(player.transform, "RightForeArm");
        Transform upperArm = FindDeep(player.transform, "RightArm");
        if (hand == null || foreArm == null || upperArm == null)
        {
            Debug.LogError("[AttachSword] RightHand/RightForeArm/RightArm 본을 찾지 못했습니다.");
            return;
        }

        // 다시 실행해도 중복되지 않도록 기존 검을 지운다.
        Transform old = FindDeep(hand, SwordName);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordModel, player.scene);
        Undo.RegisterCreatedObjectUndo(sword, "Attach Sword");
        sword.name = SwordName;
        sword.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        sword.transform.localScale = Vector3.one;

        if (!MeasureSword(sword.transform, out Vector3 dirLocal, out Vector3 thinLocal,
                out Vector3 gripLocal, out float length))
        {
            Debug.LogError("[AttachSword] 검 메시를 분석하지 못했습니다.");
            return;
        }

        // 스케일: 검 길이를 TargetLength로 맞춘다.
        float scale = TargetLength / length;
        sword.transform.localScale = Vector3.one * scale;

        // 회전: 날 방향 -> 플레이어 정면, 검 납작한 면의 법선 -> 위(T포즈 기준).
        // 팔을 내리면 칼날은 정면을 유지하고 납작한 면은 옆을 향한다.
        Quaternion localFrame = Quaternion.LookRotation(thinLocal, dirLocal);
        Quaternion worldFrame = Quaternion.LookRotation(player.transform.up, player.transform.forward);
        Quaternion rotation = worldFrame * Quaternion.Inverse(localFrame);
        sword.transform.rotation = rotation;

        // 위치: 손잡이 중심이 손바닥에 오도록 한다.
        Vector3 handDirection = (hand.position - foreArm.position).normalized;
        Vector3 gripTarget = hand.position + handDirection * HandGripOffset;
        sword.transform.position = gripTarget - rotation * (gripLocal * scale);

        sword.transform.SetParent(hand, true);

        foreach (Collider c in sword.GetComponentsInChildren<Collider>())
        {
            Object.DestroyImmediate(c);
        }
        ApplyMaterial(sword);
        SetupSwing(player, upperArm, foreArm);

        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log($"[AttachSword] 완료. 검 길이 {length * scale:F2}m, 스케일 {scale:F3}");
    }

    // 검 루트 좌표계에서 날 방향, 납작한 면 방향, 손잡이 중심, 길이를 구한다.
    private static bool MeasureSword(Transform root, out Vector3 dir, out Vector3 thin, out Vector3 grip, out float length)
    {
        dir = Vector3.up;
        thin = Vector3.forward;
        grip = Vector3.zero;
        length = 1f;

        var points = new List<Vector3>();
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Matrix4x4 toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                points.Add(toRoot.MultiplyPoint3x4(v));
            }
        }
        if (points.Count == 0) return false;

        Bounds b = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 p in points) b.Encapsulate(p);

        // 가장 긴 축 = 날 방향, 가장 얇은 축 = 납작한 면의 법선
        int longAxis = 0;
        int thinAxis = 0;
        for (int i = 1; i < 3; i++)
        {
            if (b.size[i] > b.size[longAxis]) longAxis = i;
            if (b.size[i] < b.size[thinAxis]) thinAxis = i;
        }
        int widthAxis = 3 - longAxis - thinAxis;

        // 길이 방향을 구간으로 나눠, 폭이 가장 넓은 구간 = 가드
        float minL = b.min[longAxis];
        float maxL = b.max[longAxis];
        float step = (maxL - minL) / SliceCount;
        var widths = new float[SliceCount];
        foreach (Vector3 p in points)
        {
            int idx = Mathf.Min(SliceCount - 1, (int)((p[longAxis] - minL) / step));
            widths[idx] = Mathf.Max(widths[idx], Mathf.Abs(p[widthAxis] - b.center[widthAxis]));
        }
        int guardIdx = 0;
        for (int i = 1; i < SliceCount; i++)
        {
            if (widths[i] > widths[guardIdx]) guardIdx = i;
        }
        float guardPos = minL + (guardIdx + 0.5f) * step;

        // 가드와 가까운 끝 = 손잡이 끝
        bool handleAtMin = Mathf.Abs(guardPos - minL) < Mathf.Abs(maxL - guardPos);
        float handleEnd = handleAtMin ? minL : maxL;
        float gripPos = Mathf.Lerp(handleEnd, guardPos, GripRatio);

        dir = Vector3.zero;
        dir[longAxis] = handleAtMin ? 1f : -1f; // 손잡이 -> 날 끝
        thin = Vector3.zero;
        thin[thinAxis] = 1f;
        grip = b.center;
        grip[longAxis] = gripPos;
        length = maxL - minL;
        return true;
    }

    private static void ApplyMaterial(GameObject sword)
    {
        string baseName = "Meshy_AI_Diamond_Sword_20261029_texture";
        var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SwordDir + "/" + baseName + ".png");
        var normalPath = SwordDir + "/" + baseName + "_normal.png";
        var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        if (normalTex != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        string matPath = SwordDir + "/SwordMat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", baseTex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Metallic", 0.4f);
        mat.SetFloat("_Smoothness", 0.6f);
        if (normalTex != null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(mat);

        foreach (Renderer r in sword.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = mat;
        }
    }

    private static void SetupSwing(GameObject player, Transform upperArm, Transform foreArm)
    {
        var swing = player.GetComponent<PlayerSwordSwing>();
        if (swing == null)
        {
            swing = Undo.AddComponent<PlayerSwordSwing>(player);
        }

        var so = new SerializedObject(swing);
        so.FindProperty("_upperArm").objectReferenceValue = upperArm;
        so.FindProperty("_foreArm").objectReferenceValue = foreArm;
        so.ApplyModifiedProperties();
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }
}
#endif
