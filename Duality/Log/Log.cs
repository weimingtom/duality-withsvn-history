﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Duality
{
	/// <summary>
	/// Listens for log entries and writes them to registered <see cref="ILogOutput">ILogOutputs</see>.
	/// </summary>
	public class Log
	{
		/// <summary>
		/// Holds a Logs state values.
		/// </summary>
		public class SharedState
		{
			private	int	indent	= 0;

			/// <summary>
			/// [GET / SET] The Logs indent value.
			/// </summary>
			public int Indent
			{
				get { return this.indent; }
				internal set { this.indent = value; }
			}
		}

		private	static	Log				logGame		= null;
		private	static	Log				logCore		= null;
		private	static	Log				logEditor	= null;
		private	static	DataLogOutput	data		= null;

		/// <summary>
		/// [GET] A log for game-related entries. Use this for logging data from game plugins.
		/// </summary>
		public static Log Game
		{
			get { return logGame; }
		}
		/// <summary>
		/// [GET] A log for core-related entries. This is normally only used by Duality itsself.
		/// </summary>
		public static Log Core
		{
			get { return logCore; }
		}
		/// <summary>
		/// [GET] A log for editor-related entries. This is used by the Duality editor and its plugins.
		/// </summary>
		public static Log Editor
		{
			get { return logEditor; }
		}
		/// <summary>
		/// [GET] Returns an object storing all log entries that have been made since startup
		/// </summary>
		public static DataLogOutput LogData
		{
			get { return data; }
		}

		static Log()
		{
			SharedState state = new SharedState();
			data = new DataLogOutput();
			logGame		= new Log("Game", state, new ConsoleLogOutput(ConsoleColor.DarkGray), data);
			logCore		= new Log("Core", state, new ConsoleLogOutput(ConsoleColor.DarkBlue), data);
			logEditor	= new Log("Edit", state, new ConsoleLogOutput(ConsoleColor.DarkMagenta), data);
		}


		private HashSet<string>				onceWritten = new HashSet<string>();
		private	Dictionary<string,float>	timedLast	= new Dictionary<string,float>();
		private	List<ILogOutput>			strOut		= null;
		private	SharedState					state		= null;
		private	string						name		= "Log";
		private string						prefix		= "[Log] ";

		/// <summary>
		/// [GET] The Log's name
		/// </summary>
		public string Name
		{
			get { return this.name; }
		}
		/// <summary>
		/// [GET] The Log's prefix, which is automatically determined by its name.
		/// </summary>
		public string Prefix
		{
			get { return this.prefix; }
		}
		/// <summary>
		/// [GET] The Log's current indent level.
		/// </summary>
		public int Indent
		{
			get { return this.state.Indent; }
		}

		/// <summary>
		/// Creates a new Log.
		/// </summary>
		/// <param name="name">The Logs name.</param>
		/// <param name="stateHolder">The Logs state value holder that may be shared with other Logs.</param>
		/// <param name="output">It will be initially connected to the specified outputs.</param>
		public Log(string name, SharedState stateHolder, params ILogOutput[] output)
		{
			this.state = stateHolder;
			this.name = name;
			this.prefix = "[" + name + "] ";
			this.strOut = new List<ILogOutput>(output);
		}
		/// <summary>
		/// Creates a new Log.
		/// </summary>
		/// <param name="name">The Logs name</param>
		/// <param name="output">It will be initially connected to the specified outputs.</param>
		public Log(string name, params ILogOutput[] output) : this(name, new SharedState(), output) {}

		/// <summary>
		/// Registers an output to write log entries to.
		/// </summary>
		/// <param name="writer"></param>
		public void RegisterOutput(ILogOutput writer)
		{
			this.strOut.Add(writer);
		}
		/// <summary>
		/// Unregisters a registered output.
		/// </summary>
		/// <param name="writer"></param>
		public void UnregisterOutput(ILogOutput writer)
		{
			this.strOut.Remove(writer);
		}

		/// <summary>
		/// Resets the Logs timing and "written once" counters. Outputs will remain attached.
		/// </summary>
		public void Reset()
		{
			this.onceWritten.Clear();
			this.timedLast.Clear();
		}

		/// <summary>
		/// Increases the current log entry indent.
		/// </summary>
		public void PushIndent()
		{
			this.state.Indent++;
		}
		/// <summary>
		/// Decreases the current log entry indent.
		/// </summary>
		public void PopIndent()
		{
			this.state.Indent--;
		}

		private void Write(LogMessageType type, string msg)
		{
			Performance.timeLog.BeginMeasure();
			foreach (ILogOutput log in this.strOut)
				log.Write(this, type, msg);
			Performance.timeLog.EndMeasure();
		}

		/// <summary>
		/// Writes a new log entry.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void Write(string format, params object[] obj)
		{
			this.Write(LogMessageType.Message, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
		}
		/// <summary>
		/// Writes a new warning log entry.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteWarning(string format, params object[] obj)
		{
			this.Write(LogMessageType.Warning, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
		}
		/// <summary>
		/// Writes a new error log entry.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteError(string format, params object[] obj)
		{
			this.Write(LogMessageType.Error, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
		}

		/// <summary>
		/// Writes a new log entry once. If the same message is attempted to be written again, it will be ignored.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteOnce(string format, params object[] obj)
		{
			string t = String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj);
			if (!this.onceWritten.Contains(t))
			{
				this.onceWritten.Add(t);
				this.Write(LogMessageType.Message, t);
			}
		}
		/// <summary>
		/// Writes a warning new log entry once. If the same message is attempted to be written again, it will be ignored.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteWarningOnce(string format, params object[] obj)
		{
			string t = String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj);
			if (!this.onceWritten.Contains(t))
			{
				this.onceWritten.Add(t);
				this.Write(LogMessageType.Warning, t);
			}
		}
		/// <summary>
		/// Writes a error new log entry once. If the same message is attempted to be written again, it will be ignored.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteErrorOnce(string format, params object[] obj)
		{
			string t = String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj);
			if (!this.onceWritten.Contains(t))
			{
				this.onceWritten.Add(t);
				this.Write(LogMessageType.Error, t);
			}
		}

		/// <summary>
		/// Writes a new log entry, but a maximum of one entry per fixed amount of time.
		/// </summary>
		/// <param name="delayMs">The time span in which only one message is written.</param>
		/// <param name="timerId">The id of the timer that is used for measuring the time span. Simply use any string id that seems to fit.</param>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteTimed(int delayMs, string timerId, string format, params object[] obj)
		{
			float last;
			if (!this.timedLast.TryGetValue(timerId, out last) || Time.MainTimer - last > delayMs)
			{
				this.timedLast[timerId] = Time.MainTimer;
				this.Write(LogMessageType.Message, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
			}
		}
		/// <summary>
		/// Writes a new warning log entry, but a maximum of one entry per fixed amount of time.
		/// </summary>
		/// <param name="delayMs">The time span in which only one message is written.</param>
		/// <param name="timerId">The id of the timer that is used for measuring the time span. Simply use any string id that seems to fit.</param>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteWarningTimed(int delayMs, string timerId, string format, params object[] obj)
		{
			float last;
			if (!this.timedLast.TryGetValue(timerId, out last) || Time.MainTimer - last > delayMs)
			{
				this.timedLast[timerId] = Time.MainTimer;
				this.Write(LogMessageType.Warning, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
			}
		}
		/// <summary>
		/// Writes a new error log entry, but a maximum of one entry per fixed amount of time.
		/// </summary>
		/// <param name="delayMs">The time span in which only one message is written.</param>
		/// <param name="timerId">The id of the timer that is used for measuring the time span. Simply use any string id that seems to fit.</param>
		/// <param name="format"></param>
		/// <param name="obj"></param>
		public void WriteErrorTimed(int delayMs, string timerId, string format, params object[] obj)
		{
			float last;
			if (!this.timedLast.TryGetValue(timerId, out last) || Time.MainTimer - last > delayMs)
			{
				this.timedLast[timerId] = Time.MainTimer;
				this.Write(LogMessageType.Error, String.Format(System.Globalization.CultureInfo.InvariantCulture, format, obj));
			}
		}

		/// <summary>
		/// Retrieves the current stack frame.
		/// </summary>
		/// <param name="skipFrames">The number of frames to skip. This function itsself is omitted by default.</param>
		/// <returns>The caller's stack frame.</returns>
		public static System.Diagnostics.StackFrame CurrentStackFrame(int skipFrames = 0)
		{
			return new System.Diagnostics.StackTrace(skipFrames + 1).GetFrame(0);
		}
		/// <summary>
		/// Returns the name of the caller method.
		/// </summary>
		/// <param name="skipFrames">The number of frames to skip. This function itsself is omitted by default.</param>
		/// <param name="includeDeclaringType">If true, the methods declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string CurrentMethod(int skipFrames = 0, bool includeDeclaringType = true)
		{
			return MethodInfo(CurrentStackFrame(skipFrames + 1).GetMethod(), includeDeclaringType);
		}
		/// <summary>
		/// Returns the name of the caller methods declaring type.
		/// </summary>
		/// <param name="skipFrames">The number of frames to skip. This function itsself is omitted by default.</param>
		/// <returns></returns>
		public static string CurrentType(int skipFrames = 0)
		{
			return Type(CurrentStackFrame(skipFrames + 1).GetMethod().DeclaringType);
		}

		/// <summary>
		/// Returns a string that can be used for representing a <see cref="System.Type"/> in log entries.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Type(Type type)
		{
			return type.GetTypeCSCodeName(true);
		}
		/// <summary>
		/// Returns a string that can be used for representing a method in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the methods declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string MethodInfo(MethodInfo info, bool includeDeclaringType = true)
		{
			string declTypeName = Type(info.DeclaringType);
			string[] paramNames = info.GetParameters().Select(p => Type(p.ParameterType)).ToArray();
			string[] genArgNames = info.GetGenericArguments().Select(t => Type(t)).ToArray();
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0}{1}{3}({2})",
				includeDeclaringType ? declTypeName + "." : "",
				info.Name,
				paramNames.ToString(", "),
				genArgNames.Length > 0 ? "<" + genArgNames.ToString(", ") + ">" : "");
		}
		/// <summary>
		/// Returns a string that can be used for representing a method or constructor in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the methods or constructors declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string MethodInfo(MethodBase info, bool includeDeclaringType = true)
		{
			if (info is MethodInfo)
				return MethodInfo(info as MethodInfo);
			else if (info is ConstructorInfo)
				return ConstructorInfo(info as ConstructorInfo);
			else if (info != null)
				return info.ToString();
			else
				return "null";
		}
		/// <summary>
		/// Returns a string that can be used for representing a constructor in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the constructors declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string ConstructorInfo(ConstructorInfo info, bool includeDeclaringType = true)
		{
			string declTypeName = Type(info.DeclaringType);
			string[] paramNames = info.GetParameters().Select(p => Type(p.ParameterType)).ToArray();
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0}{1}({2})",
				includeDeclaringType ? declTypeName + "." : "",
				info.DeclaringType.Name,
				paramNames.ToString(", "));
		}
		/// <summary>
		/// Returns a string that can be used for representing a property in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the properties declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string PropertyInfo(PropertyInfo info, bool includeDeclaringType = true)
		{
			string declTypeName = Type(info.DeclaringType);
			string propTypeName = Type(info.PropertyType);
			string[] paramNames = info.GetIndexParameters().Select(p => Type(p.ParameterType)).ToArray();
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0} {1}{2}{3}",
				propTypeName,
				includeDeclaringType ? declTypeName + "." : "",
				info.Name,
				paramNames.Any() ? "[" + paramNames.ToString(", ") + "]" : "");
		}
		/// <summary>
		/// Returns a string that can be used for representing a field in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the fields declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string FieldInfo(FieldInfo info, bool includeDeclaringType = true)
		{
			string declTypeName = Type(info.DeclaringType);
			string fieldTypeName = Type(info.FieldType);
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0} {1}{2}",
				fieldTypeName,
				includeDeclaringType ? declTypeName + "." : "",
				info.Name);
		}
		/// <summary>
		/// Returns a string that can be used for representing an event in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the events declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string EventInfo(EventInfo info, bool includeDeclaringType = true)
		{
			string declTypeName = Type(info.DeclaringType);
			string fieldTypeName = Type(info.EventHandlerType);
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0} {1}{2}",
				fieldTypeName,
				includeDeclaringType ? declTypeName + "." : "",
				info.Name);
		}
		/// <summary>
		/// Returns a string that can be used for representing a(ny) member in log entries.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="includeDeclaringType">If true, the members declaring type is included in the returned name.</param>
		/// <returns></returns>
		public static string MemberInfo(MemberInfo info, bool includeDeclaringType = true)
		{
			if (info is MethodInfo)
				return MethodInfo(info as MethodInfo, includeDeclaringType);
			else if (info is ConstructorInfo)
				return ConstructorInfo(info as ConstructorInfo, includeDeclaringType);
			else if (info is PropertyInfo)
				return PropertyInfo(info as PropertyInfo, includeDeclaringType);
			else if (info is FieldInfo)
				return FieldInfo(info as FieldInfo, includeDeclaringType);
			else if (info is EventInfo)
				return EventInfo(info as EventInfo, includeDeclaringType);
			else if (info is Type)
				return Type(info as Type);
			else if (info != null)
				return info.ToString();
			else
				return "null";
		}
		
		/// <summary>
		/// Returns a string that can be used for representing an exception in log entries.
		/// It usually does not include the full call stack and is significantly shorter than
		/// an <see cref="System.Exception">Exceptions</see> ToString method.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static string Exception(Exception e, bool callStack = true)
		{
			if (e == null) return null;

			string eName = Type(e.GetType());
			string eSite = e.TargetSite != null ? MemberInfo(e.TargetSite) : null;

			return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
				"{0}{1}: {2}\nCallStack:\n{3}",
				eName,
				eSite != null ? " at " + eSite : "",
				e.Message,
				e.StackTrace);
		}
	}

	/// <summary>
	/// The type of a log message / entry.
	/// </summary>
	public enum LogMessageType
	{
		/// <summary>
		/// Just a regular message. Nothing special. Neutrally informs about what's going on.
		/// </summary>
		Message,
		/// <summary>
		/// A warning message. It informs about unexpected data or behaviour that might not have caused any errors yet, but can lead to them.
		/// It might also be used for expected errors from which Duality is likely to recover.
		/// </summary>
		Warning,
		/// <summary>
		/// An error message. It informs about an unexpected and/or critical error that has occured.
		/// </summary>
		Error
	}

	/// <summary>
	/// Represents a single <see cref="Log"/> output and provides actual writing functionality for 
	/// </summary>
	public interface ILogOutput
	{
		/// <summary>
		/// Writes a single message to the output.
		/// </summary>
		/// <param name="source">The <see cref="Log"/> from which the message originates.</param>
		/// <param name="type">The type of the log message.</param>
		/// <param name="msg">The message to write.</param>
		void Write(Log source, LogMessageType type, string msg);
	}
}
