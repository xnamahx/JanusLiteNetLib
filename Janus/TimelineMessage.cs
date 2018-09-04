using System;

namespace Janus
{
	/// <summary>
	/// Used to indicate the type of timeline message being sent over the network
	/// </summary>
	public enum TimelineMessageType : byte
	{
		/// <summary>
		/// Set base for messages all timeline messages start after 128 + 64 + 32
		/// </summary>
		ManagerMessageBase = (128 + 64 + 32),
		/// <summary>
		/// Message type for a new peer connecting
		/// </summary>
		InitializePeer,
		/// <summary>
		/// Message type for initial clock synchronization message
		/// </summary>
		ClockSyncPing,
		/// <summary>
		/// Message type for clock synchronization correction message
		/// </summary>
		ClockSyncCorrection,
		/// <summary>
		/// Message type for setting a timeline value based on absolute time - message from synchronizer to peer
		/// </summary>
		SetAbsolute,
		/// <summary>
		/// Message type for setting a timeline value based on relative time - message from synchronizer to peer
		/// </summary>
		SetRelative,
		/// <summary>
		/// Message type for setting a timeline value at current time - message from synchronizer to peer
		/// </summary>
		SetImmediate,
		/// <summary>
		/// Message type for setting a timeline value sent from the TimelineSynchronizer cached values based on absolute time
		/// </summary>
		SetCachedAbsolute,
		/// <summary>
		/// Message type for setting a timeline value sent from the TimelineSynchronizer cached values based on relative time
		/// </summary>
		SetCachedRelative,
		/// <summary>
		/// Message type for setting a timeline value sent from the TimelineSynchronizer cached values at current time
		/// </summary>
		SetCachedImmediate,
		/// <summary>
		/// Set base for messages. the following  message types start after 128 + 64 + 32 + 16
		/// </summary>
		SyncerMessageBase = (128 + 64 + 32 + 16),
		/// <summary>
		/// Message type for connecting a timeline
		/// </summary>
		ConnectTimeline,
		/// <summary>
		/// Message type for disconnecting a timeline
		/// </summary>
		DisconnectTimeline,
		/// <summary>
		/// Message type for reply to initial clock synchronization message
		/// </summary>
		ClockSyncPong,
		/// <summary>
		/// Message type for relaying a timeline value based on absolute time - message from peer to synchronizer
		/// </summary>
		RelayAbsolute,
		/// <summary>
		/// Message type for setting a timeline value based on relative time - message from peer to synchronizer
		/// </summary>
		RelayRelative,
		/// <summary>
		/// Message type for relaying a timeline value  at current time - message from peer to synchronize
		/// </summary>
		RelayImmediate,
		/// <summary>
		/// Message type for setting the number of cached values stored on the TimelineSchronizer
		/// </summary>
		CacheSize
	}

	/// <summary>
	/// Holds all the information contained in a timeline message
	/// </summary>
	public class TimelineMessage
	{
		/// <summary>
		/// Type of message
		/// </summary>
		public TimelineMessageType MessageType
		{
			get { return _messageType; }
		}

		/// <summary>
		/// Message payload
		/// </summary>
		public byte[] Data
		{
			get { return _data; }
		}

		/// <summary>
		/// Type of delivery, reliable, unreliable, ordered ...
		/// </summary>
		public DeliveryMode DeliveryMode
		{
			get { return _deliveryMode; }
		}

		internal TimelineMessageType _messageType;
		internal byte[] _data;
		internal DeliveryMode _deliveryMode;

		/// <summary>
		/// Create a new timeline message
		/// </summary>
		public TimelineMessage(TimelineMessageType messageType, byte[] data,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
		{
			_deliveryMode = deliveryMode;
			_messageType = messageType;
			_data = data;
		}
    }
}