using System;
using System.Linq;

namespace Janus
{
	/// <summary>
	/// Creates functions many utility functions such as interpolation, extrapolation, encoding, decoding and send filters
	/// </summary>

	public static class TimelineUtils
	{
		/// <summary>
		/// Set default encoding, decoding, interpolation and extrapolation for standard timeline types
		/// </summary>
		public static void SetDefaultTimelineFunctions ()
		{
			Timeline<bool>.TypeDecode = x => BitConverter.ToBoolean(x, 0);
			Timeline<bool>.TypeEncode = BitConverter.GetBytes;

			Timeline<int>.TypeDecode = x => BitConverter.ToInt32(x, 0);
			Timeline<int>.TypeEncode = BitConverter.GetBytes;

			Timeline<uint>.TypeDecode = x => BitConverter.ToUInt32(x, 0);
			Timeline<uint>.TypeEncode = BitConverter.GetBytes;

			Timeline<short>.TypeDecode = x => BitConverter.ToInt16(x, 0);
			Timeline<short>.TypeEncode = BitConverter.GetBytes;

			Timeline<ushort>.TypeDecode = x => BitConverter.ToUInt16(x, 0);
			Timeline<ushort>.TypeEncode = BitConverter.GetBytes;

			Timeline<long>.TypeDecode = x => BitConverter.ToInt64(x, 0);
			Timeline<long>.TypeEncode = BitConverter.GetBytes;

			Timeline<ulong>.TypeDecode = x => BitConverter.ToUInt64(x, 0);
			Timeline<ulong>.TypeEncode = BitConverter.GetBytes;

			Timeline<float>.TypeDecode = x => BitConverter.ToSingle(x, 0);
			Timeline<float>.TypeEncode = BitConverter.GetBytes;

			Timeline<double>.TypeDecode = x => BitConverter.ToDouble(x, 0);
			Timeline<double>.TypeEncode = BitConverter.GetBytes;

			Timeline<char>.TypeDecode = x => BitConverter.ToChar(x, 0);
			Timeline<char>.TypeEncode = BitConverter.GetBytes;

			Timeline<byte>.TypeDecode = x => x[0];
			Timeline<byte>.TypeEncode = x => new byte[] { x };

			Timeline<string>.TypeDecode = TimelineUtils.DecodeString;
			Timeline<string>.TypeEncode = TimelineUtils.EncodeString;

			Timeline<byte[]>.TypeDecode = x => x;
			Timeline<byte[]>.TypeEncode = x => x;

			Timeline<float>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<float>(
				(x, y) => x + y, (x, y) => x * y);
			Timeline<double>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<double>(
				(x, y) => x + y, (x, y) => x * y);

			// No default extrapolators for raw data types. Cases where you don't want extrapolation
			// outnumber the cases where you do.
		}

		/// <summary>
		/// Convert a byte array to a string
		/// </summary>
		public static string DecodeString(byte[] bytes)
		{
			char[] chars = new char[bytes.Length / sizeof(char)];
			Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}

