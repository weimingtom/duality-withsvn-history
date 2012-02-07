﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

using Duality;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Duality.Resources
{
	/// <summary>
	/// Represents an OpenGL ShaderProgram which consists of a Vertex- and a FragmentShader
	/// </summary>
	/// <seealso cref="Duality.Resources.AbstractShader"/>
	/// <seealso cref="Duality.Resources.VertexShader"/>
	/// <seealso cref="Duality.Resources.FragmentShader"/>
	[Serializable]
	public class ShaderProgram : Resource
	{
		/// <summary>
		/// A ShaderProgram resources file extension.
		/// </summary>
		public new static string FileExt
		{ 
			get { return ".ShaderProgram" + Resource.FileExt; }
		}
		
		/// <summary>
		/// (Virtual) base path for Duality's embedded default ShaderPrograms.
		/// </summary>
		public const string VirtualContentPath = ContentProvider.VirtualContentPath + "ShaderProgram:";
		/// <summary>
		/// (Virtual) path of the <see cref="Minimal"/> ShaderProgram.
		/// </summary>
		public const string ContentPath_Minimal		= VirtualContentPath + "Minimal";
		/// <summary>
		/// (Virtual) path of the <see cref="Picking"/> ShaderProgram.
		/// </summary>
		public const string ContentPath_Picking		= VirtualContentPath + "Picking";
		/// <summary>
		/// (Virtual) path of the <see cref="SmoothAnim"/> ShaderProgram.
		/// </summary>
		public const string ContentPath_SmoothAnim	= VirtualContentPath + "SmoothAnim";

		/// <summary>
		/// A minimal ShaderProgram, using a <see cref="Duality.Resources.VertexShader.Minimal"/> VertexShader and
		/// a <see cref="Duality.Resources.FragmentShader.Minimal"/> FragmentShader.
		/// </summary>
		public static ContentRef<ShaderProgram> Minimal		{ get; private set; }
		/// <summary>
		/// A ShaderProgram designed for picking operations. It uses a 
		/// <see cref="Duality.Resources.VertexShader.Minimal"/> VertexShader and a 
		/// <see cref="Duality.Resources.FragmentShader.Picking"/> FragmentShader.
		/// </summary>
		public static ContentRef<ShaderProgram> Picking		{ get; private set; }
		/// <summary>
		/// The SmoothAnim ShaderProgram, using a <see cref="Duality.Resources.VertexShader.SmoothAnim"/> VertexShader and
		/// a <see cref="Duality.Resources.FragmentShader.SmoothAnim"/> FragmentShader. Some <see cref="Duality.Components.Renderer">Renderers</see>
		/// might react automatically to <see cref="Duality.Resources.Material">Materials</see> using this ShaderProgram and provide a suitable
		/// vertex format.
		/// </summary>
		public static ContentRef<ShaderProgram> SmoothAnim	{ get; private set; }

		internal static void InitDefaultContent()
		{
			ContentProvider.RegisterContent(ContentPath_Minimal, new ShaderProgram(VertexShader.Minimal, FragmentShader.Minimal));
			ContentProvider.RegisterContent(ContentPath_Picking, new ShaderProgram(VertexShader.Minimal, FragmentShader.Picking));
			ContentProvider.RegisterContent(ContentPath_SmoothAnim, new ShaderProgram(VertexShader.SmoothAnim, FragmentShader.SmoothAnim));

			Minimal	= ContentProvider.RequestContent<ShaderProgram>(ContentPath_Minimal);
			Picking	= ContentProvider.RequestContent<ShaderProgram>(ContentPath_Picking);
			SmoothAnim	= ContentProvider.RequestContent<ShaderProgram>(ContentPath_SmoothAnim);
		}
		
		/// <summary>
		/// Refers to a null reference ShaderProgram.
		/// </summary>
		/// <seealso cref="ContentRef{T}.Null"/>
		public static readonly ContentRef<ShaderProgram> None	= ContentRef<ShaderProgram>.Null;
		private	static	ShaderProgram	curBound	= null;
		/// <summary>
		/// [GET] Returns the currently bound ShaderProgram.
		/// </summary>
		public static ContentRef<ShaderProgram> BoundProgram
		{
			get { return new ContentRef<ShaderProgram>(curBound); }
		}

		/// <summary>
		/// Binds a ShaderProgram in order to use it.
		/// </summary>
		/// <param name="prog">The ShaderProgram to be bound.</param>
		public static void Bind(ContentRef<ShaderProgram> prog)
		{
			ShaderProgram progRes = prog.IsExplicitNull ? null : prog.Res;
			if (curBound == progRes) return;

			if (progRes == null)
			{
				GL.UseProgram(0);
				curBound = null;
			}
			else
			{
				if (!progRes.compiled) progRes.Compile();

				if (progRes.glProgramId == 0)	throw new ArgumentException("Specified shader program has no valid OpenGL program Id! Maybe it hasn't been loaded / initialized properly?", "prog");
				if (progRes.Disposed)			throw new ArgumentException("Specified shader program has already been deleted!", "prog");
					
				GL.UseProgram(progRes.glProgramId);
				curBound = progRes;
			}
		}


		private	ContentRef<VertexShader>	vert	= ContentRef<VertexShader>.Null;
		private	ContentRef<FragmentShader>	frag	= ContentRef<FragmentShader>.Null;
		[NonSerialized] private	int				glProgramId	= 0;
		[NonSerialized] private bool			compiled	= false;
		[NonSerialized] private	ShaderVarInfo[]	varInfo		= null;

		/// <summary>
		/// [GET] Returns whether this ShaderProgram has been compiled.
		/// </summary>
		public bool Compiled
		{
			get { return this.compiled; }
		}
		/// <summary>
		/// [GET] Returns an array containing information about the variables that have been declared in shader source code.
		/// </summary>
		public ShaderVarInfo[] VarInfo
		{
			get { return this.varInfo; }
		}
		/// <summary>
		/// [GET] Returns the number of vertex attributes that have been declared.
		/// </summary>
		public int AttribCount
		{
			get { return this.varInfo != null ? this.varInfo.Count(v => v.scope == ShaderVarScope.Attribute) : 0; }
		}
		/// <summary>
		/// [GET] Returns the number of uniform variables that have been declared.
		/// </summary>
		public int UniformCount
		{
			get { return this.varInfo != null ? this.varInfo.Count(v => v.scope == ShaderVarScope.Uniform) : 0; }
		}
		/// <summary>
		/// [GET / SET] The <see cref="VertexShader"/> that is used by this ShaderProgram.
		/// </summary>
		public ContentRef<VertexShader> Vertex
		{
			get { return this.vert; }
			set { this.AttachShaders(value, this.frag); this.Compile(); }
		}
		/// <summary>
		/// [GET / SET] The <see cref="FragmentShader"/> that is used by this ShaderProgram.
		/// </summary>
		public ContentRef<FragmentShader> Fragment
		{
			get { return this.frag; }
			set { this.AttachShaders(this.vert, value); this.Compile(); }
		}

		/// <summary>
		/// Creates a new, empty ShaderProgram.
		/// </summary>
		public ShaderProgram() {}
		/// <summary>
		/// Creates a new ShaderProgram based on a <see cref="VertexShader">Vertex-</see> and a <see cref="FragmentShader"/>.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="f"></param>
		public ShaderProgram(ContentRef<VertexShader> v, ContentRef<FragmentShader> f)
		{
			this.AttachShaders(v, f);
			this.Compile();
		}

		/// <summary>
		/// Re-Attaches the currently used <see cref="VertexShader">Vertex-</see> and <see cref="FragmentShader"/>.
		/// </summary>
		public void AttachShaders()
		{
			this.AttachShaders(this.vert, this.frag);
		}
		/// <summary>
		/// Attaches the specified <see cref="VertexShader">Vertex-</see> and <see cref="FragmentShader"/> to this ShaderProgram.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="f"></param>
		public void AttachShaders(ContentRef<VertexShader> v, ContentRef<FragmentShader> f)
		{
			if (this.glProgramId == 0)	this.glProgramId = GL.CreateProgram();
			else						this.DetachShaders();

			this.compiled = false;
			this.vert = v;
			this.frag = f;

			if (this.vert.IsAvailable) this.vert.Res.AttachTo(this.glProgramId);
			if (this.frag.IsAvailable) this.frag.Res.AttachTo(this.glProgramId);
		}
		/// <summary>
		/// Detaches <see cref="VertexShader">Vertex-</see> and <see cref="FragmentShader"/> from the ShaderProgram.
		/// </summary>
		public void DetachShaders()
		{
			if (this.glProgramId == 0) return;
			this.compiled = false;
			if (this.frag.IsLoaded) this.frag.Res.DetachFrom(this.glProgramId);
			if (this.vert.IsLoaded) this.vert.Res.DetachFrom(this.glProgramId);
		}

		/// <summary>
		/// Compiles the ShaderProgram. This is done automatically when loading the ShaderProgram
		/// or when binding it.
		/// </summary>
		public void Compile()
		{
			if (this.glProgramId == 0) return;
			if (this.compiled) return;

			GL.LinkProgram(this.glProgramId);

			int result;
			GL.GetProgram(this.glProgramId, ProgramParameter.LinkStatus, out result);
			if (result == 0)
			{
				string infoLog = GL.GetProgramInfoLog(this.glProgramId);
				Log.Core.WriteError("Error compiling shader program. InfoLog:\n{0}", infoLog);
				return;
			}
			this.compiled = true;

			// Collect variable infos from sub programs
			ShaderVarInfo[] fragVarArray = this.frag.IsAvailable ? this.frag.Res.VarInfo : null;
			ShaderVarInfo[] vertVarArray = this.vert.IsAvailable ? this.vert.Res.VarInfo : null;
			if (fragVarArray != null && vertVarArray != null)
				this.varInfo = vertVarArray.Union(fragVarArray).ToArray();
			else if (vertVarArray != null)
				this.varInfo = vertVarArray.ToArray();
			else
				this.varInfo = fragVarArray.ToArray();
			// Determine actual variable locations
			for (int i = 0; i < this.varInfo.Length; i++)
			{
				if (this.varInfo[i].scope == ShaderVarScope.Uniform)
					this.varInfo[i].glVarLoc = GL.GetUniformLocation(this.glProgramId, this.varInfo[i].name);
				else
					this.varInfo[i].glVarLoc = GL.GetAttribLocation(this.glProgramId, this.varInfo[i].name);
			}
		}

		protected override void OnLoaded()
		{
			this.AttachShaders();
			this.Compile();
			base.OnLoaded();
		}
		protected override void OnDisposing(bool manually)
		{
			base.OnDisposing(manually);
			if (DualityApp.ExecContext != DualityApp.ExecutionContext.Terminated &&
				this.glProgramId != 0)
			{
				this.DetachShaders();
				GL.DeleteProgram(this.glProgramId);
				this.glProgramId = 0;
			}
		}

		public override void CopyTo(Resource r)
		{
			base.CopyTo(r);
			ShaderProgram c = r as ShaderProgram;
			c.AttachShaders(this.vert, this.frag);
			if (this.compiled) c.Compile();
		}
	}
}
