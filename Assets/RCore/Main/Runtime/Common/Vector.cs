using System;
using UnityEngine;

namespace RCore
{
	[Serializable]
	public struct RVector2 : IEquatable<RVector2>
	{
		public float x;
		public float y;
		public RVector2(float xx = 0, float yy = 0)
		{
			x = xx;
			y = yy;
		}
		public static RVector2 zero => new RVector2(0);

		//Vector2 operators
		public static implicit operator Vector2(RVector2 v)
		{
			return new Vector2(v.x, v.y);
		}
		public static implicit operator RVector2(Vector2 v)
		{
			return new RVector2(v.x, v.y);
		}
		public static RVector2 operator +(RVector2 a, Vector2 b)
		{
			return new RVector2(a.x + b.x, a.y + b.y);
		}
		public static RVector2 operator -(RVector2 a, Vector2 b)
		{
			return new RVector2(a.x - b.x, a.y - b.y);
		}
		public static RVector2 operator *(RVector2 a, Vector2 b)
		{
			return new RVector2(a.x * b.x, a.y * b.y);
		}
		public static RVector2 operator /(RVector2 a, Vector2 b)
		{
			return new RVector2(a.x / b.x, a.y / b.y);
		}
		//Vector2 operators
		public static RVector2 operator +(RVector2 a, RVector2 b)
		{
			return new RVector2(a.x + b.x, a.y + b.y);
		}
		public static RVector2 operator -(RVector2 a, RVector2 b)
		{
			return new RVector2(a.x - b.x, a.y - b.y);
		}
		public static RVector2 operator *(RVector2 a, RVector2 b)
		{
			return new RVector2(a.x * b.x, a.y * b.y);
		}
		public static RVector2 operator /(RVector2 a, RVector2 b)
		{
			return new RVector2(a.x / b.x, a.y / b.y);
		}
		//Vector3 operators
		public static implicit operator Vector3(RVector2 v)
		{
			return new Vector2(v.x, v.y);
		}
		public static implicit operator RVector2(Vector3 v)
		{
			return new RVector2(v.x, v.y);
		}
		public static RVector2 operator +(RVector2 a, Vector3 b)
		{
			return new RVector2(a.x + b.x, a.y + b.y);
		}
		public static RVector2 operator -(RVector2 a, Vector3 b)
		{
			return new RVector2(a.x - b.x, a.y - b.y);
		}
		public static RVector2 operator *(RVector2 a, Vector3 b)
		{
			return new RVector2(a.x * b.x, a.y * b.y);
		}
		public static RVector2 operator /(RVector2 a, Vector3 b)
		{
			return new RVector2(a.x / b.x, a.y / b.y);
		}
		public bool Equals(RVector2 other)
		{
			return x == other.x && y == other.y;
		}
	}

	[Serializable]
	public struct RVector2Int : IEquatable<RVector2Int>
	{
		public int x;
		public int y;
		public RVector2Int(int xx = 0, int yy = 0)
		{
			x = xx;
			y = yy;
		}
		public static RVector2Int zero => new RVector2Int();

		// Define a conversion operator
		public static implicit operator Vector2Int(RVector2Int v)
		{
			return new Vector2Int(v.x, v.y);
		}
		public static implicit operator RVector2Int(Vector2Int v)
		{
			return new RVector2Int(v.x, v.y);
		}

		//Vector2Int operators
		public static RVector2Int operator +(RVector2Int a, Vector2Int b)
		{
			return new RVector2Int(a.x + b.x, a.y + b.y);
		}
		public static RVector2Int operator -(RVector2Int a, Vector2Int b)
		{
			return new RVector2Int(a.x - b.x, a.y - b.y);
		}
		public static RVector2Int operator *(RVector2Int a, Vector2Int b)
		{
			return new RVector2Int(a.x * b.x, a.y * b.y);
		}
		public static RVector2Int operator /(RVector2Int a, Vector2Int b)
		{
			return new RVector2Int(a.x / b.x, a.y / b.y);
		}

		//Vector3Int operators
		public static RVector2Int operator +(RVector2Int a, Vector3Int b)
		{
			return new RVector2Int(a.x + b.x, a.y + b.y);
		}
		public static RVector2Int operator -(RVector2Int a, Vector3Int b)
		{
			return new RVector2Int(a.x - b.x, a.y - b.y);
		}
		public static RVector2Int operator *(RVector2Int a, Vector3Int b)
		{
			return new RVector2Int(a.x * b.x, a.y * b.y);
		}
		public static RVector2Int operator /(RVector2Int a, Vector3Int b)
		{
			return new RVector2Int(a.x / b.x, a.y / b.y);
		}
		public bool Equals(RVector2Int other)
		{
			return x == other.x && y == other.y;
		}
	}

	[Serializable]
	public struct RVector3 : IEquatable<RVector3>
	{
		public float x;
		public float y;
		public float z;
		public RVector3(float xx = 0, float yy = 0, float zz = 0)
		{
			x = xx;
			y = yy;
			z = zz;
		}
		public static RVector3 zero => new RVector3();

		// Define a conversion operator from RVector3 to Vector3
		public static implicit operator Vector3(RVector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}

