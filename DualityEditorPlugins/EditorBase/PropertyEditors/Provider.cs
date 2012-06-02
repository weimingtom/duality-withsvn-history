﻿using System;
using AdamsLair.PropertyGrid;

using Duality;
using Duality.Resources;
using Duality.Components;
using Duality.Components.Renderers;

namespace EditorBase.PropertyEditors
{
	public class PropertyEditorProvider : IPropertyEditorProvider
	{
		public int IsResponsibleFor(Type baseType)
		{
			// -------- Specialized area --------
			if (baseType == typeof(GameObject))			return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Transform))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Camera.Pass))	return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(SoundEmitter))	return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(BatchInfo))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Material))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Texture))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(AudioData))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Pixmap))		return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(Font))			return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(FormattedText))	return PropertyGrid.EditorPriority_Specialized;
			else if (baseType == typeof(TextRenderer))	return PropertyGrid.EditorPriority_Specialized;

			//// -------- Semi-Specialized area --------
			else if (typeof(Collider).IsAssignableFrom(baseType))			return PropertyGrid.EditorPriority_General + 1;
			else if (typeof(Renderer).IsAssignableFrom(baseType))			return PropertyGrid.EditorPriority_General + 1;
			else if (typeof(DrawTechnique).IsAssignableFrom(baseType))		return PropertyGrid.EditorPriority_General + 1;

			// -------- General area --------
			else if (typeof(Collider.ShapeInfo).IsAssignableFrom(baseType))	return PropertyGrid.EditorPriority_General;
#if FALSE // Removed for now. Joints are an experimental feature.
			else if (typeof(Collider.JointInfo).IsAssignableFrom(baseType))	return PropertyGrid.EditorPriority_General;
#endif
			else if (typeof(Component).IsAssignableFrom(baseType))			return PropertyGrid.EditorPriority_General;
			else if (typeof(Resource).IsAssignableFrom(baseType))			return PropertyGrid.EditorPriority_General;
			else if (typeof(IContentRef).IsAssignableFrom(baseType))		return PropertyGrid.EditorPriority_General;
			else if (typeof(DualityAppData).IsAssignableFrom(baseType))		return PropertyGrid.EditorPriority_General;
			else if (typeof(DualityUserData).IsAssignableFrom(baseType))	return PropertyGrid.EditorPriority_General;
			
			else return PropertyGrid.EditorPriority_None;
		}
		public PropertyEditor CreateEditor(Type baseType)
		{
			PropertyEditor e = null;

			// -------- Specialized area --------
			if (baseType == typeof(GameObject))			e = new GameObjectOverviewPropertyEditor();
			else if (baseType == typeof(Transform))		e = new TransformPropertyEditor();
			else if (baseType == typeof(Camera.Pass))	e = new CameraRenderPassPropertyEditor();
			else if (baseType == typeof(SoundEmitter))	e = new SoundEmitterPropertyEditor();
			else if (baseType == typeof(BatchInfo))		e = new BatchInfoPropertyEditor();
			else if (baseType == typeof(Material))		e = new MaterialPropertyEditor();
			else if (baseType == typeof(Texture))		e = new TexturePropertyEditor();
			else if (baseType == typeof(AudioData))		e = new AudioDataPropertyEditor();
			else if (baseType == typeof(Pixmap))		e = new PixmapPropertyEditor();
			else if (baseType == typeof(Font))			e = new FontPropertyEditor();
			else if (baseType == typeof(FormattedText))	e = new FormattedTextPropertyEditor();
			else if (baseType == typeof(TextRenderer))	e = new TextRendererPropertyEditor();

			//// -------- Semi-Specialized area --------
			else if (typeof(Collider).IsAssignableFrom(baseType))			e = new ColliderPropertyEditor();
			else if (typeof(Renderer).IsAssignableFrom(baseType))			e = new RendererPropertyEditor();
			else if (typeof(DrawTechnique).IsAssignableFrom(baseType))		e = new DrawTechniquePropertyEditor();

			// -------- General area --------
			else if (typeof(Collider.ShapeInfo).IsAssignableFrom(baseType))	e = new ColliderShapePropertyEditor();
#if FALSE // Removed for now. Joints are an experimental feature.
			else if (typeof(Collider.JointInfo).IsAssignableFrom(baseType))	e = new ColliderJointPropertyEditor();
#endif
			else if (typeof(Component).IsAssignableFrom(baseType))			e = new ComponentPropertyEditor();
			else if (typeof(Resource).IsAssignableFrom(baseType))			e = new ResourcePropertyEditor();
			else if (typeof(IContentRef).IsAssignableFrom(baseType))		e = new IContentRefPropertyEditor();
			else if (typeof(DualityAppData).IsAssignableFrom(baseType))		e = new DualityAppDataPropertyEditor();
			else if (typeof(DualityUserData).IsAssignableFrom(baseType))	e = new DualityUserDataPropertyEditor();

			return e;
		}
	}
}
