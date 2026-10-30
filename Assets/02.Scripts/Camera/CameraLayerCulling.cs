using System;
using UnityEngine;

// 레이어별 거리 컬링: 멀리 있는 작은 물체(소품)는 그리지 않아 렌더링 비용을 줄인다.
// Camera.layerCullDistances는 씬에 저장되지 않는 런타임 값이라 시작할 때 적용한다.
[RequireComponent(typeof(Camera))]
public class CameraLayerCulling : MonoBehaviour
{
    [Serializable]
    private struct LayerCullDistance
    {
        public string LayerName;
        public float Distance;
    }

    [SerializeField] private LayerCullDistance[] _layerCullDistances;

    private void Awake()
    {
        Camera targetCamera = GetComponent<Camera>();

        // 0이면 카메라의 far 클립 거리를 따르므로, 지정하지 않은 레이어는 영향이 없다.
        float[] distances = new float[32];
        foreach (LayerCullDistance entry in _layerCullDistances)
        {
            int layer = LayerMask.NameToLayer(entry.LayerName);
            if (layer < 0)
            {
                GameLogger.LogError($"[CameraLayerCulling] 레이어를 찾을 수 없습니다: {entry.LayerName}");
                continue;
            }

            distances[layer] = entry.Distance;
        }

        targetCamera.layerCullDistances = distances;
        targetCamera.layerCullSpherical = true;   // 거리를 구 기준으로 계산해 시점 회전과 무관하게 일정하게 컬링한다.
    }
}
