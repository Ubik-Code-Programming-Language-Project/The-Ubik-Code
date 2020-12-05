using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents a variable scope in dictionary form.
    /// </summary>
    public class VariableDictionary
    {
        #region Private Variables

        private VariableScope m_variableScope;
        private VariableDictionary m_variableDicitonaryGlobal;
        private VariableDictionary m_variableDicitonaryScript;
        private Dictionary<String, object> m_dictVariables;
        private Dictionary<String, object> m_dictTemporaryVariables;

        #endregion

        #region Private Methods

        private VariableDictionary(VariableScope variableScope,
            VariableDictionary variableDicitonaryGlobal,
            VariableDictionary variableDicitonaryScript)
        {
            m_variableScope = variableScope;
            m_variableDicitonaryGlobal = variableDicitonaryGlobal;
            m_variableDicitonaryScript = variableDicitonaryScript;
            m_dictVariables = new Dictionary<string, object>();
            m_dictTemporaryVariables = new Dictionary<string, object>();
        }

        #endregion

        #region Internal Methods

        internal void HideTemporaryVariables()
        {
            foreach (String strIdentifier in m_dictVariables.Keys)
                if (strIdentifier.StartsWith("__tmp"))
                    m_dictTemporaryVariables[strIdentifier]
                        = m_dictVariables[strIdentifier];

            foreach (String strIdentifier in m_dictTemporaryVariables.Keys)
                m_dictVariables.Remove(strIdentifier);
        }

        internal void ExposeTemporaryVariables()
        {
            foreach (String strIdentifier in m_dictTemporaryVariables.Keys)
                m_dictVariables[strIdentifier]
                    = m_dictTemporaryVariables[strIdentifier];

            m_dictTemporaryVariables.Clear();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Creates a variable dictionary with a global scope.
        /// </summary>
        /// <returns></returns>
        public static VariableDictionary CreateGlobalDictionary()
        {
            return new VariableDictionary(VariableScope.Global, null, null);
        }

        /// <summary>
        /// Creates a variable dictionary with a script scope
        /// using the given global variable dictionary reference.
        /// </summary>
        /// <param name="variableDicitonaryGlobal">Global variable
        /// dictionary reference.</param>
        /// <returns></returns>
        public static VariableDictionary CreateScriptDictionary(
            VariableDictionary variableDicitonaryGlobal)
        {
            return new VariableDictionary(VariableScope.Script, variableDicitonaryGlobal, null);
        }

        /// <summary>
        /// Creates a variable dictionary with a local scope
        /// using the given script variable dictionary reference.
        /// </summary>
        /// <param name="variableDicitonaryScript"></param>
        /// <returns></returns>
        public static VariableDictionary CreateLocalDictionary(
            VariableDictionary variableDicitonaryScript)
        {
            return new VariableDictionary(
                VariableScope.Local, variableDicitonaryScript.m_variableDicitonaryGlobal, variableDicitonaryScript);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears the dictionary from all the defined variables.
        /// </summary>
        public void Clear()
        {
            m_dictVariables.Clear();
            m_dictTemporaryVariables.Clear();
        }

        /// <summary>
        /// Checks if a variable with the given identifier is
        /// available in the dictionary or the related wider
        /// scopes.
        /// </summary>
        /// <param name="strIdentifier">True if the variable
        /// is declared, or false otherwise.</param>
        /// <returns></returns>
        public bool IsDeclared(String strIdentifier)
        {
            switch (m_variableScope)
            {
                case VariableScope.Global:
                    return m_dictVariables.ContainsKey(strIdentifier);
                case VariableScope.Script:
                    if (m_dictVariables.ContainsKey(strIdentifier))
                        return true;
                    else
                        return m_variableDicitonaryGlobal.IsDeclared(strIdentifier);
                case VariableScope.Local:
                    if (m_dictVariables.ContainsKey(strIdentifier))
                        return true;
                    else
                        return m_variableDicitonaryScript.IsDeclared(strIdentifier);
                default:
                    throw new ExecutionException("Variable scope '"
                        + m_variableScope + "' not supported.");
            }
        }

        /// <summary>
        /// Removes the variable with the given identifier
        /// from the dictionary.
        /// </summary>
        /// <param name="strIdentifier">Identifier of the
        /// variable to remove.</param>
        public void Remove(String strIdentifier)
        {
            m_dictVariables.Remove(strIdentifier);
        }

        /// <summary>
        /// Returns the scope of the variable with the
        /// given identifier.
        /// </summary>
        /// <param name="strIdentifier">Variable identifier.</param>
        /// <returns>Scope of the variable given by the
        /// identifier.</returns>
        public VariableScope GetScope(String strIdentifier)
        {
            switch (m_variableScope)
            {
                case VariableScope.Global:
                    if (m_dictVariables.ContainsKey(strIdentifier))
                        return m_variableScope;
                    else
                        throw new ExecutionException("Variable '" + strIdentifier + "' undefined.");
                case VariableScope.Script:
                    if (m_dictVariables.ContainsKey(strIdentifier))
                        return m_variableScope;
                    else
                        return m_variableDicitonaryGlobal.GetScope(strIdentifier);
                case VariableScope.Local:
                    if (m_dictVariables.ContainsKey(strIdentifier))
                        return m_variableScope;
                    else
                        return m_variableDicitonaryScript.GetScope(strIdentifier);
                default:
                    throw new ExecutionException("Unsupported scope: " + m_variableScope);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of identifiers for the variables
        /// stored in the dictionary.
        /// </summary>
        public ReadOnlyCollection<String> Identifiers
        {
            get
            {
                List<String> listIdentifiers
                    = new List<String>(m_dictVariables.Keys);
                return listIdentifiers.AsReadOnly();
            }
        }

        /// <summary>
        /// Identifier-based variable indexer.
        /// </summary>
        /// <param name="strIdentifier">Variable identifier.</param>
        /// <returns>Variable value.</returns>
        public object this[String strIdentifier]
        {
            get
            {
                switch (m_variableScope)
                {
                    case VariableScope.Global:
                        if (!m_dictVariables.ContainsKey(strIdentifier))
                            throw new ExecutionException(
                                "Global identifier '" + strIdentifier + "' not initialised.");
                        return m_dictVariables[strIdentifier];
                    case VariableScope.Script:
                        if (m_dictVariables.ContainsKey(strIdentifier))
                            return m_dictVariables[strIdentifier];
                        else
                            return m_variableDicitonaryGlobal[strIdentifier];
                    case VariableScope.Local:
                        if (m_dictVariables.ContainsKey(strIdentifier))
                            return m_dictVariables[strIdentifier];
                        else
                            return m_variableDicitonaryScript[strIdentifier];
                    default:
                        throw new ExecutionException("Variable scope '"
                            + m_variableScope + "' not supported.");
                }
            }
            set
            {
                if (!IsDeclared(strIdentifier))
                    m_dictVariables[strIdentifier] = value;
                else
                {
                    switch (m_variableScope)
                    {
                        case VariableScope.Global:
                            m_dictVariables[strIdentifier] = value;
                            break;
                        case VariableScope.Script:
                            if (m_variableDicitonaryGlobal.IsDeclared(strIdentifier))
                                m_variableDicitonaryGlobal[strIdentifier] = value;
                            else
                                m_dictVariables[strIdentifier] = value;
                            break;
                        case VariableScope.Local:
                            if (m_variableDicitonaryScript.IsDeclared(strIdentifier))
                                m_variableDicitonaryScript[strIdentifier] = value;
                            else
                                m_dictVariables[strIdentifier] = value;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="Scope"/> of the variable dictionary.
        /// </summary>
        public VariableScope Scope
        {
            get { return m_variableScope; }
        }

        #endregion
    }
}
