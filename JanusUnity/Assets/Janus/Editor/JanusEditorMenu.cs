using UnityEngine;
using UnityEditor;
using System;

public class JanusEditorMenu : Editor
{
	[MenuItem("Janus/Add Client Starter")]
	static void InsertSyncManager ()
	{
		Undo.RegisterCreatedObjectUndo(
			new GameObject("client_starter", new Type[] { typeof(TimelineClientStarter) }),
			"Timeline Client Starter");
	}
}
