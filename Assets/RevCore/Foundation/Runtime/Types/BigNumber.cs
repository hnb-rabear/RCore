using System;
using System.Globalization;

namespace RevCore
{
	public readonly struct BigNumber : IComparable<BigNumber>
	{
		public static readonly BigNumber Zero = new(0d);
		public static readonly BigNumber One = new(1d);
		public static readonly BigNumber Two = new(2d);
		public static readonly BigNumber Ten = new(10d);
		public static readonly BigNumber OneHundred = new(100d);

		private readonly double m_value;

		public BigNumber(double value)
		{
			m_value = value;
		}

		public BigNumber(int value) : this((double)value) { }
		public BigNumber(long value) : this((double)value) { }
		public BigNumber(float value) : this((double)value) { }

		public BigNumber(string value)
		{
			m_value = string.IsNullOrWhiteSpace(value) ? 0d : double.Parse(value, CultureInfo.InvariantCulture);
		}

		public double ToDouble() => m_value;
		public int ToInt() => m_value > int.MaxValue ? int.MaxValue : (int)m_value;
		public long ToLong() => m_value > long.MaxValue ? long.MaxValue : (long)m_value;
		public bool HasValue() => m_value > 0d;
		public int Length() => m_value == 0d ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(m_value))) + 1;
		public int CompareTo(BigNumber other) => m_value.CompareTo(other.m_value);

		public override string ToString() => ToShortString();

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

		public static BigNumber Parse(string value) => new(value);
		public static BigNumber Create(int value) => new(value);
		public static BigNumber Create(float value) => new(value);
		public static BigNumber Create(long value) => new(value);
		public static BigNumber Create(string value) => new(value);

		public static BigNumber operator +(BigNumber left, BigNumber right) => new(left.m_value + right.m_value);
		public static BigNumber operator -(BigNumber left, BigNumber right) => new(left.m_value - right.m_value);
		public static BigNumber operator *(BigNumber left, BigNumber right) => new(left.m_value * right.m_value);
		public static BigNumber operator /(BigNumber left, BigNumber right) => new(left.m_value / right.m_value);
		public static BigNumber operator %(BigNumber left, BigNumber right) => new(left.m_value % right.m_value);
		public static bool operator >(BigNumber left, BigNumber right) => left.m_value > right.m_value;
		public static bool operator <(BigNumber left, BigNumber right) => left.m_value < right.m_value;
		public static bool operator >=(BigNumber left, BigNumber right) => left.m_value >= right.m_value;
		public static bool operator <=(BigNumber left, BigNumber right) => left.m_value <= right.m_value;
		public static BigNumber Max(BigNumber a, BigNumber b) => a > b ? a : b;
		public static BigNumber Pow(BigNumber value, int power) => new(Math.Pow(value.m_value, power));
		public static BigNumber Sqrt(BigNumber value) => new(Math.Sqrt(value.m_value));
	}
}
