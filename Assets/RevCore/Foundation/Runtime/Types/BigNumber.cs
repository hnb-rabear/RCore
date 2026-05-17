using System;
using System.Globalization;

namespace RevCore
{
	/// <summary>
	/// Game-economy "big number" value backed by <see cref="double"/>. Trades exactness for range — at
	/// roughly 15 significant digits, precision degrades long before the magnitude reaches infinity.
	/// Use for player-facing currency / scores where 1.23M reads as "1.23M" regardless of underlying
	/// precision. For exact integer arithmetic at large scale, use <see cref="System.Numerics.BigInteger"/>.
	/// </summary>
	public readonly struct BigNumber : IComparable<BigNumber>
	{
		/// <summary>The value 0.</summary>
		public static readonly BigNumber Zero = new(0d);
		/// <summary>The value 1.</summary>
		public static readonly BigNumber One = new(1d);
		/// <summary>The value 2.</summary>
		public static readonly BigNumber Two = new(2d);
		/// <summary>The value 10.</summary>
		public static readonly BigNumber Ten = new(10d);
		/// <summary>The value 100.</summary>
		public static readonly BigNumber OneHundred = new(100d);

		private readonly double m_value;

		/// <summary>Creates a BigNumber from a <see cref="double"/>.</summary>
		public BigNumber(double value)
		{
			m_value = value;
		}

		/// <summary>Creates a BigNumber from an <see cref="int"/>.</summary>
		public BigNumber(int value) : this((double)value) { }

		/// <summary>Creates a BigNumber from a <see cref="long"/>.</summary>
		public BigNumber(long value) : this((double)value) { }

		/// <summary>Creates a BigNumber from a <see cref="float"/>.</summary>
		public BigNumber(float value) : this((double)value) { }

		/// <summary>
		/// Parses a numeric string in invariant culture. Empty/whitespace input yields zero;
		/// other unparseable input throws <see cref="FormatException"/>.
		/// </summary>
		public BigNumber(string value)
		{
			m_value = string.IsNullOrWhiteSpace(value) ? 0d : double.Parse(value, CultureInfo.InvariantCulture);
		}

		/// <summary>Returns the underlying <see cref="double"/>.</summary>
		public double ToDouble() => m_value;

		/// <summary>Saturating cast to <see cref="int"/>. Values exceeding <see cref="int.MaxValue"/> clamp.</summary>
		public int ToInt() => m_value > int.MaxValue ? int.MaxValue : (int)m_value;

		/// <summary>Saturating cast to <see cref="long"/>. Values exceeding <see cref="long.MaxValue"/> clamp.</summary>
		public long ToLong() => m_value > long.MaxValue ? long.MaxValue : (long)m_value;

		/// <summary>Returns <c>true</c> when the value is strictly positive.</summary>
		public bool HasValue() => m_value > 0d;

		/// <summary>Number of digits in the value's base-10 representation (1 for zero).</summary>
		public int Length() => m_value == 0d ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(m_value))) + 1;

		/// <inheritdoc />
		public int CompareTo(BigNumber other) => m_value.CompareTo(other.m_value);

		/// <summary>Returns the short form (<see cref="ToShortString"/>).</summary>
		public override string ToString() => ToShortString();

		/// <summary>
		/// Renders the value with a unit suffix: K (10^3), M (10^6), B (10^9), T (10^12).
		/// Below 1000 the value is rendered with up to two decimal places. Decimals are
		/// trimmed when the value is integer-valued.
		/// </summary>
		public string ToShortString()
		{
			double abs = Math.Abs(m_value);
			if (abs < 1000d) return FormatNumber(m_value);

			string[] units = { "K", "M", "B", "T" };
			double scaled = abs;
			int unitIndex = -1;
			while (scaled >= 1000d && unitIndex < units.Length - 1)
			{
				scaled /= 1000d;
				unitIndex++;
			}

			double signed = Math.Sign(m_value) * scaled;
			return FormatNumber(signed) + units[unitIndex];
		}

		private static string FormatNumber(double value)
		{
			return Math.Abs(value % 1d) < 0.0000001d
				? Math.Round(value).ToString(CultureInfo.InvariantCulture)
				: Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
		}

		/// <summary>Alias for <c>new BigNumber(string)</c>.</summary>
		public static BigNumber Parse(string value) => new(value);

		/// <summary>Factory mirroring <c>new BigNumber(int)</c>.</summary>
		public static BigNumber Create(int value) => new(value);

		/// <summary>Factory mirroring <c>new BigNumber(float)</c>.</summary>
		public static BigNumber Create(float value) => new(value);

		/// <summary>Factory mirroring <c>new BigNumber(long)</c>.</summary>
		public static BigNumber Create(long value) => new(value);

		/// <summary>Factory mirroring <c>new BigNumber(string)</c>.</summary>
		public static BigNumber Create(string value) => new(value);

		/// <summary>Addition.</summary>
		public static BigNumber operator +(BigNumber left, BigNumber right) => new(left.m_value + right.m_value);
		/// <summary>Subtraction.</summary>
		public static BigNumber operator -(BigNumber left, BigNumber right) => new(left.m_value - right.m_value);
		/// <summary>Multiplication.</summary>
		public static BigNumber operator *(BigNumber left, BigNumber right) => new(left.m_value * right.m_value);
		/// <summary>Division. Behavior on divide-by-zero matches <see cref="double"/>: returns infinity, no throw.</summary>
		public static BigNumber operator /(BigNumber left, BigNumber right) => new(left.m_value / right.m_value);
		/// <summary>Modulo.</summary>
		public static BigNumber operator %(BigNumber left, BigNumber right) => new(left.m_value % right.m_value);
		/// <summary>Greater-than.</summary>
		public static bool operator >(BigNumber left, BigNumber right) => left.m_value > right.m_value;
		/// <summary>Less-than.</summary>
		public static bool operator <(BigNumber left, BigNumber right) => left.m_value < right.m_value;
		/// <summary>Greater-than-or-equal.</summary>
		public static bool operator >=(BigNumber left, BigNumber right) => left.m_value >= right.m_value;
		/// <summary>Less-than-or-equal.</summary>
		public static bool operator <=(BigNumber left, BigNumber right) => left.m_value <= right.m_value;

		/// <summary>Returns the larger of two values.</summary>
		public static BigNumber Max(BigNumber a, BigNumber b) => a > b ? a : b;

		/// <summary>Integer power.</summary>
		public static BigNumber Pow(BigNumber value, int power) => new(Math.Pow(value.m_value, power));

		/// <summary>Square root.</summary>
		public static BigNumber Sqrt(BigNumber value) => new(Math.Sqrt(value.m_value));
	}
}
