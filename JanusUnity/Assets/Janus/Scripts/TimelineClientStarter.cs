using UnityEngine;
using Janus;
using System.Collections.Generic;
using System.Reflection;
using System;

[AddComponentMenu("Janus/Timeline Client Starter")]
public class TimelineClientStarter : MonoBehaviour
{
	static TimelineClientStarter _instance;

	public bool AutoConnect = true;

	void Awake ()
	{
		if (_instance == null)
		{
			UnityTimelineUtils.SetDefautTimelineFunctions();
			DontDestroyOnLoad(this);
			_instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}
	}

	void Start ()
	{
		TimelineClient.Start(AutoConnect, true);
	}

	void OnDestroy ()
	{
		TimelineClient.Stop();

		if (_instance == this)
			_instance = null;
	}
}
