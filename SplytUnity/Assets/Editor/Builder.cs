using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Builder 
{
	static string[] splyt_files = new string[] {
		"Assets/Plugins/Splyt.dll",
		"Assets/Plugins/Splyt",
	};

	[MenuItem("Splyt/Export BubblePop Package")]
	static void MakeBPPackage()
	{
		List<string> game_files = new List<string> {
			"../samples/BubblePop/Assets/BubblePop.unity",
            "../samples/BubblePop/Assets/Resources",
            "../samples/BubblePop/Assets/Scripts",
		};
		game_files.AddRange(splyt_files);

		System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");
		AssetDatabase.ExportPackage(game_files.ToArray(), "../dist/BubblePop.unitypackage", ExportPackageOptions.Recurse);
	}

	[MenuItem("Splyt/Export Splyt Package")]
	static void MakeSplytPackage()
	{
		System.IO.Directory.CreateDirectory(Application.dataPath + "/../../dist");
		AssetDatabase.ExportPackage(splyt_files, "../dist/Splyt.unitypackage", ExportPackageOptions.Recurse);
	}
}
