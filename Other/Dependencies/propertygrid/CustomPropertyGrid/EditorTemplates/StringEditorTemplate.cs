﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using CustomPropertyGrid.Renderer;

namespace CustomPropertyGrid.EditorTemplates
{
	public class StringEditorTemplate
	{
		private	Rectangle	rect			= Rectangle.Empty;
		private	string		text			= null;
		private	bool		readOnly		= false;
		private	bool		hovered			= false;
		private	bool		focused			= false;
		private	Timer		cursorTimer		= null;
		private	bool		cursorVisible	= false;
		private	int			cursorIndex		= 0;
		private	int			selectionLength	= 0;
		private int			scroll			= 0;
		private	bool		mouseSelect		= false;

		public event EventHandler Invalidate = null;
		public event EventHandler TextEdited = null;

		public Rectangle Rect
		{
			get { return this.rect; }
			set { this.rect = value; }
		}
		public bool ReadOnly
		{
			get { return this.readOnly; }
			set { this.readOnly = value; }
		}
		public string Text
		{
			get { return this.text; }
			set { this.text = value; }
		}
		public string SelectedText
		{
			get
			{
				if (this.text == null) return null;
				int begin = Math.Min(this.cursorIndex, this.cursorIndex + this.selectionLength);
				return this.text.Substring(begin, Math.Abs(this.selectionLength));
			}
		}

		public void Select(int pos = 0, int length = -1)
		{
			if (pos == 0 && length == -1) length = this.text != null ? this.text.Length : 0;
			this.cursorIndex = pos + length;
			this.selectionLength = -length;

			this.UpdateScroll();
			this.EmitInvalidate();
		}
		public void Deselect()
		{
			this.selectionLength = 0;

			this.EmitInvalidate();
		}
		public void DeleteSelection()
		{
			if (this.selectionLength == 0) return;
			if (this.text == null) return;
			if (this.readOnly) return;

			int begin = Math.Min(this.cursorIndex, this.cursorIndex + this.selectionLength);
			this.text = this.text.Remove(begin, Math.Abs(this.selectionLength));
			this.selectionLength = 0;
			this.cursorIndex = begin;

			this.UpdateScroll();
			this.EmitTextEdited();
		}
		public void InsertText(string insertText)
		{
			if (insertText == null) return;
			if (this.readOnly) return;

			if (this.text == null)
			{
				this.text = insertText;
				this.cursorIndex = 0;
			}
			else
			{
				int begin = Math.Min(this.cursorIndex, this.cursorIndex + this.selectionLength);
				this.text = 
					this.text.Substring(0, begin) + 
					insertText + 
					this.text.Substring(begin + Math.Abs(this.selectionLength), this.text.Length - begin - Math.Abs(this.selectionLength));
				this.cursorIndex = begin;
			}

			this.cursorIndex += insertText.Length;
			this.selectionLength = 0;

			this.UpdateScroll();
			this.EmitTextEdited();
		}
		public void ShowCursor()
		{
			this.cursorTimer.Stop();
			this.cursorTimer.Start();
			this.cursorVisible = true;
			this.EmitInvalidate();
		}
		public void UpdateScroll()
		{
			int cursorPixelPos = ControlRenderer.GetCharPosTextField(
				this.rect,
				this.text,
				SystemFonts.DefaultFont,
				TextBoxStyle.Sunken,
				this.cursorIndex,
				this.scroll);
			if (cursorPixelPos - this.rect.X < 15 && this.scroll > 0)
			{
				this.scroll = Math.Max(this.scroll - (15 - cursorPixelPos + this.rect.X), 0);
				this.EmitInvalidate();
			}
			else if (cursorPixelPos - this.rect.X > this.rect.Width - 15)
			{
				this.scroll += (cursorPixelPos - this.rect.X) - (this.rect.Width - 15);
				this.EmitInvalidate();
			}
		}

