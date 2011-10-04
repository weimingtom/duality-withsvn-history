﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Duality;
using Duality.Components;
using Duality.Resources;
using Duality.ColorFormat;
using Duality.VertexFormat;

using DualityEditor;
using DualityEditor.Forms;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace EditorBase
{
	public abstract class CamViewState
	{
		[Flags]
		public enum AxisLock
		{
			None	= 0x0,

			X		= 0x1,
			Y		= 0x2,
			Z		= 0x4,

			All		= X | Y | Z
		}
		public enum CameraAction
		{
			None,
			MoveCam,
			TurnCam
		}
		public enum MouseAction
		{
			None,
			RectSelection,
			MoveObj,
			RotateObj,
			ScaleObj
		}
		public abstract class SelObj : IEquatable<SelObj>
		{
			public abstract object ActualObject { get; }
			public abstract float BoundRadius { get; }
			public abstract Vector3 Pos { get; set; }
			public virtual Vector3 Scale
			{
				get { return Vector3.One; }
				set {}
			}
			public virtual float Angle
			{
				get { return 0.0f; }
				set {}
			}
			public virtual bool ShowBoundRadius
			{
				get { return true; }
			}
			public virtual bool ShowPos
			{
				get { return true; }
			}
			public virtual bool ShowAngle
			{
				get { return false; }
			}

			public virtual bool IsActionAvailable(MouseAction action)
			{
				if (action == MouseAction.MoveObj) return true;
				return false;
			}
			
			public override bool Equals(object obj)
			{
				if (obj is SelObj)
					return this == (SelObj)obj;
				else
					return base.Equals(obj);
			}
			public override int GetHashCode()
			{
				return this.ActualObject.GetHashCode();
			}
			public bool Equals(SelObj other)
			{
				return this == other;
			}

			public static bool operator ==(SelObj first, SelObj second)
			{
				if (object.ReferenceEquals(first, null))
				{
					if (object.ReferenceEquals(second, null)) return true;
					else return false;
				}
				else if (object.ReferenceEquals(second, null))
					return false;

				return first.ActualObject == second.ActualObject;
			}
			public static bool operator !=(SelObj first, SelObj second)
			{
				return !(first == second);
			}
		}


		private	CamView			view					= null;
		private	AxisLock		lockedAxes				= AxisLock.None;
		private	Point			camActionBeginLoc		= Point.Empty;
		private Vector3			camActionBeginLocSpace	= Vector3.Zero;
		private	CameraAction	camAction				= CameraAction.None;
		private	bool			camActionAllowed		= true;
		private	bool			camTransformChanged		= false;
		private	Point			actionBeginLoc		= Point.Empty;
		private Vector3			actionBeginLocSpace	= Vector3.Zero;
		private MouseAction		action				= MouseAction.None;
		private	Vector3			selectionCenter		= Vector3.Zero;
		private	float			selectionRadius		= 0.0f;
		private	ObjectSelection	activeRectSel		= new ObjectSelection();
		protected	MouseAction		mouseoverAction	= MouseAction.None;
		protected	SelObj			mouseoverObject	= null;
		protected	bool			mouseoverSelect	= false;
		protected	List<SelObj>	actionObjSel	= new List<SelObj>();
		protected	List<SelObj>	allObjSel		= new List<SelObj>();
		protected	List<SelObj>	indirectObjSel	= new List<SelObj>();

		public CamView View
		{
			get { return this.view; }
			internal set { this.view = value; }
		}
		public AxisLock LockedAxes
		{
			get { return this.lockedAxes; }
		}
		public MouseAction SelObjAction
		{
			get { return this.action; }
		}
		public abstract string StateName { get; }
		protected bool CameraActionAllowed
		{
			get { return this.camActionAllowed; }
			set
			{ 
				this.camActionAllowed = value;
				if (!this.camActionAllowed) this.camAction = CameraAction.None;
			}
		}
		protected bool CameraTransformChanged
		{
			get { return this.camTransformChanged; }
		}

		internal protected virtual void OnEnterState()
		{
			this.View.LocalGLControl.Paint		+= this.LocalGLControl_Paint;
			this.View.LocalGLControl.MouseDown	+= this.LocalGLControl_MouseDown;
			this.View.LocalGLControl.MouseUp	+= this.LocalGLControl_MouseUp;
			this.View.LocalGLControl.MouseMove	+= this.LocalGLControl_MouseMove;
			this.View.LocalGLControl.MouseWheel += this.LocalGLControl_MouseWheel;
			this.View.LocalGLControl.KeyDown	+= this.LocalGLControl_KeyDown;
			this.View.LocalGLControl.LostFocus	+= this.LocalGLControl_LostFocus;
			this.View.AccMovementChanged		+= this.View_AccMovementChanged;
			this.View.ParallaxRefDistChanged	+= this.View_ParallaxRefDistChanged;
			EditorBasePlugin.Instance.EditorForm.AfterUpdateDualityApp += this.EditorForm_AfterUpdateDualityApp;

			Scene.Leaving += this.Scene_Changed;
			Scene.Entered += this.Scene_Changed;
			Scene.GameObjectRegistered += this.Scene_Changed;
			Scene.GameObjectUnregistered += this.Scene_Changed;
			Scene.RegisteredObjectComponentAdded += this.Scene_Changed;
			Scene.RegisteredObjectComponentRemoved += this.Scene_Changed;

			if (Scene.Current != null) this.Scene_Changed(this, EventArgs.Empty);
		}
		internal protected virtual void OnLeaveState() 
		{
			this.View.LocalGLControl.Paint		-= this.LocalGLControl_Paint;
			this.View.LocalGLControl.MouseDown	-= this.LocalGLControl_MouseDown;
			this.View.LocalGLControl.MouseUp	-= this.LocalGLControl_MouseUp;
			this.View.LocalGLControl.MouseMove	-= this.LocalGLControl_MouseMove;
			this.View.LocalGLControl.MouseWheel -= this.LocalGLControl_MouseWheel;
			this.View.LocalGLControl.KeyDown	-= this.LocalGLControl_KeyDown;
			this.View.LocalGLControl.LostFocus	-= this.LocalGLControl_LostFocus;
			this.View.AccMovementChanged		-= this.View_AccMovementChanged;
			this.View.ParallaxRefDistChanged	-= this.View_ParallaxRefDistChanged;
			EditorBasePlugin.Instance.EditorForm.AfterUpdateDualityApp -= this.EditorForm_AfterUpdateDualityApp;
			
			Scene.Leaving -= this.Scene_Changed;
			Scene.Entered -= this.Scene_Changed;
			Scene.GameObjectRegistered -= this.Scene_Changed;
			Scene.GameObjectUnregistered -= this.Scene_Changed;
			Scene.RegisteredObjectComponentAdded -= this.Scene_Changed;
			Scene.RegisteredObjectComponentRemoved -= this.Scene_Changed;
		}
		protected virtual void OnCollectStateDrawcalls(Canvas canvas)
		{
			Point cursorPos = this.View.LocalGLControl.PointToClient(Cursor.Position);
			canvas.PushState();
			
			// Draw indirectly selected object overlay
			canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, this.View.BgColor, 0.75f)));
			this.DrawSelectionMarkers(canvas, this.indirectObjSel);
			if (this.mouseoverObject != null && (this.mouseoverAction == MouseAction.RectSelection || this.mouseoverSelect) && !this.allObjSel.Contains(this.mouseoverObject)) 
				this.DrawSelectionMarkers(canvas, new [] { this.mouseoverObject });

			// Draw selected object overlay
			canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, this.View.FgColor));
			this.DrawSelectionMarkers(canvas, this.allObjSel);

			// Draw overall selection boundary
			if (this.allObjSel.Count > 1)
			{
				float midZ = this.allObjSel.Average(t => t.Pos.Z);
				float maxZDiff = this.allObjSel.Max(t => MathF.Abs(t.Pos.Z - midZ));
				if (maxZDiff > 0.001f)
				{
					canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, this.View.BgColor, 0.5f)));
					canvas.DrawSphere(
						this.selectionCenter.X, 
						this.selectionCenter.Y, 
						this.selectionCenter.Z, 
						this.selectionRadius);
				}
				else
				{
					canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, this.View.BgColor, 0.5f)));
					canvas.DrawCircle(
						this.selectionCenter.X, 
						this.selectionCenter.Y, 
						this.selectionCenter.Z, 
						this.selectionRadius);
				}
			}

			// Draw scale action dots
			bool canMove = this.actionObjSel.Any(s => s.IsActionAvailable(MouseAction.MoveObj));
			bool canScale = (canMove && this.actionObjSel.Count > 1) || this.actionObjSel.Any(s => s.IsActionAvailable(MouseAction.ScaleObj));
			if (canScale)
			{
				float dotR = 3.0f / this.View.GetScaleAtZ(this.selectionCenter.Z);
				canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, this.View.FgColor));
				canvas.FillCircle(
					this.selectionCenter.X + this.selectionRadius, 
					this.selectionCenter.Y, 
					this.selectionCenter.Z - 0.01f,
					dotR);
				canvas.FillCircle(
					this.selectionCenter.X - this.selectionRadius, 
					this.selectionCenter.Y, 
					this.selectionCenter.Z - 0.01f,
					dotR);
				canvas.FillCircle(
					this.selectionCenter.X, 
					this.selectionCenter.Y + this.selectionRadius, 
					this.selectionCenter.Z - 0.01f,
					dotR);
				canvas.FillCircle(
					this.selectionCenter.X, 
					this.selectionCenter.Y - this.selectionRadius, 
					this.selectionCenter.Z - 0.01f,
					dotR);
			}

			// Draw action lock axes
			if (this.action == MouseAction.MoveObj)
				this.DrawLockedAxes(canvas, this.selectionCenter.X, this.selectionCenter.Y, this.selectionCenter.Z, this.selectionRadius * 4);

			canvas.PopState();
		}
		protected virtual void OnCollectStateOverlayDrawcalls(Canvas canvas)
		{
			Point cursorPos = this.View.LocalGLControl.PointToClient(Cursor.Position);
			canvas.PushState();
			canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, this.View.FgColor));

			// Draw camera movement indicators
			if (this.camAction == CameraAction.MoveCam)
				canvas.DrawLine(this.camActionBeginLoc.X, this.camActionBeginLoc.Y, cursorPos.X, cursorPos.Y);
			else if (this.camAction == CameraAction.TurnCam)
				canvas.DrawLine(this.camActionBeginLoc.X, this.camActionBeginLoc.Y, cursorPos.X, this.camActionBeginLoc.Y);

			// Draw rect selection
			if (this.action == MouseAction.RectSelection)
				canvas.DrawRect(this.actionBeginLoc.X, this.actionBeginLoc.Y, cursorPos.X - this.actionBeginLoc.X, cursorPos.Y - this.actionBeginLoc.Y);

			canvas.PopState();
		}
		protected virtual void OnRenderState()
		{
			// Render CamView
			this.View.CameraComponent.Render();
		}
		protected virtual void OnUpdateState()
		{
			GameObject camObj = this.View.CameraObj;
			Point cursorPos = this.View.LocalGLControl.PointToClient(Cursor.Position);

			this.camTransformChanged = false;
			if (this.View.AccMovement)
			{
				if (this.camAction == CameraAction.MoveCam)
				{
					Vector3 moveVec = new Vector3(
						0.125f * MathF.Sign(cursorPos.X - this.camActionBeginLoc.X) * MathF.Pow(MathF.Abs(cursorPos.X - this.camActionBeginLoc.X), 1.25f),
						0.125f * MathF.Sign(cursorPos.Y - this.camActionBeginLoc.Y) * MathF.Pow(MathF.Abs(cursorPos.Y - this.camActionBeginLoc.Y), 1.25f),
						camObj.Transform.RelativeVel.Z);

					MathF.TransformCoord(ref moveVec.X, ref moveVec.Y, camObj.Transform.Angle);
					camObj.Transform.RelativeVel = moveVec;

					this.camTransformChanged = true;
				}
				else if (camObj.Transform.RelativeVel.Length > 0.01f)
				{
					camObj.Transform.RelativeVel *= MathF.Pow(0.9f, Time.TimeMult);
					this.camTransformChanged = true;
				}
				else
					camObj.Transform.RelativeVel = Vector3.Zero;
			

				if (this.camAction == CameraAction.TurnCam)
				{
					float turnDir = 
						0.000125f * MathF.Sign(cursorPos.X - this.camActionBeginLoc.X) * 
						MathF.Pow(MathF.Abs(cursorPos.X - this.camActionBeginLoc.X), 1.25f);
					camObj.Transform.RelativeAngleVel = turnDir;

					this.camTransformChanged = true;
				}
				else if (Math.Abs(camObj.Transform.RelativeAngleVel) > 0.001f)
				{
					camObj.Transform.RelativeAngleVel *= MathF.Pow(0.9f, Time.TimeMult);
					this.camTransformChanged = true;
				}
				else
					camObj.Transform.RelativeAngleVel = 0.0f;
			}
			else
			{
				camObj.Transform.RelativeVel = Vector3.Zero;
				camObj.Transform.RelativeAngleVel = 0.0f;
			}


			if (this.camTransformChanged)
			{
				this.OnCursorSpacePosChanged();

				this.View.UpdateStatusTransformInfo();
				this.View.LocalGLControl.Invalidate();
			}
			
			if (DualityApp.ExecContext == DualityApp.ExecutionContext.Launcher)
			{
				this.UpdateSelectionStats();
				this.View.LocalGLControl.Invalidate();
			}
		}
		protected virtual void OnSceneChanged()
		{
			this.View.LocalGLControl.Invalidate();
		}
		protected virtual void OnCursorSpacePosChanged()
		{
			this.UpdateAction();
		}

		public virtual SelObj PickSelObjAt(int x, int y)
		{
			return null;
		}
		public virtual List<SelObj> PickSelObjIn(int x, int y, int w, int h)
		{
			return new List<SelObj>();
		}
		public virtual void SelectObjects(IEnumerable<SelObj> selObjEnum, MainForm.SelectMode mode = MainForm.SelectMode.Set) {}
		public virtual void ClearSelection() {}
		protected virtual void PostPerformAction(IEnumerable<SelObj> selObjEnum, MouseAction action) {}

		public virtual void DeleteObjects(IEnumerable<SelObj> objEnum) {}
		public virtual List<SelObj> CloneObjects(IEnumerable<SelObj> objEnum) { return new List<SelObj>(); }
		protected bool DisplayConfirmDeleteSelectedObjects()
		{
			if (EditorBasePlugin.Instance.EditorForm.CurrentSandboxState == MainForm.SandboxState.Playing) return true;
			DialogResult result = MessageBox.Show(
				PluginRes.EditorBaseRes.SceneView_MsgBox_ConfirmDeleteSelectedObjects_Text, 
				PluginRes.EditorBaseRes.SceneView_MsgBox_ConfirmDeleteSelectedObjects_Caption, 
				MessageBoxButtons.YesNo, 
				MessageBoxIcon.Question);
			return result == DialogResult.Yes;
		}

		protected void DrawSelectionMarkers(Canvas canvas, IEnumerable<SelObj> obj)
		{
			// Determine turned Camera axes for angle-independent drawing
			Vector2 catDotX, catDotY;
			float camAngle = this.View.CameraObj.Transform.Angle;
			MathF.GetTransformDotVec(camAngle, out catDotX, out catDotY);
			Vector3 right = new Vector3(1.0f, 0.0f, 0.0f);
			Vector3 down = new Vector3(0.0f, 1.0f, 0.0f);
			MathF.TransformDotVec(ref right, ref catDotX, ref catDotY);
			MathF.TransformDotVec(ref down, ref catDotX, ref catDotY);

			foreach (SelObj selObj in obj)
			{
				Vector3 posTemp = selObj.Pos;
				float scaleTemp = 1.0f;
				float radTemp = selObj.BoundRadius;

				if (!canvas.DrawDevice.IsCoordInView(posTemp, radTemp)) continue;

				// Draw selection marker
				if (selObj.ShowPos)
				{
					canvas.DrawDevice.PreprocessCoords(ref posTemp, ref scaleTemp);
					posTemp.Z = 0.0f;
					canvas.DrawDevice.AddVertices(canvas.CurrentState.Material, BeginMode.Lines,
						new VertexP3(posTemp - right * 10.0f),
						new VertexP3(posTemp + right * 10.0f),
						new VertexP3(posTemp - down * 10.0f),
						new VertexP3(posTemp + down * 10.0f));
				}

				// Draw angle marker
				if (selObj.ShowAngle)
				{
					posTemp = selObj.Pos + 
						radTemp * right * MathF.Sin(selObj.Angle - camAngle) - 
						radTemp * down * MathF.Cos(selObj.Angle - camAngle);
					canvas.DrawLine(selObj.Pos.X, selObj.Pos.Y, selObj.Pos.Z - 0.01f, posTemp.X, posTemp.Y, posTemp.Z - 0.01f);
				}

				// Draw boundary
				if (selObj.ShowBoundRadius && radTemp > 0.0f)
					canvas.DrawCircle(selObj.Pos.X, selObj.Pos.Y, selObj.Pos.Z - 0.01f, radTemp);
			}
		}
		protected void DrawLockedAxes(Canvas canvas, float x, float y, float z, float r)
		{
			canvas.PushState();
			if ((this.LockedAxes & AxisLock.X) != AxisLock.None)
			{
				canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, ColorRgba.Red, 0.5f)));
				canvas.DrawLine(x - r, y, z, x + r, y, z);
			}
			if ((this.LockedAxes & AxisLock.Y) != AxisLock.None)
			{
				canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, ColorRgba.Green, 0.5f)));
				canvas.DrawLine(x, y - r, z, x, y + r, z);
			}
			if ((this.LockedAxes & AxisLock.Z) != AxisLock.None)
			{
				canvas.CurrentState.SetMaterial(new BatchInfo(DrawTechnique.Solid, ColorRgba.Mix(this.View.FgColor, ColorRgba.Blue, 0.5f)));
				canvas.DrawLine(x, y, z - r, x, y, z);
				canvas.DrawLine(x, y, z, x, y, z + r);
			}
			canvas.PopState();
		}
		
		protected Vector3 ApplyAxisLock(Vector3 vec, float lockedVal = 0.0f)
		{
			AxisLock lockAxes = this.lockedAxes;
			if (lockAxes == AxisLock.None) return vec;
			if ((lockAxes & AxisLock.X) == AxisLock.None) vec.X = lockedVal;
			if ((lockAxes & AxisLock.Y) == AxisLock.None) vec.Y = lockedVal;
			if ((lockAxes & AxisLock.Z) == AxisLock.None) vec.Z = lockedVal;
			return vec;
		}
		private void UpdateAxisLockInfo()
		{
			this.View.ToolLabelAxisX.Enabled = (this.lockedAxes & AxisLock.X) != AxisLock.None;
			this.View.ToolLabelAxisY.Enabled = (this.lockedAxes & AxisLock.Y) != AxisLock.None;
			this.View.ToolLabelAxisZ.Enabled = (this.lockedAxes & AxisLock.Z) != AxisLock.None;
		}
		
		protected void BeginAction(MouseAction action)
		{
			Point mouseLoc = this.View.LocalGLControl.PointToClient(Cursor.Position);

			this.actionBeginLoc = mouseLoc;
			this.action = action;

			this.View.CameraObj.Transform.Vel = Vector3.Zero;

			if (EditorBasePlugin.Instance.EditorForm.CurrentSandboxState == MainForm.SandboxState.Playing)
				EditorBasePlugin.Instance.EditorForm.SandboxSceneStartFreeze();

			// Begin movement
			if (this.action == MouseAction.MoveObj)
			{
				this.actionBeginLocSpace = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
				this.actionBeginLocSpace.Z = this.View.CameraObj.Transform.Pos.Z;
			}
			// Begin rotation
			else if (this.action == MouseAction.RotateObj)
			{
				this.actionBeginLocSpace = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
			}
			// Begin scale
			else if (this.action == MouseAction.ScaleObj)
			{
				this.actionBeginLocSpace = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
			}
			// Begin rect selection
			else if (this.action == MouseAction.RectSelection)
			{
				this.actionBeginLocSpace = this.View.GetSpaceCoord(new Vector2(mouseLoc.X, mouseLoc.Y));
			}
		}
		protected void EndAction()
		{
			Point mouseLoc = this.View.LocalGLControl.PointToClient(Cursor.Position);

			if (this.action == MouseAction.RectSelection)
			{
				this.activeRectSel = new ObjectSelection();
			}

			if (EditorBasePlugin.Instance.EditorForm.CurrentSandboxState == MainForm.SandboxState.Playing)
				EditorBasePlugin.Instance.EditorForm.SandboxSceneStopFreeze();

			this.action = MouseAction.None;
		}
		protected void UpdateAction()
		{
			Point mouseLoc = this.View.LocalGLControl.PointToClient(Cursor.Position);

			if (this.action == MouseAction.RectSelection)
				this.UpdateRectSelection(mouseLoc);
			else if (this.action == MouseAction.MoveObj)
				this.UpdateObjMove(mouseLoc);
			else if (this.action == MouseAction.RotateObj)
				this.UpdateObjRotate(mouseLoc);
			else if (this.action == MouseAction.ScaleObj)
				this.UpdateObjScale(mouseLoc);
			else
				this.UpdateMouseover(mouseLoc);

			if (this.action != MouseAction.None)
				this.UpdateSelectionStats();
		}
		protected void UpdateSelectionStats()
		{
			this.selectionCenter = Vector3.Zero;
			this.selectionRadius = 0.0f;

			foreach (SelObj s in this.allObjSel)
				this.selectionCenter += s.Pos;
			if (this.allObjSel.Count > 0) this.selectionCenter /= this.allObjSel.Count;

			foreach (SelObj s in this.allObjSel)
				this.selectionRadius = MathF.Max(this.selectionRadius, s.BoundRadius + (s.Pos - this.selectionCenter).Length);
		}
		private void UpdateMouseover(Point mouseLoc)
		{
			bool lastMouseoverSelect = this.mouseoverSelect;
			SelObj lastMouseoverObject = this.mouseoverObject;

			// Determine object at mouse position
			this.mouseoverObject = this.PickSelObjAt(mouseLoc.X, mouseLoc.Y);

			// Determine action variables
			Vector3 mouseSpaceCoord = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
			float scale = this.View.GetScaleAtZ(this.selectionCenter.Z);
			float boundaryThickness = MathF.Max(10.0f, 5.0f / scale);
			bool mouseOverBoundary = MathF.Abs((mouseSpaceCoord - this.selectionCenter).Length - this.selectionRadius) < boundaryThickness;
			bool mouseInsideBoundary = !mouseOverBoundary && (mouseSpaceCoord - this.selectionCenter).Length < this.selectionRadius;
			bool mouseAtCenterAxis = MathF.Abs(mouseSpaceCoord.X - this.selectionCenter.X) < boundaryThickness || MathF.Abs(mouseSpaceCoord.Y - this.selectionCenter.Y) < boundaryThickness;
			bool shift = (Control.ModifierKeys & Keys.Shift) != Keys.None;
			bool ctrl = (Control.ModifierKeys & Keys.Control) != Keys.None;

			bool anySelection = this.actionObjSel.Count > 0;
			bool canMove = this.actionObjSel.Any(s => s.IsActionAvailable(MouseAction.MoveObj));
			bool canRotate = (canMove && this.actionObjSel.Count > 1) || this.actionObjSel.Any(s => s.IsActionAvailable(MouseAction.RotateObj));
			bool canScale = (canMove && this.actionObjSel.Count > 1) || this.actionObjSel.Any(s => s.IsActionAvailable(MouseAction.ScaleObj));

			// Select which action to propose
			this.mouseoverSelect = false;
			if (shift || ctrl)
				this.mouseoverAction = MouseAction.RectSelection;
			else if (anySelection && mouseOverBoundary && mouseAtCenterAxis && this.selectionRadius > 0.0f && canScale)
				this.mouseoverAction = MouseAction.ScaleObj;
			else if (anySelection && mouseOverBoundary && canRotate)
				this.mouseoverAction = MouseAction.RotateObj;
			else if (anySelection && mouseInsideBoundary && canMove)
				this.mouseoverAction = MouseAction.MoveObj;
			else if (this.mouseoverObject != null && this.mouseoverObject.IsActionAvailable(MouseAction.MoveObj))
			{
				this.mouseoverAction = MouseAction.MoveObj; 
				this.mouseoverSelect = true;
			}
			else
				this.mouseoverAction = MouseAction.RectSelection;

			// Adjust mouse cursor based on proposed action
			if (this.mouseoverAction == MouseAction.MoveObj)
				this.View.LocalGLControl.Cursor = CursorHelper.ArrowActionMove;
			else if (this.mouseoverAction == MouseAction.RotateObj)
				this.View.LocalGLControl.Cursor = CursorHelper.ArrowActionRotate;
			else if (this.mouseoverAction == MouseAction.ScaleObj)
				this.View.LocalGLControl.Cursor = CursorHelper.ArrowActionScale;
			else
				this.View.LocalGLControl.Cursor = CursorHelper.Arrow;

			// Redraw if mouseover changed
			if (this.mouseoverObject != lastMouseoverObject || 
				this.mouseoverSelect != lastMouseoverSelect)
				this.View.LocalGLControl.Invalidate();
		}
		private void UpdateRectSelection(Point mouseLoc)
		{
			bool shift = (Control.ModifierKeys & Keys.Shift) != Keys.None;
			bool ctrl = (Control.ModifierKeys & Keys.Control) != Keys.None;

			// Determine picked rect
			int pX = Math.Max(Math.Min(mouseLoc.X, this.actionBeginLoc.X), 0);
			int pY = Math.Max(Math.Min(mouseLoc.Y, this.actionBeginLoc.Y), 0);
			int pX2 = Math.Max(mouseLoc.X, this.actionBeginLoc.X);
			int pY2 = Math.Max(mouseLoc.Y, this.actionBeginLoc.Y);
			int pW = Math.Max(pX2 - pX, 1);
			int pH = Math.Max(pY2 - pY, 1);

			// Check which renderers are picked
			List<SelObj> picked = this.PickSelObjIn(pX, pY, pW, pH);

			// Store in internal rect selection
			ObjectSelection oldRectSel = this.activeRectSel;
			this.activeRectSel = new ObjectSelection(picked);

			// Apply internal selection to actual editor selection
			if (shift || ctrl)
			{
				if (this.activeRectSel.ObjectCount > 0)
				{
					ObjectSelection added = (this.activeRectSel - oldRectSel) + (oldRectSel - this.activeRectSel);
					this.SelectObjects(added.Objects.OfType<SelObj>(), shift ? MainForm.SelectMode.Append : MainForm.SelectMode.Toggle);
				}
			}
			else if (this.activeRectSel.ObjectCount > 0)
				this.SelectObjects(this.activeRectSel.Objects.OfType<SelObj>());
			else
				this.ClearSelection();

			this.View.LocalGLControl.Invalidate();
		}
		private void UpdateObjMove(Point mouseLoc)
		{
			float zMovement = this.View.CameraObj.Transform.Pos.Z - this.actionBeginLocSpace.Z;
			Vector3 spaceCoord = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z + zMovement));
			Vector3 movement = spaceCoord - this.actionBeginLocSpace;
			movement.Z = zMovement;
			movement = this.ApplyAxisLock(movement);
			if (movement != Vector3.Zero)
			{
				foreach (SelObj s in this.actionObjSel) s.Pos += movement;
				this.PostPerformAction(this.actionObjSel, this.action);
			}
			this.UpdateSelectionStats();
			this.actionBeginLocSpace = spaceCoord;
			this.actionBeginLocSpace.Z = this.View.CameraObj.Transform.Pos.Z;
			this.View.LocalGLControl.Invalidate();
		}
		private void UpdateObjRotate(Point mouseLoc)
		{
			Vector3 spaceCoord = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
			float lastAngle = MathF.Angle(this.selectionCenter.X, this.selectionCenter.Y, this.actionBeginLocSpace.X, this.actionBeginLocSpace.Y);
			float curAngle = MathF.Angle(this.selectionCenter.X, this.selectionCenter.Y, spaceCoord.X, spaceCoord.Y);
			float rotation = curAngle - lastAngle;
			if (rotation != 0.0f)
			{
				foreach (SelObj s in this.actionObjSel)
				{
					Vector3 posRelCenter = s.Pos - this.selectionCenter;
					Vector3 posRelCenterTarget = posRelCenter;
					MathF.TransformCoord(ref posRelCenterTarget.X, ref posRelCenterTarget.Y, rotation);
					//posRelCenterTarget = this.LockAxis(posRelCenterTarget, this.actionAxisLock, 1.0f);

					s.Pos = this.selectionCenter + posRelCenterTarget;
					s.Angle += rotation;
				}
				this.PostPerformAction(this.actionObjSel, this.action);
			}
			this.UpdateSelectionStats();
			this.actionBeginLocSpace = spaceCoord;
			this.View.LocalGLControl.Invalidate();
		}
		private void UpdateObjScale(Point mouseLoc)
		{
			if (this.selectionRadius == 0.0f) return;

			Vector3 spaceCoord = this.View.GetSpaceCoord(new Vector3(mouseLoc.X, mouseLoc.Y, this.selectionCenter.Z));
			float lastRadius = this.selectionRadius;
			float curRadius = (this.selectionCenter - spaceCoord).Length;
			float scale = MathF.Clamp(curRadius / lastRadius, 0.0001f, 10000.0f);
			if (scale != 1.0f)
			{
				foreach (SelObj s in this.actionObjSel)
				{
					Vector3 scaleVec = new Vector3(scale, scale, scale);
					//scaleVec = this.LockAxis(scaleVec, this.actionAxisLock, 1.0f);
					Vector3 posRelCenter = s.Pos - this.selectionCenter;
					Vector3 posRelCenterTarget;
					Vector3.Multiply(ref posRelCenter, ref scaleVec, out posRelCenterTarget);

					s.Pos = this.selectionCenter + posRelCenterTarget;
					s.Scale = Vector3.Multiply(s.Scale, scaleVec);
				}
				this.PostPerformAction(this.actionObjSel, this.action);
			}
			this.UpdateSelectionStats();
			this.actionBeginLocSpace = spaceCoord;
			this.View.LocalGLControl.Invalidate();
		}
		
		private void LocalGLControl_Paint(object sender, PaintEventArgs e)
		{
			// Retrieve OpenGL context
 			try { this.View.MainContextControl.Context.MakeCurrent(this.View.LocalGLControl.WindowInfo); } catch (Exception) { return; }
			this.View.MakeDualityTarget();

			try
			{
				this.View.CameraComponent.CollectRendererDrawcalls	+= this.CameraComponent_CollectRendererDrawcalls;
				this.View.CameraComponent.CollectOverlayDrawcalls	+= this.CameraComponent_CollectOverlayDrawcalls;

				this.OnRenderState();

				this.View.CameraComponent.CollectRendererDrawcalls	-= this.CameraComponent_CollectRendererDrawcalls;
				this.View.CameraComponent.CollectOverlayDrawcalls	-= this.CameraComponent_CollectOverlayDrawcalls;
			}
			catch (Exception exception)
			{
				Log.Editor.Write("An error occured during CamView {1} rendering: {0}", Log.Exception(exception), this.View.CameraComponent.ToString());
			}

			this.View.MainContextControl.SwapBuffers();
		}
		private void LocalGLControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (!this.View.AccMovement)
			{
				if (this.camAction == CameraAction.MoveCam)
				{
					Vector3 movVec = new Vector3(
						5.0f * (e.X - this.camActionBeginLoc.X),
						5.0f * (e.Y - this.camActionBeginLoc.Y),
						0.0f);
					MathF.TransformCoord(ref movVec.X, ref movVec.Y, this.View.CameraObj.Transform.RelativeAngle);
					this.View.CameraObj.Transform.RelativePos = this.camActionBeginLocSpace + movVec;
					this.View.LocalGLControl.Invalidate();
					this.View.UpdateStatusTransformInfo();
				}
				else if (this.camAction == CameraAction.TurnCam)
				{
					this.View.CameraObj.Transform.RelativeAngle = MathF.NormalizeAngle(this.camActionBeginLocSpace.X + 0.01f * (e.X - this.camActionBeginLoc.X));
					this.View.LocalGLControl.Invalidate();
					this.View.UpdateStatusTransformInfo();
				}
			}

			this.OnCursorSpacePosChanged();
		}
		private void LocalGLControl_MouseUp(object sender, MouseEventArgs e)
		{
			if (this.action == MouseAction.RectSelection && this.actionBeginLoc == e.Location)
				this.UpdateRectSelection(e.Location);

			if (e.Button == MouseButtons.Left)
				this.EndAction();

			if (this.camAction == CameraAction.MoveCam && e.Button == MouseButtons.Middle)
				this.camAction = CameraAction.None;
			else if (this.camAction == CameraAction.TurnCam && e.Button == MouseButtons.Right)
				this.camAction = CameraAction.None;

			this.View.LocalGLControl.Invalidate();
		}
		private void LocalGLControl_MouseDown(object sender, MouseEventArgs e)
		{
			if (this.action == MouseAction.None)
			{
				if (e.Button == MouseButtons.Left)
				{
					if (this.mouseoverSelect)
					{
						// To interact with an object that isn't selected yet: Select it.
						if (!this.allObjSel.Contains(this.mouseoverObject))
							this.SelectObjects(new [] { this.mouseoverObject });
					}
					this.BeginAction(this.mouseoverAction);
				}
			}

			if (this.camActionAllowed && this.camAction == CameraAction.None)
			{
				this.camActionBeginLoc = e.Location;
				if (e.Button == MouseButtons.Middle)
				{
					this.camAction = CameraAction.MoveCam;
					this.camActionBeginLocSpace = this.View.CameraObj.Transform.RelativePos;
				}
				else if (e.Button == MouseButtons.Right)
				{
					this.camAction = CameraAction.TurnCam;
					this.camActionBeginLocSpace = new Vector3(this.View.CameraObj.Transform.RelativeAngle, 0.0f, 0.0f);
				}
			}
		}
		private void LocalGLControl_MouseWheel(object sender, MouseEventArgs e)
		{
			if (e.Delta != 0)
			{
				if (this.View.ParallaxActive)
				{
					GameObject camObj = this.View.CameraObj;
					if (this.View.AccMovement)
					{
						float curVel = camObj.Transform.RelativeVel.Length * MathF.Sign(camObj.Transform.RelativeVel.Z);
						Vector2 curTemp = new Vector2(
							(e.X * 2.0f / this.View.LocalGLControl.Width) - 1.0f,
							(e.Y * 2.0f / this.View.LocalGLControl.Height) - 1.0f);
						MathF.TransformCoord(ref curTemp.X, ref curTemp.Y, camObj.Transform.RelativeAngle);

						if (MathF.Sign(e.Delta) == MathF.Sign(curVel))
							curVel *= 0.0125f * MathF.Abs(e.Delta);
						curVel += 0.075f * e.Delta;
						curVel = MathF.Sign(curVel) * MathF.Min(MathF.Abs(curVel), 500.0f);

						Vector3 movVec = new Vector3(
							MathF.Sign(e.Delta) * MathF.Sign(curTemp.X) * MathF.Pow(curTemp.X, 2.0f), 
							MathF.Sign(e.Delta) * MathF.Sign(curTemp.Y) * MathF.Pow(curTemp.Y, 2.0f), 
							1.0f);
						movVec.Normalize();
						camObj.Transform.RelativeVel = movVec * curVel;
					}
					else
					{
						camObj.Transform.Pos += new Vector3(0.0f, 0.0f, e.Delta * 5 / 12);
						this.View.LocalGLControl.Invalidate();
						this.View.UpdateStatusTransformInfo();
					}
				}
				else
				{
					this.View.ParallaxRefDist = this.View.ParallaxRefDist + this.View.ParallaxRefDistIncrement * e.Delta / 40;
				}

				this.OnCursorSpacePosChanged();
			}
		}
		private void LocalGLControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				this.DeleteObjects(this.actionObjSel);
			}
			else if (e.KeyCode == Keys.C && (Control.ModifierKeys & Keys.Control) != Keys.None)
			{
				List<SelObj> cloneList = this.CloneObjects(this.actionObjSel);
				this.SelectObjects(cloneList);
			}
			else
			{
				bool axisLockChanged = false;
				if (e.KeyCode == Keys.X) { this.lockedAxes ^= AxisLock.X; axisLockChanged = true; }
				if (e.KeyCode == Keys.Y) { this.lockedAxes ^= AxisLock.Y; axisLockChanged = true; }
				if (e.KeyCode == Keys.Z) { this.lockedAxes ^= AxisLock.Z; axisLockChanged = true; }

				if (axisLockChanged)
				{
					this.UpdateAxisLockInfo();
					this.View.LocalGLControl.Invalidate();
				}
			}
		}
		private void LocalGLControl_LostFocus(object sender, EventArgs e)
		{
			this.camAction = CameraAction.None;
			this.EndAction();
			this.View.LocalGLControl.Invalidate();
		}
		private void View_AccMovementChanged(object sender, EventArgs e)
		{
			Point curPos = this.View.LocalGLControl.PointToClient(Cursor.Position);
			if (this.camAction == CameraAction.MoveCam)
			{
				Vector3 movVec = new Vector3(
					5.0f * (curPos.X - this.camActionBeginLoc.X),
					5.0f * (curPos.Y - this.camActionBeginLoc.Y),
					0.0f);
				MathF.TransformCoord(ref movVec.X, ref movVec.Y, this.View.CameraObj.Transform.RelativeAngle);
				this.camActionBeginLocSpace = this.View.CameraObj.Transform.RelativePos - movVec;
			}
			else if (this.camAction == CameraAction.TurnCam)
			{
				this.camActionBeginLocSpace = new Vector3(this.View.CameraObj.Transform.RelativeAngle - 0.01f * (curPos.X - this.camActionBeginLoc.X), 0.0f, 0.0f);
			}
		}
		private void View_ParallaxRefDistChanged(object sender, EventArgs e)
		{
			this.OnCursorSpacePosChanged();
		}
		private void EditorForm_AfterUpdateDualityApp(object sender, EventArgs e)
		{
			this.OnUpdateState();
		}
		private void Scene_Changed(object sender, EventArgs e)
		{
			this.OnSceneChanged();
		}
		private void CameraComponent_CollectOverlayDrawcalls(object sender, EventArgs e)
		{
			this.OnCollectStateOverlayDrawcalls(new Canvas(this.View.CameraComponent.DrawDevice));
		}
		private void CameraComponent_CollectRendererDrawcalls(object sender, EventArgs e)
		{
			this.OnCollectStateDrawcalls(new Canvas(this.View.CameraComponent.DrawDevice));
		}
	}
}