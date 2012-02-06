﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Duality.Serialization;

namespace Duality
{
	/// <summary>
	/// The abstract Resource class is inherited by any kind of Duality content. Instances of it or one of its subclasses
	/// are usually handled wrapped inside a <see cref="ContentRef{T}"/> and requested from the <see cref="ContentProvider"/>.
	/// </summary>
	/// <seealso cref="ContentRef{T}"/>
	/// <seealso cref="ContentProvider"/>
	[Serializable]
	public abstract class Resource : IManageableObject, IDisposable
	{
		/// <summary>
		/// A Resource files extension.
		/// </summary>
		public const string FileExt = ".res";

		private	static	List<Resource>	finalizeSched	= new List<Resource>();
		
		/// <summary>
		/// The path of this Resource.
		/// </summary>
		[NonSerialized]	protected	string	path		= null;
		[NonSerialized]	private		bool	disposed	= false;

		/// <summary>
		/// [GET] Returns whether the Resource has been disposed. 
		/// Disposed Resources are not to be used and are treated the same as a null value by most methods.
		/// </summary>
		public bool Disposed
		{
			get { return this.disposed; }
		}
		/// <summary>
		/// [GET] The path where this Resource has been originally loaded from or was first saved to.
		/// It is also the path under which this Resource is registered at the ContentProvider.
		/// </summary>
		public string Path
		{
			get { return this.path; }
			internal set { this.path = value; }
		}
		/// <summary>
		/// [GET] The name of the Resource.
		/// </summary>
		public string Name
		{
			get
			{
				string nameTemp = this.path ?? "";
				if (this.IsDefaultContent) nameTemp = nameTemp.Replace(':', '/');
				return System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(nameTemp));
			}
		}
		/// <summary>
		/// [GET] Returns whether the Resource is part of Duality's embedded default content.
		/// </summary>
		public bool IsDefaultContent
		{
			get { return this.path != null && this.path.Contains(':'); }
		}
		bool IManageableObject.Active
		{
			get { return !this.Disposed; }
		}

		/// <summary>
		/// Saves the Resource to the specified path. If it has been generated at runtime, i.e. has
		/// not been loaded from file before, this will set the Resources <see cref="Path"/> Property.
		/// </summary>
		/// <param name="saveAsPath">The path to which this Resource is saved to. If null, the Resources <see cref="Path"/> is used as destination.</param>
		public void Save(string saveAsPath = null)
		{
			if (this.disposed) throw new ApplicationException("Can't save Ressource that already has been disposed.");
			if (saveAsPath == null) saveAsPath = this.path;

			// We're saving a new Ressource for the first time: Register it in the library
			if (this.path == null)
			{
				this.path = saveAsPath;
				ContentProvider.RegisterContent(this.path, this);
			}

			string dirName = System.IO.Path.GetDirectoryName(saveAsPath);
			if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
			using (FileStream str = File.Open(saveAsPath, FileMode.Create))
			{
				this.Save(str);
			}
		}
		/// <summary>
		/// Saves the Resource to the specified stream.
		/// </summary>
		/// <param name="str"></param>
		public void Save(Stream str)
		{
			this.OnSaving();
			FormatterBase formatter = DualityApp.RequestSerializer(str);
			formatter.AddFieldBlocker(NonSerializedResourceBlocker);
			formatter.WriteObject(this);
			this.OnSaved();
			Log.Core.Write("Resource saved: {0}", (str is FileStream) ? (str as FileStream).Name : str.ToString());
		}

		/// <summary>
		/// Creates a deep copy of this Resource.
		/// </summary>
		/// <returns></returns>
		public Resource Clone()
		{
			Resource r = ReflectionHelper.CreateInstanceOf(this.GetType()) as Resource;
			this.CopyTo(r);
			return r;
		}
		/// <summary>
		/// Deep-copies this Resource to the specified target Resource. The target Resource's Type must
		/// match this Resource's Type.
		/// </summary>
		/// <param name="r">The target Resource to copy this Resource's data to</param>
		public virtual void CopyTo(Resource r)
		{
			r.path	= this.path;
		}

		/// <summary>
		/// Called when this Resource is now beginning to be saved.
		/// </summary>
		protected virtual void OnSaving() {}
		/// <summary>
		/// Called when this Resource has just been saved.
		/// </summary>
		protected virtual void OnSaved() {}
		/// <summary>
		/// Called when this Resource has just been loaded.
		/// </summary>
		protected virtual void OnLoaded() {}

		~Resource()
		{
			finalizeSched.Add(this);
		}
		/// <summary>
		/// Disposes the Resource.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool manually)
		{
			if (!this.disposed)
			{
				this.OnDisposing(manually);
				this.disposed = true;
			}
		}
		/// <summary>
		/// Called when beginning to dispose the Resource.
		/// </summary>
		/// <param name="manually"></param>
		protected virtual void OnDisposing(bool manually)
		{

		}

