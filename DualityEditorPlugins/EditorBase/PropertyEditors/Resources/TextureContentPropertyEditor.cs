﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using AdamsLair.PropertyGrid;
using AdamsLair.PropertyGrid.PropertyEditors;

using Duality;
using Duality.Resources;
using DualityEditor.Controls.PropertyEditors;

namespace EditorBase.PropertyEditors
{
	public class TextureContentPropertyEditor : ResourcePropertyEditor
	{
		public TextureContentPropertyEditor()
		{
			this.Hints = HintFlags.None;
			this.HeaderHeight = 0;
			this.HeaderValueText = null;
			this.Expanded = true;
		}

		protected override void OnPropertySet(PropertyInfo property, IEnumerable<object> targets)
		{
			base.OnPropertySet(property, targets);
			Texture[] texArr = targets.Cast<Texture>().ToArray();
			bool anyReload = false;
			foreach (Texture tex in texArr)
			{
				if (tex.NeedsReload) 
				{
					tex.ReloadData();
					anyReload = true;
				}
			}

			if (anyReload)
			{
				this.PerformGetValue();
				(this.ParentEditor as TexturePropertyEditor).UpdatePreview();
			}
		}
	}
}
