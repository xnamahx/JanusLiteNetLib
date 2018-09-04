using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Janus;

/// <summary>
/// Class to hold data for peers connected to the timeline server
/// </summary>
public class PeerData  {
		
	public  float rtt;
	public  int toTLS;
	public  int fromTLS;
	
	public PeerData (float rtt, int toTLS, int fromTLS)
	{
		this.rtt = rtt;
		this.toTLS = toTLS;
		this.fromTLS = fromTLS;
	}
	
	public PeerData ()
	{
		this.rtt = 0;
		this.toTLS = 0;
		this.fromTLS = 0;
	}

}

/// <summary>
/// A window that displays information about the peers and timelines that are
/// connected to the timeline server
/// </summary>
public class TimelineServerDebugWindow : MonoBehaviour
{

	public GUISkin customSkin;
	public KeyCode toggleKey = KeyCode.BackQuote;

	public int columnWidth = 100;
	public int timelineNameColumnWidth = 200;

	
	SortedDictionary<ushort, PeerData> peerMessages = new SortedDictionary<ushort,PeerData>();
	SortedDictionary<string, int> timelineMessages = new SortedDictionary<string,int>();
	
	bool show = true;
	
	// Visual elements:
	
	const int margin = 20;
    Rect windowRect;
	Vector2 scrollPos1 = new Vector2(0,0);
	Vector2 scrollPos2 = new Vector2(0,0);

	int scrollBarWidth = 16;
	int numPeerFields = 4;
	
	System.Object  _peerLock;
	System.Object  _timelineLock;
	
	void Start()
	{
		windowRect = new Rect(margin, margin, columnWidth*(numPeerFields+1) + timelineNameColumnWidth + (4 * margin) +(2 * scrollBarWidth), Screen.height * 0.7f);

		_peerLock = new System.Object();
		_timelineLock = new System.Object();
				
		TimelineServer.TimelineSynchronizer.PeerConnected += OnPeerConnected;
		TimelineServer.TimelineSynchronizer.PeerDisconnected += OnPeerDisconnected;
		TimelineServer.TimelineSynchronizer.PeerUpdated += OnPeerUpdated;	
		TimelineServer.TimelineSynchronizer.TimelineCreated += OnTimelineCreated;
		TimelineServer.TimelineSynchronizer.TimelineUpdated += OnTimelineUpdated;
		TimelineServer.TimelineSynchronizer.TimelineDestroyed += OnTimelineDestroyed;
	}

	public void Show()
	{
		show = true;
	}
	public void Hide()
	{
		show = false;
	}
	
	void Update()
	{
		if (Input.GetKeyDown(toggleKey))
		{
			show = !show;
		}
	}
	
	void OnGUI()
	{
		if (!show)
		{
			return;
		}
		windowRect = GUILayout.Window(17890, windowRect, DebugWindow, "Timeline Server Debug Window");
	}
	
