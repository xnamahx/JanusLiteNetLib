using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Janus
{
	/// <summary>
	/// Triggered when a new peer connects to the TimelineSynchronizer
	/// </summary>
	/// <param name="peerIndex">Index number for the peer that connected.</param>
	public delegate void PeerConnectedHandler (ushort peerIndex);
	/// <summary>
	/// Triggered when a peer disconnects from the TimelineSynchronizer
	/// </summary>
	/// <param name="peerIndex">Index number for the peer that disconnected.</param>
	public delegate void PeerDisconnectedHandler(ushort peerIndex);
	/// <summary>
	/// Triggered after each clock synchronization
	/// </summary>
	/// <param name="peerIndex">Index number for the peer.</param>
	/// <param name="rtt">Round trip time in seconds between the peer and the TimelineSchronizer.</param>
	/// <param name="toTLS">Kilobytes per second from the peer to the TimelineSchronizer. Not currently imlemented.</param>
	/// <param name="fromTLS">Kilobytes per second from the TimelineSchronizer to the peer. Not currently imlemented.</param>
	public delegate void PeerUpdatedHandler(ushort peerIndex, float rtt, int toTLS, int fromTLS);
	/// <summary>
	/// Triggered when a timeline is first connected to the TimelineSynchronizer
	/// </summary>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineCreatedHandler(byte[] timelineId);
	/// <summary>
	/// Triggered when the number of  peers connected to a timeline changes, (but not from 0 to 1 or from 1 to 0)
	/// </summary>
	/// <param name="numConnections">Number of peers connecting to the timeline</param>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineUpdatedHandler(int numConnections, byte[] timelineId);
	/// <summary>
	/// Triggered when a new peer connects to an existing timeline
	/// </summary>
	/// <param name="peerIndex">Index number for the peer.</param>
	/// <param name="remoteTimelineIndex">Index number for the timeline on the TimelineSchronizer.</param>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineConnectedHandler(ushort peerIndex, ushort remoteTimelineIndex, byte[] timelineId);
	/// <summary>
	/// Triggered when a  peer disconnects from a timeline
	/// </summary>
	/// <param name="peerIndex">Index number for the peer.</param>
	/// <param name="remoteTimelineIndex">Index number for the timeline on the TimelineSchronizer.</param>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineDisconnectedHandler(ushort peerIndex, ushort remoteTimelineIndex, byte[] timelineId);
	/// <summary>
	/// Triggered when all peers have disconnected from a timeline
	/// </summary>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineDestroyedHandler(byte[] timelineId);
	/// <summary>
	/// Triggered when a set message is processed
	/// </summary>
	/// <param name="timelineIndex">Index number for the timeline on the TimelineSchronizer.</param>
	/// <param name="timelineId">String identifier for the timeline</param>
	public delegate void TimelineSetHandler(ushort timelineIndex, byte[] timelineId);

	/// <summary>
	/// The way clock sync data samples are selected for constructing an average.
	/// </summary>
	public enum ClockSyncSamplingRule
	{
		BestRTTs,
		MostRecent
	}
	
	/// <summary>
	/// Provides a message interface to synchronizes multiple, remote timeline managers.
	/// </summary>
	public class TimelineSynchronizer
	{
		/// <summary>
		/// Triggered when a new peer connects to the TimelineSynchronizer
		/// </summary>
		public event PeerConnectedHandler PeerConnected;
		/// <summary>
		/// Triggered when a peer disconnects from the TimelineSynchronizer
		/// </summary>
		public event PeerDisconnectedHandler PeerDisconnected;
		/// <summary>
		/// Triggered after each clock synchronization.  
		/// </summary>
		public event PeerUpdatedHandler PeerUpdated;
		/// <summary>
		/// Triggered when a timeline is first connected to the TimelineSynchronizer
		/// </summary>
		public event TimelineCreatedHandler TimelineCreated;
		/// <summary>
		/// Triggered when the number of  peers connected to a timeline changes, (but not from 0 to 1 or from 1 to 0)
		/// </summary>
		public event TimelineUpdatedHandler TimelineUpdated;
		/// <summary>
		/// Triggered when a new peer connects to an existing timeline
		/// </summary>
		public event TimelineConnectedHandler TimelineConnected;
		/// <summary>
		/// Triggered when a  peer disconnects from a timeline
		/// </summary>
		public event TimelineDisconnectedHandler TimelineDisconnected;
		/// <summary>
		/// Triggered when all peers have disconnected from a timeline
		/// </summary>
		public event TimelineDestroyedHandler TimelineDestroyed;
		/// <summary>
		/// Triggered when a set message is processed
		/// </summary>
		public event TimelineSetHandler TimelineSet;

		static Dictionary<TimelineMessageType, TimelineMessageType> _relayToSetMessageTypes =
			new Dictionary<TimelineMessageType, TimelineMessageType>()
		{
			{ TimelineMessageType.RelayAbsolute, TimelineMessageType.SetAbsolute },
			{ TimelineMessageType.RelayRelative, TimelineMessageType.SetRelative },
			{ TimelineMessageType.RelayImmediate, TimelineMessageType.SetImmediate }
		};

		static Dictionary<TimelineMessageType, TimelineMessageType> _relayToCacheMessageTypes =
			new Dictionary<TimelineMessageType, TimelineMessageType>()
		{
			{ TimelineMessageType.RelayAbsolute, TimelineMessageType.SetCachedAbsolute },
			{ TimelineMessageType.RelayRelative, TimelineMessageType.SetCachedRelative },
			{ TimelineMessageType.RelayImmediate, TimelineMessageType.SetCachedImmediate }
		};

        public class PeerInfo
		{
			public ushort Index;
			public Dictionary<ushort, TimelineInfo> ConnectedTimelines = new Dictionary<ushort, TimelineInfo>();
			public Queue<ClockSyncData> ClockSyncDataByAge = new Queue<ClockSyncData>();
			public List<ClockSyncData> ClockSyncDataByImportance = new List<ClockSyncData>();
			public Queue<TimelineMessage> OutgoingMessages = new Queue<TimelineMessage>();
			public int BytesToTLS = 0;
			public int BytesFromTLS = 0;
			public bool Pinging;
		}

        public class ClockSyncData
		{
			public float RTT;
			public float Offset;
		}

		public class TimelineInfo
		{
			public byte[] ID;
			public Dictionary<PeerInfo, ushort> ConnectedPeers = new Dictionary<PeerInfo, ushort>();
			public List<TimelineMessage> CachedEntries = new List<TimelineMessage>();
			public ushort EntryCacheSize = DefaultEntryCacheSize;
		}

		/// <summary>
		/// Default number of values to store on the TimelineSynchronizer
		/// </summary>
		public static ushort DefaultEntryCacheSize
		{
			get { return 3; }
		}

		/// <summary>
		/// Current time measured in seconds since the TimelineSynchronizer started
		/// </summary>
		public double Now
		{
			get { return _now; }
		}


		/// <summary>
		/// Frequency of ping messages used for synchronizing the clocks between
		/// the peers and the TimelineSynchronizer
		/// </summary>
		public float PingRate
		{
			get { return _pingRate; }
			set
			{
				_pingRate = value;
				_clockSyncHistorySize = (int)(60 * _pingRate);
			}
		}

		/// <summary>
		/// Array of the indices for all the peers currently connected to the TimelineSynchronizer
		/// </summary>
		public ushort[] PeerIndices
		{
			get
			{
				ushort[] peerIndices;
				lock (_peersByIndex)
				{
					peerIndices = new ushort[_peersByIndex.Count];
					_peersByIndex.Keys.CopyTo(peerIndices, 0);
				}
				return peerIndices;
			}
		}

		public ClockSyncSamplingRule ClockSyncSamplingRule = ClockSyncSamplingRule.BestRTTs;

        /// <summary>
        /// Obtain timeline values
        /// </summary>
        public float GetTimeline(byte[] timelineId, int relTime)
        {
            var retVal = 0f;

            if (_timelinesById.ContainsKey(timelineId))
            {
                retVal = TimelineManager.Default.Get<float>(timelineId)[relTime];
            }

            return retVal;
        }

        double _now;
		Dictionary<ushort, PeerInfo> _peersByIndex;
		Dictionary<byte[], TimelineInfo> _timelinesById;
		DateTime _prevFrameTime;
		float _pingRate;
		DateTime _lastPingTime;
		Thread _pingThread;
		bool _pinging;
		int _clockSyncHistorySize;
		object _peerLock;
		object _timelineLock;

		/// <summary>
		/// Creates a new timeline synchronizer.
		/// </summary>
		public TimelineSynchronizer()
		{
			_peersByIndex = new Dictionary<ushort, PeerInfo>();
			_timelinesById = new Dictionary<byte[], TimelineInfo>(TimelineBase._idComparer);
			_prevFrameTime = DateTime.Now;
			_lastPingTime = DateTime.Now;
			_now = 0f;

			_peerLock = new object();
			_timelineLock = new object();

			PingRate = .33f;
		}

		/// <summary>
		/// Connect a peer to the timeline synchronizer
		/// </summary>
		/// <param name="peerIndex">Index number for the peer that connected.</param>
		/// <param name="rtt">round trip time between the peer and the timeline synchronizer</param>
		public void ConnectPeer(ushort peerIndex, float rtt = 0f)
		{
			lock (_peerLock)
			{
				if (!_peersByIndex.ContainsKey(peerIndex))
				{
					PeerInfo peerInfo = new PeerInfo()
					{
						Index = peerIndex,
					};

					_peersByIndex.Add(peerIndex, peerInfo);

					// InitializePeer:
					// 8 bytes - initial time
					TimelineMessage initMessage = new TimelineMessage(
						TimelineMessageType.InitializePeer, BitConverter.GetBytes(_now + rtt / 2));


					peerInfo.OutgoingMessages.Enqueue(initMessage);

					// do a quick ping

					TimelineMessage pingMessage = new TimelineMessage(TimelineMessageType.ClockSyncPing,
							BitConverter.GetBytes(_now), DeliveryMode.Unreliable);

					lock (_peerLock)
					{
						peerInfo.OutgoingMessages.Enqueue(pingMessage);
					}

					if (PeerConnected != null)
						PeerConnected(peerIndex);
				}
			}
			return;
		}

		/// <summary>
		/// Disconnect a peer from the timeline synchronizer
		/// </summary>
		/// <param name="peerIndex">Index number for the peer that connected.</param>
		public void DisconnectPeer(ushort peerIndex)
		{
			PeerInfo peerInfo;
			lock (_peerLock)
			{
				if (_peersByIndex.TryGetValue(peerIndex, out peerInfo))
				{
					TimelineInfo[] connectedTimelines = new TimelineInfo[peerInfo.ConnectedTimelines.Count];
					peerInfo.ConnectedTimelines.Values.CopyTo(connectedTimelines, 0);

					foreach (var timelineInfo in connectedTimelines)
					{
						DisconnectPeerFromTimeline(peerInfo, timelineInfo);
					}

					_peersByIndex.Remove(peerIndex);

					if (PeerDisconnected != null)
						PeerDisconnected(peerIndex);
				}
			}
			return;
		}

		/// <summary>
		/// Connect a peer to a timeline. For internal use only.
		/// </summary>
		/// <param name="peerInfo">The peer which wants to connect to a timeline.</param>
		/// <param name="remoteTimelineIndex">The index used by the peer to refer to the timeline.</param>
		/// <param name="timelineId">The unique ID of the timeline.</param>
		void ConnectPeerToTimeline (PeerInfo peerInfo, ushort remoteTimelineIndex, byte[] timelineId)
		{
			// Check if timeline exists first. If not, create it.
			TimelineInfo timelineInfo;

			if (_timelinesById.ContainsKey(timelineId))
			{
				timelineInfo = _timelinesById[timelineId];
				if (TimelineUpdated != null)
					TimelineUpdated(timelineInfo.ConnectedPeers.Count + 1, timelineId);
			}
			else
			{
				timelineInfo = new TimelineInfo();
				timelineInfo.ID = timelineId;
				_timelinesById.Add(timelineId, timelineInfo);

				if (TimelineCreated != null)
					TimelineCreated(timelineId);
			}

			timelineInfo.ConnectedPeers[peerInfo] = remoteTimelineIndex;
			peerInfo.ConnectedTimelines[remoteTimelineIndex] = timelineInfo;

			// Send cached messages if there are any

			foreach (TimelineMessage cachedMessage in timelineInfo.CachedEntries)
			{
				TimelineMessage relayMessage = CreateSetCachedMessageFromTemplate(
					cachedMessage, remoteTimelineIndex);

				peerInfo.OutgoingMessages.Enqueue(relayMessage);
			}

			if (TimelineConnected != null)
				TimelineConnected(peerInfo.Index, remoteTimelineIndex, timelineId);
		}

		void DisconnectPeerFromTimeline (PeerInfo peerInfo, TimelineInfo timelineInfo)
		{
			// Disconnect peer from timeline.
			// Check if timeline is no longer connected to any peers. If so, remove timeline.

			ushort remoteTimelineIndex;

			if (timelineInfo.ConnectedPeers.TryGetValue(peerInfo, out remoteTimelineIndex))
			{
				peerInfo.ConnectedTimelines.Remove(remoteTimelineIndex);
				timelineInfo.ConnectedPeers.Remove(peerInfo);

				if (TimelineDisconnected != null)
					TimelineDisconnected(peerInfo.Index, remoteTimelineIndex, timelineInfo.ID);
			}
			else return;

			if (timelineInfo.ConnectedPeers.Count == 0)
			{
				_timelinesById.Remove(timelineInfo.ID);

				if (TimelineDestroyed != null)
					TimelineDestroyed(timelineInfo.ID);
			}
			else
			{
				if (TimelineUpdated != null)
					TimelineUpdated(timelineInfo.ConnectedPeers.Count, timelineInfo.ID);
			}
		}

		/// <summary>
		/// Increments the local time and processes messages.
		/// Time delta is automatically calculated from previous frame.
		/// </summary>
		public void Step ()
		{
			DateTime frameTime = DateTime.Now;
			Step(frameTime.Subtract(_prevFrameTime).TotalSeconds);
			_prevFrameTime = frameTime;
		}

		/// <summary>
		/// Increments the local time and processes messages.
		/// </summary>
		/// <param name="deltaTime">Step amount in seconds.</param>
		public void Step(double deltaTime)
		{
			_now += deltaTime;

			if (!_pinging && DateTime.Now.Subtract(_lastPingTime).TotalSeconds >= (1 / _pingRate))
			{
				_pingThread = new Thread(new ThreadStart(PingPeers));
				_pingThread.Start();
				_lastPingTime = DateTime.Now;
			}
		}

		/// <summary>
		/// Process a message from one of the peers
		/// </summary>
		/// <param name="peerIndex">Index number for the peer.</param>
		/// <param name="message">The message to be processed.</param>
		public void ProcessIncomingMessage(ushort peerIndex, TimelineMessage message)
		{
			lock (_peerLock)
			{
				lock (_timelineLock)
				{
					PeerInfo peerInfo = null;

					if (!_peersByIndex.TryGetValue(peerIndex, out peerInfo))
						return;

					peerInfo.BytesToTLS += message.Data.Length;

					// Be sure to arrange the message types in descending order of frequency.
					// For example, sets are most frequent, so they are checked first.

					if (message.MessageType == TimelineMessageType.RelayAbsolute)
					{
						BinaryReader reader = new BinaryReader(new MemoryStream(message.Data));
						// We only need to read the timeline index, since we are only relaying, not receiving.
						ushort timelineIndex = reader.ReadUInt16();
						reader.Close();

						// Get the timeline this message was intended for.
						TimelineInfo timelineInfo = null;
						
						if (!peerInfo.ConnectedTimelines.TryGetValue(timelineIndex, out timelineInfo))
							return;

						// Add message to cache queue
						timelineInfo.CachedEntries.Add(message);

						if (TimelineSet != null)
							TimelineSet(timelineIndex, timelineInfo.ID);

						while (timelineInfo.CachedEntries.Count > timelineInfo.EntryCacheSize)
						{
							timelineInfo.CachedEntries.RemoveAt(0);
						}

						// Loop through all the subscribers except for the sender...
						foreach (var otherPeer in timelineInfo.ConnectedPeers)
						{
							if (peerInfo == otherPeer.Key)
								continue;

							// Create a new message to relay.
							TimelineMessage relayMessage = CreateRelayMessageFromTemplate(message, otherPeer.Value);
							otherPeer.Key.OutgoingMessages.Enqueue(relayMessage);
						}
					}
					else if (message.MessageType == TimelineMessageType.ClockSyncPong)
					{
						BinaryReader reader = new BinaryReader(new MemoryStream(message.Data));
						double localPingTime = reader.ReadDouble();
						double remotePongTime = reader.ReadDouble();
						double localPongTime = (localPingTime + _now) / 2;
						reader.Close();

						ClockSyncData newData = new ClockSyncData()
						{
							RTT = (float)(_now - localPingTime),
							Offset = (float)(localPongTime - remotePongTime)
						};

						AddClockSyncData(peerInfo, newData);
						float correction = GetClockSyncCorrection(peerInfo);

						TimelineMessage correctionMessage = new TimelineMessage(TimelineMessageType.ClockSyncCorrection,
							BitConverter.GetBytes(correction), DeliveryMode.Unreliable);

						peerInfo.OutgoingMessages.Enqueue(correctionMessage);
						peerInfo.Pinging = false;
					}
					else if (message.MessageType == TimelineMessageType.ConnectTimeline)
					{
						// Message format:
						// 2 bytes (ushort) remote timeline index
						// byte array (byte[]) timeline id

						BinaryReader reader = new BinaryReader(new MemoryStream(message.Data));
						ushort remoteTimelineIndex = reader.ReadUInt16();
						byte[] timelineId = reader.ReadBytes(message.Data.Length - sizeof(ushort));
						reader.Close();

						ConnectPeerToTimeline(peerInfo, remoteTimelineIndex, timelineId);
					}
					else if (message.MessageType == TimelineMessageType.DisconnectTimeline)
					{
						// Call disconnect method
						BinaryReader reader = new BinaryReader(new MemoryStream(message.Data));
						ushort remoteTimelineIndex = reader.ReadUInt16();
						reader.Close();

						TimelineInfo timelineInfo;

						if (peerInfo.ConnectedTimelines.TryGetValue(remoteTimelineIndex, out timelineInfo))
							DisconnectPeerFromTimeline(peerInfo, timelineInfo);
					}
					else if (message.MessageType == TimelineMessageType.CacheSize)
					{
						BinaryReader reader = new BinaryReader(new MemoryStream(message.Data));
						ushort remoteTimelineIndex = reader.ReadUInt16();
						ushort cacheSize = reader.ReadUInt16();
						reader.Close();

						TimelineInfo timelineInfo;

						if (peerInfo.ConnectedTimelines.TryGetValue(remoteTimelineIndex, out timelineInfo))
						{
							timelineInfo.EntryCacheSize = cacheSize;

							while (timelineInfo.CachedEntries.Count > cacheSize)
							{
								timelineInfo.CachedEntries.RemoveAt(0);
							}
						}
					}
				}
			}
		}

		TimelineMessage CreateRelayMessageFromTemplate (TimelineMessage template, ushort remoteTimelineIndex)
		{
			byte[] relayBytes = new byte[template.Data.Length];

			Buffer.BlockCopy(BitConverter.GetBytes(remoteTimelineIndex), 0, relayBytes, 0, sizeof(ushort));
			Buffer.BlockCopy(template.Data, sizeof(ushort), relayBytes, sizeof(ushort),
				template.Data.Length - sizeof(ushort));

			TimelineMessage relayMessage = new TimelineMessage(
				_relayToSetMessageTypes[template.MessageType], relayBytes, template.DeliveryMode);

			return relayMessage;
		}

		TimelineMessage CreateSetCachedMessageFromTemplate (TimelineMessage template, ushort remoteTimelineIndex)
		{
			byte[] cacheBytes = new byte[template.Data.Length];

			Buffer.BlockCopy(BitConverter.GetBytes(remoteTimelineIndex), 0, cacheBytes, 0, sizeof(ushort));
			Buffer.BlockCopy(template.Data, sizeof(ushort), cacheBytes, sizeof(ushort),
				template.Data.Length - sizeof(ushort));

			TimelineMessage cacheMessage = new TimelineMessage(
				_relayToCacheMessageTypes[template.MessageType], cacheBytes, template.DeliveryMode);

			return cacheMessage;
		}

		void PingPeers ()
		{
			HashSet<PeerInfo> peerInfos;
			_pinging = true;

			lock (_peerLock)
			{
				peerInfos = new HashSet<PeerInfo>(_peersByIndex.Values);
			}

			foreach (PeerInfo peerInfo in peerInfos)
			{
				if (!_pinging)
					return;

				/*if (peerInfo.Pinging)
					continue;*/

				TimelineMessage pingMessage = new TimelineMessage(TimelineMessageType.ClockSyncPing,
					BitConverter.GetBytes(_now), DeliveryMode.Unreliable);

				lock (_peerLock)
				{
					peerInfo.OutgoingMessages.Enqueue(pingMessage);
					peerInfo.Pinging = true;
				}

				//Thread.Sleep(1);
			}

			_pinging = false;
			_pingThread = null;
		}

		void AddClockSyncData (PeerInfo peerInfo, ClockSyncData newData)
		{
			peerInfo.ClockSyncDataByAge.Enqueue(newData);

			int newDataIndex = 0;

			// Sort by RTT is using BestRTT sampling rule.
			// Otherwise, just "sort" by recency by inserting at index = 0.
			if (ClockSyncSamplingRule == ClockSyncSamplingRule.BestRTTs)
			{
				// Add the new clock sync data into the RTT-sorted list.
				// We keep a persistent list instead of sorting on the fly, since that would be slow.
				for (newDataIndex = 0; newDataIndex < peerInfo.ClockSyncDataByImportance.Count; newDataIndex++)
				{
					if (peerInfo.ClockSyncDataByImportance[newDataIndex].RTT >= newData.RTT)
						break;
				}
			}

			peerInfo.ClockSyncDataByImportance.Insert(newDataIndex, newData);

			// Limit clock sync history.
			while (peerInfo.ClockSyncDataByAge.Count > _clockSyncHistorySize)
			{
				// Here is an awesome way to remove from the queue and the RTT-sorted list at the same time.
				peerInfo.ClockSyncDataByImportance.Remove(peerInfo.ClockSyncDataByAge.Dequeue());
			}

			if (PeerUpdated != null)
			{
				PeerUpdated(peerInfo.Index, newData.RTT, (int)(peerInfo.BytesToTLS * _pingRate * 0.008), (int)(peerInfo.BytesFromTLS * _pingRate * 0.008));
			}
			peerInfo.BytesToTLS = 0;
			peerInfo.BytesFromTLS = 0;
		}

		float GetClockSyncCorrection (PeerInfo peerInfo)
		{
			// Just return the average correction of the ~half clock sync results that had the lowest RTT.
			int numBest = 1;
			if (peerInfo.ClockSyncDataByImportance.Count > 8)
			{
				numBest = Math.Min(_clockSyncHistorySize / 2, (peerInfo.ClockSyncDataByImportance.Count + 1) / 2);
			}
			float totalOffset = 0;

			for (int i = 0; i < numBest; i++)
			{
				totalOffset += peerInfo.ClockSyncDataByImportance[i].Offset;
			}

			return totalOffset / numBest;
		}

		/// <summary>
		/// Creates an array of all outgoing messages for one peer
		/// </summary>
		/// <param name="peerIndex">Index number for the peer</param>
		public TimelineMessage[] GetOutgoingMessages(ushort peerIndex)
		{
			PeerInfo peerInfo;
			TimelineMessage[] messages;

			lock (_peerLock)
			{
				if (!_peersByIndex.TryGetValue(peerIndex, out peerInfo))
					return new TimelineMessage[0];

				messages = peerInfo.OutgoingMessages.ToArray();
				foreach (TimelineMessage m in messages)
				{
					peerInfo.BytesFromTLS += m.Data.Length;
				}
				peerInfo.OutgoingMessages.Clear();
			}

			return messages;
		}
	}
}