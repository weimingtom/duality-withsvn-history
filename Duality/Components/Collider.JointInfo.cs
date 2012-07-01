﻿using System;

using OpenTK;

using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Dynamics.Joints;

using Duality.EditorHints;
using Duality.Resources;

namespace Duality.Components
{
	public partial class Collider
	{
		/// <summary>
		/// Describes a <see cref="Collider"/> joint. Joints limit a Colliders degree of freedom 
		/// by connecting it to fixed world coordinates or other Colliders.
		/// </summary>
		[Serializable]
		public abstract class JointInfo : Duality.Cloning.ICloneable
		{
			[NonSerialized]	
			protected	Joint		joint		= null;
			private		Collider	colA		= null;
			private		Collider	colB		= null;
			private		bool		collide		= false;
			private		bool		enabled		= true;
			private		float		breakPoint	= -1.0f;
			
			[EditorHintFlags(MemberFlags.Invisible)]
			public bool IsInitialized
			{
				get { return this.joint != null; }
			}
			[EditorHintFlags(MemberFlags.Invisible)]
			public Collider ColliderA
			{
				get { return this.colA; }
				internal set { this.colA = value; }
			}
			[EditorHintFlags(MemberFlags.Invisible)]
			public Collider ColliderB
			{
				get { return this.colB; }
				internal set { this.colB = value; }
			}
			/// <summary>
			/// [GET] Returns whether the joint is connecting two Colliders (instead of connecting one to the world)
			/// </summary>
			[EditorHintFlags(MemberFlags.Invisible)]
			public abstract bool DualJoint { get; }
			/// <summary>
			/// [GET / SET] Specifies whether the connected Colliders will collide with each other.
			/// </summary>
			public bool CollideConnected
			{
				get { return this.collide; }
				set { this.collide = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] Whether or not the joint is active.
			/// </summary>
			public bool Enabled
			{
				get { return this.enabled; }
				set { this.enabled = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] Maximum joint error value before the joint break. Breaking does not remove the joint, but disable it.
			/// A value of zero or lower is interpreted as unbreakable. Note that some joints might not have breaking point support 
			/// and will ignore this value.
			/// </summary>
			[EditorHintRange(-1.0f, float.MaxValue)]
			public float BreakPoint
			{
				get { return this.breakPoint; }
				set { this.breakPoint = value; this.UpdateJoint(); }
			}

			
			protected abstract Joint CreateJoint(Body bodyA, Body bodyB);
			internal void DestroyJoint()
			{
				if (this.joint == null) return;
				Scene.PhysicsWorld.RemoveJoint(this.joint);
				this.joint.Broke -= this.joint_Broke;
				this.joint = null;
			}
			internal virtual void UpdateJoint()
			{
				if (this.joint == null)
				{
					this.joint = this.CreateJoint(this.colA != null ? this.colA.body : null, this.colB != null ? this.colB.body : null);
					if (this.joint == null) return; // Failed to create the joint? Return.

					this.joint.UserData = this;
					this.joint.Broke += this.joint_Broke;
				}

				this.joint.CollideConnected = this.collide;
				this.joint.Enabled = this.enabled;
				this.joint.Breakpoint = this.breakPoint <= 0.0f ? float.MaxValue : this.breakPoint;
			}
			private void joint_Broke(Joint arg1, float arg2)
			{
				this.enabled = false;
			}

			/// <summary>
			/// Copies this JointInfos data to another one. It is assumed that both are of the same type.
			/// </summary>
			/// <param name="target"></param>
			protected virtual void CopyTo(JointInfo target)
			{
				// Don't copy the parents!
				target.collide = this.collide;
				target.enabled = this.enabled;
				target.breakPoint = this.breakPoint;
			}
			/// <summary>
			/// Clones the JointInfo.
			/// </summary>
			/// <returns></returns>
			public JointInfo Clone()
			{
				JointInfo newObj = this.GetType().CreateInstanceOf() as JointInfo;
				this.CopyTo(newObj);
				return newObj;
			}

			object Cloning.ICloneable.CreateTargetObject(Cloning.CloneProvider provider)
			{
				return this.GetType().CreateInstanceOf() ?? this.GetType().CreateInstanceOf(true);
			}
			void Cloning.ICloneable.CopyDataTo(object targetObj, Cloning.CloneProvider provider)
			{
				JointInfo targetJoint = targetObj as JointInfo;
				this.CopyTo(targetJoint);
			}

			protected static Vector2 GetFarseerPoint(Collider c, Vector2 dualityPoint)
			{
				if (c == null) return PhysicsConvert.ToPhysicalUnit(dualityPoint);

				Vector2 scale = (c.GameObj != null && c.GameObj.Transform != null) ? c.GameObj.Transform.Scale.Xy : Vector2.One;
				return PhysicsConvert.ToPhysicalUnit(dualityPoint * scale);
			}
			protected static Vector2 GetDualityPoint(Collider c, Vector2 farseerPoint)
			{
				if (c == null) return PhysicsConvert.ToDualityUnit(farseerPoint);

				Vector2 scale = (c.GameObj != null && c.GameObj.Transform != null) ? c.GameObj.Transform.Scale.Xy : Vector2.One;
				return PhysicsConvert.ToDualityUnit(farseerPoint / scale);
			}
		}

		/// <summary>
		/// Constrains the Collider to a fixed angle
		/// </summary>
		[Serializable]
		public sealed class FixedAngleJointInfo : JointInfo
		{
			private	float	angle	= 0.0f;


			public override bool DualJoint
			{
				get { return false; }
			}
			/// <summary>
			/// [GET / SET] The Colliders target angle.
			/// </summary>
			public float TargetAngle
			{
				get { return this.angle; }
				set { this.angle = value; this.UpdateJoint(); }
			}


			public FixedAngleJointInfo() : this(0.0f) {}
			public FixedAngleJointInfo(float angle)
			{
				this.angle = angle;
			}

			protected override Joint CreateJoint(Body bodyA, Body bodyB)
			{
				return bodyA != null ? JointFactory.CreateFixedAngleJoint(Scene.PhysicsWorld, bodyA) : null;
			}
			internal override void UpdateJoint()
			{
				base.UpdateJoint();
				if (this.joint == null) return;

				FixedAngleJoint j = this.joint as FixedAngleJoint;
				j.TargetAngle = this.angle;
			}

			protected override void CopyTo(JointInfo target)
			{
				base.CopyTo(target);
				FixedAngleJointInfo c = target as FixedAngleJointInfo;
				c.angle = this.angle;
			}
		}

		/// <summary>
		/// Constrains the Collider to obtain a fixed distance to a world coordinate
		/// </summary>
		[Serializable]
		public sealed class FixedDistanceJointInfo : JointInfo
		{
			private	Vector2		localAnchor		= Vector2.Zero;
			private	Vector2		worldAnchor		= Vector2.Zero;
			private	float		dampingRatio	= 0.5f;
			private	float		frequency		= 1.0f;
			private	float		length			= 200.0f;


			public override bool DualJoint
			{
				get { return false; }
			}
			/// <summary>
			/// [GET / SET] The Colliders local anchor point.
			/// </summary>
			[EditorHintIncrement(1)]
			public Vector2 LocalAnchor
			{
				get { return this.localAnchor; }
				set { this.localAnchor = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The world anchor point to which the Collider will be attached.
			/// </summary>
			[EditorHintIncrement(1)]
			public Vector2 WorldAnchor
			{
				get { return this.worldAnchor; }
				set { this.worldAnchor = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The damping ratio. Zero means "no damping", one means "critical damping".
			/// </summary>
			[EditorHintRange(0.0f, 1.0f)]
			public float DampingRatio
			{
				get { return this.dampingRatio; }
				set { this.dampingRatio = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The mass spring damper frequency in hertz.
			/// </summary>
			public float Frequency
			{
				get { return this.frequency; }
				set { this.frequency = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The target distance between local and world anchor
			/// </summary>
			[EditorHintIncrement(1)]
			public float TargetDistance
			{
				get { return this.length; }
				set { this.length = value; this.UpdateJoint(); }
			}


			public FixedDistanceJointInfo() {}

			protected override Joint CreateJoint(Body bodyA, Body bodyB)
			{
				return bodyA != null ? JointFactory.CreateFixedDistanceJoint(Scene.PhysicsWorld, bodyA, Vector2.Zero, Vector2.Zero) : null;
			}
			internal override void UpdateJoint()
			{
				base.UpdateJoint();
				if (this.joint == null) return;

				FixedDistanceJoint j = this.joint as FixedDistanceJoint;
				j.WorldAnchorB = PhysicsConvert.ToPhysicalUnit(this.worldAnchor);
				j.LocalAnchorA = GetFarseerPoint(this.ColliderA, this.localAnchor);
				j.DampingRatio = this.dampingRatio;
				j.Frequency = this.frequency;
				j.Length = PhysicsConvert.ToPhysicalUnit(this.length);
			}

			protected override void CopyTo(JointInfo target)
			{
				base.CopyTo(target);
				FixedDistanceJointInfo c = target as FixedDistanceJointInfo;
				c.worldAnchor = this.worldAnchor;
				c.localAnchor = this.localAnchor;
				c.dampingRatio = this.dampingRatio;
				c.frequency = this.frequency;
				c.length = this.length;
			}
		}

		/// <summary>
		/// "Welds" two Colliders together so they share a common point and relative angle.
		/// </summary>
		[Serializable]
		public sealed class WeldJointInfo : JointInfo
		{
			private Vector2 localAnchorA	= Vector2.Zero;
			private	Vector2	localAnchorB	= Vector2.Zero;
			private	float	refAngle		= 0.0f;
			

			public override bool DualJoint
			{
				get { return true; }
			}
			/// <summary>
			/// [GET / SET] The welding point, locally to the first object.
			/// </summary>
			public Vector2 LocalAnchorA
			{
				get { return this.localAnchorA; }
				set { this.localAnchorA = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The welding point, locally to the second object.
			/// </summary>
			public Vector2 LocalAnchorB
			{
				get { return this.localAnchorB; }
				set { this.localAnchorB = value; this.UpdateJoint(); }
			}
			/// <summary>
			/// [GET / SET] The relative angle both objects need to keep.
			/// </summary>
			public float RefAngle
			{
				get { return this.refAngle; }
				set { this.refAngle = value; this.UpdateJoint(); }
			}


			protected override Joint CreateJoint(Body bodyA, Body bodyB)
			{
				return bodyA != null && bodyB != null ? JointFactory.CreateWeldJoint(Scene.PhysicsWorld, bodyA, bodyB, Vector2.Zero) : null;
			}
			internal override void UpdateJoint()
			{
				base.UpdateJoint();
				if (this.joint == null) return;

				WeldJoint j = this.joint as WeldJoint;
				j.LocalAnchorA = GetFarseerPoint(this.ColliderA, this.localAnchorA);
				j.LocalAnchorB = GetFarseerPoint(this.ColliderB, this.localAnchorB);
				j.ReferenceAngle = this.refAngle;
			}

			protected override void CopyTo(JointInfo target)
			{
				base.CopyTo(target);
				WeldJointInfo c = target as WeldJointInfo;
				c.localAnchorA = this.localAnchorA;
				c.localAnchorB = this.localAnchorB;
				c.refAngle = this.refAngle;
			}
		}
	}
}
