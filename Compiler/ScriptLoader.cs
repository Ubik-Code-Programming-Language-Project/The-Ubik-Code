using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Compiler
{
    /// <summary>
    /// Interface for script loading and inclusion. The
    /// interface is provided to allow implementation of custom script
    /// loading and inclusion mechanisms beyond the default disk-based
    /// implementation provided by the library.
    /// </summary>
    public interface ScriptLoader
    {
        #region Public Methods

        /// <summary>
        /// Returns a script in the form of a collection of source lines
        /// using the given script resource name.
        /// </summary>
        /// <param name="strResourceName">Script resource name.</param>
        /// <returns></returns>
        List<String> LoadScript(String strResourceName);

        #endregion
    }
}