	/// <summary>
	/// A window displaying the logged messages.
	/// </summary>
	/// <param name="windowID">The window's ID.</param>
	void DebugWindow(int windowID)
	{
		GUI.skin = customSkin;
		GUILayout.Space(margin);
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(margin);
		GUILayout.TextField("Peers", GUILayout.Width(columnWidth * numPeerFields + scrollBarWidth));
		GUILayout.Space(margin);
		GUILayout.TextField("Timelines", GUILayout.Width(timelineNameColumnWidth + columnWidth+scrollBarWidth));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(margin);
		GUILayout.TextField("Index",  GUILayout.Width(columnWidth));
		GUILayout.TextField("RTT (s)",  GUILayout.Width(columnWidth));		
		GUILayout.TextField("To TLS",  GUILayout.Width(columnWidth));		
		GUILayout.TextField("From TLS",  GUILayout.Width(columnWidth));
		GUILayout.TextField("",  GUILayout.Width(scrollBarWidth));
		GUILayout.Space(margin);
		GUILayout.TextField("Timeline Name",  GUILayout.Width(timelineNameColumnWidth));
		GUILayout.TextField("Connections",  GUILayout.Width(columnWidth));		
		GUILayout.TextField("",  GUILayout.Width(scrollBarWidth));		
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(margin);
		GUILayout.BeginScrollView(scrollPos1, GUILayout.Width ((columnWidth*numPeerFields)+scrollBarWidth));
		lock(_peerLock)
		{				
			foreach (KeyValuePair<ushort, PeerData> entry  in peerMessages)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(entry.Key.ToString(),  GUILayout.Width(columnWidth));
				GUILayout.Label(entry.Value.rtt.ToString("F3"),  GUILayout.Width(columnWidth));		
				GUILayout.Label(entry.Value.toTLS.ToString()+ " kbps",  GUILayout.Width(columnWidth));		
				GUILayout.Label(entry.Value.fromTLS.ToString()+ " kbps",  GUILayout.Width(columnWidth));		
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndScrollView();

		GUILayout.Space(margin);

		scrollPos2 = GUILayout.BeginScrollView(scrollPos2, GUILayout.Width(timelineNameColumnWidth + columnWidth+scrollBarWidth));
		lock(_timelineLock)
		{
			foreach (KeyValuePair<string, int> entry  in timelineMessages)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(entry.Key,  GUILayout.Width(timelineNameColumnWidth));
				GUILayout.Label(entry.Value.ToString(),  GUILayout.Width(columnWidth));		
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndScrollView();
		GUILayout.EndHorizontal();

		GUI.DragWindow(new Rect(0, 0, 10000, 20));

	}
	
	/// <summary>
	/// Peer connection messages are sent through this callback function.
	/// </summary>
	/// <param name="index">Id number for the peer</param>
	private void OnPeerConnected (ushort index)
	{
		lock(_peerLock)
		{
			if (!peerMessages.ContainsKey(index))
		    {
				peerMessages.Add (index, new PeerData());
			}
		}
	}
		
	/// <summary>
	/// Peer status update messages are sent through this callback function.
	/// </summary>
	/// <param name="index">Id number for the peer</param>
	/// <param name="rtt">Round trip time in seconds</param>
	/// <param name="toTLS">kbps received by the timeline server</param>
	/// <param name="fromTLS">kbps sent from the timeline server</param>
	private void OnPeerUpdated(ushort index, float rtt, int toTLS, int fromTLS)
	{
    	lock(_peerLock)
		{
		if (peerMessages.ContainsKey(index))
			{
				peerMessages[index].rtt = rtt;
				peerMessages[index].toTLS = toTLS;
				peerMessages[index].fromTLS = fromTLS;
			}
		}
	}
	
	/// <summary>
	/// Peer disconnection messages are sent through this callback function.
	/// </summary>
	/// <param name="index">Id number for the peer</param>
	private void OnPeerDisconnected (ushort index)
	{
		lock(_peerLock)
		{
			if (peerMessages.ContainsKey(index))
			{
				peerMessages.Remove(index);
			}
		}
	}
		
	/// <summary>
	/// Timeline creation messages are sent through this callback function.
	/// </summary>
	/// <param name="timelineId">Timeline unique identifier</param>
	private void OnTimelineCreated (byte[] timelineId)
	{		
		lock(_timelineLock)
		{
			string timelineName = Encoding.UTF8.GetString(timelineId);
			if (!timelineMessages.ContainsKey(timelineName))
			{
				timelineMessages.Add(timelineName, 1);
			}
		}
	}
	
	/// <summary>
	/// Timeline update messages are sent through this callback function.
	/// </summary>
	/// <param name="timelineId">Timeline unique identifier</param>
	private void OnTimelineUpdated(int numConnections, byte[] timelineId)
	{
		string timelineName = Encoding.UTF8.GetString(timelineId);
		lock(_timelineLock)
		{
			if (timelineMessages.ContainsKey(timelineName))
			{
				timelineMessages[timelineName] = numConnections;				
			}
		}
	}
		
	/// <summary>
	/// Timeline destroyed messages are sent through this callback function.
	/// </summary>
	/// <param name="timelineId">Timeline unique identifier</param>
	private void OnTimelineDestroyed (byte[] timelineId)
	{		
		string timelineName = Encoding.UTF8.GetString(timelineId);
		lock(_timelineLock)
		{			
			if (timelineMessages.ContainsKey(timelineName))
			{
				timelineMessages.Remove(timelineName);
			}			
		}		
	}	
}