		public void OnPaint(PaintEventArgs e, bool enabled, bool multiple)
		{
			TextBoxState textBoxState;

			if (!enabled)
				textBoxState = TextBoxState.Disabled;
			else if (this.focused)
				textBoxState = TextBoxState.Focus;
			else if (this.hovered)
				textBoxState = TextBoxState.Hot;
			else
				textBoxState = TextBoxState.Normal;

			if (this.readOnly)
				textBoxState |= TextBoxState.ReadOnlyFlag;

			ControlRenderer.DrawTextField(
				e.Graphics, 
				rect, 
				text, 
				SystemFonts.DefaultFont, 
				SystemColors.ControlText, 
				multiple ? Color.Bisque : SystemColors.Window,
				textBoxState, 
				TextBoxStyle.Sunken,
				this.scroll,
				(this.selectionLength != 0 || this.cursorVisible) ? this.cursorIndex : -1,
				this.selectionLength);
		}

		public void OnGotFocus(EventArgs e)
		{
			this.focused = true;
			if (this.cursorTimer == null)
			{
				this.cursorTimer = new Timer();
				this.cursorTimer.Interval = 500;
				this.cursorTimer.Tick += this.cursorTimer_Tick;
				this.cursorTimer.Enabled = true;
				this.cursorVisible = true;
			}
		}
		public void OnLostFocus(EventArgs e)
		{
			this.focused = false;
			if (this.cursorTimer != null)
			{
				this.cursorTimer.Tick -= this.cursorTimer_Tick;
				this.cursorTimer.Dispose();
				this.cursorTimer = null;
				this.cursorVisible = false;
			}
			this.scroll = 0;
		}
		public void OnKeyPress(KeyPressEventArgs e)
		{
			if (char.IsControl(e.KeyChar)) return;
			this.InsertText(e.KeyChar.ToString());
			e.Handled = true;
		}
		public void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				if (!this.readOnly && this.selectionLength == 0 && this.text != null && this.cursorIndex < this.text.Length)
				{
					this.text = this.text.Remove(this.cursorIndex, 1);
					this.UpdateScroll();
					this.EmitTextEdited();
				}
				else
					this.DeleteSelection();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Back)
			{
				if (!this.readOnly && this.selectionLength == 0 && this.text != null && this.cursorIndex > 0)
				{
					this.text = this.text.Remove(this.cursorIndex - 1, 1);
					this.cursorIndex--;
					this.UpdateScroll();
					this.EmitTextEdited();
				}
				else
					this.DeleteSelection();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Left)
			{
				if (e.Shift)
				{
					if (this.cursorIndex > 0)
					{
						this.cursorIndex--;
						this.selectionLength = Math.Min(this.selectionLength + 1, this.text != null ? this.text.Length : 0);
						this.UpdateScroll();
						this.ShowCursor();
					}
				}
				else
				{
					if (this.selectionLength < 0) this.cursorIndex += this.selectionLength;
					this.cursorIndex = Math.Max(this.cursorIndex - 1, 0);
					this.selectionLength = 0;
					this.UpdateScroll();
					this.ShowCursor();
				}
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Right)
			{
				if (e.Shift)
				{
					if (this.cursorIndex < (this.text != null ? this.text.Length : 0))
					{
						this.cursorIndex++;
						this.selectionLength = Math.Max(this.selectionLength - 1, -this.cursorIndex);
						this.UpdateScroll();
						this.ShowCursor();
					}
				}
				else
				{
					if (this.selectionLength > 0) this.cursorIndex += this.selectionLength;
					this.cursorIndex = Math.Min(this.cursorIndex + 1, this.text != null ? this.text.Length : 0);
					this.selectionLength = 0;
					this.UpdateScroll();
					this.ShowCursor();
				}
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.End)
			{
				if (e.Shift)
				{
					int oldSelEnd = this.cursorIndex + this.selectionLength;
					this.cursorIndex = (this.text != null ? this.text.Length : 0);
					this.selectionLength = oldSelEnd - this.cursorIndex;
					this.UpdateScroll();
					this.ShowCursor();
				}
				else
				{
					this.cursorIndex = (this.text != null ? this.text.Length : 0);
					this.selectionLength = 0;
					this.UpdateScroll();
					this.ShowCursor();
				}
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Home)
			{
				if (e.Shift)
				{
					int oldSelEnd = this.cursorIndex + this.selectionLength;
					this.cursorIndex = 0;
					this.selectionLength = oldSelEnd - this.cursorIndex;
					this.UpdateScroll();
					this.ShowCursor();
				}
				else
				{
					this.cursorIndex = 0;
					this.selectionLength = 0;
					this.UpdateScroll();
					this.ShowCursor();
				}
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.A && e.Control)
			{
				this.Select();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.C && e.Control)
			{
				Clipboard.SetText(this.SelectedText ?? "");
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.X && e.Control)
			{
				Clipboard.SetText(this.SelectedText ?? "");
				this.DeleteSelection();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.V && e.Control)
			{
				string newText = Clipboard.GetText();
				this.InsertText(newText);
				e.Handled = true;
			}
		}
		public void OnMouseDown(MouseEventArgs e)
		{
			if (!this.rect.Contains(e.Location)) return;
			Cursor.Current = Cursors.IBeam;

			// Pick char
			int pickedCharIndex;
			Point pickLoc = new Point(Math.Min(Math.Max(e.X, rect.X + 2), rect.Right - 2), rect.Y + rect.Height / 2);
			pickedCharIndex = ControlRenderer.PickCharTextField( 
				this.rect, 
				this.text,
				SystemFonts.DefaultFont,
				TextBoxStyle.Sunken,
				pickLoc,
				this.scroll);
			if (pickedCharIndex == -1) pickedCharIndex = this.text != null ? this.text.Length : 0;

			this.cursorIndex = pickedCharIndex;
			this.selectionLength = 0;
			this.UpdateScroll();
			this.EmitInvalidate();

			this.mouseSelect = true;
		}
		public void OnMouseUp(MouseEventArgs e)
		{
			Cursor.Current = Cursors.IBeam;
			this.mouseSelect = false;
		}
		public void OnMouseMove(MouseEventArgs e)
		{
			bool lastHovered = this.hovered;
			this.hovered = this.rect.Contains(e.Location);
			if (lastHovered != this.hovered) this.EmitInvalidate();

			Cursor.Current = (this.hovered || this.mouseSelect) ? Cursors.IBeam : Cursors.Default;
			if (this.mouseSelect)
			{
				// Pick char
				int pickedCharIndex;
				Point pickLoc = new Point(Math.Min(Math.Max(e.X, rect.X + 2), rect.Right - 2), rect.Y + rect.Height / 2);
				pickedCharIndex = ControlRenderer.PickCharTextField(
					this.rect, 
					this.text,
					SystemFonts.DefaultFont,
					TextBoxStyle.Sunken,
					pickLoc,
					this.scroll);
				if (pickedCharIndex == -1) pickedCharIndex = this.text != null ? this.text.Length : 0;

				this.selectionLength = (this.cursorIndex + this.selectionLength) - pickedCharIndex;
				this.cursorIndex = pickedCharIndex;
				this.UpdateScroll();
				this.EmitInvalidate();
			}
		}
		public void OnMouseLeave(EventArgs e)
		{
			if (this.hovered) this.EmitInvalidate();
			this.hovered = false;

			Cursor.Current = (this.hovered || this.mouseSelect) ? Cursors.IBeam : Cursors.Default;
		}

		private void cursorTimer_Tick(object sender, EventArgs e)
		{
			if (this.selectionLength > 0) return;
			this.EmitInvalidate();
			this.cursorVisible = !this.cursorVisible;
		}
		protected void EmitInvalidate()
		{
			if (this.Invalidate != null)
				this.Invalidate(this, EventArgs.Empty);
		}
		protected void EmitTextEdited()
		{
			if (this.TextEdited != null)
				this.TextEdited(this, EventArgs.Empty);
		}
	}
}