		/// <summary>
		/// Convert a string to a byte array
		/// </summary>
		public static byte[] EncodeString(string value)
		{
			char[] chars = value.ToCharArray();
			byte[] bytes = new byte[chars.Length * sizeof(char)];
			Buffer.BlockCopy(chars, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		/// <summary>
		/// Stepping interpolation
		/// </summary>
		public static T SteppingInterpolate<T>(Timeline<T> timeline, TimelineContext<T> context)
		{
			return context.Prev != null ? context.Prev.Value : default(T);
		}

		/// <summary>
		/// Stepping extrapolation
		/// </summary>
		public static T SteppingExtrapolate<T>(Timeline<T> timeline, TimelineContext<T> context)
		{
			return context.Prev != null ? context.Prev.Value : default(T);
		}

		/// <summary>
		/// Filters timeline value sends based on a maximum send rate
		/// </summary>
		public static TimelineSendFilter<T> BuildRateFilter<T>(Func<float> getSendRate)
		{
			return (timeline, entry) =>
				DateTime.Now.Subtract(timeline.LastSendTime).TotalSeconds >= (1f / getSendRate());
		}

		/// <summary>
		/// Filters timeline value sends based on whether or not the value has changed at all
		/// </summary>
		public static TimelineSendFilter<T> BuildInequalityFilter<T> ()
		{
			return (timeline, entry) =>
				timeline.LastSentEntry == null ||
				!object.Equals(entry._value, timeline.LastSentEntry._value);
		}

		/// <summary>
		/// Filters timeline value sends based on whether or not the value has changed by a fixed amount
		/// </summary>
		public static TimelineSendFilter<T> BuildDeltaFilter<T> 
		(Func<T, T, float> getDelta, Func<float> getThreshold)
		{
			return (timeline, entry) =>
				timeline.LastSentEntry == null ||
				getDelta(entry._value, timeline.LastSentEntry._value) > getThreshold();
		}

		/// <summary>
		/// Filters timeline value sends based on extrapolation
		/// </summary>
		public static TimelineSendFilter<T> BuildExtrapolatedDeltaFilter<T>
		(Func<T, T, float> getDelta, Func<float> getThreshold)
		{
			return (timeline, entry) =>
			{
				if (timeline.LastSentEntry == null)
					return true;

				TimelineContext<T> currentContext = new TimelineContext<T>();
				currentContext._prev = timeline.LastSentEntry;
				currentContext._prevPrev = timeline.LastLastSentEntry;
				currentContext._time = entry._time;

				return getDelta(entry._value, timeline.Extrapolate(timeline, currentContext)) > getThreshold();
			};
		}
		/// <summary>
		/// Filters timeline value sends based on extrapolation, but also forces a send 
		/// periodically even if the value has not changed
		/// </summary>
		public static TimelineSendFilter<T> BuildDeltaRateFilter<T>
		(Func<T, T, float> getDelta, Func<float> getThreshold, Func<float> getRate)
		{
			return (timeline, entry) =>
			{
				TimelineContext<T> currentContext = new TimelineContext<T>();
				currentContext._prev = timeline.LastSentEntry;
				currentContext._prevPrev = timeline.LastLastSentEntry;
				currentContext._time = entry._time;

				return (getDelta(entry._value, timeline.Extrapolate(timeline, currentContext)) > getThreshold()) ||
					(DateTime.Now.Subtract(timeline.LastSendTime).TotalSeconds >= (1f / getRate()));
			};
		}

		/// <summary>
		/// Builder for linear interpolation function
		/// </summary>
		public static TimelineInterpolator<T> BuildLinearInterpolator<T>
			(Func<T, T, T> add, Func<T, float, T> mul)
		{
			return (timeline, context) =>
			{
				double tPrev = context._prev._time;
				double tNext = context._next._time;
				double t = context._time;
				float factorNext = (float) ((t - tPrev) / (tNext - tPrev));
				float factorPrev = 1 - factorNext;
				return (add(mul(context._prev._value, factorPrev), mul(context._next._value, factorNext)));
			};
		}

		/// <summary>
		/// Builder for linear extrapolation function
		/// </summary>
		public static TimelineExtrapolator<T> BuildLinearExtrapolator<T>
			(Func<T, T, T> add, Func<T, float, T> mul, float maxTimeJump = float.PositiveInfinity)
		{
			return (timeline, context) =>
			{
				TimelineEntry<T> prevPrev = context._prevPrev;
				TimelineEntry<T> prev = context._prev;
				if (prevPrev == null || prevPrev._time == prev._time) return prev._value; // either there is no previous value or it has the same time
				double tPrevPrev = prevPrev._time;
				double tPrev = prev._time;
				double t = context._time;
				float timeJump = Math.Min(maxTimeJump, (float)(t - tPrev));
				t = tPrev + timeJump;
				float factorPrevPrev = (float)((tPrev - t) / (tPrev - tPrevPrev));
				float factorPrev = 1 - factorPrevPrev;
				return (add(mul(prevPrev._value, factorPrevPrev), mul(prev._value, factorPrev)));
			};
		}

		/// <summary>
		/// Builder for quadratic interpolation function
		/// </summary>
		public static TimelineInterpolator<T> BuildQuadraticInterpolator<T>
			(Func<T, T, T> add, Func<T, float, T> mul)
		{
			return (timeline, context) =>
			{
				// Lagrange basis functions
				// L0(t) = ((t-t1)(t-t2))/((t0-t1)(t0-t2))
				// L1(t) = ((t-t0)(t-t2))/((t1-t0)(t1-t2))
				// L2(t) = ((t-t0)(t-t1))/((t2-t0)(t2-t1))
				// P(t) = y0*L0(t) + y1*L1(t) + y2*L2(t)

				double tPrev = context._prev._time;
				double tNext = context._next._time;
				double t = context._time;

				// need a third value for interpolation
				// look for a value before tPrev - if there isn't one we
				// will just do linear interplation
				TimelineEntry<T> prevPrev = context._prev;
				while (prevPrev._prev != null && prevPrev._time >= context._prev._time) // need loop to make sure that there are not 2 entries with the same time
				{
					prevPrev = context._prev._prev;
				}
				if (prevPrev != null) // we have three values so do quadratic
				{
					double tPrevPrev = prevPrev._time;
					float L0 = (float)(((t - tPrev) * (t - tNext)) / ((tPrevPrev - tPrev) * (tPrevPrev - tNext)));
					float L1 = (float)(((t - tPrevPrev) * (t - tNext)) / ((tPrev - tPrevPrev) * (tPrev - tNext)));
					float L2 = (float)(((t - tPrevPrev) * (t - tPrev)) / ((tNext - tPrevPrev) * (tNext - tPrev)));
					return (add(mul(prevPrev._value,L0),add(mul(context._prev._value, L1), mul(context._next._value, L2))));
				}
				else // no third value so do linear interpolation
				{
					float factorNext = (float)((t - tPrev) / (tNext - tPrev));
					float factorPrev = 1 - factorNext;
					return (add(mul(context._prev._value, factorPrev), mul(context._next._value, factorNext)));
				}
			  
			};
		}

		/// <summary>
		/// Builder for quadratic extrapolation function
		/// </summary>
		public static TimelineExtrapolator<T> BuildQuadraticExtrapolator<T>
			(Func<T, T, T> add, Func<T, float, T> mul)
		{
			return (timeline, context) =>
			{
				// first need to get the 2 previous values
				TimelineEntry<T> pp = context._prev._prev;

				// either there is no previous value or it has the same time
				if (pp == null || pp._time == context.Prev._time)
					return context._prev._value;

				TimelineEntry<T> ppp = pp._prev;
				double t = context._time;
				double tp = context._prev._time;
				double tpp = pp._time;

				if (ppp == null || ppp._time == pp._time) // only 2 values so do linear extrapolation
				{
					float factorPrevPrev = (float)((tp - t) / (tp - tpp));
					float factorPrev = 1 - factorPrevPrev;
					return (add(mul(pp._value, factorPrevPrev), mul(context._prev._value, factorPrev)));

				}
				else  // we have three values so can do quadratic 
				{
					double tppp = ppp._time;
					float L0 = (float)(((t - tpp) * (t - tp)) / ((tppp - tpp) * (tppp - tp)));  //PPP
					float L1 = (float)(((t - tppp) * (t - tp)) / ((tpp - tppp) * (tpp - tp)));  //PP
					float L2 = (float)(((t - tppp) * (t - tpp)) / ((tp - tppp) * (tp - tpp)));  //P
					return (add(mul(ppp._value, L0), add(mul(pp._value, L1), mul(context._prev._value, L2))));

				}
			};
		}
	}
}