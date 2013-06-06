﻿using System;
using System.Linq;

using Duality.ColorFormat;
using Duality.Resources;
using Duality.EditorHints;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Duality.Components.Renderers
{
	/// <summary>
	/// Renders a text to represent the <see cref="GameObject"/>.
	/// </summary>
	[Serializable]
	public class TextRenderer : Renderer, ICmpInitializable
	{
		protected	Alignment				blockAlign	= Alignment.Center;
		protected	FormattedText			text		= new FormattedText("Hello World");
		protected	BatchInfo				customMat	= null;
		protected	ColorRgba				colorTint	= ColorRgba.White;
		protected	ContentRef<Material>	iconMat		= ContentRef<Material>.Null;
		[NonSerialized] protected	FormattedText.Metrics			metrics		= new FormattedText.Metrics(Vector2.Zero, new Rect[0], new Rect[0]);
		[NonSerialized]	protected	Rect							textRect	= Rect.Empty;
		[NonSerialized] protected	VertexFormat.VertexC1P3T2[][]	vertFont	= null;
		[NonSerialized] protected	VertexFormat.VertexC1P3T2[]		vertIcon	= null;

		[EditorHintFlags(MemberFlags.Invisible)]
		public override float BoundRadius
		{
			get { return this.textRect.Transform(this.gameobj.Transform.Scale, this.gameobj.Transform.Scale).BoundingRadius; }
		}
		/// <summary>
		/// [GET / SET] The text blocks alignment relative to the <see cref="GameObject"/>.
		/// </summary>
		public Alignment BlockAlign
		{
			get { return this.blockAlign; }
			set
			{
				this.blockAlign = value;
				this.UpdateText();
			}
		}
		/// <summary>
		/// [GET / SET] The text to display. If you change this without re-assigning it, be sure to call <see cref="UpdateText"/>.
		/// </summary>
		[EditorHintFlags(MemberFlags.ForceWriteback)]
		public FormattedText Text
		{
			get { return this.text; }
			set
			{
				this.text = value;
				this.UpdateText();
			}
		}
		/// <summary>
		/// [GET / SET] A color by which the displayed text is tinted.
		/// </summary>
		public ColorRgba ColorTint
		{
			get { return this.colorTint; }
			set { this.colorTint = value; }
		}
		/// <summary>
		/// [GET / SET] The <see cref="Duality.Resources.Material"/> to use for displaying icons ithin the text.
		/// </summary>
		public ContentRef<Material> IconMat
		{
			get { return this.iconMat; }
			set { this.iconMat = value; }
		}
		/// <summary>
		/// [GET] The current texts metrics.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public FormattedText.Metrics Metrics
		{
			get { return this.metrics; }
		}
		/// <summary>
		/// [GET / SET] A custom, local <see cref="Duality.Resources.BatchInfo"/> overriding the texts own <see cref="Duality.Resources.Font.Material">
		/// Materials</see>. Note that it does not override each <see cref="Duality.Resources.Font">Fonts</see> Texture, but their DrawTechniques and
		/// main colors.
		/// </summary>
		public BatchInfo CustomMaterial
		{
			get { return this.customMat; }
			set { this.customMat = value; }
		}


		public TextRenderer() 
		{
			this.text.Fonts = new[] { Font.GenericMonospace10 };
			this.UpdateText();
		}

		/// <summary>
		/// Updates the texts <see cref="Metrics"/> and other internal data. Should be called everytime the text changes.
		/// </summary>
		public void UpdateText()
		{
			this.metrics = this.text.Measure();
			this.textRect = Rect.Align(this.blockAlign, 0.0f, 0.0f, 
				MathF.Max(this.metrics.Size.X, this.text.MaxWidth), 
				MathF.Min(this.metrics.Size.Y, this.text.MaxHeight));
		}

		public override void Draw(IDrawDevice device)
		{
			Vector3 posTemp = this.gameobj.Transform.Pos;
			float scaleTemp = 1.0f;
			device.PreprocessCoords(ref posTemp, ref scaleTemp);

			Vector2 xDot, yDot;
			MathF.GetTransformDotVec(this.GameObj.Transform.Angle, this.gameobj.Transform.Scale * scaleTemp, out xDot, out yDot);

			// Apply block alignment
			Vector2 textOffset = Vector2.Zero;
			this.blockAlign.ApplyTo(ref textOffset, this.metrics.Size);
			MathF.TransformDotVec(ref textOffset, ref xDot, ref yDot);
			posTemp.X += textOffset.X;
			posTemp.Y += textOffset.Y;
			if (this.text.Fonts != null && this.text.Fonts.Any(r => r.IsAvailable && !r.Res.Filtering))
			{
				posTemp.X = MathF.Round(posTemp.X);
				posTemp.Y = MathF.Round(posTemp.Y);
				if (MathF.RoundToInt(device.TargetSize.X) != (MathF.RoundToInt(device.TargetSize.X) / 2) * 2)
					posTemp.X += 0.5f;
				if (MathF.RoundToInt(device.TargetSize.Y) != (MathF.RoundToInt(device.TargetSize.Y) / 2) * 2)
					posTemp.Y += 0.5f;
			}

			// Draw design time metrics data
			if (DualityApp.ExecContext == DualityApp.ExecutionContext.Editor)
			{
				bool showLimits		= true;
				bool showLines		= false;
				bool showElements	= false;
				Vector3 metricsOffset = new Vector3(0.0f, 0.0f, 0.01f);
				Vector3 lineOffset = new Vector3(0.5f, 0.5f, 0.0f);
				Vector3 tUnitX = Vector3.UnitX;
				Vector3 tUnitY = Vector3.UnitY;
				MathF.TransformDotVec(ref tUnitX, ref xDot, ref yDot);
				MathF.TransformDotVec(ref tUnitY, ref xDot, ref yDot);

				// Actual text size and maximum text size
				if (showLimits)
				{
					Vector3 textWidth = tUnitX * this.metrics.Size.X;
					Vector3 textHeight = tUnitY * this.metrics.Size.Y;
					Vector3 textMaxWidth = tUnitX * this.text.MaxWidth;
					Vector3 textMaxHeight = tUnitY * MathF.Max(this.text.MaxHeight, this.metrics.Size.Y);

					ColorRgba clrSize = ColorRgba.Green.WithAlpha(128);
					ColorRgba clrMaxSize = ColorRgba.Red.WithAlpha(128);
					device.AddVertices(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White), VertexMode.LineLoop,
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp, clrSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textWidth, clrSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textWidth + textHeight, clrSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textHeight, clrSize));
					device.AddVertices(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White), VertexMode.LineLoop,
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp, clrMaxSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textMaxWidth, clrMaxSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textMaxWidth + textMaxHeight, clrMaxSize),
						new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + textMaxHeight, clrMaxSize));
				}

				// Individual line sizes
				if (showLines)
				{
					ColorRgba clrLineBg = (ColorRgba.Blue + ColorRgba.Red).WithAlpha(64);
					for (int i = 0; i < this.metrics.LineBounds.Length; i++)
					{
						Rect lineRect = this.metrics.LineBounds[i];
						device.AddVertices(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White), VertexMode.Quads,
							new VertexFormat.VertexC1P3(metricsOffset + posTemp + lineRect.TopLeft.X * tUnitX + lineRect.TopLeft.Y * tUnitY, clrLineBg),
							new VertexFormat.VertexC1P3(metricsOffset + posTemp + lineRect.BottomLeft.X * tUnitX + lineRect.BottomLeft.Y * tUnitY, clrLineBg),
							new VertexFormat.VertexC1P3(metricsOffset + posTemp + lineRect.BottomRight.X * tUnitX + lineRect.BottomRight.Y * tUnitY, clrLineBg),
							new VertexFormat.VertexC1P3(metricsOffset + posTemp + lineRect.TopRight.X * tUnitX + lineRect.TopRight.Y * tUnitY, clrLineBg));
					}
				}

				// Individual line sizes
				if (showElements)
				{
					ColorRgba clrElementBg = (ColorRgba.Blue + ColorRgba.Green).WithAlpha(128);
					for (int i = 0; i < this.metrics.ElementBounds.Length; i++)
					{
						Rect elemRect = this.metrics.ElementBounds[i];
						device.AddVertices(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White), VertexMode.LineLoop,
							new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + elemRect.TopLeft.X * tUnitX + elemRect.TopLeft.Y * tUnitY, clrElementBg),
							new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + elemRect.BottomLeft.X * tUnitX + elemRect.BottomLeft.Y * tUnitY, clrElementBg),
							new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + elemRect.BottomRight.X * tUnitX + elemRect.BottomRight.Y * tUnitY, clrElementBg),
							new VertexFormat.VertexC1P3(metricsOffset + lineOffset + posTemp + elemRect.TopRight.X * tUnitX + elemRect.TopRight.Y * tUnitY, clrElementBg));
					}
				}
			}

			ColorRgba matColor = this.customMat != null ? this.customMat.MainColor : ColorRgba.White;
			this.text.EmitVertices(ref this.vertFont, ref this.vertIcon, posTemp.X, posTemp.Y, posTemp.Z, this.colorTint * matColor, xDot, yDot);
			if (this.text.Fonts != null)
			{
				for (int i = 0; i < this.text.Fonts.Length; i++)
				{
					if (this.text.Fonts[i] != null && this.text.Fonts[i].IsAvailable) 
					{
						if (this.customMat == null)
							device.AddVertices(this.text.Fonts[i].Res.Material, VertexMode.Quads, this.vertFont[i]);
						else
						{
							BatchInfo cm = new BatchInfo(this.customMat);
							cm.Textures = this.text.Fonts[i].Res.Material.Textures;
							device.AddVertices(cm, VertexMode.Quads, this.vertFont[i]);
						}
					}
				}
			}
			if (this.text.Icons != null && this.iconMat.IsAvailable)
			{
				device.AddVertices(this.iconMat, VertexMode.Quads, this.vertIcon);
			}
		}

		
		void ICmpInitializable.OnInit(InitContext context)
		{
			if (context == InitContext.Loaded)
			{
				this.text.ApplySource();
				this.UpdateText();
			}
		}
		void ICmpInitializable.OnShutdown(ShutdownContext context) {}

		protected override void OnCopyTo(Component target, Duality.Cloning.CloneProvider provider)
		{
			base.OnCopyTo(target, provider);
			TextRenderer t = target as TextRenderer;
			t.blockAlign		= this.blockAlign;
			t.text		= this.text.Clone();
			t.colorTint	= this.colorTint;
			t.customMat	= this.customMat != null ? new BatchInfo(this.customMat) : null;
			t.iconMat	= this.iconMat;
			t.UpdateText();
		}
	}
}