		// Define a conversion operator from Vector3 to RVector3
		public static implicit operator RVector3(Vector3 v)
		{
			return new RVector3(v.x, v.y, v.z);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector2
		public static RVector3 operator +(RVector3 a, Vector2 b)
		{
			return new RVector3(a.x + b.x, a.y + b.y, a.z);
		}
		public static RVector3 operator -(RVector3 a, Vector2 b)
		{
			return new RVector3(a.x - b.x, a.y - b.y, a.z);
		}
		public static RVector3 operator *(RVector3 a, Vector2 b)
		{
			return new RVector3(a.x * b.x, a.y * b.y, a.z);
		}
		public static RVector3 operator /(RVector3 a, Vector2 b)
		{
			return new RVector3(a.x / b.x, a.y / b.y, a.z);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector3
		public static RVector3 operator +(RVector3 a, Vector3 b)
		{
			return new RVector3(a.x + b.x, a.y + b.y, a.z + b.z);
		}
		public static RVector3 operator -(RVector3 a, Vector3 b)
		{
			return new RVector3(a.x - b.x, a.y - b.y, a.z - b.z);
		}
		public static RVector3 operator *(RVector3 a, Vector3 b)
		{
			return new RVector3(a.x * b.x, a.y * b.y, a.z * b.z);
		}
		public static RVector3 operator /(RVector3 a, Vector3 b)
		{
			return new RVector3(a.x / b.x, a.y / b.y, a.z / b.z);
		}

		public bool Equals(RVector3 other)
		{
			return x == other.x && y == other.y && z == other.z;
		}
	}

	[Serializable]
	public struct RVector3Int : IEquatable<RVector3Int>
	{
		public int x;
		public int y;
		public int z;
		public RVector3Int(int xx = 0, int yy = 0, int zz = 0)
		{
			x = xx;
			y = yy;
			z = zz;
		}
		public static RVector3Int zero => new RVector3Int();

		// Define a conversion operator from RVector3Int to Vector3Int
		public static implicit operator Vector3Int(RVector3Int v)
		{
			return new Vector3Int(v.x, v.y, v.z);
		}

		// Define a conversion operator from Vector3Int to RVector3Int
		public static implicit operator RVector3Int(Vector3Int v)
		{
			return new RVector3Int(v.x, v.y, v.z);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector2Int
		public static RVector3Int operator +(RVector3Int a, Vector2Int b)
		{
			return new RVector3Int(a.x + b.x, a.y + b.y, a.z);
		}
		public static RVector3Int operator -(RVector3Int a, Vector2Int b)
		{
			return new RVector3Int(a.x - b.x, a.y - b.y, a.z);
		}
		public static RVector3Int operator *(RVector3Int a, Vector2Int b)
		{
			return new RVector3Int(a.x * b.x, a.y * b.y, a.z);
		}
		public static RVector3Int operator /(RVector3Int a, Vector2Int b)
		{
			return new RVector3Int(a.x / b.x, a.y / b.y, a.z);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector3Int
		public static RVector3Int operator +(RVector3Int a, Vector3Int b)
		{
			return new RVector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
		}
		public static RVector3Int operator -(RVector3Int a, Vector3Int b)
		{
			return new RVector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
		}
		public static RVector3Int operator *(RVector3Int a, Vector3Int b)
		{
			return new RVector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
		}
		public static RVector3Int operator /(RVector3Int a, Vector3Int b)
		{
			return new RVector3Int(a.x / b.x, a.y / b.y, a.z / b.z);
		}

		public bool Equals(RVector3Int other)
		{
			return x == other.x && y == other.y && z == other.z;
		}
	}

	[Serializable]
	public struct RVector4 : IEquatable<RVector4>
	{
		public float x;
		public float y;
		public float z;
		public float w;
		public RVector4(float xx = 0, float yy = 0, float zz = 0, float ww = 0)
		{
			x = xx;
			y = yy;
			z = zz;
			w = ww;
		}
		public static RVector4 zero => new RVector4();

		// Define a conversion operator from RVector4 to Vector4
		public static implicit operator Vector4(RVector4 v)
		{
			return new Vector4(v.x, v.y, v.z, v.w);
		}

		// Define a conversion operator from Vector4 to RVector4
		public static implicit operator RVector4(Vector4 v)
		{
			return new RVector4(v.x, v.y, v.z, v.w);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector2
		public static RVector4 operator +(RVector4 a, Vector2 b)
		{
			return new RVector4(a.x + b.x, a.y + b.y, a.z, a.w);
		}
		public static RVector4 operator -(RVector4 a, Vector2 b)
		{
			return new RVector4(a.x - b.x, a.y - b.y, a.z, a.w);
		}
		public static RVector4 operator *(RVector4 a, Vector2 b)
		{
			return new RVector4(a.x * b.x, a.y * b.y, a.z, a.w);
		}
		public static RVector4 operator /(RVector4 a, Vector2 b)
		{
			return new RVector4(a.x / b.x, a.y / b.y, a.z, a.w);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector3
		public static RVector4 operator +(RVector4 a, Vector3 b)
		{
			return new RVector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w);
		}
		public static RVector4 operator -(RVector4 a, Vector3 b)
		{
			return new RVector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w);
		}
		public static RVector4 operator *(RVector4 a, Vector3 b)
		{
			return new RVector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w);
		}
		public static RVector4 operator /(RVector4 a, Vector3 b)
		{
			return new RVector4(a.x / b.x, a.y / b.y, a.z / b.z, a.w);
		}

		// Define operator functions for addition, subtraction, multiplication, and division with Vector4
		public static RVector4 operator +(RVector4 a, Vector4 b)
		{
			return new RVector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
		}
		public static RVector4 operator -(RVector4 a, Vector4 b)
		{
			return new RVector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
		}
		public static RVector4 operator *(RVector4 a, Vector4 b)
		{
			return new RVector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
		}
		public static RVector4 operator /(RVector4 a, Vector4 b)
		{
			return new RVector4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
		}

		public bool Equals(RVector4 other)
		{
			return x == other.x && y == other.y && z == other.z && w == other.w;
		}
	}
}