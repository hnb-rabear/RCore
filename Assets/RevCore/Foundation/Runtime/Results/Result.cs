using System;

namespace RevCore
{
	public readonly struct Result
	{
		public bool IsOk { get; }
		public bool IsError => !IsOk;
		public string ErrorMessage { get; }

		private Result(bool ok, string error)
		{
			IsOk = ok;
			ErrorMessage = error;
		}

		public static Result Ok() => new(true, null);
		public static Result Fail(string error) => new(false, error);
	}

	public readonly struct Result<T>
	{
		public bool IsOk { get; }
		public bool IsError => !IsOk;
		public string ErrorMessage { get; }

		private readonly T m_value;

		public T Value
		{
			get
			{
				if (!IsOk)
					throw new InvalidOperationException($"Cannot access Value on error result: {ErrorMessage}");
				return m_value;
			}
		}

		private Result(bool ok, T value, string error)
		{
			IsOk = ok;
			m_value = value;
			ErrorMessage = error;
		}

		public bool TryGetValue(out T value)
		{
			value = m_value;
			return IsOk;
		}

		public T ValueOr(T fallback) => IsOk ? m_value : fallback;

		public static Result<T> Ok(T value) => new(true, value, null);
		public static Result<T> Fail(string error) => new(false, default, error);
	}
}
