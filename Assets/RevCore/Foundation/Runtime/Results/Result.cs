using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RevCore.Foundation.Tests")]

namespace RevCore
{
	/// <summary>
	/// Value-type result for operations that either succeed or fail with a message but produce no value.
	/// Construct via <see cref="Ok"/> or <see cref="Fail"/>. Branch on <see cref="IsOk"/>/<see cref="IsError"/>.
	/// </summary>
	public readonly struct Result
	{
		/// <summary>True when the operation succeeded.</summary>
		public bool IsOk { get; }

		/// <summary>True when the operation failed.</summary>
		public bool IsError => !IsOk;

		/// <summary>Human-readable error message, or <c>null</c> on success.</summary>
		public string ErrorMessage { get; }

		private Result(bool ok, string error)
		{
			IsOk = ok;
			ErrorMessage = error;
		}

		/// <summary>Creates a success result.</summary>
		public static Result Ok() => new(true, null);

		/// <summary>Creates a failure result with the given message.</summary>
		public static Result Fail(string error) => new(false, error);
	}

	/// <summary>
	/// Value-type result for operations that produce a value on success. Construct via <see cref="Ok"/>
	/// or <see cref="Fail"/>. Branch with <see cref="TryGetValue"/> (preferred), or read <see cref="Value"/>
	/// after asserting <see cref="IsOk"/> — note that <see cref="Value"/> throws on error.
	/// </summary>
	/// <typeparam name="T">Payload type for the success case.</typeparam>
	public readonly struct Result<T>
	{
		/// <summary>True when the operation succeeded and <see cref="Value"/> is meaningful.</summary>
		public bool IsOk { get; }

		/// <summary>True when the operation failed.</summary>
		public bool IsError => !IsOk;

		/// <summary>Human-readable error message, or <c>null</c> on success.</summary>
		public string ErrorMessage { get; }

		private readonly T m_value;

		/// <summary>The success value. Internal — the public API surface for reading the value is <see cref="TryGetValue"/> or <see cref="ValueOr"/>. Retained for test pins and internal call sites that need to assert the throw-on-error contract.</summary>
		/// <exception cref="InvalidOperationException">Thrown when accessed on an error result.</exception>
		internal T Value
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

		/// <summary>Reads the value when present. Returns <c>true</c> on success; on error returns <c>false</c> with <paramref name="value"/> set to <c>default(T)</c>.</summary>
		public bool TryGetValue(out T value)
		{
			value = m_value;
			return IsOk;
		}

		/// <summary>Returns the success value, or <paramref name="fallback"/> when this is an error result.</summary>
		public T ValueOr(T fallback) => IsOk ? m_value : fallback;

		/// <summary>Creates a success result carrying <paramref name="value"/>.</summary>
		public static Result<T> Ok(T value) => new(true, value, null);

		/// <summary>Creates a failure result with the given message.</summary>
		public static Result<T> Fail(string error) => new(false, default, error);
	}
}
