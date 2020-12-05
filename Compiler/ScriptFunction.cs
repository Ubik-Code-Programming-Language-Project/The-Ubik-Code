using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Ubik.Compiler;
using Ubik.Runtime;

namespace Ubik.Compiler
{
    /// <summary>
    /// Represents a compiled function within a script. The function defines
    /// the number of parameters accepted by the function and the associated
    /// entry point into the executable.
    /// </summary>
    public class ScriptFunction
    {
        #region Private Variables

        private ScriptExecutable m_scriptExecutable;
        private String m_strName;
        private List<String> m_listParameters;
        private ScriptInstruction m_scriptInstructionEntryPoint;

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs a script function for the given
        /// <see cref="ScriptExecutable"/>, with the given name, parameter
        /// count and entry point into the executable.
        /// </summary>
        /// <param name="scriptExecutable">Executable form of the
        /// <see cref="Script"/>.</param>
        /// <param name="strName">Script function name.</param>
        /// <param name="listParameters">List of parameter names.</param>
        /// <param name="scriptInstructionEntryPoint">Entry point
        /// <see cref="ScriptInstruction"/> in the executable.</param>
        public ScriptFunction(ScriptExecutable scriptExecutable,
            String strName, List<String> listParameters,
            ScriptInstruction scriptInstructionEntryPoint)
        {
            m_scriptExecutable = scriptExecutable;
            m_strName = strName;
            m_listParameters = new List<string>(listParameters);
            m_scriptInstructionEntryPoint = scriptInstructionEntryPoint;
        }

        /// <summary>
        /// Returns a string representation of the function.
        /// </summary>
        /// <returns>String representation of the function.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_strName);
            stringBuilder.Append("(");
            for (int iIndex = 0; iIndex < m_listParameters.Count; iIndex++)
            {
                if (iIndex > 0) stringBuilder.Append(", ");
                stringBuilder.Append(m_listParameters[iIndex]);
            }
            stringBuilder.Append(") [");
            stringBuilder.Append(ParameterCount.ToString("00000000"));
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// The <see cref="ScriptExecutable"/> associated with the
        /// <see cref="Script"/> that contains the function.
        /// </summary>
        public ScriptExecutable Executable
        {
            get { return m_scriptExecutable; }
        }

        /// <summary>
        /// Name of the function.
        /// </summary>
        public String Name
        {
            get { return m_strName; }
        }

        /// <summary>
        /// Number of parameters accepted by the function.
        /// </summary>
        public uint ParameterCount
        {
            get { return (uint) m_listParameters.Count; }
        }

        /// <summary>
        /// List of parameter names.
        /// </summary>
        public ReadOnlyCollection<String> Parameters
        {
            get { return m_listParameters.AsReadOnly(); }
        }

        /// <summary>
        /// Entry point into the <see cref="ScriptExecutable"/> in
        /// <see cref="ScriptInstruction"/> form.
        /// </summary>
        public ScriptInstruction EntryPoint
        {
            get { return m_scriptInstructionEntryPoint; }
            set { m_scriptInstructionEntryPoint = value; }
        }

        #endregion
    }
}
