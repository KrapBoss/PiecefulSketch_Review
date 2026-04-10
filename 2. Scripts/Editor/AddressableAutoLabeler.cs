#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// 에셋 변경 사항을 감지하여 자동으로 어드레서블 라벨을 관리하는 클래스
/// </summary>
public class AddressableAutoLabeler : AssetPostprocessor
{
    private const string DEFAULT_LABEL = "default";

    /// <summary>
    /// 에셋이 추가, 삭제, 이동될 때 호출되는 엔진 콜백
    /// </summary>
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        ApplyDefaultLabels();
    }

    /// <summary>
    /// 모든 어드레서블 엔트리를 검사하여 라벨이 없는 경우 기본 라벨을 부여합니다.
    /// </summary>
    [MenuItem("Tools/Addressables/Force Apply Default Labels")]
    public static void ApplyDefaultLabels()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        // 라벨이 없으면 생성
        if (!settings.GetLabels().Contains(DEFAULT_LABEL))
        {
            settings.AddLabel(DEFAULT_LABEL);
        }

        bool isDirty = false;

        foreach (var group in settings.groups)
        {
            if (group.ReadOnly) continue; // Built-in 그룹 제외

            foreach (var entry in group.entries)
            {
                // 라벨이 하나도 없는 경우에만 부여
                if (entry.labels.Count == 0)
                {
                    entry.SetLabel(DEFAULT_LABEL, true);
                    isDirty = true;
                }
            }
        }

        if (isDirty)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[Addressables] 라벨 누락 에셋에 '{DEFAULT_LABEL}' 라벨을 자동 적용했습니다.");
        }
    }
}
#endif
