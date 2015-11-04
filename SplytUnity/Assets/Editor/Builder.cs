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
		// We are only excluding Builder.cs and PostprocessBuildPlayer.sh, since these should not be a part of the customer sample
		List<string> game_files = new List<string> {
			// bubble pop files
			"Assets/BubblePop.unity",
			"Assets/Plugins/Android/AndroidManifest.xml",
			"Assets/Resources",
			"Assets/Scripts",
		};
		game_files.AddRange(splyt_files);

		System.IO.Directory.CreateDirectory(Application.dataPath + "/../bin");
		AssetDatabase.ExportPackage(game_files.ToArray(), "../../bin/BubblePop.unitypackage", ExportPackageOptions.Recurse);
	}

	[MenuItem("Splyt/Export Splyt Package")]
	static void MakeSplytPackage()
	{
		System.IO.Directory.CreateDirectory(Application.dataPath + "/../../bin");
		AssetDatabase.ExportPackage(splyt_files, "../bin/Splyt.unitypackage", ExportPackageOptions.Recurse);
	}
}
