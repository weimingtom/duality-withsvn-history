﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Duality.ColorFormat;
using Duality.Resources;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Duality.Components.Renderers
{
	[Serializable]
	[RequiredComponent(typeof(Transform))]
	public class AnimSpriteRenderer : SpriteRenderer, ICmpUpdatable, ICmpInitializable
	{
		public enum LoopMode
		{
			Once,
			Loop,
			PingPong,
			RandomSingle,
			FixedSingle
		}

		private	int			animFirstFrame		= 0;
		private	int			animFrameCount		= 0;
		private	int			animDuration		= 0;
		private	LoopMode	animLoopMode		= LoopMode.Loop;
		private	bool		animSmooth			= false;
		private	float		animTime			= 0.0f;
		private	int			animCycle			= 0;

		public int AnimFirstFrame
		{
			get { return this.animFirstFrame; }
			set { this.animFirstFrame = value; }
		}
		public int AnimFrameCount
		{
			get { return this.animFrameCount; }
			set { this.animFrameCount = value; }
		}
		public int AnimDuration
		{
			get { return this.animDuration; }
			set { this.animDuration = value; }
		}
		public float AnimTime
		{
			get { return this.animTime; }
			set { this.animTime = value; }
		}
		public LoopMode AnimLoopMode
		{
			get { return this.animLoopMode; }
			set { this.animLoopMode = value; }
		}
		public bool AnimSmooth
		{
			get { return this.animSmooth; }
			set { this.animSmooth = value; }
		}
		public bool IsAnimationRunning
		{
			get
			{
				switch (this.animLoopMode)
				{
					case LoopMode.FixedSingle:
					case LoopMode.RandomSingle:
						return false;
					case LoopMode.Loop:
					case LoopMode.PingPong:
						return true;
					case LoopMode.Once:
						return this.animTime < this.animDuration;
					default:
						return false;
				}
			}
		}


		public AnimSpriteRenderer() {}
		public AnimSpriteRenderer(Rect rect, ContentRef<Material> mainMat) : base(rect, mainMat) {}
		
		void ICmpUpdatable.OnUpdate()
		{
			if (this.animLoopMode == LoopMode.Loop)
			{
				this.animTime += Time.TimeMult * Time.MsPFMult;
				if (this.animTime > this.animDuration)
				{
					int n = (int)this.animTime / this.animDuration;
					this.animTime -= this.animDuration * n;
					this.animCycle += n;
				}
			}
			else if (this.animLoopMode == LoopMode.Once)
			{
				this.animTime = MathF.Min(this.animTime + Time.TimeMult * Time.MsPFMult, this.animDuration);
			}
			else if (this.animLoopMode == LoopMode.PingPong)
			{
				if (this.animCycle % 2 == 0)
				{
					this.animTime += Time.TimeMult * Time.MsPFMult;
					if (this.animTime > this.animDuration)
					{
						int n = (int)this.animTime / this.animDuration;
						if (n % 2 == 1) this.animTime = this.animDuration;
						else this.animTime = 0.0f;
						this.animCycle += n;
					}
				}
				else
				{
					this.animTime -= Time.TimeMult * Time.MsPFMult;
					if (this.animTime < 0.0f)
					{
						int n = (int)(this.animDuration - this.animTime) / this.animDuration;
						if (n % 2 == 1) this.animTime = 0.0f;
						else this.animTime = this.animDuration;
						this.animCycle += n;
					}
				}
			}
		}
		void ICmpInitializable.OnInit(Component.InitContext context)
		{
			if (context == InitContext.Loaded || context == InitContext.Activate)
			{
				if (this.animLoopMode == LoopMode.RandomSingle)
					this.animTime = MathF.Rnd.NextFloat(this.animDuration);
			}
		}
		void ICmpInitializable.OnShutdown(Component.ShutdownContext context) {}
		
		protected void PrepareVerticesSmooth(ref VertexFormat.VertexC4P3T2[] vertices, IDrawDevice device, float curAnimFrameFade, ColorRGBA mainClr, Rect uvRect, Rect uvRectNext)
		{
			DrawTechnique tech;
			if (this.sharedMat.IsAvailable)
				tech = this.sharedMat.Res.Technique.Res;
			else if (this.customMat != null)
				tech = this.customMat.Technique.Res;
			else
				tech = null;
			BlendMode blend = tech != null ? tech.Blending : BlendMode.Solid;

			Vector3 posTemp = this.gameobj.Transform.Pos;
			float scaleTemp = 1.0f;
			device.PreprocessCoords(this, ref posTemp, ref scaleTemp);

			Vector2 xDot, yDot;
			MathF.GetTransformDotVec(this.GameObj.Transform.Angle, scaleTemp, out xDot, out yDot);

			Rect rectTemp = this.rect.Transform(this.gameobj.Transform.Scale.Xy);
			Vector2 edge1 = rectTemp.TopLeft;
			Vector2 edge2 = rectTemp.BottomLeft;
			Vector2 edge3 = rectTemp.BottomRight;
			Vector2 edge4 = rectTemp.TopRight;

			MathF.TransdormDotVec(ref edge1, ref xDot, ref yDot);
			MathF.TransdormDotVec(ref edge2, ref xDot, ref yDot);
			MathF.TransdormDotVec(ref edge3, ref xDot, ref yDot);
			MathF.TransdormDotVec(ref edge4, ref xDot, ref yDot);

			if (vertices == null || vertices.Length != 8) vertices = new VertexFormat.VertexC4P3T2[8];

			float alphaOld;
			bool affectColor = false;
			if (blend == BlendMode.Add)
			{
				alphaOld = 1.0f - curAnimFrameFade;
			}
			else if (blend == BlendMode.Light || blend == BlendMode.Multiply || blend == BlendMode.Invert)
			{
				alphaOld = 1.0f - curAnimFrameFade;
				affectColor = true;
			}
			else
			{
				alphaOld = 1.0f - MathF.Max(curAnimFrameFade * 2.0f - 1.0f, 0.0f);
				curAnimFrameFade = MathF.Min(curAnimFrameFade * 2.0f, 1.0f);
			}

			vertices[0].pos.X = posTemp.X + edge1.X;
			vertices[0].pos.Y = posTemp.Y + edge1.Y;
			vertices[0].pos.Z = posTemp.Z;
			vertices[0].texCoord.X = uvRect.x;
			vertices[0].texCoord.Y = uvRect.y;
			vertices[0].clr = new ColorRGBA(
				affectColor ? (byte)MathF.Clamp(alphaOld * mainClr.r, 0.0f, 255.0f) : mainClr.r, 
				affectColor ? (byte)MathF.Clamp(alphaOld * mainClr.g, 0.0f, 255.0f) : mainClr.g, 
				affectColor ? (byte)MathF.Clamp(alphaOld * mainClr.b, 0.0f, 255.0f) : mainClr.b, 
				!affectColor ? (byte)MathF.Clamp(alphaOld * mainClr.a, 0.0f, 255.0f) : mainClr.a);

			vertices[1].pos.X = posTemp.X + edge2.X;
			vertices[1].pos.Y = posTemp.Y + edge2.Y;
			vertices[1].pos.Z = posTemp.Z;
			vertices[1].texCoord.X = uvRect.x;
			vertices[1].texCoord.Y = uvRect.MaxY;
			vertices[1].clr = vertices[0].clr;

			vertices[2].pos.X = posTemp.X + edge3.X;
			vertices[2].pos.Y = posTemp.Y + edge3.Y;
			vertices[2].pos.Z = posTemp.Z;
			vertices[2].texCoord.X = uvRect.MaxX;
			vertices[2].texCoord.Y = uvRect.MaxY;
			vertices[2].clr = vertices[0].clr;
				
			vertices[3].pos.X = posTemp.X + edge4.X;
			vertices[3].pos.Y = posTemp.Y + edge4.Y;
			vertices[3].pos.Z = posTemp.Z;
			vertices[3].texCoord.X = uvRect.MaxX;
			vertices[3].texCoord.Y = uvRect.y;
			vertices[3].clr = vertices[0].clr;

			vertices[4].pos.X = posTemp.X + edge1.X;
			vertices[4].pos.Y = posTemp.Y + edge1.Y;
			vertices[4].pos.Z = posTemp.Z;
			vertices[4].texCoord.X = uvRectNext.x;
			vertices[4].texCoord.Y = uvRectNext.y;
			vertices[4].clr = new ColorRGBA(
				affectColor ? (byte)MathF.Clamp(curAnimFrameFade * mainClr.r, 0.0f, 255.0f) : mainClr.r, 
				affectColor ? (byte)MathF.Clamp(curAnimFrameFade * mainClr.g, 0.0f, 255.0f) : mainClr.g, 
				affectColor ? (byte)MathF.Clamp(curAnimFrameFade * mainClr.b, 0.0f, 255.0f) : mainClr.b, 
				!affectColor ? (byte)MathF.Clamp(curAnimFrameFade * mainClr.a, 0.0f, 255.0f) : mainClr.a);

			vertices[5].pos.X = posTemp.X + edge2.X;
			vertices[5].pos.Y = posTemp.Y + edge2.Y;
			vertices[5].pos.Z = posTemp.Z;
			vertices[5].texCoord.X = uvRectNext.x;
			vertices[5].texCoord.Y = uvRectNext.MaxY;
			vertices[5].clr = vertices[4].clr;

			vertices[6].pos.X = posTemp.X + edge3.X;
			vertices[6].pos.Y = posTemp.Y + edge3.Y;
			vertices[6].pos.Z = posTemp.Z;
			vertices[6].texCoord.X = uvRectNext.MaxX;
			vertices[6].texCoord.Y = uvRectNext.MaxY;
			vertices[6].clr = vertices[4].clr;
				
			vertices[7].pos.X = posTemp.X + edge4.X;
			vertices[7].pos.Y = posTemp.Y + edge4.Y;
			vertices[7].pos.Z = posTemp.Z;
			vertices[7].texCoord.X = uvRectNext.MaxX;
			vertices[7].texCoord.Y = uvRectNext.y;
			vertices[7].clr = vertices[4].clr;
		}

		public override void Draw(IDrawDevice device)
		{
			Texture mainTex = this.RetrieveMainTex();
			ColorRGBA mainClr = this.RetrieveMainColor();

			bool isAnimated = this.animFrameCount > 0 && this.animDuration > 0 && mainTex != null && mainTex.Atlas != null;
			int curAnimFrame = 0;
			int nextAnimFrame = 0;
			float curAnimFrameFade = 0.0f;

			Rect uvRect;
			Rect uvRectNext;
			if (mainTex != null)
			{
				if (isAnimated)
				{
					float frameTemp = this.animFrameCount * this.animTime / (float)this.animDuration;
					curAnimFrame = this.animFirstFrame + MathF.Clamp((int)frameTemp, 0, this.animFrameCount - 1);
					curAnimFrame = MathF.Clamp(curAnimFrame, 0, mainTex.Atlas.Count - 1);

					if (this.animSmooth)
					{
						if (this.animLoopMode == LoopMode.Loop)
						{
							nextAnimFrame = MathF.NormalizeVar(curAnimFrame + 1, this.animFirstFrame, this.animFirstFrame + this.animFrameCount);
							nextAnimFrame = MathF.Clamp(nextAnimFrame, 0, mainTex.Atlas.Count - 1);
							curAnimFrameFade = frameTemp - (int)frameTemp;
						}
						else if (this.animLoopMode == LoopMode.Once)
						{
							nextAnimFrame = MathF.Clamp(curAnimFrame + 1, this.animFirstFrame, this.animFirstFrame + this.animFrameCount - 1);
							nextAnimFrame = MathF.Clamp(nextAnimFrame, 0, mainTex.Atlas.Count - 1);
							curAnimFrameFade = frameTemp - (int)frameTemp;
						}
						else if (this.animLoopMode == LoopMode.PingPong)
						{
							if (this.animCycle % 2 == 0)
							{
								nextAnimFrame = MathF.Clamp(curAnimFrame + 1, this.animFirstFrame, this.animFirstFrame + this.animFrameCount - 1);
								nextAnimFrame = MathF.Clamp(nextAnimFrame, 0, mainTex.Atlas.Count - 1);
								curAnimFrameFade = frameTemp - (int)frameTemp;
							}
							else
							{
								nextAnimFrame = MathF.Clamp(curAnimFrame - 1, this.animFirstFrame, this.animFirstFrame + this.animFrameCount - 1);
								nextAnimFrame = MathF.Clamp(nextAnimFrame, 0, mainTex.Atlas.Count - 1);
								curAnimFrameFade = 1.0f + MathF.Min((int)frameTemp, this.animFrameCount - 1) - frameTemp;
							}
						}
					}

					uvRect = mainTex.Atlas[curAnimFrame];
					uvRectNext = mainTex.Atlas[nextAnimFrame];
				}
				else
					uvRect = uvRectNext = new Rect(mainTex.UVRatio.X, mainTex.UVRatio.Y);
			}
			else
				uvRect = uvRectNext = new Rect(1.0f, 1.0f);

			if (!animSmooth)
				this.PrepareVertices(ref this.vertices, device, mainClr, uvRect);
			else
				this.PrepareVerticesSmooth(ref this.vertices, device, curAnimFrameFade, mainClr, uvRect, uvRectNext);

			if (this.customMat != null)
				device.AddVertices(this.customMat, BeginMode.Quads, this.vertices);
			else
				device.AddVertices(this.sharedMat, BeginMode.Quads, this.vertices);
		}
		internal override void CopyToInternal(Component target)
		{
			base.CopyToInternal(target);
			AnimSpriteRenderer t = target as AnimSpriteRenderer;
			t.animCycle = this.animCycle;
			t.animDuration = this.animDuration;
			t.animFirstFrame = this.animFirstFrame;
			t.animFrameCount = this.animFrameCount;
			t.animLoopMode = this.animLoopMode;
			t.animTime = this.animTime;
			t.animSmooth = this.animSmooth;
		}
	}
}