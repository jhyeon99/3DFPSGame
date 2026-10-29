#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Tools > Fix Bacon Model 로 실행한다.
// 1) FBX 내장 텍스처/머티리얼을 추출해 URP Lit 머티리얼로 연결한다. (회색 문제)
// 2) 플레이어 이동 애니메이션 클립을 Humanoid로 바꿔 Bacon 아바타를 공유한다. (애니메이션 문제)
public static class FixBaconModel
{
    private const string ModelPath = "Assets/05.Models/Roblox Bacon Hair.fbx";
    private const string OutDir = "Assets/05.Models/Bacon";
    private static readonly string[] ClipPaths =
    {
        "Assets/LowPolySoldiers_demo/animation/demo_combat_idle.FBX",
        "Assets/LowPolySoldiers_demo/animation/demo_combat_run.FBX",
    };

    // Unity Humanoid 본 이름 -> Varco FBX 본 이름
    private static readonly string[,] HumanMap =
    {
        { "Hips", "Hips" }, { "Spine", "Spine" }, { "Chest", "Spine1" }, { "UpperChest", "Spine2" },
        { "Neck", "Neck" }, { "Head", "Head" },
        { "LeftShoulder", "LeftShoulder" }, { "LeftUpperArm", "LeftArm" },
        { "LeftLowerArm", "LeftForeArm" }, { "LeftHand", "LeftHand" },
        { "RightShoulder", "RightShoulder" }, { "RightUpperArm", "RightArm" },
        { "RightLowerArm", "RightForeArm" }, { "RightHand", "RightHand" },
        { "LeftUpperLeg", "LeftUpLeg" }, { "LeftLowerLeg", "LeftLeg" },
        { "LeftFoot", "LeftFoot" }, { "LeftToes", "LeftToeBase" },
        { "RightUpperLeg", "RightUpLeg" }, { "RightLowerLeg", "RightLeg" },
        { "RightFoot", "RightFoot" }, { "RightToes", "RightToeBase" },
    };

    [MenuItem("Tools/Fix Bacon Model")]
    public static void Run()
    {
        FixRig();
        ResetModelPosition();
        FixMaterials();
        FixAnimations();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // 자동 매핑이 실패하므로(Hips not found) 본 매핑과 스켈레톤을 직접 지정한다.
    private static void FixRig()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Transform[] transforms = model.GetComponentsInChildren<Transform>(true);

        var skeleton = new System.Collections.Generic.List<SkeletonBone>();
        var boneNames = new System.Collections.Generic.HashSet<string>();
        foreach (Transform t in transforms)
        {
            boneNames.Add(t.name);
            skeleton.Add(new SkeletonBone
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale,
            });
        }

        var human = new System.Collections.Generic.List<HumanBone>();
        for (int i = 0; i < HumanMap.GetLength(0); i++)
        {
            if (!boneNames.Contains(HumanMap[i, 1]))
            {
                Debug.LogWarning("[FixBacon] 본을 찾지 못했습니다: " + HumanMap[i, 1]);
                continue;
            }

            var bone = new HumanBone { humanName = HumanMap[i, 0], boneName = HumanMap[i, 1] };
            bone.limit.useDefaultValues = true;
            human.Add(bone);
        }

        HumanDescription desc = importer.humanDescription;
        desc.human = human.ToArray();
        desc.skeleton = skeleton.ToArray();
        importer.humanDescription = desc;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();

        var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
        bool ok = avatar != null && avatar.isValid && avatar.isHuman;
        Debug.Log("[FixBacon] Bacon 아바타 " + (ok ? "유효(OK)" : "여전히 무효 - Rig > Configure 확인 필요"));
    }

    // 이전에 Fit 스크립트가 내려놓은 모델 위치를 원점으로 되돌린다.
    private static void ResetModelPosition()
    {
        GameObject player = GameObject.Find("Player");
        Transform model = player != null ? player.transform.Find("Roblox Bacon Hair") : null;
        if (model == null)
        {
            return;
        }

        Undo.RecordObject(model, "Reset Bacon Position");
        model.localPosition = Vector3.zero;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(model.gameObject.scene);
    }

    private static void FixMaterials()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        Directory.CreateDirectory(OutDir);

        importer.ExtractTextures(OutDir);
        AssetDatabase.Refresh();

        Texture2D baseColor = Find("base_color");
        Texture2D normal = Find("normal");
        if (baseColor == null)
        {
            Debug.LogError("base_color 텍스처를 찾지 못했습니다. " + OutDir + " 를 확인하세요.");
            return;
        }

        if (normal != null)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
            if (ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { name = "BaconMat" };
        mat.SetTexture("_BaseMap", baseColor);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.2f);
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        string matPath = OutDir + "/BaconMat.mat";
        AssetDatabase.CreateAsset(mat, matPath);

        // FBX의 모든 머티리얼 슬롯을 BaconMat으로 교체
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
            ModelImporterMaterialSearch.Local);
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (obj is Material m)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name),
                    AssetDatabase.LoadAssetAtPath<Material>(matPath));
            }
        }
        importer.SaveAndReimport();
        Debug.Log("[FixBacon] 머티리얼 연결 완료: " + matPath);
    }

    private static void FixAnimations()
    {
        // 모델(Bacon)의 Humanoid 아바타 검사
        var baconAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(ModelPath);
        if (baconAvatar == null || !baconAvatar.isValid || !baconAvatar.isHuman)
        {
            Debug.LogError("[FixBacon] Bacon 모델의 Humanoid 아바타가 유효하지 않습니다. " +
                           "FBX 선택 > Rig > Configure 에서 본 매핑(빨간 항목)을 수정하세요.");
        }

        // 클립은 스켈레톤(Bip001)이 달라서 Bacon 아바타를 복사하지 않고,
        // 각자의 모델로 Humanoid 아바타를 만들어 리타게팅한다.
        foreach (var path in ClipPaths)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp == null)
            {
                Debug.LogWarning("[FixBacon] 없음: " + path);
                continue;
            }

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();

            var clipAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            if (clipAvatar == null || !clipAvatar.isValid || !clipAvatar.isHuman)
            {
                Debug.LogError("[FixBacon] 클립 Humanoid 아바타 생성 실패(Rig > Configure에서 매핑 확인): " + path);
                continue;
            }

            // CharacterController가 이동을 담당하므로 루트 변환을 포즈에 고정(Bake)한다.
            // Y는 발 기준으로 맞춰 높이가 흔들리지 않게 한다.
            ModelImporterClipAnimation[] clips = imp.clipAnimations.Length > 0
                ? imp.clipAnimations
                : imp.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = false;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }
        Debug.Log("[FixBacon] 애니메이션 클립을 Humanoid(각자 아바타)로 변경 완료");
    }

    private static Texture2D Find(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Texture2D", new[] { OutDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).ToLower().Contains(name))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }
}
#endif
