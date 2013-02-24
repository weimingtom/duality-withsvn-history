﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

using AdamsLair.PropertyGrid;

using Duality;
using Duality.ColorFormat;

using DualityEditor;
using DualityEditor.CorePluginInterface;

namespace EditorBase.PropertyEditors
{
	public class ComponentPropertyEditor : MemberwisePropertyEditor
	{
		public ComponentPropertyEditor()
		{
			this.Hints |= HintFlags.HasActiveCheck | HintFlags.ActiveEnabled;
			this.PropertyName = "Component";
			this.HeaderHeight = 20;
			this.HeaderStyle = AdamsLair.PropertyGrid.Renderer.GroupHeaderStyle.Emboss;
		}

		public void PerformSetActive(bool active)
		{
			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();
			foreach (Component c in values) c.ActiveSingle = active;

			// Notify ActiveSingle changed
			DualityEditorApp.NotifyObjPropChanged(this, 
				new ObjectSelection(values), 
				ReflectionInfo.Property_Component_ActiveSingle);
		}

		protected override bool IsAutoCreateMember(MemberInfo info)
		{
			return base.IsAutoCreateMember(info) && info.DeclaringType != typeof(Component);
		}
		protected override bool IsChildValueModified(PropertyEditor childEditor)
		{
			MemberInfo info = childEditor.EditedMember;
			if (info == null) return false;

			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();
			return values.Any(delegate (Component c)
			{
				Duality.Resources.PrefabLink l = c.GameObj.AffectedByPrefabLink;
				return l != null && l.HasChange(c, info as PropertyInfo);
			});
		}
		protected override bool IsChildNonPublic(PropertyEditor childEditor)
		{
			if (base.IsChildNonPublic(childEditor)) return true;
			if (childEditor.EditedMember is FieldInfo) return true; // Discourage use of fields in Components
			return false;
		}
		protected override void OnUpdateFromObjects(object[] values)
		{
			base.OnUpdateFromObjects(values);

			this.Hints |= HintFlags.HasButton | HintFlags.ButtonEnabled;
			this.ButtonIcon = PluginRes.EditorBaseRes.DropdownSettingsBlack;
			this.PropertyName = this.EditedType.GetTypeCSCodeName(true);
			this.HeaderValueText = null;
			if (!values.Any() || values.All(o => o == null))
				this.Active = false;
			else
				this.Active = (values.First(o => o is Component) as Component).ActiveSingle;
		}
		protected override void OnValueChanged(object sender, PropertyEditorValueEventArgs args)
		{
			base.OnValueChanged(sender, args);

			// Find the direct descendant editor on the path to the changed one
			PropertyEditor directChild = args.Editor;
			while (directChild != null && !this.HasPropertyEditor(directChild))
				directChild = directChild.ParentEditor;

			// If an editor has changed that was NOT a direct descendant, invoke its setter to trigger OnPropertySet / OnFieldSet.
			// Always remember: If we don't emit a PropertyChanged event, PrefabLinks won't update their change lists!
			if (directChild != null && directChild != args.Editor && directChild.EditedMember != null)
			{
			//	Is all this really wants to do fire all the PropertyChanged events? Why not do that directly here?
			//	Console.WriteLine("OnValueChanged: {0}", directChild.PropertyName);
				directChild.PerformSetValue();
			}
		}
		protected override void OnPropertySet(PropertyInfo property, IEnumerable<object> targets)
		{
			base.OnPropertySet(property, targets);
			DualityEditorApp.NotifyObjPropChanged(this.ParentGrid, new DualityEditor.ObjectSelection(targets), property);
		}
		protected override void OnFieldSet(FieldInfo field, IEnumerable<object> targets)
		{
			base.OnFieldSet(field, targets);
			// This is a bad workaround for having a purely Property-based change event system: Simply notify "something" changed.
			// Change to something better in the future.
			DualityEditorApp.NotifyObjPropChanged(this.ParentGrid, new DualityEditor.ObjectSelection(targets));
		}
		protected override void OnActiveChanged()
		{
			base.OnActiveChanged();
			if (!this.IsUpdatingFromObject) this.PerformSetActive(this.Active);
		}
		protected override void OnEditedTypeChanged()
		{
			base.OnEditedTypeChanged();

			System.Drawing.Bitmap iconBitmap = CorePluginRegistry.RequestTypeImage(this.EditedType) as System.Drawing.Bitmap;
			ColorHsva avgClr = iconBitmap != null ? 
				iconBitmap.GetAverageColor().ToHsva() : 
				Duality.ColorFormat.ColorHsva.TransparentWhite;
			if (avgClr.S <= 0.05f)
			{
				avgClr = new ColorHsva(
					0.001f * (float)(this.EditedType.Name.GetHashCode() % 1000), 
					1.0f, 
					0.5f);
			}

			this.PropertyName = this.EditedType.GetTypeCSCodeName(true);
			this.HeaderIcon = iconBitmap;
			this.HeaderColor = ExtMethodsSystemDrawingColor.ColorFromHSV(avgClr.H, 0.2f, 0.8f);
		}

