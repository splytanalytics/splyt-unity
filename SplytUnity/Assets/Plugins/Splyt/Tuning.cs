using UnityEngine;
using System.Runtime.InteropServices;

using Splyt.External.MiniJSON; 

namespace Splyt
{
	/// <summary>
	/// For using Splyt's dynamic tuning system
	/// </summary>
	public class Tuning
	{
		/// <summary>
		/// Retrieves updated values from Splyt for all tuning variables. If multiple users are registered, updated values will be retrieved for all of them.
		/// </summary>
		/// <param name="cb">Application defined callback which will occur on completion</param>
		public static void refresh(Callback cb)
		{
            Error error = Error.Success;

			if (null == cb) 
			{
				Util.logError("Please provide a valid Splyt.Callback");
                error = Error.InvalidArgs;
			}

            if (Error.Success == error) 
			{
                TuningSubsystem.refresh(cb);
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
		}

		/// <summary>
		/// Get the value of a named tuning variable from Splyt.
		/// 
		/// <b>Note:</b> This is not an async or blocking operation. Tuning values are proactively cached by
		/// the Splyt Framework during Splyt.Core.init, Splyt.Core.registerUser, and Splyt.Tuning.refresh
		/// 
		/// <b>Note:</b> The return value is guaranteed to match the type of the defaultValue. If a dynamic value is set in Splyt which cannot
		/// be converted into the proper type, the default will be returned.
		/// </summary>
		/// <returns>The dynamic value of the variable (or the default value)</returns>
		/// <param name="varName">Application defined name of a variable to retrieve</param>
		/// <param name="defaultValue">A default value for the tuning variable, used when a dynamic value has not been specified or is otherwise not available</param>
		/// <typeparam name="T">In practice, this will always be automatically inferred from defaultValue</typeparam>
		public static T getVar<T>(string varName, T defaultValue)
		{
			T val = defaultValue;

            val = TuningSubsystem.getVar(CoreSubsystem.UserId, CoreSubsystem.DeviceId, varName, defaultValue);

			return val;
		}
	}
}
