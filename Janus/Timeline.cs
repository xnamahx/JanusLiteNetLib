using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Janus
{
	/// <summary>
	/// Decodes an array of value bytes into a value.
	/// </summary>
	/// <param name="valueBytes">The encoded value bytes.</param>
	/// <returns>The decoded value.</returns>
	public delegate T TimelineDecoder<T> (byte[] valueBytes);

	/// <summary>
	/// Encodes a value into an array of value bytes.
	/// </summary>
	/// <param name="value">The value to encode.</param>
	/// <returns>The encoded value bytes.</returns>
	public delegate byte[] TimelineEncoder<T> (T value);

	/// <summary>
	/// Gets a value by interpolating from a <see cref="TimelineContext&lt;T>"/>.
	/// </summary>
	/// <param name="timeline">The timeline being interpolated</param>
	/// <param name="context">The context from which to interpolate.</param>
	/// <returns>The interpolated value.</returns>
	public delegate T TimelineInterpolator<T> (Timeline<T> timeline, TimelineContext<T> context);

	/// <summary>
	/// Gets a value by extrapolating from a <see cref="TimelineContext&lt;T>"/>.
	/// </summary>
	/// <param name="timeline">The timeline being interpolated</param>
	/// <param name="context">The context from which to extrapolate.</param>
	/// <returns>The extrapolated value.</returns>
	public delegate T TimelineExtrapolator<T> (Timeline<T> timeline, TimelineContext<T> context);

	/// <summary>
	/// Determines whether or not a change should be propagated.
	/// </summary>
	/// <returns>Whether or not the specified change should be propagated.</returns>
	public delegate bool TimelineSendFilter<T> (Timeline<T> timeline, TimelineEntry<T> entry);

	/// <summary>
	/// Responds to a value after it has been inserted, wether of remote or local origin.
	/// </summary>
	/// <param name="timeline">The timeline which raised the event.</param>
	/// <param name="entry">The entry which has been inserted.</param>
	public delegate void TimelineEntryHandler<T> (Timeline<T> timeline, TimelineEntry<T> entry);

	/// <summary>
	/// A timeline of values. This is the main interface for storing, manipulating, and synchronizing data in Janus.
	/// </summary>
	/// <typeparam name="T">The type of the values contained in this timeline.</typeparam>
	public partial class Timeline<T> : TimelineBase
	{
		/// <summary>
		/// Triggered when a new entry has been inserted, whether of remote or local origin.
		/// </summary>
		public event TimelineEntryHandler<T> EntryInserted;

		/// <summary>
		/// Triggered when a new remote entry has been inserted.
		/// </summary>
		public event TimelineEntryHandler<T> RemoteEntryInserted;

		/// <summary>
		/// Triggered when a new entry has been detected in the past, whether of remote or local origin.
		/// </summary>
		public event TimelineEntryHandler<T> EntryPassed;

		/// <summary>
		/// Triggered when an entry has just passed through <see cref="TimelineBase.Now"/>, whether of remote or local origin.
		/// </summary>
		public event TimelineEntryHandler<T> EntryMet;

		/// <summary>
		/// The unique ID of this timeline as a string.
		/// </summary>
		public string StringID
		{
			get { return Encoding.UTF8.GetString(_id); }
			set
			{
				if (_manager == null)
					_id = Encoding.UTF8.GetBytes(value);
				else throw new InvalidOperationException("Cannot change timeline ID while added to a manager.");
			}
		}

		/// <summary>
		/// The unique ID of this timeline as a numeral.
		/// </summary>
		public ushort NumericID
		{
			get { return BitConverter.ToUInt16(_id, 0); }
			set
			{
				if (_manager == null)
					_id = BitConverter.GetBytes(value);
				else throw new InvalidOperationException("Cannot change timeline ID while added to a manager.");
			}
		}

		/// <summary>
		/// Gets the absolute first entry in the timeline.
		/// </summary>
		public TimelineEntry<T> FirstEntry
		{
			get { return _firstEntry; }
		}

		/// <summary>
		/// Gets the time of the absolute first entry in the timeline.
		/// </summary>
		public double FirstTime
		{
			get
			{
				if (_firstEntry != null)
					return _firstEntry._time;
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of the absolute first entry in the timeline.
		/// </summary>
		public T FirstValue
		{
			get
			{
				if (_firstEntry != null)
					return _firstEntry._value;
				return default(T);
			}
		}
		
		/// <summary>
		/// Gets the absolute last entry in the timeline.
		/// </summary>
		public TimelineEntry<T> LastEntry
		{
			get { return _lastEntry; }
		}

		/// <summary>
		/// Gets the time of the absolute last entry in the timeline.
		/// </summary>
		public double LastTime
		{
			get
			{
				if (_lastEntry != null)
					return _lastEntry._time;
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of the absolute last entry in the timeline.
		/// </summary>
		public T LastValue
		{
			get
			{
				if (_lastEntry != null)
					return _lastEntry._value;
				return default(T);
			}
		}

		/// <summary>
		/// Finds the duration of the last value in the timeline. This includes all trailing entries with the same value.
		/// </summary>
		public float LastValueDuration
		{
			get
			{
				if (_lastEntry == null)
					return 0f;

				var lastSameEntry = _lastEntry;

				while (lastSameEntry._prev != null && lastSameEntry._prev._value.Equals(lastSameEntry._value))
				{
					lastSameEntry = lastSameEntry._prev;
				}

				return Math.Max(0f, (float)(_now - lastSameEntry._time));
			}
		}

		/// <summary>
		/// Gets the last entry sent to the remote clients
		/// </summary>
		public TimelineEntry<T> LastSentEntry
		{
			get { return _lastSentEntry; }
		}

		/// <summary>
		/// Gets the second last entry sent to the remote clients
		/// </summary>
		public TimelineEntry<T> LastLastSentEntry
		{
			get { return _lastLastSentEntry; }
		}

		/// <summary>
		/// Gets the immediate entry before the current time.
		/// </summary>
		public TimelineEntry<T> PreviousEntry
		{
			get { return _prevEntry; }
		}

		/// <summary>
		/// Gets the time of the immediate entry before the current time.
		/// </summary>
		public double PreviousTime
		{
			get
			{
				if (_prevEntry != null)
					return _prevEntry._time;
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of the immediate entry before the current time.
		/// </summary>
		public T PreviousValue
		{
			get
			{
				if (_prevEntry != null)
					return _prevEntry._value;
				return default(T);
			}
		}

		/// <summary>
		/// Gets the immediate entry after the current time.
		/// </summary>
		public TimelineEntry<T> NextEntry
		{
			get { return _nextEntry; }
		}

		/// <summary>
		/// Gets the time of the immediate entry after the current time.
		/// </summary>
		public double NextTime
		{
			get
			{
				if (_nextEntry != null)
					return _nextEntry._time;
				return 0;
			}
		}

		/// <summary>
		/// Gets the value of the immediate entry after the current time.
		/// </summary>
		public T NextValue
		{
			get
			{
				if (_nextEntry != null)
					return _nextEntry._value;
				return default(T);
			}
		}

		/// <summary>
		/// Gets if the timeline is empty (has no entries).
		/// </summary>
		public bool IsEmpty
		{
			get { return _numEntries == 0; }
		}

		/// <summary>
		/// Gets the send filters active on this timeline.
		/// </summary>
		public TimelineSendFilter<T>[] SendFilters
		{
			get { return _sendFilters.ToArray(); }
		}

		/// <summary>
		/// Specifies the default function used to convert a byte array to a timeline value for each timeline type
		/// </summary>
		/// <code>
		/// public class Position
		/// {
		///     public int x;
		///     public int y;
		///     
		/// 	public Position(int x, int y)
		///     {
		///	        this.x = x;
		///	        this.y = y;
		///     }
		///
		///     public static Position DecodePosition(byte[] bytes)
		///     {
		///	        BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		///	        var value = new Position(br.ReadInt32(), br.ReadInt32());
		///	        br.Close();
		///	        return value;
		///     }
		///     
		///     public static void SetDefautTimelineFunctions()
		///     {
		///	        Timeline &lt; Position &gt; .TypeDecode = DecodePosition;
		///	    }   
		/// }    
		/// </code>
		public static TimelineDecoder<T> TypeDecode;
		/// <summary>
		/// Specifies the default function used to convert a timeline value to a byte array for each timeline type
		/// </summary>
		/// <code>
		/// public class Position
		/// {
		///     public int x;
		///     public int y;
		///     
		/// 	public Position(int x, int y)
		///     {
		///	        this.x = x;
		///	        this.y = y;
		///     }
		///
		///     public static byte[] EncodePosition(Position value)
		///     {
		///         byte[] bytes = new byte[2 * sizeof(Int32)];
		///         BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		///         bw.Write(value.x); 
		///         bw.Write(value.y);
		///         bw.Close();
		///         return bytes;
		///     }
		///     
		///     public static void SetDefautTimelineFunctions()
		///     {
		///	        Timeline &lt; Position &gt; .TypeEncode = EncodePosition;
		///	    }   
		/// }    
		/// 
		/// </code>
		public static TimelineEncoder<T> TypeEncode;
		/// <summary>
		/// Specifies the default function used for interpolation for each timeline type
		/// </summary>
		public static TimelineInterpolator<T> TypeInterpolate;
		/// <summary>
		/// Specifies the default function used for extrapolation for each timeline type
		/// </summary>
		public static TimelineExtrapolator<T> TypeExtrapolate;
		
		static XmlSerializer _xmlSerializer;

		/// <summary>
		/// Specifies the function used to convert a byte array to a timeline value for a timeline
		/// </summary>
		public TimelineDecoder<T> Decode;
		/// <summary>
		/// Specifies the function used to convert a timeline value to a byte array for a timeline
		/// </summary>
		public TimelineEncoder<T> Encode;
		/// <summary>
		/// Specifies the function used for interpolation for a timeline
		/// </summary>
		public TimelineInterpolator<T> Interpolate;
		/// <summary>
		/// Specifies the function used for extrapolation for a timeline
		/// </summary>
		public TimelineExtrapolator<T> Extrapolate;

		Queue<TimelineSendFilter<T>> _sendFilters;
		TimelineEntry<T> _firstEntry;
		TimelineEntry<T> _prevEntry;
		TimelineEntry<T> _nextEntry;
		TimelineEntry<T> _lastEntry;
		TimelineEntry<T> _lastSentEntry;
		TimelineEntry<T> _lastLastSentEntry;
		Queue<TimelineEntry<T>> _delayedInsertionEvents;
		Queue<TimelineEntry<T>> _delayedRemoteInsertionEvents;
		Queue<TimelineEntry<T>> _delayedPassingEvents;
		object _entryLock = new object();

		static Timeline ()
		{
			TimelineUtils.SetDefaultTimelineFunctions();
		}

		/// <summary>
		/// Creates a timeline with no name and not added to the TimelineManager
		/// </summary>
		public Timeline ()
			: this((string)null, false)
		{
		}

		/// <summary>
		/// Creates a timeline. This is equivalent to calling <see cref="TimelineManager.Get(string)"/>
		/// on <see cref="TimelineManager.Default"/> for the first time.
		/// <para/>
		/// </summary>
		/// <param name="id">The unique ID of the timeline.</param>
		/// <param name="autoAdd">Whether or not to connect this timeline automatically.</param>
		/// <remarks>
		/// Use of this constructor is discouraged, as it will fail if another timeline with the same ID exists.
		/// <see cref="TimelineManager.Get(string)"/> will check for an existing timeline and return it.
		/// </remarks>
		public Timeline (string id, bool autoAdd = true)
			: this(id != null ? Encoding.UTF8.GetBytes(id) : null, autoAdd)
		{
		}

		/// <summary>
		/// Creates a timeline. This is equivalent to calling <see cref="TimelineManager.Get(byte[])"/>
		/// on <see cref="TimelineManager.Default"/> for the first time.
		/// <para/>
		/// </summary>
		/// <param name="id">The unique ID of the timeline.</param>
		/// <param name="autoAdd">Whether or not to connect this timeline automatically.</param>
		/// <remarks>
		/// Use of this constructor is discouraged, as it will fail if another timeline with the same ID exists.
		/// <see cref="TimelineManager.Get(byte[])"/> will check for an existing timeline and return it.
		/// </remarks>
		public Timeline (ushort id, bool autoAdd = true)
			: this(BitConverter.GetBytes(id), autoAdd)
		{
		}

		/// <summary>
		/// Creates a timeline. For internal use only.
		/// </summary>
		/// <param name="id">The unique ID of the timeline.</param>
		/// <param name="autoAdd">Whether or not to connect this timeline automatically.</param>
		public Timeline (byte[] id, bool autoAdd = true)
			: base(id)
		{
			_valueType = typeof(T);

			if (TypeDecode != null)
				Decode = TypeDecode;
			else Decode = XmlDecode;

			if (TypeEncode != null)
				Encode = TypeEncode;
			else Encode = XmlEncode;

			// The default interpolator simply returns the previous value (stepping).
			if (TypeInterpolate != null)
				Interpolate = TypeInterpolate;
			else Interpolate = TimelineUtils.SteppingInterpolate<T>;

			// The default extrapolator simply returns the previous value (stepping).
			if (TypeExtrapolate != null)
				Extrapolate = TypeExtrapolate;
			else Extrapolate = TimelineUtils.SteppingExtrapolate<T>;

			_sendFilters = new Queue<TimelineSendFilter<T>>();

			_delayedInsertionEvents = new Queue<TimelineEntry<T>>();
			_delayedRemoteInsertionEvents = new Queue<TimelineEntry<T>>();
			_delayedPassingEvents = new Queue<TimelineEntry<T>>();

			if (autoAdd)
				TimelineManager.Default.Add(this);
		}

		/// <summary>
		/// Adds the specified send filter to a timeline
		/// </summary>
		public void AddSendFilter (TimelineSendFilter<T> sendFilter)
		{
			_sendFilters.Enqueue(sendFilter);
		}

		/// <summary>
		/// Removes all send filters from a timeline
		/// </summary>
		public void ClearSendFilters()
		{
			_sendFilters.Clear();
		}

		/// <summary>
		/// Gets or sets the value at a particular time.
		/// Equivalent to using <see cref="Set"/> and <see cref="Get"/>.
		/// </summary>
		/// <param name="time">The time at which to get or set the value.</param>
		/// <returns>The value at the specified time.</returns>
		public T this[double time]
		{
			get { return Get(time); }
			set { Set(time, value); }
		}

		/// <summary>
		/// Gets the value at a particular time.
		/// </summary>
		/// <param name="time">The time at which to get the value.</param>
		/// <param name="isTimeAbsolute">Whether or not the specified time is absolute.</param>
		/// <returns>The value at the specified time.</returns>
		public T Get (double time, bool isTimeAbsolute = false)
		{
			T result;

			lock (_entryLock)
			{
				// check that there is at least one value in the timeline
				if (_firstEntry == null)
				{
					return default(T);
				}

				double absoluteTime = isTimeAbsolute ? time : _now + time;

				TimelineContext<T> currentContext = new TimelineContext<T>();
				currentContext._time = absoluteTime;

				// set Context
				if (currentContext._time == _now)  // time 0 so no need to search
				{
					currentContext._prev = _prevEntry;
				}
				else // starting at _lastEntry search back in time
				{
					currentContext._prev = _lastEntry;
					while (currentContext._prev != null && currentContext._prev._time > absoluteTime)
					{
						currentContext._prev = currentContext._prev._prev;
					}
				}

				if (currentContext._prev == null) // all entries are after the requested time
				{
					return _firstEntry._value;
				}
				if (currentContext._prev._time == absoluteTime)
				{
					return currentContext._prev._value;
				}

				currentContext._next = currentContext._prev._next;
				currentContext._prevPrev = currentContext._prev._prev;

				if (currentContext._next == null)
				{
					result = Extrapolate(this, currentContext);
				}
				else
				{
					result = Interpolate(this, currentContext);
				}
			}

			return result;
		}
		
		/// <summary>
		/// Gets the list of entries which fall within a time range.
		/// </summary>
		/// <param name="startTime">The lower time bound.</param>
		/// <param name="isStartTimeInclusive">Whether or not the start time is inclusive.</param>
		/// <param name="endTime">The upper time bound.</param>
		/// <param name="isEndTimeInclusive">Whether or not the end time is inclusive.</param>
		/// <param name="isTimeAbsolute">Whether or not te specified time is absolute.</param>
		/// <returns>The list of entries which fall within the specified time range.</returns>
		public TimelineEntry<T>[] GetRange (double startTime, bool isStartTimeInclusive,
			double endTime, bool isEndTimeInclusive, bool isTimeAbsolute = false)
		{
			double absStartTime = isTimeAbsolute ? startTime : _now + startTime;
			double absEndTime = isTimeAbsolute ? endTime : _now + endTime;

			Func<double, bool> isTimeInRange;

			if (isStartTimeInclusive)
			{
				if (isEndTimeInclusive)
					isTimeInRange = time => time >= absStartTime && time <= absEndTime;
				else isTimeInRange = time => time >= absStartTime && time < absEndTime;
			}
			else
			{
				if (isEndTimeInclusive)
					isTimeInRange = time => time > absStartTime && time <= absEndTime;
				else isTimeInRange = time => time > absStartTime && time < absEndTime;
			}

			List<TimelineEntry<T>> entriesInRange = new List<TimelineEntry<T>>();

			TimelineEntry<T> currentEntry = _firstEntry;

			lock (_entryLock)
			{
				while (currentEntry != null)
				{
					if (isTimeInRange(currentEntry._time))
						entriesInRange.Add(currentEntry);

					currentEntry = currentEntry._next;
				}
			}

			return entriesInRange.ToArray();
		}

		/// <summary>
		/// Removes all entries which fall within a time range.
		/// </summary>
		/// <param name="startTime">The lower time bound.</param>
		/// <param name="isStartTimeInclusive">Whether or not the start time is inclusive.</param>
		/// <param name="endTime">The upper time bound.</param>
		/// <param name="isEndTimeInclusive">Whether or not the end time is inclusive.</param>
		/// <param name="isTimeAbsolute">Whether or not te specified time is absolute.</param>
		/// <returns>The number of entries removed.</returns>
		public int RemoveRange (double startTime, bool isStartTimeInclusive,
			double endTime, bool isEndTimeInclusive, bool isTimeAbsolute = false)
		{
			double absStartTime = isTimeAbsolute ? startTime : _now + startTime;
			double absEndTime = isTimeAbsolute ? endTime : _now + endTime;
			int numRemoved = 0;

			Func<double, bool> isTimeInRange;

			if (isStartTimeInclusive)
			{
				if (isEndTimeInclusive)
					isTimeInRange = time => time >= absStartTime && time <= absEndTime;
				else isTimeInRange = time => time >= absStartTime && time < absEndTime;
			}
			else
			{
				if (isEndTimeInclusive)
					isTimeInRange = time => time > absStartTime && time <= absEndTime;
				else isTimeInRange = time => time > absStartTime && time < absEndTime;
			}

			TimelineEntry<T> currentEntry = _firstEntry;
			TimelineEntry<T> beforeRemoveEntry = null;
			TimelineEntry<T> afterRemoveEntry = null;

			lock (_entryLock)
			{
				// find the entry before the first entry to be removed
				while (currentEntry != null && (currentEntry._time < absStartTime || 
						 (isStartTimeInclusive && currentEntry._time == absStartTime)))
				{
					beforeRemoveEntry = currentEntry;
					currentEntry = currentEntry._next;
				}

				// find the entry after the last entry to be removed
				while (currentEntry != null && (currentEntry._time < absEndTime || 
						 (isEndTimeInclusive && currentEntry._time == absEndTime)))
				{
					currentEntry = currentEntry._next;
					afterRemoveEntry = currentEntry;
					numRemoved++;
				}
				// join together the two entries: beforeRemoveEntry and afterRemoveEntry

				if (beforeRemoveEntry != null)
				{
					beforeRemoveEntry._next = afterRemoveEntry;
				}
				if (afterRemoveEntry != null)
				{
					afterRemoveEntry._prev = beforeRemoveEntry;
				}

				// fix up _firstEntry, _lastEntry, _prevEntry and _next Entry if they have been 
				// affected by the removal
				if (_prevEntry != null && isTimeInRange(_prevEntry._time)) _prevEntry = beforeRemoveEntry;
				if (_nextEntry != null && isTimeInRange(_nextEntry._time)) _nextEntry = afterRemoveEntry;
				if (beforeRemoveEntry == null) _firstEntry = afterRemoveEntry;
				if (afterRemoveEntry == null) _lastEntry = beforeRemoveEntry;
				_numEntries -= numRemoved;
			}
			return numRemoved;
		}

		/// <summary>
		/// Adds a value to the timeline at the current time.
		/// </summary>
		/// <param name="value">The value to insert.</param>
		/// <remarks>
		/// Entry will be processed. Insertion is not guaranteed. Insertion may be propagated, depending on settings.
		/// </remarks>
		public void Set (T value)
		{
			Set(_now, value);
		}

		/// <summary>
		/// Adds a value to the timeline.
		/// </summary>
		/// <param name="time">The time at which to insert the value.</param>
		/// <param name="value">The value to insert.</param>
		/// <param name="isTimeAbsolute">Whether or not te specified time is absolute.</param>
		/// <remarks>
		/// Entry will be processed. Insertion is not guaranteed. Insertion may be propagated, depending on settings.
		/// </remarks>
		public void Set (double time, T value, bool isTimeAbsolute = false)
		{
			double absoluteTime = isTimeAbsolute ? time : _now + time;
			var currentEntry = new TimelineEntry<T>(absoluteTime, value);

			Insert(currentEntry, false);

			// message format:
			//     2 bytes (ushort) timelineIndex
			//     8 bytes (double) time
			//     n bytes (byte[]) 

			if (_numGuaranteedSends <= 0)
			{
				foreach (TimelineSendFilter<T> sendFilter in _sendFilters)
				{
					if (sendFilter != null && !sendFilter(this, currentEntry))
						return;
				}
			}

			lock (_entryLock)
			{
				_lastSendTime = DateTime.Now;
				currentEntry._sent = true;

				_lastLastSentEntry = _lastSentEntry;
				_lastSentEntry = currentEntry;

				if (_numGuaranteedSends > 0)
					_numGuaranteedSends--;
			}

			byte[] timelineData = Encode(value);

			byte[] data = new byte[sizeof(ushort) + sizeof(double) + timelineData.Length * sizeof(byte)];
			BinaryWriter writer = new BinaryWriter(new MemoryStream(data));
			writer.Write(_index);
			writer.Write(absoluteTime);
			writer.Write(timelineData);
			writer.Close();

			if (_manager != null)
				_manager.EnqueueOutgoingMessage(new TimelineMessage(TimelineMessageType.RelayAbsolute, data, _deliveryMode));
		}

		/// <summary>
		/// Adds a remote value.
		/// </summary>
		/// <param name="time">The time at which to insert the value.</param>
		/// <param name="valueBytes">The encoded bytes of the value to insert.</param>
		/// <param name="isTimeAbsolute">Whether or not the specified time is absolute.</param>
		/// <param name="isCached">Indicates whether the value comes from a remote peer or is a cached value from the synchronizer</param>
		internal override void RemoteSet(double time, byte[] valueBytes, bool isTimeAbsolute, bool isCached)
		{
			double absoluteTime = isTimeAbsolute ? time : _now + time;
			T value = Decode(valueBytes);

			var entry = new TimelineEntry<T>(absoluteTime, value);

			Insert(entry, isCached);

			if (!(_ignoreCachedEvents && isCached))
				_delayedRemoteInsertionEvents.Enqueue(entry);
		}

		/// <summary>
		/// Adds a value locally.
		/// </summary>
		/// <param name="time">The time at which to insert the value.</param>
		/// <param name="value">The value to insert.</param>
		/// <param name="isTimeAbsolute">Whether or not the specified time is absolute.</param>
		/// <remarks>
		/// Entry is not processed. Insertion is guaranteed, and will not be propagated. Use within entry processors.
		/// </remarks>
		public void Insert (double time, T value, bool isTimeAbsolute = false)
		{
			double absoluteTime = isTimeAbsolute ? time : _now + time;

			Insert(new TimelineEntry<T>(absoluteTime, value), false);
		}

		/// <summary>
		/// Adds an entry locally. For internal use only.
		/// </summary>
		/// <param name="entry">The entry to insert.</param>
		/// <param name="isCached">Whether or not the entry is a cached value.</param>
		/// <remarks>
		/// Entry is not processed. Insertion is guaranteed, and will not be propagated.
		/// </remarks>
		void Insert (TimelineEntry<T> entry, bool isCached)
		{
			lock (_entryLock)
			{
				entry._isCached = isCached;

				// Start with last entry in timeline and look for where the new Entry 
				// should be inserted
				TimelineEntry<T> entryBefore = _lastEntry;
				TimelineEntry<T> entryAfter = null;

				while (entryBefore != null && entryBefore._time > entry._time)
				{
					entryAfter = entryBefore;
					entryBefore = entryBefore._prev;
				}

				if (entryBefore != null) entryBefore._next = entry;
				if (entryAfter != null) entryAfter._prev = entry;

				entry._prev = entryBefore;
				entry._next = entryAfter;

				_numEntries++;

				// update _prevEntry and _nextEntry if inserting between them
				if ((_prevEntry == null || _prevEntry._time <= entry._time) && entry._time <= _now) _prevEntry = entry;
				if ((_nextEntry == null || _nextEntry._time > entry._time) && entry._time > _now) _nextEntry = entry;

				// update _firstEntry and _lastEntry if inserting before or after them
				if ((_firstEntry == null || _firstEntry._time > entry._time)) _firstEntry = entry;
				if ((_lastEntry == null || _lastEntry._time <= entry._time)) _lastEntry = entry;

				GuaranteeSize();

				if (!(_ignoreCachedEvents && isCached))
				{
					_delayedInsertionEvents.Enqueue(entry);

					if (entry._time <= _now)
						_delayedPassingEvents.Enqueue(entry);
				}
			}
		}

		/// <summary>
		/// Delete entries from the timeline starting at the first entry so 
		/// that the number of entries in the timeline does not exceed MaxEntries
		/// Also update _prevEntry and _nextEntry if the current values are removed
		/// from the timeline
		/// </summary>
		internal override void GuaranteeSize ()
		{
			lock (_entryLock)
			{
				while (_numEntries > _maxEntries && _numEntries > 0)
				{
					_firstEntry = _firstEntry._next;
					_numEntries--;
				}

				if (_firstEntry != null)
				{
					_firstEntry._prev = null;
					if (_prevEntry != null && _firstEntry._time > _prevEntry._time) _prevEntry = null;
					if (_nextEntry != null && _firstEntry._time > _nextEntry._time) _nextEntry = _firstEntry;
				}
				else // the timeline has 0 entries
				{
					_prevEntry = null;
					_nextEntry = null;
					_lastEntry = null;
				}
			}
		}

		/// <summary>
		/// Perform updates for the current frame. For internal use only.
		/// </summary>
		internal override void Step ()
		{
			lock (_entryLock)
			{
				if (EntryInserted != null)
				{
					while (_delayedInsertionEvents.Count != 0)
					{
						EntryInserted(this, _delayedInsertionEvents.Dequeue());
					}
				}
				else _delayedInsertionEvents.Clear();

				if (RemoteEntryInserted != null)
				{
					while (_delayedRemoteInsertionEvents.Count != 0)
					{
						RemoteEntryInserted(this, _delayedRemoteInsertionEvents.Dequeue());
					}
				}
				else _delayedRemoteInsertionEvents.Clear();

				if (EntryPassed != null)
				{
					while (_delayedPassingEvents.Count != 0)
					{
						EntryPassed(this, _delayedPassingEvents.Dequeue());
					}
				}
				else _delayedPassingEvents.Clear();

				// Update prev and next entry references.
				while (_nextEntry != null && _now >= _nextEntry._time)
				{
					if (EntryPassed != null)
						EntryPassed(this, _nextEntry);

					if (EntryMet != null)
						EntryMet(this, _nextEntry);

					_nextEntry = _nextEntry._next;
				}

				if (_nextEntry != null)
					_prevEntry = _nextEntry._prev;
				else _prevEntry = _lastEntry;
			}
		}

		/// <summary>
		/// Fallback decoder when nothing else is available.
		/// </summary>
		/// <param name="valueBytes">The bytes to decode.</param>
		/// <returns>The decoded value.</returns>
		static T XmlDecode (byte[] valueBytes)
		{
			if (_xmlSerializer == null)
				_xmlSerializer = new XmlSerializer(typeof(T));

			using (var stream = new MemoryStream(valueBytes))
			{
				return (T)_xmlSerializer.Deserialize(stream);
			}
		}

		/// <summary>
		/// Fallback encoder when nothing else is available.
		/// </summary>
		/// <param name="value">The value to encode.</param>
		/// <returns>The encoded bytes.</returns>
		static byte[] XmlEncode (T value)
		{
			if (_xmlSerializer == null)
				_xmlSerializer = new XmlSerializer(typeof(T));

			using (var stream = new MemoryStream())
			{
				_xmlSerializer.Serialize(stream, value);
				return stream.GetBuffer();
			}
		}
	}
}