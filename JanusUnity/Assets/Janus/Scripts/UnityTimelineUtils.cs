using UnityEngine;
using Janus;
using System;
using System.IO;

public static class UnityTimelineUtils
{
	public static void SetDefautTimelineFunctions ()
	{
		Timeline<Vector2>.TypeEncode = EncodeVector2;
		Timeline<Vector2>.TypeDecode = DecodeVector2;
		Timeline<Vector3>.TypeEncode = EncodeVector3;
		Timeline<Vector3>.TypeDecode = DecodeVector3;
		Timeline<Vector4>.TypeEncode = EncodeVector4;
		Timeline<Vector4>.TypeDecode = DecodeVector4;
		Timeline<Quaternion>.TypeEncode = EncodeQuaternion;
		Timeline<Quaternion>.TypeDecode = DecodeQuaternion;
		Timeline<Matrix4x4>.TypeEncode = EncodeMatrix4x4;
		Timeline<Matrix4x4>.TypeDecode = DecodeMatrix4x4;
		Timeline<Ray>.TypeEncode = EncodeRay;
		Timeline<Ray>.TypeDecode = DecodeRay;
		Timeline<Color>.TypeEncode = EncodeColor;
		Timeline<Color>.TypeDecode = DecodeColor;

		Timeline<Vector2>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<Vector2>(
			(x, y) => x + y, (x, y) => x * y);
		Timeline<Vector2>.TypeExtrapolate = TimelineUtils.BuildLinearExtrapolator<Vector2>(
			(x, y) => x + y, (x, y) => x * y);
		Timeline<Vector3>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<Vector3>(
			(x, y) => x + y, (x, y) => x * y);
		Timeline<Vector3>.TypeExtrapolate = TimelineUtils.BuildLinearExtrapolator<Vector3>(
			(x, y) => x + y, (x, y) => x * y);
		Timeline<Vector4>.TypeInterpolate = TimelineUtils.BuildLinearInterpolator<Vector4>(
			(x, y) => x + y, (x, y) => x * y);
		Timeline<Vector4>.TypeExtrapolate = TimelineUtils.BuildLinearExtrapolator<Vector4>(
			(x, y) => x + y, (x, y) => x * y);

		Timeline<Quaternion>.TypeInterpolate = InterpolateQuaternionSlerp;
		Timeline<Quaternion>.TypeExtrapolate = ExtrapolateQuaternionSlerp;
		Timeline<Ray>.TypeInterpolate = InterpolateRay;
		Timeline<Ray>.TypeExtrapolate = ExtrapolateRay;
	}

	public static byte[] EncodeVector2 (Vector2 value)
	{
		byte[] bytes = new byte[2 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.x); bw.Write(value.y);
		bw.Close();

		return bytes;
	}

	public static Vector2 DecodeVector2 (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Vector2(br.ReadSingle(), br.ReadSingle());
		br.Close();

		return value;
	}
	
	public static byte[] EncodeVector3 (Vector3 value)
	{
		byte[] bytes = new byte[3 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.x); bw.Write(value.y); bw.Write(value.z);
		bw.Close();

		return bytes;
	}
	
	public static Vector3 DecodeVector3 (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		br.Close();

		return value;
	}
	
	public static byte[] EncodeVector4 (Vector4 value)
	{
		byte[] bytes = new byte[4 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.x); bw.Write(value.y); bw.Write(value.z); bw.Write(value.w);
		bw.Close();

		return bytes;
	}

	public static Vector4 DecodeVector4 (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		br.Close();

		return value;
	}
	
	public static byte[] EncodeQuaternion (Quaternion value)
	{
		byte[] bytes = new byte[4 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.x); bw.Write(value.y); bw.Write(value.z); bw.Write(value.w);
		bw.Close();

		return bytes;
	}
	
	public static Quaternion DecodeQuaternion (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		br.Close();

		return value;
	}

	public static byte[] EncodeState<T> (T value)
	{
		return new byte[] { (byte)(object)value };
	}

