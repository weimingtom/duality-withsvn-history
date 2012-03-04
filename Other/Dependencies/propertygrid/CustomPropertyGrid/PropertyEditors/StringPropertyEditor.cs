﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using CustomPropertyGrid.Renderer;
using CustomPropertyGrid.EditorTemplates;

namespace CustomPropertyGrid.PropertyEditors
{
	public class StringPropertyEditor : PropertyEditor
	{
		private	StringEditorTemplate	stringEditor	= new StringEditorTemplate();
		private string	val				= null;
		private	bool	valMultiple		= false;

		public override object DisplayedValue
		{
			get { return Convert.ChangeType(this.val, this.EditedType); }
		}
		

		public StringPropertyEditor()
		{
			this.stringEditor.Invalidate += this.stringEditor_Invalidate;
			this.stringEditor.TextEdited += this.stringEditor_TextEdited;
			this.stringEditor.EditingFinished += this.stringEditor_EditingFinished;

			this.Height = 18;
		}

		public override void PerformGetValue()
		{
			base.PerformGetValue();
			this.BeginUpdate();
			object[] values = this.GetValue().ToArray();

			// Apply values to editors
			if (!values.Any())
				this.val = null;
			else
			{
				this.val = (string)values.First();
				this.valMultiple = values.Any(o => o == null || (string)o != this.val);
			}

			this.stringEditor.Text = this.val;
			this.EndUpdate();
		}

		protected internal override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			this.stringEditor.OnPaint(e, this.Enabled, this.valMultiple);
		}
		protected internal override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			this.stringEditor.OnGotFocus(e);
			this.stringEditor.Select();
		}
		protected internal override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			this.stringEditor.OnLostFocus(e);
		}
		protected internal override void OnKeyPress(KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			this.stringEditor.OnKeyPress(e);
		}
		protected internal override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			this.stringEditor.OnKeyDown(e);
		}
		protected internal override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			this.stringEditor.OnMouseMove(e);
		}
		protected internal override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			this.stringEditor.OnMouseLeave(e);
		}
		protected internal override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			this.stringEditor.OnMouseDown(e);
		}
		protected internal override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			this.stringEditor.OnMouseUp(e);
		}

		protected override void UpdateGeometry()
		{
			base.UpdateGeometry();
			this.stringEditor.Rect = new Rectangle(
				this.ClientRectangle.X + 1,
				this.ClientRectangle.Y + 1,
				this.ClientRectangle.Width - 2,
				this.ClientRectangle.Height - 1);
		}
		protected internal override void OnReadOnlyChanged()
		{
			base.OnReadOnlyChanged();
			this.stringEditor.ReadOnly = this.ReadOnly;
		}

		private void stringEditor_Invalidate(object sender, EventArgs e)
		{
			this.Invalidate();
		}
		private void stringEditor_TextEdited(object sender, EventArgs e)
		{
			this.val = this.stringEditor.Text;
			this.Invalidate();
			this.PerformSetValue();
			this.OnValueChanged();
			this.PerformGetValue();
		}
		private void stringEditor_EditingFinished(object sender, EventArgs e)
		{
			this.OnEditingFinished();
		}
	}
}