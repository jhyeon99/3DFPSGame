using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

// 최적화 전후 비교용 성능 측정기. 지정한 프레임 수만큼 샘플링해 JSON으로 저장한다.
public class PerfSampler : MonoBehaviour
{
    private const int DefaultWarmupFrames = 120;
    private const int DefaultSampleFrames = 900;
    private const float MillisecondsPerSecond = 1000f;
    private const float BytesPerMegabyte = 1024f * 1024f;
    private const float P95Ratio = 0.95f;

    [SerializeField] private string _label = "after";
    [SerializeField] private string _outputFolder = "Docs/Lighting_Report";
    [SerializeField] private int _warmupFrames = DefaultWarmupFrames;
    [SerializeField] private int _sampleFrames = DefaultSampleFrames;

    private ProfilerRecorder _batches;
    private ProfilerRecorder _setPassCalls;
    private ProfilerRecorder _drawCalls;
    private ProfilerRecorder _triangles;
    private ProfilerRecorder _vertices;
    private ProfilerRecorder _shadowCasters;
    private ProfilerRecorder _gcAlloc;
    private ProfilerRecorder _usedMemory;

    public static void Begin(string label)
    {
        GameObject go = new GameObject("PerfSampler");
        PerfSampler sampler = go.AddComponent<PerfSampler>();
        sampler._label = label;
    }

    private void OnEnable()
    {
        _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        _shadowCasters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
        _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _usedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
    }

    private void OnDisable()
    {
        _batches.Dispose();
        _setPassCalls.Dispose();
        _drawCalls.Dispose();
        _triangles.Dispose();
        _vertices.Dispose();
        _shadowCasters.Dispose();
        _gcAlloc.Dispose();
        _usedMemory.Dispose();
    }

    private void Start()
    {
        StartCoroutine(Sample_Coroutine());
    }

    private IEnumerator Sample_Coroutine()
    {
        for (int i = 0; i < _warmupFrames; i++)
            yield return null;

        List<float> frameTimes = new List<float>(_sampleFrames);
        double batches = 0;
        double setPass = 0;
        double drawCalls = 0;
        double triangles = 0;
        double vertices = 0;
        double shadowCasters = 0;
        double gcAlloc = 0;
        double usedMemory = 0;

        for (int i = 0; i < _sampleFrames; i++)
        {
            yield return null;
            frameTimes.Add(Time.unscaledDeltaTime * MillisecondsPerSecond);
            batches += _batches.LastValue;
            setPass += _setPassCalls.LastValue;
            drawCalls += _drawCalls.LastValue;
            triangles += _triangles.LastValue;
            vertices += _vertices.LastValue;
            shadowCasters += _shadowCasters.LastValue;
            gcAlloc += _gcAlloc.LastValue;
            usedMemory += _usedMemory.LastValue;
        }

        Save(frameTimes, batches, setPass, drawCalls, triangles, vertices, shadowCasters, gcAlloc, usedMemory);
        Destroy(gameObject);
    }

    private void Save(List<float> frameTimes, double batches, double setPass, double drawCalls,
        double triangles, double vertices, double shadowCasters, double gcAlloc, double usedMemory)
    {
        int count = frameTimes.Count;
        float sum = 0f;
        float max = 0f;
        foreach (float t in frameTimes)
        {
            sum += t;
            if (t > max)
                max = t;
        }

        frameTimes.Sort();
        float avg = sum / count;
        float p95 = frameTimes[Mathf.Min(count - 1, (int)(count * P95Ratio))];

        string json = "{\n" +
            $"  \"label\": \"{_label}\",\n" +
            $"  \"frames\": {count},\n" +
            $"  \"avgFps\": {MillisecondsPerSecond / avg:F1},\n" +
            $"  \"avgFrameMs\": {avg:F2},\n" +
            $"  \"p95FrameMs\": {p95:F2},\n" +
            $"  \"maxFrameMs\": {max:F2},\n" +
            $"  \"batches\": {batches / count:F0},\n" +
            $"  \"setPassCalls\": {setPass / count:F0},\n" +
            $"  \"drawCalls\": {drawCalls / count:F0},\n" +
            $"  \"triangles\": {triangles / count:F0},\n" +
            $"  \"vertices\": {vertices / count:F0},\n" +
            $"  \"shadowCasters\": {shadowCasters / count:F0},\n" +
            $"  \"gcAllocBytesPerFrame\": {gcAlloc / count:F0},\n" +
            $"  \"totalUsedMemoryMB\": {usedMemory / count / BytesPerMegabyte:F1}\n" +
            "}\n";

        Directory.CreateDirectory(_outputFolder);
        File.WriteAllText(Path.Combine(_outputFolder, $"perf_{_label}.json"), json);
        GameLogger.Info($"[PerfSampler] 측정 완료: {_label}");
    }
}