		protected override void OnButtonPressed()
		{
			//base.OnButtonPressed(); // We don't want the base implementation

			// Is it safe to remove this Component?
			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();
			bool canRemove = true;
			Component removeConflict = null;
			foreach (Component c in values)
			{
				foreach (Component r in c.GameObj.GetComponents<Component>())
				{
					if (!r.IsComponentRequirementMet(c))
					{
						canRemove = false;
						removeConflict = r;
						break;
					}
				}
				if (!canRemove) break;
			}

			// Create a ContextMenu
			ContextMenuStrip contextMenu = new ContextMenuStrip();
			Point menuPos = new Point(this.ButtonRectangle.Right, this.ButtonRectangle.Bottom);
			Point thisLoc = this.ParentEditor.GetChildLocation(this);
			menuPos.X += thisLoc.X;
			menuPos.Y += thisLoc.Y;

			// Default items
			ToolStripItem itemReset = contextMenu.Items.Add(PluginRes.EditorBaseRes.MenuItemName_ResetComponent, null, this.contextMenu_ResetComponent);
			ToolStripItem itemRemove = contextMenu.Items.Add(PluginRes.EditorBaseRes.MenuItemName_RemoveComponent, Properties.Resources.cross, this.contextMenu_RemoveComponent);
			itemRemove.Enabled = canRemove;
			if (!canRemove) 
			{
				itemRemove.ToolTipText = string.Format(
					PluginRes.EditorBaseRes.MenuItemDesc_CantRemoveComponent, 
					values.First().GetType().Name, 
					removeConflict.GetType().Name);
			}
			ToolStripSeparator itemDefaultSep = new ToolStripSeparator();
			contextMenu.Items.Add(itemDefaultSep);

			// Custom actions
			var customActions = CorePluginRegistry.RequestEditorActions(
				values.First().GetType(), 
				CorePluginRegistry.ActionContext_ContextMenu, 
				values)
				.ToArray();
			foreach (var actionEntry in customActions)
			{
				ToolStripMenuItem actionItem = new ToolStripMenuItem(actionEntry.Name, actionEntry.Icon);
				actionItem.Click += this.contextMenu_CustomAction;
				actionItem.Tag = actionEntry;
				actionItem.ToolTipText = actionEntry.Description;
				contextMenu.Items.Add(actionItem);
			}
			if (customActions.Length == 0) itemDefaultSep.Visible = false;

			contextMenu.Closed += this.contextMenu_Closed;
			contextMenu.Show(this.ParentGrid, menuPos, ToolStripDropDownDirection.BelowLeft);
		}
		private void contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			this.ParentGrid.Focus();
		}
		private void contextMenu_ResetComponent(object sender, EventArgs e)
		{
			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();
			foreach (Component c in values)
			{
				Type cmpType = c.GetType();
				Duality.Resources.PrefabLink l = c.GameObj.AffectedByPrefabLink;
				if (l != null)
				{
					if (l.Prefab.IsAvailable) l.Prefab.Res.CopyTo(l.Obj.IndexPathOfChild(c.GameObj), c);
					l.ClearChanges(c.GameObj, cmpType, null);
				}
				else
				{
					Component resetBase = (cmpType.CreateInstanceOf() ?? cmpType.CreateInstanceOf(true)) as Component;
					resetBase.CopyTo(c);
				}
			}
			this.PerformGetValue();
			DualityEditorApp.NotifyObjPropChanged(this, new ObjectSelection(values));
		}
		private void contextMenu_RemoveComponent(object sender, EventArgs e)
		{
			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();

			// Ask user if he really wants to delete stuff
			ObjectSelection objSel = new ObjectSelection(values);
			if (!DualityEditorApp.DisplayConfirmDeleteObjects(objSel)) return;
			if (!DualityEditorApp.DisplayConfirmBreakPrefabLink(objSel)) return;

			// Delete Components
			foreach (Component c in values)
			{
				if (c.Disposed) continue;
				c.Dispose();
			}

			this.ParentEditor.PerformGetValue();
			DualityEditorApp.NotifyObjPropChanged(this, new ObjectSelection(values.GameObject()));
		}
		private void contextMenu_CustomAction(object sender, EventArgs e)
		{
			Component[] values = this.GetValue().Cast<Component>().NotNull().ToArray();
			ToolStripMenuItem clickedItem = sender as ToolStripMenuItem;
			IEditorAction action = clickedItem.Tag as IEditorAction;
			action.Perform(values);
		}
	}
}
