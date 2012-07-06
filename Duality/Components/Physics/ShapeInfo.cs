﻿using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision.Shapes;

using Duality.EditorHints;

namespace Duality.Components.Physics
{
	/// <summary>
	/// Describes a <see cref="RigidBody">Colliders</see> primitive shape. A Colliders overall shape may be combined of any number of primitive shapes.
	/// </summary>
	[Serializable]
	public abstract class ShapeInfo : Duality.Cloning.ICloneable
	{
		[NonSerialized]	
		protected	Fixture		fixture		= null;
		private		RigidBody	parent		= null;
		private		float		density		= 1.0f;
		private		float		friction	= 0.3f;
		private		float		restitution	= 0.3f;
		private		bool		sensor		= false;
			
		/// <summary>
		/// [GET] The shape's parent <see cref="RigidBody"/>.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public RigidBody Parent
		{
			get { return this.parent; }
			internal set { this.parent = value; }
		}
		/// <summary>
		/// [GET / SET] The shapes density.
		/// </summary>
		[EditorHintIncrement(0.05f)]
		[EditorHintRange(0.0f, 100.0f)]
		public float Density
		{
			get { return this.density; }
			set 
			{
				this.density = value;
				if (this.parent != null) // Full update to recalculate mass
					this.parent.UpdateBody();
				else
					this.UpdateFixture();
			}
		}
		/// <summary>
		/// [GET / SET] Whether or not the shape acts as sensor i.e. is not part of a rigid body.
		/// </summary>
		public bool IsSensor
		{
			get { return this.sensor; }
			set { this.sensor = value; this.UpdateFixture(); }
		}
		/// <summary>
		/// [GET / SET] The shapes friction value.
		/// </summary>
		[EditorHintIncrement(0.05f)]
		[EditorHintRange(0.0f, 1.0f)]
		public float Friction
		{
			get { return this.friction; }
			set { this.friction = value; this.UpdateFixture(); }
		}
		/// <summary>
		/// [GET / SET] The shapes restitution value.
		/// </summary>
		[EditorHintIncrement(0.05f)]
		[EditorHintRange(0.0f, 1.0f)]
		public float Restitution
		{
			get { return this.restitution; }
			set { this.restitution = value; this.UpdateFixture(); }
		}
		/// <summary>
		/// [GET] Returns the Shapes axis-aligned bounding box
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public abstract Rect AABB { get; }

		protected ShapeInfo()
		{
		}
		protected ShapeInfo(float density)
		{
			this.density = density;
		}

		internal void DestroyFixture(Body body)
		{
			if (this.fixture == null) return;
			body.DestroyFixture(this.fixture);
			this.fixture = null;
		}
		protected abstract Fixture CreateFixture(Body body);
		internal virtual void UpdateFixture()
		{
			if (this.fixture == null)
			{
				if (this.parent != null && this.parent.PhysicsBody != null)
				{
					this.fixture = this.CreateFixture(this.parent.PhysicsBody);
					this.fixture.UserData = this;
				}
				else return;
			}

			this.fixture.Shape.Density = this.density;
			this.fixture.IsSensor = this.sensor;
			this.fixture.Restitution = this.restitution;
			this.fixture.Friction = this.friction;
		}

		/// <summary>
		/// Copies this ShapeInfos data to another one. It is assumed that both are of the same type.
		/// </summary>
		/// <param name="target"></param>
		protected virtual void CopyTo(ShapeInfo target)
		{
			// Don't copy the parent!
			target.density = this.density;
			target.sensor = this.sensor;
			target.friction = this.friction;
			target.restitution = this.restitution;
		}
		/// <summary>
		/// Clones the ShapeInfo.
		/// </summary>
		/// <returns></returns>
		public ShapeInfo Clone()
		{
			return Cloning.CloneProvider.DeepClone(this);
		}

		object Cloning.ICloneable.CreateTargetObject(Cloning.CloneProvider provider)
		{
			return this.GetType().CreateInstanceOf() ?? this.GetType().CreateInstanceOf(true);
		}
		void Cloning.ICloneable.CopyDataTo(object targetObj, Cloning.CloneProvider provider)
		{
			ShapeInfo targetShape = targetObj as ShapeInfo;
			this.CopyTo(targetShape);
		}
	}
}