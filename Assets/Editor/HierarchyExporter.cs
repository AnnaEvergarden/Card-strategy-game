using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text;

public static class HierarchyExporter
{
    [MenuItem("Tools/导出 Hierarchy")]
    static void Export()
    {
        var sb = new StringBuilder();
        foreach (var root in UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().GetRootGameObjects())
            PrintHierarchy(root, 0, sb);

        var path = Application.dataPath + "/../hierarchy_export.txt";
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log("已导出到: " + path);
        EditorUtility.RevealInFinder(path);
    }

    static void PrintHierarchy(GameObject obj, int depth, StringBuilder sb)
    {
        var comps = string.Join(", ", obj.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => c.GetType().Name));
        var active = obj.activeInHierarchy ? "" : " [INACTIVE]";
        sb.AppendLine(new string(' ', depth * 2) + obj.name + active + " (" + comps + ")");
        foreach (Transform child in obj.transform)
            PrintHierarchy(child.gameObject, depth + 1, sb);
    }
}
