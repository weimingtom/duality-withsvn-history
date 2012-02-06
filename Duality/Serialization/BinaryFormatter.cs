﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Duality.Serialization.Surrogates;

namespace Duality.Serialization
{
	/// <summary>
	/// De/Serializes object data.
	/// </summary>
	/// <seealso cref="Duality.Serialization.BinaryMetaFormatter"/>
	public class BinaryFormatter : BinaryFormatterBase
	{
		public BinaryFormatter() : this(null) {}
		public BinaryFormatter(Stream stream) : base(stream)
		{
			this.AddSurrogate(new BitmapSurrogate());
			this.AddSurrogate(new DictionarySurrogate());
		}

		protected override void GetWriteObjectData(object obj, out SerializeType objSerializeType, out DataType dataType, out uint objId)
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

			if (!objSerializeType.Type.IsSerializable && !typeof(ISerializable).IsAssignableFrom(objSerializeType.Type) && this.GetSurrogateFor(objSerializeType.Type) == null) 
				this.SerializationLog.WriteWarning("Serializing object of Type '{0}' which isn't [Serializable]", 
				Log.Type(objSerializeType.Type));
		}
		protected override void WriteObjectBody(DataType dataType, object obj, SerializeType objSerializeType, uint objId)
		{
			if (dataType.IsPrimitiveType())				this.WritePrimitive(obj);
			else if (dataType == DataType.Enum)			this.WriteEnum(obj as Enum, objSerializeType);
			else if (dataType == DataType.String)		this.WriteString(obj as string);
			else if (dataType == DataType.Struct)		this.WriteStruct(obj, objSerializeType);
			else if (dataType == DataType.ObjectRef)	this.writer.Write(objId);
			else if	(dataType == DataType.Array)		this.WriteArray(obj, objSerializeType, objId);
			else if (dataType == DataType.Class)		this.WriteStruct(obj, objSerializeType, objId);
			else if (dataType == DataType.Delegate)		this.WriteDelegate(obj, objSerializeType, objId);
			else if (dataType.IsMemberInfoType())		this.WriteMemberInfo(obj, objId);
		}
		/// <summary>
		/// Writes the specified <see cref="System.Reflection.MemberInfo"/>, including references objects.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		/// <param name="id">The objects id.</param>
		protected void WriteMemberInfo(object obj, uint id = 0)
		{
			this.writer.Write(id);
			if (obj is Type)
			{
				Type type = obj as Type;
				SerializeType cachedType = ReflectionHelper.GetSerializeType(type);

				this.writer.Write(cachedType.TypeString);
			}
			else if (obj is MemberInfo)
			{
				MemberInfo member = obj as MemberInfo;

				this.writer.Write(member.GetMemberId());
			}
			else if (obj == null)
				throw new ArgumentNullException("obj");
			else
				throw new ArgumentException(string.Format("Type '{0}' is not a supported MemberInfo.", obj.GetType()));
		}
		/// <summary>
		/// Writes the specified <see cref="System.Array"/>, including references objects.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		/// <param name="objSerializeType">The <see cref="Duality.Serialization.SerializeType"/> describing the object.</param>
		/// <param name="id">The objects id.</param>
		protected void WriteArray(object obj, SerializeType objSerializeType, uint id = 0)
		{
			Array objAsArray = obj as Array;

			if (objAsArray.Rank != 1) throw new ArgumentException("Non single-Rank arrays are not supported");
			if (objAsArray.GetLowerBound(0) != 0) throw new ArgumentException("Non zero-based arrays are not supported");

			this.writer.Write(objSerializeType.TypeString);
			this.writer.Write(id);
			this.writer.Write(objAsArray.Rank);
			this.writer.Write(objAsArray.Length);

			if		(objAsArray is bool[])		this.WriteArrayData(objAsArray as bool[]);
			else if (objAsArray is byte[])		this.WriteArrayData(objAsArray as byte[]);
			else if (objAsArray is sbyte[])		this.WriteArrayData(objAsArray as sbyte[]);
			else if (objAsArray is short[])		this.WriteArrayData(objAsArray as short[]);
			else if (objAsArray is ushort[])	this.WriteArrayData(objAsArray as ushort[]);
			else if (objAsArray is int[])		this.WriteArrayData(objAsArray as int[]);
			else if (objAsArray is uint[])		this.WriteArrayData(objAsArray as uint[]);
			else if (objAsArray is long[])		this.WriteArrayData(objAsArray as long[]);
			else if (objAsArray is ulong[])		this.WriteArrayData(objAsArray as ulong[]);
			else if (objAsArray is float[])		this.WriteArrayData(objAsArray as float[]);
			else if (objAsArray is double[])	this.WriteArrayData(objAsArray as double[]);
			else if (objAsArray is decimal[])	this.WriteArrayData(objAsArray as decimal[]);
			else if (objAsArray is char[])		this.WriteArrayData(objAsArray as char[]);
			else if (objAsArray is string[])	this.WriteArrayData(objAsArray as string[]);
			else
			{
				for (long l = 0; l < objAsArray.Length; l++)
					this.WriteObject(objAsArray.GetValue(l));
			}
		}
		/// <summary>
		/// Writes the specified structural object, including references objects.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		/// <param name="objSerializeType">The <see cref="Duality.Serialization.SerializeType"/> describing the object.</param>
		/// <param name="id">The objects id.</param>
		protected void WriteStruct(object obj, SerializeType objSerializeType, uint id = 0)
		{
			ISerializable objAsCustom = obj as ISerializable;
			ISurrogate objSurrogate = this.GetSurrogateFor(objSerializeType.Type);

			// Write the structs data type
			this.writer.Write(objSerializeType.TypeString);
			this.writer.Write(id);
			this.writer.Write(objAsCustom != null);
			this.writer.Write(objSurrogate != null);

			if (objSurrogate != null)
			{
				objSurrogate.RealObject = obj;
				objAsCustom = objSurrogate.SurrogateObject;

				CustomSerialIO customIO = new CustomSerialIO();
				try { objSurrogate.WriteConstructorData(customIO); }
				catch (Exception e) { this.LogCustomSerializationError(id, objSerializeType.Type, e); }
				customIO.Serialize(this);
			}

			if (objAsCustom != null)
			{
				CustomSerialIO customIO = new CustomSerialIO();
				try { objAsCustom.WriteData(customIO); }
				catch (Exception e) { this.LogCustomSerializationError(id, objSerializeType.Type, e); }
				customIO.Serialize(this);
			}
			else
			{
				// Assure the type data layout has bee written (only once per file)
				this.WriteTypeDataLayout(objSerializeType);

				// Write the structs fields
				foreach (FieldInfo field in objSerializeType.Fields)
				{
					object val = field.GetValue(obj);

					if (val != null && this.IsFieldBlocked(field))
						val = ReflectionHelper.GetDefaultInstanceOf(field.FieldType);

					this.WriteObject(val);
				}
			}
		}
		/// <summary>
		/// Writes the specified <see cref="System.Delegate"/>, including references objects.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		/// <param name="objSerializeType">The <see cref="Duality.Serialization.SerializeType"/> describing the object.</param>
		/// <param name="id">The objects id.</param>
		protected void WriteDelegate(object obj, SerializeType objSerializeType, uint id = 0)
		{
			bool multi = obj is MulticastDelegate;

			// Write the delegates type
			this.writer.Write(objSerializeType.TypeString);
			this.writer.Write(id);
			this.writer.Write(multi);

			if (!multi)
			{
				Delegate objAsDelegate = obj as Delegate;
				this.WriteObject(objAsDelegate.Method);
				this.WriteObject(objAsDelegate.Target);
			}
			else
			{
				MulticastDelegate objAsDelegate = obj as MulticastDelegate;
				Delegate[] invokeList = objAsDelegate.GetInvocationList();
				this.WriteObject(objAsDelegate.Method);
				this.WriteObject(objAsDelegate.Target);
				this.WriteObject(invokeList);
			}
		}
		/// <summary>
		/// Writes the specified <see cref="System.Enum"/>.
		/// </summary>
		/// <param name="obj">The object to write.</param>
		/// <param name="objSerializeType">The <see cref="Duality.Serialization.SerializeType"/> describing the object.</param>
		protected void WriteEnum(Enum obj, SerializeType objSerializeType)
		{
			this.writer.Write(objSerializeType.TypeString);
			this.writer.Write(obj.ToString());
			this.writer.Write(Convert.ToInt64(obj));
		}

