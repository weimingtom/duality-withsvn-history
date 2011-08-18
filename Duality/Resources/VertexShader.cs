﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Duality;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Duality.Resources
{
	/// <summary>
	/// Represents an OpenGL VertexShader.
	/// </summary>
	[Serializable]
	public class VertexShader : AbstractShader
	{
		/// <summary>
		/// A VertexShader resources file extension.
		/// </summary>
		public new const string FileExt = ".VertexShader" + Resource.FileExt;
		
		/// <summary>
		/// (Virtual) base path for Duality's embedded default VertexShaders.
		/// </summary>
		public const string VirtualContentPath = ContentProvider.VirtualContentPath + "VertexShader:";
		/// <summary>
		/// (Virtual) path of the <see cref="Minimal"/> VertexShader.
		/// </summary>
		public const string ContentPath_Minimal		= VirtualContentPath + "Minimal";
		/// <summary>
		/// (Virtual) path of the <see cref="SmoothAnim"/> VertexShader.
		/// </summary>
		public const string ContentPath_SmoothAnim	= VirtualContentPath + "SmoothAnim";

		/// <summary>
		/// [GET] Provides access to a minimal VertexShader. It performs OpenGLs default transformation
		/// and forwards a single texture coordinate and color to the fragment stage.
		/// </summary>
		public static ContentRef<VertexShader> Minimal		{ get; private set; }
		/// <summary>
		/// [GET] Provides access to the SmoothAnim VertexShader. In addition to the <see cref="Minimal"/>
		/// setup, it forwards the custom animBlend vertex attribute to the fragment stage.
		/// </summary>
		public static ContentRef<VertexShader> SmoothAnim	{ get; private set; }

		internal static void InitDefaultContent()
		{
			VertexShader tmp;

			tmp = new VertexShader(); tmp.path = ContentPath_Minimal;
			tmp.LoadSource(ReflectionHelper.GetEmbeddedResourceStream(typeof(FragmentShader).Assembly, @"Resources\Default\Minimal.vert"));
			ContentProvider.RegisterContent(tmp.Path, tmp);

			tmp = new VertexShader(); tmp.path = ContentPath_SmoothAnim;
			tmp.LoadSource(ReflectionHelper.GetEmbeddedResourceStream(typeof(FragmentShader).Assembly, @"Resources\Default\SmoothAnim.vert"));
			ContentProvider.RegisterContent(tmp.Path, tmp);

			Minimal		= ContentProvider.RequestContent<VertexShader>(ContentPath_Minimal);
			SmoothAnim	= ContentProvider.RequestContent<VertexShader>(ContentPath_SmoothAnim);
		}


		protected override ShaderType OglShaderType
		{
			get { return ShaderType.VertexShader; }
		}

		public VertexShader() {}
	}
}
