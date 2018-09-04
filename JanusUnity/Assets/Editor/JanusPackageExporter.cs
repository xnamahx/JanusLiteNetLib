using UnityEngine;
using UnityEditor;

public class JanusPackageExporter : Editor
{
	[MenuItem("Janus/Build Package")]
	static void BuildPackage ()
	{
		string fileName = EditorUtility.SaveFilePanel(
			"Build Package", "", "Janus.unityPackage", "unityPackage");

		if (!string.IsNullOrEmpty(fileName))
		{
			AssetDatabase.ExportPackage(new string[] { "Assets/Janus", "Assets/Plugins" },
				fileName, ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);
		}
	}
}
