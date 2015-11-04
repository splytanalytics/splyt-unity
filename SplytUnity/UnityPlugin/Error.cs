namespace Splyt
{
	/// <summary>
	/// All of the errors the application may see from Splyt
	/// </summary>
	public enum Error
	{
		/// <summary>
		/// Success (no error)
		/// </summary>
		Success = 0,
		
		/// <summary>
		/// Generic error
		/// </summary>
		Generic = -1,
		
		/// <summary>
		/// Splyt has not been initialized
		/// </summary>
		NotInitialized = -2,
		
		/// <summary>
		/// Splyt has already been initialized
		/// </summary>
		AlreadyInitialized = -3,
		
		/// <summary>
		/// Invalid arguments passed into a function
		/// </summary>
		InvalidArgs = -4,
		
		/// <summary>
		/// Invalid configuation prior to initialization
		/// </summary>
		MissingId = -5,
		
		/// <summary>
		/// A web request timed out
		/// </summary>
		RequestTimedOut = -6,
		LastKnown = RequestTimedOut,

		/// <summary>
		/// Occurs when an error string cannot be parsed into a proper error
		/// </summary>
		Unknown = -99
	}
}

public static class ErrorExtension
{
	public static Splyt.Error toSplytError(this string value)
	{
		int intVal;
		if(int.TryParse(value, out intVal))
		{
			if(intVal <= (int) Splyt.Error.Success && intVal >= (int) Splyt.Error.LastKnown)
			{
				return (Splyt.Error) intVal;
			}
		}
		
		return Splyt.Error.Unknown;
	}
}

