﻿namespace Duality.Serialization
{
	/// <summary>
	/// Provides a general interface for an object type with custom serialization.
	/// </summary>
	public interface ISerializable
	{
		/// <summary>
		/// Writes the object data to the specified <see cref="IDataWriter"/>.
		/// </summary>
		/// <param name="writer"></param>
		void WriteData(IDataWriter writer);
		/// <summary>
		/// Reads and applies the object data to the specified <see cref="IDataReader"/>.
		/// </summary>
		/// <param name="reader"></param>
		void ReadData(IDataReader reader);
	}
}