	public static T DecodeState<T> (byte[] bytes)
	{
		return (T)(object)bytes[0];
	}

	public static Vector2 InterpolateVector2Slerp (Timeline<Vector2> timeline, TimelineContext<Vector2> context)
	{
		return Vector3.Slerp(context.Prev.Value, context.Next.Value,
			(float)(context.Time - context.Prev.Time) / (float)(context.Next.Time - context.Prev.Time));
	}

	public static Vector2 ExtrapolateVector2Slerp (Timeline<Vector2> timeline, TimelineContext<Vector3> context)
	{
		if (context.Prev.Prev != null && context.Prev.Prev.Time != context.Prev.Time)
		{
			return ExtrapolateVector3Slerp(context.Prev.Prev.Value, context.Prev.Value,
				(float)(context.Prev.Time - context.Prev.Prev.Time), (float)(context.Time - context.Prev.Prev.Time));
		}
		else return context.Prev.Value;
	}

	public static Vector3 InterpolateVector3Slerp (Timeline<Vector3> timeline, TimelineContext<Vector3> context)
	{
		return Vector3.Slerp(context.Prev.Value, context.Next.Value,
			(float)(context.Time - context.Prev.Time) / (float)(context.Next.Time - context.Prev.Time));
	}

	public static Vector3 ExtrapolateVector3Slerp (Timeline<Vector3> timeline, TimelineContext<Vector3> context)
	{
		if (context.Prev.Prev != null && context.Prev.Prev.Time != context.Prev.Time)
		{
			return ExtrapolateVector3Slerp(context.Prev.Prev.Value, context.Prev.Value,
				(float)(context.Prev.Time - context.Prev.Prev.Time), (float)(context.Time - context.Prev.Prev.Time));
		}
		else return context.Prev.Value;
	}

	public static Vector3 ExtrapolateVector3Slerp (Vector3 v1, Vector3 v2, float t12, float t13)
	{
		var baseRot = Quaternion.FromToRotation(v1, v2);

		Vector3 rotAxis;
		float baseRotAngle;
		baseRot.ToAngleAxis(out baseRotAngle, out rotAxis);

		float rotSpeed = baseRotAngle / t12;
		float newRotAngle = rotSpeed * t13;

		var newRot = Quaternion.AngleAxis(newRotAngle, rotAxis);
		return newRot * v1;
	}

	public static Quaternion InterpolateQuaternionSlerp (Timeline<Quaternion> timeline,
		TimelineContext<Quaternion> context)
	{
		return Quaternion.Slerp(context.Prev.Value, context.Next.Value,
			(float)(context.Time - context.Prev.Time) / (float)(context.Next.Time - context.Prev.Time));
	}

	public static Quaternion ExtrapolateQuaternionSlerp (Timeline<Quaternion> timeline,
		TimelineContext<Quaternion> context)
	{
		if (context.Prev.Prev != null && context.Prev.Prev.Time != context.Prev.Time)
		{
			return ExtrapolateQuaternionSlerp(
				context.Prev.Prev.Value, context.Prev.Value,
				(float)(context.Prev.Time - context.Prev.Prev.Time),
				(float)(context.Time - context.Prev.Prev.Time));
		}
		else return context.Prev.Value;
	}

	public static Quaternion ExtrapolateQuaternionSlerp (Quaternion q1, Quaternion q2, float t12, float t13)
	{
		var baseRot = q2 * Quaternion.Inverse(q1);
		
		Vector3 rotAxis;
		float baseRotAngle;
		baseRot.ToAngleAxis(out baseRotAngle, out rotAxis);

		float rotSpeed = baseRotAngle / t12;
		float newRotAngle = rotSpeed * t13;

		var newRot = Quaternion.AngleAxis(newRotAngle, rotAxis);
		return newRot * q1;
	}

	public static float InterpolateAngleSlerp (Timeline<float> timeline, TimelineContext<float> context)
	{
		return InterpolateAngleSlerp(context.Prev.Value, context.Next.Value,
			(float)(context.Time - context.Prev.Time) / (float)(context.Next.Time - context.Prev.Time));
	}

