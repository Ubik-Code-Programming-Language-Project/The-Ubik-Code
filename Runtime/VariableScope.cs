using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents the scope of a variable dictionary.
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// Global scope (per <see cref="ScriptManager"/> instance).
        /// </summary>
        Global,

        /// <summary>
        /// Script scope shared by all <see cref="ScriptContext"/>s
        /// assocated with a <see cref="Script"/>.
        /// </summary>
        Script,

        /// <summary>
        /// Local scope associated with a the topmost function frame of a
        /// <see cref="ScriptContext"/>.
        /// </summary>
        Local
    }
}
