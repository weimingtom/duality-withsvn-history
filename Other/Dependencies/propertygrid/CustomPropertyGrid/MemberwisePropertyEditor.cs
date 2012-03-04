﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace CustomPropertyGrid
{
	public class MemberwisePropertyEditor : GroupedPropertyEditor
	{
		private	bool	buttonIsCreate	= false;
		private	Predicate<MemberInfo>			memberPredicate		= null;
		private	Predicate<MemberInfo>			memberAffectsOthers	= null;
		private	Func<MemberInfo,PropertyEditor>	memberEditorCreator	= null;

		public override object DisplayedValue
		{
			get { return this.GetValue().FirstOrDefault(); }
		}
		public Predicate<MemberInfo> MemberPredicate
		{
			get { return this.memberPredicate; }
			set
			{
				if (value == null) value = this.DefaultMemberPredicate;
				if (this.memberPredicate != value)
				{
					this.memberPredicate = value;
					if (this.ContentInitialized) this.InitContent();
				}
			}
		}
		public Predicate<MemberInfo> MemberAffectsOthers
		{
			get { return this.memberAffectsOthers; }
			set
			{
				if (value == null) value = this.DefaultMemberAffectsOthers;
				if (this.memberAffectsOthers != value)
				{
					this.memberAffectsOthers = value;
					if (this.ContentInitialized) this.InitContent();
				}
			}
		}
		public Func<MemberInfo,PropertyEditor> MemberEditorCreator
		{
			get { return this.memberEditorCreator; }
			set
			{
				if (value == null) value = this.DefaultMemberEditorCreator;
				if (this.memberEditorCreator != value)
				{
					this.memberEditorCreator = value;
					if (this.ContentInitialized) this.InitContent();
				}
			}
		}


		public MemberwisePropertyEditor()
		{
			this.Hints |= HintFlags.HasButton | HintFlags.ButtonEnabled;
			this.memberEditorCreator = this.DefaultMemberEditorCreator;
			this.memberAffectsOthers = this.DefaultMemberAffectsOthers;
			this.memberPredicate = this.DefaultMemberPredicate;
		}

		public override void InitContent()
		{
			this.ClearContent();
			if (this.EditedType != null)
			{
				base.InitContent();

				// Generate and add property editors for the current type
				this.BeginUpdate();
				// Properties
				{
					PropertyInfo[] propArr = this.EditedType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
					var propQuery = 
						from p in propArr
						where p.CanRead && p.GetIndexParameters().Length == 0 && this.memberPredicate(p)
						orderby GetTypeHierarchyLevel(p.DeclaringType) ascending, p.Name
						select p;
					foreach (PropertyInfo prop in propQuery)
					{
						this.AddEditorForProperty(prop);
					}
				}
				// Fields
				{
					FieldInfo[] fieldArr = this.EditedType.GetFields(BindingFlags.Instance | BindingFlags.Public);
					var fieldQuery =
						from f in fieldArr
						where this.memberPredicate(f)
						orderby GetTypeHierarchyLevel(f.DeclaringType) ascending, f.Name
						select f;
					foreach (FieldInfo field in fieldQuery)
					{
						this.AddEditorForField(field);
					}
				}
				this.EndUpdate();
				this.PerformGetValue();
			}
		}

		public PropertyEditor AddEditorForProperty(PropertyInfo prop)
		{
			PropertyEditor e = this.memberEditorCreator(prop);
			if (e == null) e = this.ParentGrid.CreateEditor(prop.PropertyType);
			if (e == null) return null;
			e.Getter = this.CreatePropertyValueGetter(prop);
			e.Setter = prop.CanWrite ? this.CreatePropertyValueSetter(prop) : null;
			e.PropertyName = prop.Name;
			e.EditedMember = prop;
			this.ParentGrid.ConfigureEditor(e);
			this.AddPropertyEditor(e);
			return e;
		}
		public PropertyEditor AddEditorForField(FieldInfo field)
		{
			PropertyEditor e = this.memberEditorCreator(field);
			if (e == null) e = this.ParentGrid.CreateEditor(field.FieldType);
			if (e == null) return null;
			e.Getter = this.CreateFieldValueGetter(field);
			e.Setter = this.CreateFieldValueSetter(field);
			e.PropertyName = field.Name;
			e.EditedMember = field;
			this.ParentGrid.ConfigureEditor(e);
			this.AddPropertyEditor(e);
			return e;
		}

		public override void PerformGetValue()
		{
			base.PerformGetValue();
			object[] curObjects = this.GetValue().ToArray();

			if (curObjects == null)
			{
				this.HeaderValueText = null;
				return;
			}

			this.OnUpdateFromObjects(curObjects);

			foreach (PropertyEditor e in this.Children)
				e.PerformGetValue();
		}
		public override void PerformSetValue()
		{
			if (this.ReadOnly) return;
			if (!this.Children.Any()) return;
			base.PerformSetValue();

			foreach (PropertyEditor e in this.Children)
				e.PerformSetValue();
		}
		protected virtual void OnUpdateFromObjects(object[] values)
		{
			string valString = null;

			if (!values.Any() || values.All(o => o == null))
			{
				this.ClearContent();

				this.Hints &= ~HintFlags.ExpandEnabled;
				this.ButtonIcon = CustomPropertyGrid.Properties.Resources.ImageAdd;
				this.buttonIsCreate = true;
				this.Expanded = false;
					
				valString = "null";
			}
			else
			{
				this.Hints |= HintFlags.ExpandEnabled;
				if (!this.CanExpand) this.Expanded = false;
				this.ButtonIcon = CustomPropertyGrid.Properties.Resources.ImageDelete;
				this.buttonIsCreate = false;

				valString = values.Count() == 1 ? 
					values.First().ToString() :
					string.Format(CustomPropertyGrid.Properties.Resources.PropertyGrid_N_Objects, values.Count());
			}

			this.HeaderValueText = valString;
		}
		protected override void OnEditedTypeChanged()
		{
			base.OnEditedTypeChanged();
			if (this.ContentInitialized) this.InitContent();
		}
		protected override void OnEditedMemberChanged()
		{
			base.OnEditedTypeChanged();
			if (this.ContentInitialized) this.InitContent();
		}
		protected override void OnButtonPressed()
		{
			base.OnButtonPressed();
			if (this.EditedType.IsValueType)
			{
				this.SetValue(this.ParentGrid.CreateObjectInstance(this.EditedType));
			}
			else
			{
				if (this.buttonIsCreate)
				{
					this.SetValue(this.ParentGrid.CreateObjectInstance(this.EditedType));
					this.Expanded = true;
				}
				else
				{
					this.SetValue(null);
				}
			}

			this.PerformGetValue();
		}

		protected Func<IEnumerable<object>> CreatePropertyValueGetter(PropertyInfo property)
		{
			return () => this.GetValue().Select(o => o != null ? property.GetValue(o, null) : null);
		}
		protected Func<IEnumerable<object>> CreateFieldValueGetter(FieldInfo field)
		{
			return () => this.GetValue().Select(o => o != null ? field.GetValue(o) : null);
		}
		protected Action<IEnumerable<object>> CreatePropertyValueSetter(PropertyInfo property)
		{
			bool affectsOthers = this.memberAffectsOthers(property);
			return delegate(IEnumerable<object> values)
			{
				IEnumerator<object> valuesEnum = values.GetEnumerator();
				object[] targetArray = this.GetValue().ToArray();

				object curValue = null;
				if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
				foreach (object target in targetArray)
				{
					if (target != null) property.SetValue(target, curValue, null);
					if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
				}
				this.OnPropertySet(property, targetArray);
				if (affectsOthers) this.PerformGetValue();

				// Fixup struct values by assigning the modified struct copy to its original member
				if (this.EditedType.IsValueType || this.ForceWriteBack) this.SetValues((IEnumerable<object>)targetArray);
			};
		}
		protected Action<IEnumerable<object>> CreateFieldValueSetter(FieldInfo field)
		{
			bool affectsOthers = this.memberAffectsOthers(field);
			return delegate(IEnumerable<object> values)
			{
				IEnumerator<object> valuesEnum = values.GetEnumerator();
				object[] targetArray = this.GetValue().ToArray();

				object curValue = null;
				if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
				foreach (object target in targetArray)
				{
					if (target != null) field.SetValue(target, curValue);
					if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
				}
				this.OnFieldSet(field, targetArray);
				if (affectsOthers) this.PerformGetValue();

				// Fixup struct values by assigning the modified struct copy to its original member
				if (this.EditedType.IsValueType || this.ForceWriteBack) this.SetValues((IEnumerable<object>)targetArray);
			};
		}

		protected virtual void OnPropertySet(PropertyInfo property, IEnumerable<object> targets)
		{

		}
		protected virtual void OnFieldSet(FieldInfo property, IEnumerable<object> targets)
		{

		}

		private bool DefaultMemberPredicate(MemberInfo info)
		{
			return true;
		}
		private bool DefaultMemberAffectsOthers(MemberInfo info)
		{
			return false;
		}
		private	PropertyEditor DefaultMemberEditorCreator(MemberInfo info)
		{
			return null;
		}

		private static int GetTypeHierarchyLevel(Type t)
		{
			int level = 0;
			while (t.BaseType != null) { t = t.BaseType; level++; }
			return level;
		}
	}
}