		/// <summary>
		/// Creates a <see cref="ContentRef{T}"/> referring to this Resource.
		/// </summary>
		/// <returns>A <see cref="ContentRef{T}"/> referring to this Resource.</returns>
		public IContentRef GetContentRef()
		{
			Type refType = typeof(ContentRef<>).MakeGenericType(this.GetType());
			return Activator.CreateInstance(refType, this) as IContentRef;
		}
		
		public override string ToString()
		{
			return this.Name;
		}

		/// <summary>
		/// Loads the Resource that is located at the specified path. You usually don't need this method. 
		/// Consider requesting the Resource from the <see cref="ContentProvider"/> instead.
		/// </summary>
		/// <typeparam name="T">
		/// Desired Type of the returned reference. Does not affect the loaded Resource in any way - it is simply returned as T.
		/// Results in returning null if the loaded Resource's Type isn't assignable to T.
		/// </typeparam>
		/// <param name="path">The path to load the Resource from.</param>
		/// <returns>The Resource that has been loaded.</returns>
		public static T LoadResource<T>(string path) where T : Resource
		{
			if (!File.Exists(path)) return null;

			T newContent = null;
			using (FileStream str = File.OpenRead(path))
			{
				newContent = LoadResource<T>(str, path);
			}
			return newContent;
		}
		/// <summary>
		/// Loads the Resource from the specified <see cref="Stream"/>. You usually don't need this method. 
		/// Consider requesting the Resource from the <see cref="ContentProvider"/> instead.
		/// </summary>
		/// <typeparam name="T">
		/// Desired Type of the returned reference. Does not affect the loaded Resource in any way - it is simply returned as T.
		/// Results in returning null if the loaded Resource's Type isn't assignable to T.
		/// </typeparam>
		/// <param name="str">The stream to load the Resource from.</param>
		/// <param name="resPath">The path that is assumed as the loaded Resource's origin.</param>
		/// <returns>The Resource that has been loaded.</returns>
		public static T LoadResource<T>(Stream str, string resPath = null) where T : Resource
		{
			T newContent = null;
			try
			{
				FormatterBase formatter = DualityApp.RequestSerializer(str);
				Resource res = formatter.ReadObject() as Resource;
				if (res == null) throw new ApplicationException("Loading Resource failed");

				res.path = resPath;
				res.OnLoaded();
				newContent = res as T;
			}
			catch (System.Runtime.Serialization.SerializationException)
			{
				Log.Core.WriteError("Can't load {0} from Stream '{1}'",
					Log.Type(typeof(T)),
					(str is FileStream) ? (str as FileStream).Name : str.ToString());
			}
			return newContent;
		}

		/// <summary>
		/// Returns the Resource file extension for a specific Resource Type.
		/// </summary>
		/// <param name="resType">The Resource Type to return the file extension from.</param>
		/// <returns>The specified Resource Type's file extension.</returns>
		public static string GetFileExtByType(Type resType)
		{
			return "." + resType.Name + Resource.FileExt;
		}
		/// <summary>
		/// Returns the Resource Type that is associated with the specified file, based on its extension.
		/// </summary>
		/// <param name="filePath">Path to the file of whichs Resource Type will be returned</param>
		/// <returns>The Resource Type of the specified file</returns>
		public static Type GetTypeByFileName(string filePath)
		{
			if (filePath == null || filePath.Contains(':')) return null;
			filePath = System.IO.Path.GetFileNameWithoutExtension(filePath);
			string[] token = filePath.Split('.');
			if (token.Length < 2) return null;
			return DualityApp.GetAvailDualityTypes(typeof(Duality.Resource)).FirstOrDefault(t => t.Name == token[token.Length - 1]);
		}

		/// <summary>
		/// A <see cref="Duality.Serialization.BinaryFormatter.FieldBlockers">FieldBlocker</see> to prevent
		/// fields flagged with a <see cref="NonSerializedResourceAttribute"/> from being serialized.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		protected static bool NonSerializedResourceBlocker(FieldInfo field)
		{
			return field.GetCustomAttributes(typeof(NonSerializedResourceAttribute), true).Any();
		}

		internal static void RunCleanup()
		{
			while (finalizeSched.Count > 0)
			{
				finalizeSched[finalizeSched.Count - 1].Dispose(false);
				finalizeSched.RemoveAt(finalizeSched.Count - 1);
			}
		}
	}

	/// <summary>
	/// Indicates that a field will be assumed null when serializing it as part of a Resource serialization.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class NonSerializedResourceAttribute : Attribute
	{
		public NonSerializedResourceAttribute() {}
	}
}