	public static float ExtrapolateAngleSlerp (Timeline<float> timeline, TimelineContext<float> context)
	{
		if (context.Prev.Prev != null && context.Prev.Prev.Time != context.Prev.Time)
		{
			return ExtrapolateAngleSlerp(
				context.Prev.Prev.Value, context.Prev.Value,
				(float)(context.Prev.Time - context.Prev.Prev.Time),
				(float)(context.Time - context.Prev.Prev.Time));
		}
		else return context.Prev.Value;
	}

	public static float InterpolateAngleSlerp (float a1, float a2, float t)
	{
		return NormalizeAngle(Quaternion.Slerp(
			Quaternion.Euler(0, 0, NormalizeAngle(a1)),
			Quaternion.Euler(0, 0, NormalizeAngle(a2)), t).eulerAngles.z);
	}

	public static float ExtrapolateAngleSlerp (float a1, float a2, float t12, float t13)
	{
		return NormalizeAngle(ExtrapolateQuaternionSlerp(
			Quaternion.Euler(0, 0, NormalizeAngle(a1)),
			Quaternion.Euler(0, 0, NormalizeAngle(a2)), t12, t13).eulerAngles.z);
	}

	public static float NormalizeAngle (float a)
	{
		while (a > 180) { a -= 360; }
		while (a < -180) { a += 360; }

		return a;
	}

	public static byte[] EncodeMatrix4x4 (Matrix4x4 value)
	{
		byte[] bytes = new byte[16 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.m00); bw.Write(value.m01); bw.Write(value.m02); bw.Write(value.m03);
		bw.Write(value.m10); bw.Write(value.m11); bw.Write(value.m12); bw.Write(value.m13);
		bw.Write(value.m20); bw.Write(value.m21); bw.Write(value.m22); bw.Write(value.m23);
		bw.Write(value.m30); bw.Write(value.m31); bw.Write(value.m32); bw.Write(value.m33);
		bw.Close();

		return bytes;
	}
	
	public static Matrix4x4 DecodeMatrix4x4 (byte[] bytes)
	{
		var value = new Matrix4x4();

		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		value.SetRow(0, new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
		value.SetRow(1, new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
		value.SetRow(2, new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
		value.SetRow(3, new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
		br.Close();

		return value;
	}
	
	public static byte[] EncodeRay (Ray value)
	{
		byte[] bytes = new byte[6 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.origin.x); bw.Write(value.origin.y); bw.Write(value.origin.z);
		bw.Write(value.direction.x); bw.Write(value.direction.y); bw.Write(value.direction.z);
		bw.Close();

		return bytes;
	}

	public static Ray DecodeRay (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Ray(
			new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
			new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
		br.Close();

		return value;
	}

	public static Ray InterpolateRay (Timeline<Ray> timeline, TimelineContext<Ray> context)
	{
		float interpFactor = (float)(context.Time - context.Prev.Time) / (float)(context.Next.Time - context.Prev.Time);

		return new Ray(
			Vector3.Lerp(context.Prev.Value.origin, context.Next.Value.origin, interpFactor),
			Vector3.Slerp(context.Prev.Value.direction, context.Next.Value.direction, interpFactor));
	}

	public static Ray ExtrapolateRay (Timeline<Ray> timeline, TimelineContext<Ray> context)
	{
		if (context.Prev.Prev != null && context.Prev.Prev.Time != context.Prev.Time)
		{
			return context.Prev.Value;
		}
		else return context.Prev.Value;
	}
	
	public static byte[] EncodeColor (Color value)
	{
		byte[] bytes = new byte[4 * sizeof(float)];

		BinaryWriter bw = new BinaryWriter(new MemoryStream(bytes));
		bw.Write(value.r); bw.Write(value.g); bw.Write(value.b); bw.Write(value.a);
		bw.Close();

		return bytes;
	}

	public static Color DecodeColor (byte[] bytes)
	{
		BinaryReader br = new BinaryReader(new MemoryStream(bytes));
		var value = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
		br.Close();

		return value;
	}
}