		protected override object ReadObjectBody(DataType dataType)
		{
			object result = null;

			if (dataType.IsPrimitiveType())				result = this.ReadPrimitive(dataType);
			else if (dataType == DataType.String)		result = this.ReadString();
			else if (dataType == DataType.Enum)			result = this.ReadEnum();
			else if (dataType == DataType.Struct)		result = this.ReadStruct();
			else if (dataType == DataType.ObjectRef)	result = this.ReadObjectRef();
			else if (dataType == DataType.Array)		result = this.ReadArray();
			else if (dataType == DataType.Class)		result = this.ReadStruct();
			else if (dataType == DataType.Delegate)		result = this.ReadDelegate();
			else if (dataType.IsMemberInfoType())		result = this.ReadMemberInfo(dataType);

			return result;
		}
		/// <summary>
		/// Reads an <see cref="System.Array"/>, including referenced objects.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		protected Array ReadArray()
		{
			string	arrTypeString	= this.reader.ReadString();
			uint	objId			= this.reader.ReadUInt32();
			int		arrRang			= this.reader.ReadInt32();
			int		arrLength		= this.reader.ReadInt32();
			Type	arrType			= ReflectionHelper.ResolveType(arrTypeString, false);
			if (arrType == null) this.LogCantResolveTypeError(objId, arrTypeString);

			Array arrObj = arrType != null ? Array.CreateInstance(arrType.GetElementType(), arrLength) : null;
			
			// Prepare object reference
			this.InjectObjectId(arrObj, objId);

			if		(arrObj is bool[])		this.ReadArrayData(arrObj as bool[]);
			else if (arrObj is byte[])		this.ReadArrayData(arrObj as byte[]);
			else if (arrObj is sbyte[])		this.ReadArrayData(arrObj as sbyte[]);
			else if (arrObj is short[])		this.ReadArrayData(arrObj as short[]);
			else if (arrObj is ushort[])	this.ReadArrayData(arrObj as ushort[]);
			else if (arrObj is int[])		this.ReadArrayData(arrObj as int[]);
			else if (arrObj is uint[])		this.ReadArrayData(arrObj as uint[]);
			else if (arrObj is long[])		this.ReadArrayData(arrObj as long[]);
			else if (arrObj is ulong[])		this.ReadArrayData(arrObj as ulong[]);
			else if (arrObj is float[])		this.ReadArrayData(arrObj as float[]);
			else if (arrObj is double[])	this.ReadArrayData(arrObj as double[]);
			else if (arrObj is decimal[])	this.ReadArrayData(arrObj as decimal[]);
			else if (arrObj is char[])		this.ReadArrayData(arrObj as char[]);
			else if (arrObj is string[])	this.ReadArrayData(arrObj as string[]);
			else
			{
				for (int l = 0; l < arrLength; l++)
				{
					object elem = this.ReadObject();
					if (arrObj != null) arrObj.SetValue(elem, l);
				}
			}

			return arrObj;
		}
		/// <summary>
		/// Reads a structural object, including referenced objects.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		protected object ReadStruct()
		{
			// Read struct type
			string	objTypeString	= this.reader.ReadString();
			uint	objId			= this.reader.ReadUInt32();
			bool	custom			= this.reader.ReadBoolean();
			bool	surrogate		= this.reader.ReadBoolean();
			Type	objType			= ReflectionHelper.ResolveType(objTypeString, false);
			if (objType == null) this.LogCantResolveTypeError(objId, objTypeString);

			SerializeType objSerializeType = null;
			if (objType != null) objSerializeType = ReflectionHelper.GetSerializeType(objType);
			
			// Retrieve surrogate if requested
			ISurrogate objSurrogate = null;
			if (surrogate && objType != null) objSurrogate = this.GetSurrogateFor(objType);

			// Construct object
			object obj = null;
			if (objType != null)
			{
				if (objSurrogate != null)
				{
					custom = true;

					// Set fake object reference for surrogate constructor: No self-references allowed here.
					this.InjectObjectId(null, objId);

					CustomSerialIO customIO = new CustomSerialIO();
					customIO.Deserialize(this);
					try { obj = objSurrogate.ConstructObject(customIO, objType); }
					catch (Exception e) { this.LogCustomDeserializationError(objId, objType, e); }
				}
				if (obj == null) obj = ReflectionHelper.CreateInstanceOf(objType);
				if (obj == null) obj = ReflectionHelper.CreateInstanceOf(objType, true);
			}

			// Prepare object reference
			this.InjectObjectId(obj, objId);

			// Read custom object data
			if (custom)
			{
				CustomSerialIO customIO = new CustomSerialIO();
				customIO.Deserialize(this);

				ISerializable objAsCustom;
				if (objSurrogate != null)
				{
					objSurrogate.RealObject = obj;
					objAsCustom = objSurrogate.SurrogateObject;
				}
				else
					objAsCustom = obj as ISerializable;

				if (objAsCustom != null)
				{
					try { objAsCustom.ReadData(customIO); }
					catch (Exception e) { this.LogCustomDeserializationError(objId, objType, e); }
				}
				else if (obj != null && objType != null)
				{
					this.SerializationLog.WriteWarning(
						"Object data (Id {0}) is flagged for custom deserialization, yet the objects Type ('{1}') does not support it. Guessing associated fields...",
						objId,
						Log.Type(objType));
					this.SerializationLog.PushIndent();
					foreach (var pair in customIO.Values)
					{
						FieldInfo field = objSerializeType.Fields.FirstOrDefault(f => f.Name == pair.Key);
						if (field == null)
						{
							this.SerializationLog.WriteWarning("No match found: {0}", pair.Key);
						}
						else if (field.FieldType.IsAssignableFrom(pair.Value.GetType()))
						{
							this.SerializationLog.WriteWarning("Match '{0}' differs in FieldType: '{1}', but required '{2}", pair.Key, 
								Log.Type(field.FieldType), 
								Log.Type(pair.Value.GetType()));
						}
						else
						{
							field.SetValue(obj, pair.Value);
						}
					}
					this.SerializationLog.PopIndent();
				}
			}
			// Red non-custom object data
			else
			{
				// Determine data layout
				TypeDataLayout layout	= this.ReadTypeDataLayout(objTypeString);

				// Read fields
				for (int i = 0; i < layout.Fields.Length; i++)
				{
					FieldInfo field = objSerializeType != null ? objSerializeType.Fields.FirstOrDefault(f => f.Name == layout.Fields[i].name) : null;
					Type fieldType = ReflectionHelper.ResolveType(layout.Fields[i].typeString, false);
					object fieldValue = this.ReadObject();

					if (obj != null)
					{
						if (field == null)
							this.SerializationLog.WriteWarning("Field '{0}' not found. Discarding value '{1}'", layout.Fields[i].name, fieldValue);
						else if (field.FieldType != fieldType)
						{
							this.SerializationLog.WriteWarning("Data layout Type '{0}' of field '{1}' does not match reflected Type '{2}'. Trying to convert...'", layout.Fields[i].typeString, layout.Fields[i].name, Log.Type(field.FieldType));
							this.SerializationLog.PushIndent();
							object castVal;
							try
							{
								castVal = Convert.ChangeType(fieldValue, fieldType, System.Globalization.CultureInfo.InvariantCulture);
								this.SerializationLog.Write("...succeeded! Assigning value '{0}'", castVal);
								field.SetValue(obj, castVal);
							}
							catch (Exception)
							{
								this.SerializationLog.WriteWarning("...failed! Discarding value '{0}'", fieldValue);
							}
							this.SerializationLog.PopIndent();
						}
						else if (field.IsNotSerialized)
							this.SerializationLog.WriteWarning("Field '{0}' flagged as [NonSerialized]. Discarding value '{1}'", layout.Fields[i].name, fieldValue);
						else
							field.SetValue(obj, fieldValue);
					}
				}
			}

			return obj;
		}
		/// <summary>
		/// Reads an object reference.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		protected object ReadObjectRef()
		{
			object obj;
			uint objId = this.reader.ReadUInt32();

			if (!this.LookupObjectId(objId, out obj)) throw new ApplicationException(string.Format("Can't resolve object reference '{0}'.", objId));

			return obj;
		}
		/// <summary>
		/// Reads a <see cref="System.Reflection.MemberInfo"/>, including referenced objects.
		/// </summary>
		/// <param name="dataType">The <see cref="Duality.Serialization.DataType"/> of the object to read.</param>
		/// <returns>The object that has been read.</returns>
		protected MemberInfo ReadMemberInfo(DataType dataType)
		{
			uint objId = this.reader.ReadUInt32();
			MemberInfo result = null;

			try
			{
				#region Version 1
				if (this.dataVersion == 1)
				{
					if (dataType == DataType.Type)
					{
						string typeString = this.reader.ReadString();
						Type type = ReflectionHelper.ResolveType(typeString);
						result = type;
					}
					else if (dataType == DataType.FieldInfo)
					{
						bool isStatic = this.reader.ReadBoolean();
						string declaringTypeString = this.reader.ReadString();
						string fieldName = this.reader.ReadString();

						Type declaringType = ReflectionHelper.ResolveType(declaringTypeString);
						FieldInfo field = declaringType.GetField(fieldName, isStatic ? ReflectionHelper.BindStaticAll : ReflectionHelper.BindInstanceAll);
						result = field;
					}
					else if (dataType == DataType.PropertyInfo)
					{
						bool isStatic = this.reader.ReadBoolean();
						string declaringTypeString = this.reader.ReadString();
						string propertyName = this.reader.ReadString();
						string propertyTypeString = this.reader.ReadString();

						int paramCount = this.reader.ReadInt32();
						string[] paramTypeStrings = new string[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypeStrings[i] = this.reader.ReadString();

						Type declaringType = ReflectionHelper.ResolveType(declaringTypeString);
						Type propertyType = ReflectionHelper.ResolveType(propertyTypeString);
						Type[] paramTypes = new Type[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypes[i] = ReflectionHelper.ResolveType(paramTypeStrings[i]);

						PropertyInfo property = declaringType.GetProperty(
							propertyName, 
							isStatic ? ReflectionHelper.BindStaticAll : ReflectionHelper.BindInstanceAll, 
							null, 
							propertyType, 
							paramTypes, 
							null);

						result = property;
					}
					else if (dataType == DataType.MethodInfo)
					{
						bool isStatic = this.reader.ReadBoolean();
						string declaringTypeString = this.reader.ReadString();
						string methodName = this.reader.ReadString();

						int paramCount = this.reader.ReadInt32();
						string[] paramTypeStrings = new string[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypeStrings[i] = this.reader.ReadString();

						Type declaringType = ReflectionHelper.ResolveType(declaringTypeString);
						Type[] paramTypes = new Type[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypes[i] = ReflectionHelper.ResolveType(paramTypeStrings[i]);

						MethodInfo method = declaringType.GetMethod(methodName, isStatic ? ReflectionHelper.BindStaticAll : ReflectionHelper.BindInstanceAll, null, paramTypes, null);
						result = method;
					}
					else if (dataType == DataType.ConstructorInfo)
					{
						bool isStatic = this.reader.ReadBoolean();
						string declaringTypeString = this.reader.ReadString();

						int paramCount = this.reader.ReadInt32();
						string[] paramTypeStrings = new string[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypeStrings[i] = this.reader.ReadString();

						Type declaringType = ReflectionHelper.ResolveType(declaringTypeString);
						Type[] paramTypes = new Type[paramCount];
						for (int i = 0; i < paramCount; i++)
							paramTypes[i] = ReflectionHelper.ResolveType(paramTypeStrings[i]);

						ConstructorInfo method = declaringType.GetConstructor(isStatic ? ReflectionHelper.BindStaticAll : ReflectionHelper.BindInstanceAll, null, paramTypes, null);
						result = method;
					}
					else if (dataType == DataType.EventInfo)
					{
						bool isStatic = this.reader.ReadBoolean();
						string declaringTypeString = this.reader.ReadString();
						string eventName = this.reader.ReadString();

						Type declaringType = ReflectionHelper.ResolveType(declaringTypeString);
						EventInfo e = declaringType.GetEvent(eventName, isStatic ? ReflectionHelper.BindStaticAll : ReflectionHelper.BindInstanceAll);
						result = e;
					}
					else
						throw new ApplicationException(string.Format("Invalid DataType '{0}' in ReadMemberInfo method.", dataType));
				}
				#endregion
				else if (dataVersion == 2)
				{
					if (dataType == DataType.Type)
					{
						string typeString = this.reader.ReadString();
						Type type = ReflectionHelper.ResolveType(typeString);
						result = type;
					}
					else
					{
						string memberString = this.reader.ReadString();
						result = ReflectionHelper.ResolveMember(memberString);
					}
				}
			}
			catch (Exception e)
			{
				result = null;
				this.SerializationLog.WriteError(
					"An error occured in deserializing MemberInfo object Id {0} of type '{1}': {2}",
					objId,
					Log.Type(dataType.ToActualType()),
					Log.Exception(e));
			}
			
			// Prepare object reference
			this.InjectObjectId(result, objId);

			return result;
		}
		/// <summary>
		/// Reads a <see cref="System.Delegate"/>, including referenced objects.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		protected Delegate ReadDelegate()
		{
			string		delegateTypeString	= this.reader.ReadString();
			uint		objId				= this.reader.ReadUInt32();
			bool		multi				= this.reader.ReadBoolean();
			Type		delType				= ReflectionHelper.ResolveType(delegateTypeString, false);
			if (delType == null) this.LogCantResolveTypeError(objId, delegateTypeString);

			// Create the delegate without target and fix it later, so we can register its object id before loading its target object
			MethodInfo	method	= this.ReadObject() as MethodInfo;
			object		target	= null;
			Delegate	del		= delType != null ? Delegate.CreateDelegate(delType, target, method) : null;

			// Prepare object reference
			this.InjectObjectId(del, objId);

			// Read the target object now and replace the dummy
			target = this.ReadObject();
			if (del != null)
			{
				FieldInfo targetField = delType.GetField("_target", ReflectionHelper.BindInstanceAll);
				targetField.SetValue(del, target);
			}

			// Combine multicast delegates
			if (multi)
			{
				Delegate[] invokeList = (this.ReadObject() as Delegate[]).NotNull().ToArray();
				del = invokeList.Length > 0 ? Delegate.Combine(invokeList) : null;
			}

			return del;
		}
		/// <summary>
		/// Reads an <see cref="System.Enum"/>.
		/// </summary>
		/// <returns>The object that has been read.</returns>
		protected Enum ReadEnum()
		{
			object result;

			string typeName = this.reader.ReadString();
			string name = this.reader.ReadString();
			long val = this.reader.ReadInt64();
			Type enumType = ReflectionHelper.ResolveType(typeName);

			result = Enum.Parse(enumType, name);
			if (result != null) return (Enum)result;

			this.SerializationLog.WriteWarning("Can't parse enum value '{0}' of Type '{1}'. Using numerical value '{2}' instead.", name, typeName, val);
			return (Enum)Enum.ToObject(enumType, val);
		}
	}
}
