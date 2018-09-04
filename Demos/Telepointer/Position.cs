using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Janus;

namespace Equis.Telepointer
{
	/// <summary>
	/// Class for 2 integers representing a position on the screen
	/// </summary>
	public class Position
	{
		/// <summary>
		/// x coordinate
		/// </summary>
		public int x;
		/// <summary>
		/// y coordinate
		/// </summary>
		public int y;

		/// <summary>
		/// Add two positions - required for linear interpolation
		/// </summary>
		/// <param name="pos1">First position</param>
		/// <param name="pos2">Second position</param>
		/// <returns>Sum of two positions</returns>
		public static Position Add(Position pos1, Position pos2)
		{
			return new Position(pos1.x + pos2.x, pos1.y + pos2.y);
		}

		/// <summary>
		/// Multiply a position by a constant - required for linear interpolation
		/// </summary>
		/// <param name="pos">the position</param>
		/// <param name="v">value by which to multiply the position</param>
		/// <returns>Resulting position</returns>
		public static Position Multiply(Position pos, float v)
		{
			return new Position((int) (v * (float)pos.x), (int) (v * (float)pos.y));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public Position(int _x, int _y)
		{
			x = _x;
			y = _y;
		}

		/// <summary>
		/// Sets encoding, decoding and interpolation functions for all position timelines
		/// Must be executed before any objects of type Timeline<Position> are created
		/// </summary>
		public static void SetDefautTimelineFunctions()
		{
			Timeline<Position>.TypeEncode = EncodePosition;
			Timeline<Position>.TypeDecode = DecodePosition;

			Timeline<Position>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<Position>(
				(x, y) => Position.Add(x,y), (x, y) => Position.Multiply(x, y));
		}

		/// <summary>
		/// Convert a position to a byte array
		/// </summary>
		public static byte[] EncodePosition(Position value)
		{
			byte[] bytes = new byte[2 * sizeof(Int32)];
			BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
			bw.Write(value.x); bw.Write(value.y);
			bw.Close();
			return bytes;
		}

		/// <summary>
		/// Convert a  a byte array to a position
		/// </summary>
		public static Position DecodePosition(byte[] bytes)
		{
			BinaryReader br = new BinaryReader(new MemoryStream(bytes));
			var value = new Position(br.ReadInt32(), br.ReadInt32());
			br.Close();
			return value;
		}
	}
}





