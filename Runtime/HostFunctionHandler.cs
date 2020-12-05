using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Defines an interface for implementing host function calls.
    /// </summary>
    public interface HostFunctionHandler
    {
        #region Public Methods

        /// <summary>
        /// Invoked on a call to a host function.
        /// </summary>
        /// <param name="strFunctionName">Name of the invoked host function.</param>
        /// <param name="listParameters">List of parameters passed to the function.</param>
        /// <returns>Return value of the host function.</returns>
        object OnHostFunctionCall(String strFunctionName, List<object> listParameters);

        #endregion
    }
}
