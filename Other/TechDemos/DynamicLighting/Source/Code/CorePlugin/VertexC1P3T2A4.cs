﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Duality;
using Duality.ColorFormat;
using Duality.VertexFormat;
using Duality.Resources;

namespace Debug
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexC1P3T2A4 : IVertexData
	{
		public ColorRgba clr;
		public Vector3 pos;
		public Vector2 texCoord;
		public Vector4 attrib;

		public OpenTK.Vector3 Pos
		{
			get { return this.pos; }
			set { this.pos = value; }
		}
		public int VertexTypeIndex
		{
			get { return (int)VertexDataFormat.Count; }
		}
		
		void IVertexData.SetupVBO<T>(T[] vertexData, BatchInfo mat)
		{
			if (mat.Technique != DrawTechnique.Picking) GL.EnableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);

			if (mat.Technique != DrawTechnique.Picking) GL.ColorPointer(4, ColorPointerType.UnsignedByte, Size, (IntPtr)OffsetColor);
			GL.VertexPointer(3, VertexPointerType.Float, Size, (IntPtr)OffsetPos);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, Size, (IntPtr)OffsetTex0);

			if (mat.Technique.Res.Shader.IsAvailable)
			{
				ShaderVarInfo[] varInfo = mat.Technique.Res.Shader.Res.VarInfo;
				for (int i = 0; i < varInfo.Length; i++)
				{
					if (varInfo[i].glVarLoc == -1) continue;
					if (varInfo[i].scope != ShaderVarScope.Attribute) continue;
					if (varInfo[i].type != ShaderVarType.Vec4) continue;
				
					GL.EnableVertexAttribArray(varInfo[i].glVarLoc);
					GL.VertexAttribPointer(varInfo[i].glVarLoc, 4, VertexAttribPointerType.Float, false, Size, (IntPtr)OffsetAttrib);
					break;
				}
			}

			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Size * vertexData.Length), IntPtr.Zero, BufferUsageHint.StreamDraw);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Size * vertexData.Length), vertexData, BufferUsageHint.StreamDraw);
		}
		void IVertexData.FinishVBO(BatchInfo mat)
		{
			GL.DisableClientState(ArrayCap.ColorArray);
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.TextureCoordArray);

			if (mat.Technique.Res.Shader.IsAvailable)
			{
				ShaderVarInfo[] varInfo = mat.Technique.Res.Shader.Res.VarInfo;
				for (int i = 0; i < varInfo.Length; i++)
				{
					if (varInfo[i].glVarLoc == -1) continue;
					if (varInfo[i].scope != ShaderVarScope.Attribute) continue;
					if (varInfo[i].type != ShaderVarType.Vec4) continue;
				
					GL.DisableVertexAttribArray(varInfo[i].glVarLoc);
					break;
				}
			}
		}


		public const int OffsetColor	= 0;
		public const int OffsetPos		= OffsetColor + 4 * sizeof(byte);
		public const int OffsetTex0		= OffsetPos + 3 * sizeof(float);
		public const int OffsetAttrib	= OffsetTex0 + 2 * sizeof(float);
		public const int Size			= OffsetAttrib + 4 * sizeof(float);
	}
}