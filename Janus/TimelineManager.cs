using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Janus
{


	/// <summary>
	/// Function to use to generate a byte id from a string name
	/// </summary>
	/// <param name="fieldName">String name of the timeline.</param>
	public delegate byte[] TimelineIDGenerator(string fieldName);

	/// <summary>
	/// Manages timelines locally. Provides a message interface to communicate with a remote synchronizer.
	/// </summary>
	public class TimelineManager
	{
		/// <summary>
		/// Gets the default instance of this class.
		/// </summary>
		/// <remarks>
		/// For convenience only in simple networking scenarios. Multiple instances are allowed.
		/// This member simply refers to the first instance created, and is used as the manager
		/// for independently instantiated timelines.
		/// </remarks>
		public static TimelineManager Default
		{
			get { return _default; }
		}

		static TimelineManager _default;

		/// <summary>
		/// Gets the current time in seconds.
		/// Incremented locally through <see cref="Step()"/> and corrected by synchronizer.
		/// </summary>
		public double Now
		{
			get { return _now; }
		}

		/// <summary>
		/// Returns an array of all Timelines
		/// </summary>
		public TimelineBase[] Timelines
		{
			get
			{
				lock (_timelineLock)
				{
					TimelineBase[] timelines = new TimelineBase[_timelinesByIndex.Count];
					_timelinesByIndex.Values.CopyTo(timelines, 0);
					return timelines;
				}
			}
		}

		/// <summary>
		/// Returns the number of Timelines
		/// </summary>
		public int NumTimelines
		{
			get
			{
				lock (_timelineLock)
				{
					return _timelinesByIndex.Count;
				}
			}
		}

		/// <summary>
		/// How quickly to correct clock sync offset. A factor multiplied by the error.
		/// </summary>
		public float ClockSyncCorrectionFactor = 1f;

		/// <summary>
		/// Minimum rate at which to correct clock sync offset.
		/// </summary>
		public float MinClockSyncCorrectionRate = 1f;

		double _now;

		/// <summary>
		/// Time offset to correct for the difference in time between the Timeline Manager and the Timeline Synchronizer
		/// May be different than the target offset because we need to make the correction over several frames
		/// </summary>
		double _timeOffset;
		/// <summary>
		/// Required time offset to correct for the difference in time between the Timeline Manager and the Timeline Synchronizer
		/// </summary>
		double _targetOffset;
		DateTime _prevFrameTime;
		Dictionary<ushort, TimelineBase> _timelinesByIndex;
		Dictionary<byte[], TimelineBase> _timelinesById;
		Queue<TimelineMessage> _outgoingMessages;
		ushort _nextTimelineIndex;
		object _timelineLock;

		static TimelineManager ()
		{
			_default = new TimelineManager();
		}

		/// <summary>
		/// Creates a new timeline manager.
		/// </summary>
		public TimelineManager ()
		{
			_timeOffset = 0;
			_targetOffset = 0;

			_timelinesByIndex = new Dictionary<ushort, TimelineBase>();
			_timelinesById = new Dictionary<byte[], TimelineBase>(TimelineBase._idComparer);
			_outgoingMessages = new Queue<TimelineMessage>();

			_prevFrameTime = DateTime.Now;
			_nextTimelineIndex = 0;
			_now = 0f;

			_timelineLock = new object();
		}

		/// <summary>
		/// Returns the timeline with a given ID and value type.
		/// If no timeline with the ID exists, it is created.
		/// If a timeline with the same ID but a different type exists, null is returned.
		/// </summary>
		public Timeline<T> Get<T> (string id)
		{
			return Get<T>(Encoding.UTF8.GetBytes(id));
		}

		/// <summary>
		/// Returns the timeline with a given ID and value type.
		/// If no timeline with the ID exists, it is created.
		/// If a timeline with the same ID but a different type exists, null is returned.
		/// </summary>
		public Timeline<T> Get<T> (ushort id)
		{
			return Get<T>(BitConverter.GetBytes(id));
		}

		/// <summary>
		/// Returns the timeline with a given ID and value type.
		/// If no timeline with the ID exists, it is created.
		/// If a timeline with the same ID but a different type exists, null is returned.
		/// </summary>
		public Timeline<T> Get<T> (byte[] id)
		{
			TimelineBase timeline;

			lock (_timelineLock)
			{
				if (!_timelinesById.TryGetValue(id, out timeline))
				{
					timeline = new Timeline<T>(id, false);
					Add(timeline);
				}
			}

			if (timeline._valueType == typeof(T))
				return (Timeline<T>)timeline;
			return null;
		}

		/// <summary>
		/// Returns the timeline with a given ID. If it does not exist, null is returned.
		/// </summary>
		public TimelineBase Get (string id)
		{
			return Get(Encoding.UTF8.GetBytes(id));
		}

		/// <summary>
		/// Returns the timeline with a given ID. If it does not exist, null is returned.
		/// </summary>
		public TimelineBase Get (ushort id)
		{
			return Get(BitConverter.GetBytes(id));
		}

		/// <summary>
		/// Returns the timeline with a given ID. If it does not exist, null is returned.
		/// </summary>
		public TimelineBase Get (byte[] id)
		{
			TimelineBase timeline = null;

			lock (_timelineLock)
			{
				_timelinesById.TryGetValue(id, out timeline);
				return timeline;
			}
		}

		/// <summary>
		/// Adds a timeline to the manager.
		/// </summary>
		public void Add (TimelineBase timeline)
		{
			if (timeline._manager == this)
				return;

			if (timeline._manager != null)
				timeline._manager.Remove(timeline);

			timeline._manager = this;
			timeline._index = _nextTimelineIndex;
			timeline._now = _now;

			lock (_timelineLock)
			{
				_timelinesById[timeline._id] = timeline;
				_timelinesByIndex[timeline._index] = timeline;
			}

			Connect(timeline);

			if (_nextTimelineIndex == ushort.MaxValue)
				_nextTimelineIndex = 0;
			else _nextTimelineIndex++;
		}
		
		/// <summary>
		/// Add an object's timeline fields.
		/// </summary>
		/// <param name="obj">The object whose timelines to add.</param>
		/// <param name="generateId">The ID generator to use.</param>
		/// <returns>An array containing the generated timelines.</returns>
		public TimelineBase[] AddTimelines (object obj, TimelineIDGenerator generateId = null)
		{
			List<TimelineBase> timelines = new List<TimelineBase>();
			Type objType = obj.GetType();

			// Loop through each field...
			foreach (FieldInfo field in objType.GetFields(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				Type fieldType = field.FieldType;

				// Only generate timelines for timeline fields.
				if (!fieldType.IsSubclassOf(typeof(TimelineBase)))
					continue;

				// Fetch the timeline from the field.
				TimelineBase timeline = (TimelineBase)field.GetValue(obj);

				// Don't add null timelines or timelines which already have managers.
				if (timeline == null || timeline._manager != null)
					continue;

				// Don't add timelines specifically marked with Ignore.
				if (field.GetCustomAttributes(typeof(IgnoreAttribute), false).Length != 0)
					continue;

				timeline.Owner = obj;

				var tagsAttrs = field.GetCustomAttributes(typeof(TimelineTagsAttribute), false);

				// Save timeline tags.
				if (tagsAttrs.Length != 0)
				{
					var tagsAttr = (TimelineTagsAttribute)tagsAttrs[0];

					foreach (var tag in tagsAttr.Tags)
						timeline._tags.Add(tag);
				}

				// Generate an ID for the timeline if it doesn't already have one.
				if (timeline._id == null)
					timeline._id = generateId(field.Name);

				// Add the timeline to this manager.
				Add(timeline);

				// Add the timeline to the list of timelines to return.
				timelines.Add(timeline);
			}

			return timelines.ToArray();
		}

		/// <summary>
		/// Returns an array of timelines from the given object which belong to this manager.
		/// </summary>
		/// <param name="obj">The object to inspect.</param>
		/// <returns>An array of timelines.</returns>
		public TimelineBase[] GetTimelines(object obj)
		{
			List<TimelineBase> timelines = new List<TimelineBase>();
			Type objType = obj.GetType();

			// Loop through each field...
			foreach (FieldInfo field in objType.GetFields(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				Type fieldType = field.FieldType;

				// Only generate timelines for timeline fields.
				if (!fieldType.IsSubclassOf(typeof(TimelineBase)))
					continue;

				// Fetch the timeline from the field.
				TimelineBase timeline = (TimelineBase)field.GetValue(obj);

				// Don't add null timelines or timelines which belong to other managers.
				if (timeline == null || timeline._manager != this)
					continue;

				// Don't add timelines specifically marked with Ignore.
				if (field.GetCustomAttributes(typeof(IgnoreAttribute), false).Length != 0)
					continue;

				// Add the timeline to the list of timelines to return.
				timelines.Add(timeline);
			}

			return timelines.ToArray();
		}

		/// <summary>
		/// Removes a timeline from the manager.
		/// </summary>
		public void Remove (TimelineBase timeline)
		{
			if (timeline == null || timeline._manager != this)
				return;

			Disconnect(timeline);

			lock (_timelineLock)
			{
				_timelinesById.Remove(timeline._id);
				_timelinesByIndex.Remove(timeline._index);
			}

			timeline._manager = null;
			timeline._index = 0;
		}

		/// <summary>
		/// Remove a Timeline from the Timeline Manager
		/// </summary>
		public void Remove (string id)
		{
			Remove(Encoding.UTF8.GetBytes(id));
		}

		/// <summary>
		/// Remove a Timeline from the Timeline Manager
		/// </summary>
		public void Remove(ushort id)
		{
			Remove(BitConverter.GetBytes(id));
		}

		/// <summary>
		/// Remove a Timeline from the Timeline Manager
		/// </summary>
		void Remove(byte[] id)
		{
			TimelineBase timeline;

			if (_timelinesById.TryGetValue(id, out timeline))
				Remove(timeline);
		}

		/// <summary>
		/// Remove all Timelines from the Timeline Manager
		/// </summary>
		public void Clear()
		{
			lock (_timelineLock)
			{
				TimelineBase[] timelines = new TimelineBase[_timelinesById.Count];
				_timelinesById.Values.CopyTo(timelines, 0);

				foreach (var timeline in timelines)
				{
					if (timeline._manager != this)
						return;

					Disconnect(timeline);

					timeline._manager = null;
					timeline._index = 0;
				}

				_timelinesById.Clear();
				_timelinesByIndex.Clear();
			}
		}

		/// <summary>
		/// Connects a timeline to the network. For internal use only.
		/// </summary>
		internal void Connect (TimelineBase timeline)
		{
			byte[] data = new byte[sizeof(ushort) + timeline._id.Length * sizeof(byte)];
			BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
			writer.Write(timeline._index);
			writer.Write(timeline._id);
			writer.Close();

			EnqueueOutgoingMessage(new TimelineMessage(TimelineMessageType.ConnectTimeline, data));

			timeline._isConnected = true;

			// Send a cache size message if the timeline wants a cache size different from the default.
			if (timeline._cacheSize != TimelineBase._defaultCacheSize)
				timeline.CacheSize = timeline._cacheSize;
		}

		/// <summary>
		/// Disconnects a timeline from the network. For internal use only.
		/// </summary>
		internal void Disconnect (TimelineBase timeline)
		{
			timeline._isConnected = false;

			byte[] data = new byte[sizeof(ushort)];
			BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
			writer.Write(timeline._index);
			writer.Close();

			EnqueueOutgoingMessage(new TimelineMessage(TimelineMessageType.DisconnectTimeline, data));
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
		/// Increments the local time and process messages.
		/// </summary>
		/// <param name="deltaTime">The time elapsed since the last step.</param>
		/// 

		public void Step (double deltaTime)
		{
			// Used to prevent overcorrection.
			var totalError = _targetOffset - _timeOffset;
			// Used to prevent backwards flow of time.
			var minAllowedCorrection = -deltaTime;

			// Speed of correction is based on the amount of error.
			// This results in an inverse-exponential rate of convergence.
			var correctionRate = totalError * ClockSyncCorrectionFactor;

			// Make sure that the rate of correction is never too low,
			// so as to avoid prolonged periods of "close-but-not-quite".
			// We do this by always correcting "as if" the error is at least a certain size.
			correctionRate = Math.Sign(correctionRate) * Math.Max(MinClockSyncCorrectionRate, Math.Abs(correctionRate));

			var correction = correctionRate * deltaTime;
			correction = Math.Max(Math.Min(correction, totalError), minAllowedCorrection);

			_now += deltaTime + correction;
			_timeOffset += correction;

			lock (_timelineLock)
			{
				var timelines = new TimelineBase[_timelinesById.Count];
				_timelinesById.Values.CopyTo(timelines, 0);

				foreach (TimelineBase timeline in timelines)
				{
					timeline._now = _now;
					timeline.Step();
				}
			}
		}

		/// <summary>
		/// Process incoming messages depending upon the message type
		/// </summary>
		public void ProcessIncomingMessage(TimelineMessage message)
		{
			if (message.MessageType == TimelineMessageType.SetAbsolute ||
				message.MessageType == TimelineMessageType.SetCachedAbsolute)
			{
				// message format:
				//     2 bytes (ushort) TimelineIndex
				//     8 bytes (double) time
				//     n bytes (byte[]) timelineData
				ushort timelineIndex = BitConverter.ToUInt16(message.Data, 0);

				lock (_timelineLock)
				{
					if (_timelinesByIndex.ContainsKey(timelineIndex))
					{
						double time = BitConverter.ToDouble(message.Data, sizeof(ushort));
						int valueBytesLength = message.Data.Length - sizeof(ushort) - sizeof(double);
						byte[] valueBytes = new byte[valueBytesLength];
						Array.Copy(message.Data, sizeof(ushort) + sizeof(double), valueBytes, 0, valueBytesLength);

						_timelinesByIndex[timelineIndex].RemoteSet(time, valueBytes, true,
							message.MessageType == TimelineMessageType.SetCachedAbsolute);
					}
				}
			}
			else if (message.MessageType == TimelineMessageType.InitializePeer)
			{
				// message format:
				//     8 bytes (double) time

				// initial synchronizing of time to the synchronizer

				_now = BitConverter.ToDouble(message.Data, 0);

				lock (_timelineLock)
				{
					foreach (var timeline in _timelinesByIndex.Values)
					{
						timeline._now = _now;
					}
				}
			}
			else if (message.MessageType == TimelineMessageType.ClockSyncPing)
			{
				// message from synchronizer with current time on synchonizer
				// message format 8 bytes (double)
				// need to reply with synchronizer time and current local time (16 bytes)

				byte[] data = new byte[sizeof(double) + sizeof(double)];
				BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
				writer.Write(message._data);
				writer.Write(_now - _timeOffset);
				writer.Close();

				EnqueueOutgoingMessage(new TimelineMessage(
					TimelineMessageType.ClockSyncPong, data, DeliveryMode.Unreliable));
			}
			else if (message.MessageType == TimelineMessageType.ClockSyncCorrection)
			{
				// message from synchronizer with adjustment to current time
				// message format 4 bytes (float)
				_targetOffset = BitConverter.ToSingle(message.Data, 0);
			}
		}

		/// <summary>
		/// Move all the messages in the outgoing message queue to an array and clear the queue
		/// </summary>
		public TimelineMessage[] GetOutgoingMessages()
		{
			TimelineMessage[] messages;

			lock (_outgoingMessages)
			{ 
				messages = _outgoingMessages.ToArray();
				_outgoingMessages.Clear();
			}

			return messages;
		}

		internal void EnqueueOutgoingMessage (TimelineMessage message)
		{
			lock (_outgoingMessages)
			{
				_outgoingMessages.Enqueue(message);
			}
		}
	}
}
