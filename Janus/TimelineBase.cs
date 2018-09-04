using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Janus
{
	/// <summary>
	/// The base class of <see cref="Timeline&lt;T>"/>. Needed for untyped collections of timelines.
	/// </summary>
	public abstract class TimelineBase
	{
		internal class IDComparer : IEqualityComparer<byte[]>
		{
			public bool Equals (byte[] a, byte[] b)
			{
				return a.SequenceEqual(b);
			}

			public int GetHashCode (byte[] a)
			{
				return Encoding.UTF8.GetString(a).GetHashCode();
			}
		}

		/// <summary>
		/// Gets the unique ID of the timeline.
		/// </summary>
		public byte[] ID
		{
			get { return _id; }
		}

		/// <summary>
		/// Gets the manager of the timeline.
		/// </summary>
		public TimelineManager Manager
		{
			get { return _manager; }
		}

		/// <summary>
		/// Gets the current time as dictated by the manager. See <see cref="TimelineManager.Now"/>.
		/// </summary>
		public double Now
		{
			get { return _now; }
			set
			{
				if (_manager == null)
					_now = value;
				else throw new InvalidOperationException("Cannot set time on a timeline which is added to a manager.");
			}
		}

		/// <summary>
		/// Indicates whether or not the timeline is connected to the network.
		/// </summary>
		public bool IsConnected
		{
			get { return _isConnected; }
		}


		/// <summary>
		/// Time of the last message sent for this timeline
		/// </summary>
		public DateTime LastSendTime
		{
			get { return _lastSendTime; }
		}

		/// <summary>
		/// Gets or sets the timestamp mode of the timeline.
		/// </summary>
		public TimestampMode TimestampMode
		{
			get { return _timestampMode; }
			set { _timestampMode = value; }
		}

		/// <summary>
		/// Gets or sets the delivery mode of the timeline.
		/// </summary>
		public DeliveryMode DeliveryMode
		{
			get { return _deliveryMode; }
			set { _deliveryMode = value; }
		}

		/// <summary>
		/// Sets or gets the maximum allowed number of entries.
		/// </summary>
		public int MaxEntries
		{
			get { return _maxEntries; }
			set
			{
				_maxEntries = value;
				GuaranteeSize();
			}
		}

		/// <summary>
		/// Gets the current number of entries in the timeline.
		/// </summary>
		public int NumEntries
		{
			get { return _numEntries; }
		}

		/// <summary>
		/// Whether or not to ignore events about cached entries. False by default.
		/// </summary>
		public bool IgnoreCachedEvents
		{
			get { return _ignoreCachedEvents; }
			set { _ignoreCachedEvents = value; }
		}

		/// <summary>
		/// The number of entries to cache on the server.
		/// </summary>
		public int CacheSize
		{
			get { return _cacheSize; }
			set
			{
				_cacheSize = value;

				if (_isConnected)
				{
					byte[] data = new byte[sizeof(ushort) + sizeof(ushort)];
					BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
					writer.Write(_index);
					writer.Write((ushort)_cacheSize);
					writer.Close();

					_manager.EnqueueOutgoingMessage(new TimelineMessage(
						TimelineMessageType.CacheSize, data, DeliveryMode.ReliableOrdered));
				}
			}
		}

		/// <summary>
		/// User string tags for the timeline. Use TimelineTagsAttribute to set these.
		/// </summary>
		public HashSet<string> Tags
		{
			get { return _tags; }
		}

		/// <summary>
		/// Gets the type of values this timeline contains.
		/// </summary>
		public Type ValueType
		{
			get { return _valueType; }
		}

		/// <summary>
		/// The owner of this timeline. This is an arbitrary relationship that is completely user-defined.
		/// When using TimelineManager.AddTimelines, the obj argument becomes the owner of all the timelines it contains.
		/// </summary>
		public object Owner;

		internal static IDComparer _idComparer = new IDComparer();
		internal static int _defaultCacheSize = 3;

		internal TimelineManager _manager;
		internal double _now;
		internal bool _isConnected;
		internal byte[] _id;
		internal ushort _index;
		internal TimestampMode _timestampMode;
		internal DeliveryMode _deliveryMode;
		internal ushort _numGuaranteedSends;
		internal DateTime _lastSendTime;
		internal int _numEntries;
		internal int _maxEntries;
		internal bool _ignoreCachedEvents;
		internal int _cacheSize;
		internal HashSet<string> _tags;
		internal Type _valueType;

		/// <summary>
		/// The base class of Timelines. Needed for untyped collections of timelines.
		/// </summary>
		protected TimelineBase(byte[] id)
		{
			_id = id;
			_numGuaranteedSends = 3; 
			_lastSendTime = DateTime.Now;
			_timestampMode = TimestampMode.Absolute;
			_deliveryMode = DeliveryMode.ReliableOrdered;
			_maxEntries = 10;
			_numEntries = 0;
			_ignoreCachedEvents = false;
			_cacheSize = _defaultCacheSize;
			_tags = new HashSet<string>();
		}

		internal virtual void RemoteSet (double time, byte[] value, bool absoluteTime, bool isCached) { }

		internal virtual void Step () { }

		internal virtual void GuaranteeSize () { }
	}
}