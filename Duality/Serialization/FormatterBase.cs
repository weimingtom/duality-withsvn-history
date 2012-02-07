﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Duality.Serialization
{
	/// <summary>
	/// Base class for Dualitys serializers.
	/// </summary>
	public abstract class FormatterBase : IDisposable
	{
		/// <summary>
		/// Buffer object for <see cref="Duality.Serialization.ISerializable">custom de/serialization</see>, 
		/// providing read and write functionality.
		/// </summary>
		protected abstract class CustomSerialIOBase<T> : IDataReader, IDataWriter where T : FormatterBase
		{
			protected	Dictionary<string,object>	data;
			
			/// <summary>
			/// [GET] Enumerates all available keys.
			/// </summary>
			public IEnumerable<string> Keys
			{
				get { return this.data.Keys; }
			}
			/// <summary>
			/// [GET] Enumerates all currently stored <see cref="System.Collections.Generic.KeyValuePair{T,U}">KeyValuePairs</see>.
			/// </summary>
			public IEnumerable<KeyValuePair<string,object>> Data
			{
				get { return this.data; }
			}

			public CustomSerialIOBase()
			{
				this.data = new Dictionary<string,object>();
			}

			/// <summary>
			/// Writes the contained data to the specified serializer.
			/// </summary>
			/// <param name="formatter">The serializer to write data to.</param>
			public abstract void Serialize(T formatter);
			/// <summary>
			/// Reads data from the specified serializer
			/// </summary>
			/// <param name="formatter">The serializer to read data from.</param>
			public abstract void Deserialize(T formatter);
			/// <summary>
			/// Clears all contained data.
			/// </summary>
			public void Clear()
			{
				this.data.Clear();
			}
			
			/// <summary>
			/// Writes the specified name and value.
			/// </summary>
			/// <param name="name">
			/// The name to which the written value is mapped. 
			/// May, for example, be the name of a <see cref="System.Reflection.FieldInfo">Field</see>
			/// to which the written value belongs, but there are no naming restrictions, except that one name can't be used twice.
			/// </param>
			/// <param name="value">The value to write.</param>
			/// <seealso cref="IDataWriter"/>
			public void WriteValue(string name, object value)
			{
				this.data[name] = value;
			}
			/// <summary>
			/// Reads the value that is associated with the specified name.
			/// </summary>
			/// <param name="name">The name that is used for retrieving the value.</param>
			/// <returns>The value that has been read using the given name.</returns>
			/// <seealso cref="IDataReader"/>
			/// <seealso cref="ReadValue{T}(string)"/>
			/// <seealso cref="ReadValue{T}(string, out T)"/>
			public object ReadValue(string name)
			{
				object result;
				if (this.data.TryGetValue(name, out result))
					return result;
				else
					return null;
			}
			/// <summary>
			/// Reads the value that is associated with the specified name.
			/// </summary>
			/// <typeparam name="U">The expected value type.</typeparam>
			/// <param name="name">The name that is used for retrieving the value.</param>
			/// <returns>The value that has been read and cast using the given name and type.</returns>
			/// <seealso cref="IDataReader"/>
			/// <seealso cref="ReadValue(string)"/>
			/// <seealso cref="ReadValue{U}(string, out U)"/>
			public U ReadValue<U>(string name)
			{
				object read = this.ReadValue(name);
				if (read is U)
					return (U)read;
				else
				{
					try { return (U)Convert.ChangeType(read, typeof(U), System.Globalization.CultureInfo.InvariantCulture); }
					catch (Exception) { return default(U); }
				}
			}
			/// <summary>
			/// Reads the value that is associated with the specified name.
			/// </summary>
			/// <typeparam name="U">The expected value type.</typeparam>
			/// <param name="name">The name that is used for retrieving the value.</param>
			/// <param name="value">The value that has been read and cast using the given name and type.</param>
			/// <seealso cref="IDataReader"/>
			/// <seealso cref="ReadValue(string)"/>
			/// <seealso cref="ReadValue{U}(string)"/>
			public void ReadValue<U>(string name, out U value)
			{
				value = this.ReadValue<U>(name);
			}
		}


		/// <summary>
		/// Operations, the serializer is able to perform.
		/// </summary>
		protected enum Operation
		{
			/// <summary>
			/// No operation.
			/// </summary>
			None,

			/// <summary>
			/// Read a dataset / object
			/// </summary>
			Read,
			/// <summary>
			/// Write a dataset / object
			/// </summary>
			Write
		}

		/// <summary>
		/// The de/serialization <see cref="Duality.Log"/>.
		/// </summary>
		/// <summary>
		/// A list of <see cref="System.Reflection.FieldInfo">field</see> blockers. If any registered field blocker
		/// returns true upon serializing a specific field, a default value is assumed instead.
		/// </summary>
		protected	List<Predicate<FieldInfo>>	fieldBlockers	= new List<Predicate<FieldInfo>>();
		/// <summary>
		/// A list of <see cref="Duality.Serialization.ISurrogate">Serialization Surrogates</see>. If any of them
		/// matches the <see cref="System.Type"/> of an object that is to be serialized, instead of letting it
		/// serialize itsself, the <see cref="Duality.Serialization.ISurrogate"/> with the highest <see cref="Duality.Serialization.ISurrogate.Priority"/>
		/// is used instead.
		/// </summary>
		protected	List<ISurrogate>			surrogates		= new List<ISurrogate>();

		private	bool		disposed		= false;
		private	Log			log				= Log.Core;
		private	uint		idCounter		= 0;
		private	Dictionary<object,uint>	objRefIdMap	= new Dictionary<object,uint>();
		private	Dictionary<uint,object>	idObjRefMap	= new Dictionary<uint,object>();


		/// <summary>
		/// [GET / SET] The de/serialization <see cref="Duality.Log"/>.
		/// </summary>
		public Log SerializationLog
		{
			get { return this.log; }
			set { this.log = value ?? new Log("Serialize"); }
		}
		/// <summary>
		/// [GET] Enumerates registered <see cref="System.Reflection.FieldInfo">field</see> blockers. If any registered field blocker
		/// returns true upon serializing a specific field, a default value is assumed instead.
		/// </summary>
		public IEnumerable<Predicate<FieldInfo>> FieldBlockers
		{
			get { return this.fieldBlockers; }
		}
		/// <summary>
		/// [GET] Enumerates registered <see cref="Duality.Serialization.ISurrogate">Serialization Surrogates</see>. If any of them
		/// matches the <see cref="System.Type"/> of an object that is to be serialized, instead of letting it
		/// serialize itsself, the <see cref="Duality.Serialization.ISurrogate"/> with the highest <see cref="Duality.Serialization.ISurrogate.Priority"/>
		/// is used instead.
		/// </summary>
		public IEnumerable<ISurrogate> Surrogates
		{
			get { return this.surrogates; }
		}
		/// <summary>
		/// [GET] Whether this binary serializer has been disposed. A disposed object cannot be used anymore.
		/// </summary>
		public bool Disposed
		{
			get { return this.disposed; }
		}


		~FormatterBase()
		{
			this.Dispose(false);
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			this.Dispose(true);
		}
		protected virtual void Dispose(bool manually) {}

		
		/// <summary>
		/// Writes the specified object including all referenced objects.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		public abstract object ReadObject();
		/// <summary>
		/// Reads an object including all referenced objects.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		public abstract void WriteObject(object obj);
		
		/// <summary>
		/// Returns an object indicating a "null" value.
		/// </summary>
		/// <returns></returns>
		protected virtual object GetNullObject() 
		{
			return null;
		}
		/// <summary>
		/// Determines internal data for writing a given object.
		/// </summary>
		/// <param name="obj">The object to write</param>
		/// <param name="objSerializeType">The <see cref="Duality.Serialization.SerializeType"/> that describes the specified object.</param>
		/// <param name="dataType">The <see cref="Duality.Serialization.DataType"/> that is used for writing the specified object.</param>
		/// <param name="objId">An object id that is assigned to the specified object.</param>
		protected virtual void GetWriteObjectData(object obj, out SerializeType objSerializeType, out DataType dataType, out uint objId)
		{
			Type objType = obj.GetType();
			objSerializeType = ReflectionHelper.GetSerializeType(objType);
			objId = 0;
			dataType = objSerializeType.DataType;
			
			// Check whether it's going to be an ObjectRef or not
			if (dataType == DataType.Array || dataType == DataType.Class || dataType == DataType.Delegate || dataType.IsMemberInfoType())
			{
				bool newId;
				objId = this.RequestObjectId(obj, out newId);

				// If its not a new id, write a reference
				if (!newId) dataType = DataType.ObjectRef;
			}

			if (!objSerializeType.Type.IsSerializable && 
				!typeof(ISerializable).IsAssignableFrom(objSerializeType.Type) &&
				this.GetSurrogateFor(objSerializeType.Type) == null) 
			{
				this.SerializationLog.WriteWarning("Serializing object of Type '{0}' which isn't [Serializable]", Log.Type(objSerializeType.Type));
			}
		}


		/// <summary>
		/// Unregisters all <see cref="FieldBlockers"/>.
		/// </summary>
		public void ClearFieldBlockers()
		{
			this.fieldBlockers.Clear();
		}
		/// <summary>
		/// Registers a new <see cref="FieldBlockers">FieldBlocker</see>.
		/// </summary>
		/// <param name="blocker"></param>
		public void AddFieldBlocker(Predicate<FieldInfo> blocker)
		{
			if (this.fieldBlockers.Contains(blocker)) return;
			this.fieldBlockers.Add(blocker);
		}
		/// <summary>
		/// Unregisters an existing <see cref="FieldBlockers">FieldBlocker</see>.
		/// </summary>
		/// <param name="blocker"></param>
		public void RemoveFieldBlocker(Predicate<FieldInfo> blocker)
		{
			this.fieldBlockers.Remove(blocker);
		}
		/// <summary>
		/// Determines whether a specific <see cref="System.Reflection.FieldInfo">field</see> is blocked.
		/// Instead of writing the value of a blocked field, the matching <see cref="System.Type">Types</see>
		/// defautl value is assumed.
		/// </summary>
		/// <param name="field">The <see cref="System.Reflection.FieldInfo">field</see> in question</param>
		/// <returns>True, if the <see cref="System.Reflection.FieldInfo">field</see> is blocked, false if not.</returns>
		public bool IsFieldBlocked(FieldInfo field)
		{
			foreach (var blocker in this.fieldBlockers)
				if (blocker(field)) return true;
			return false;
		}

		/// <summary>
		/// Unregisters all <see cref="Duality.Serialization.ISurrogate">Surrogates</see>.
		/// </summary>
		public void ClearSurrogates()
		{
			this.surrogates.Clear();
		}
		/// <summary>
		/// Registers a new <see cref="Duality.Serialization.ISurrogate">Surrogate</see>.
		/// </summary>
		/// <param name="surrogate"></param>
		public void AddSurrogate(ISurrogate surrogate)
		{
			if (this.surrogates.Contains(surrogate)) return;
			this.surrogates.Add(surrogate);
			this.surrogates.StableSort((s1, s2) => s1.Priority - s2.Priority);
		}
		/// <summary>
		/// Unregisters an existing <see cref="Duality.Serialization.ISurrogate">Surrogate</see>.
		/// </summary>
		/// <param name="surrogate"></param>
		public void RemoveSurrogate(ISurrogate surrogate)
		{
			this.surrogates.Remove(surrogate);
		}
		/// <summary>
		/// Retrieves a matching <see cref="Duality.Serialization.ISurrogate"/> for the specified <see cref="System.Type"/>.
		/// </summary>
		/// <param name="t">The <see cref="System.Type"/> to retrieve a <see cref="Duality.Serialization.ISurrogate"/> for.</param>
		/// <returns></returns>
		public ISurrogate GetSurrogateFor(Type t)
		{
			return this.surrogates.FirstOrDefault(s => s.MatchesType(t));
		}

		/// <summary>
		/// Clears all object id mappings.
		/// </summary>
		protected void ClearObjectIds()
		{
			this.objRefIdMap.Clear();
			this.idObjRefMap.Clear();
			this.idCounter = 0;
		}
		/// <summary>
		/// Returns the id that is assigned to the specified object. Assigns one, if
		/// there is none yet.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="isNewId"></param>
		/// <returns></returns>
		protected uint RequestObjectId(object obj, out bool isNewId)
		{
			uint id;
			if (this.objRefIdMap.TryGetValue(obj, out id))
			{
				isNewId = false;
				return id;
			}

			id = ++idCounter;
			this.objRefIdMap[obj] = id;
			this.idObjRefMap[id] = obj;

			isNewId = true;
			return id;
		}
		/// <summary>
		/// Assigns an id to a specific object.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="id">The id to assign. Zero ids are rejected.</param>
		protected void InjectObjectId(object obj, uint id)
		{
			if (id == 0) return;

			if (obj != null) this.objRefIdMap[obj] = id;
			this.idObjRefMap[id] = obj;
		}
		/// <summary>
		/// Tries to lookup an object based on its id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		protected bool LookupObjectId(uint id, out object obj)
		{
			return this.idObjRefMap.TryGetValue(id, out obj);
		}


		/// <summary>
		/// Logs an error that occured during <see cref="Duality.Serialization.ISerializable">custom serialization</see>.
		/// </summary>
		/// <param name="objId">The object id of the affected object.</param>
		/// <param name="serializeType">The <see cref="System.Type"/> of the affected object.</param>
		/// <param name="e">The <see cref="System.Exception"/> that occured.</param>
		protected void LogCustomSerializationError(uint objId, Type serializeType, Exception e)
		{
			this.log.WriteError(
				"An error occured in custom serialization in object Id {0} of type '{1}': {2}",
				objId,
				Log.Type(serializeType),
				Log.Exception(e));
		}
		/// <summary>
		/// Logs an error that occured during <see cref="Duality.Serialization.ISerializable">custom deserialization</see>.
		/// </summary>
		/// <param name="objId">The object id of the affected object.</param>
		/// <param name="serializeType">The <see cref="System.Type"/> of the affected object.</param>
		/// <param name="e">The <see cref="System.Exception"/> that occured.</param>
		protected void LogCustomDeserializationError(uint objId, Type serializeType, Exception e)
		{
			this.log.WriteError(
				"An error occured in custom deserialization in object Id {0} of type '{1}': {2}",
				objId,
				Log.Type(serializeType),
				Log.Exception(e));
		}
		/// <summary>
		/// Logs an error that occured trying to resolve a <see cref="System.Type"/> by its <see cref="ReflectionHelper.GetTypeId">type string</see>.
		/// </summary>
		/// <param name="objId">The object id of the affected object.</param>
		/// <param name="typeString">The <see cref="ReflectionHelper.GetTypeId">type string</see> that couldn't be resolved.</param>
		protected void LogCantResolveTypeError(uint objId, string typeString)
		{
			this.log.WriteError("Can't resolve Type '{0}' in object Id {1}. Type not found.", typeString, objId);
		}
	}
}
