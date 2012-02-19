﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Duality.EditorHints
{
	/// <summary>
	/// Some general flags for Type members that indicate preferred editor behaviour.
	/// </summary>
	[Flags]
	public enum MemberFlags
	{
		/// <summary>
		/// No flags set.
		/// </summary>
		None			= 0x0,

		/// <summary>
		/// When editing the Properties or Fields value, a final set operation is requested to finish editing.
		/// </summary>
		ForceWriteback	= 0x1,
		/// <summary>
		/// The member is considered invisible.
		/// </summary>
		Invisible		= 0x2,
		/// <summary>
		/// The member is considered read-only, even if writing is possible via reflection.
		/// </summary>
		ReadOnly		= 0x4,
		/// <summary>
		/// Indicates that editing the member may have an effect on any other member of the current object.
		/// </summary>
		AffectsOthers	= 0x8
	}

	/// <summary>
	/// An attribute that provides member-related information about preferred editor behaviour
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public abstract class EditorHintMemberAttribute : Attribute {}

	/// <summary>
	/// Provides general information about a members preferred editor behaviour.
	/// </summary>
	public class EditorHintFlagsAttribute : EditorHintMemberAttribute
	{
		private	MemberFlags	flags;
		/// <summary>
		/// [GET] Flags that indicate the members general behaviour
		/// </summary>
		public MemberFlags Flags
		{
			get { return this.flags; }
		}
		public EditorHintFlagsAttribute(MemberFlags flags)
		{
			this.flags = flags;
		}
	}

	/// <summary>
	/// Provides information about a numerical members allowed value range.
	/// </summary>
	public class EditorHintRangeAttribute : EditorHintMemberAttribute
	{
		private	decimal	min;
		private	decimal	max;
		/// <summary>
		/// [GET] The members minimum value
		/// </summary>
		public decimal Min
		{
			get { return this.min; }
		}
		/// <summary>
		/// [GET] The members maximum value
		/// </summary>
		public decimal Max
		{
			get { return this.max; }
		}
		public EditorHintRangeAttribute(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
		public EditorHintRangeAttribute(float min, float max)
		{
			this.min = (decimal)min;
			this.max = (decimal)max;
		}
	}

	/// <summary>
	/// Provides information about a numerical members value increment.
	/// </summary>
	public class EditorHintIncrementAttribute : EditorHintMemberAttribute
	{
		private	decimal	inc;
		/// <summary>
		/// [GET] The members value increment.
		/// </summary>
		public decimal Increment
		{
			get { return this.inc; }
		}
		public EditorHintIncrementAttribute(int inc)
		{
			this.inc = inc;
		}
		public EditorHintIncrementAttribute(float inc)
		{
			this.inc = (decimal)inc;
		}
	}

	/// <summary>
	/// Provides information about a numerical members decimal accuracy
	/// </summary>
	public class EditorHintDecimalPlacesAttribute : EditorHintMemberAttribute
	{
		private	int places;
		/// <summary>
		/// [GET] The preferred number of displayed decimal places
		/// </summary>
		public int Places
		{
			get { return this.places; }
		}
		public EditorHintDecimalPlacesAttribute(int places)
		{
			this.places = places;
		}
	}
}