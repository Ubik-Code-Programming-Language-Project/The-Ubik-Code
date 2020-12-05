using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Runtime;

namespace Ubik.Compiler
{
    /// <summary>
    /// Represents an external function defined in the host application
    /// that is accessible to the scripting engine.
    /// </summary>
    public class HostFunctionPrototype
    {
        #region Private Variables

        private String m_strName;
        private List<Type> m_listParameterTypes;
        private Type m_typeResult;
        private HostFunctionHandler m_hostFunctionHandler;

        #endregion

        #region Private Methods

        private void ValidateType(Type type)
        {
            if (type == null) return;

            if (type != typeof(int)
                && type != typeof(float)
                && type != typeof(bool)
                && type != typeof(String)
                && type != typeof(AssociativeArray))
                throw new ExecutionException("Type '" + type.Name
                    + "' not allowed in host function prototypes.");
        }

        private String ToString(Type type)
        {
            if (type == null) return "(any)";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(String)) return "string";
            if (type == typeof(AssociativeArray)) return "array";
            throw new UbikException(
                "Type '" + type.Name +
                "' is not allowed in host function prototypes.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs a host function prototype with the given name,
        /// list of parameter <see cref="Type"/>s and return <see cref="Type"/>.
        /// </summary>
        /// <param name="typeResult">Return <see cref="Type"/> of the host
        /// function.</param>
        /// <param name="strName">Host function name.</param>
        /// <param name="listParameterTypes">Parameter <see cref="Type"/>
        /// list passed to the host function.</param>
        public HostFunctionPrototype(
            Type typeResult, String strName,
            List<Type> listParameterTypes)
        {
            ValidateType(typeResult);
            foreach (Type typeParameter in listParameterTypes)
                ValidateType(typeParameter);

            m_typeResult = typeResult;
            m_strName = strName;
            m_listParameterTypes = listParameterTypes;

            m_hostFunctionHandler = null;
        }

        /// <summary>
        /// Constructs a parameterless host function prototype with
        /// the given name that may return a variable of any
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="strName">Host function name.</param>
        public HostFunctionPrototype(String strName)
            : this(null, strName, new List<Type>())
        {
        }

        /// <summary>
        /// Constructs a parameterless host function prototype with
        /// the given name that returns a variable of the given
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="typeResult">Return value <see cref="Type"/>.</param>
        /// <param name="strName">Host function name.</param>
        public HostFunctionPrototype(Type typeResult, String strName)
            : this(typeResult, strName, new List<Type>())
        {
        }

        /// <summary>
        /// Constructs a host function prototype with the given name,
        /// paramater <see cref="Type"/> and return <see cref="Type"/>.
        /// </summary>
        /// <param name="typeResult">Return value <see cref="Type"/>.</param>
        /// <param name="strName">Host function name.</param>
        /// <param name="typeParameter">Parameter <see cref="Type"/>.</param>
        public HostFunctionPrototype(
            Type typeResult, String strName, Type typeParameter)
            : this(typeResult, strName, new List<Type>())
        {
            m_listParameterTypes.Add(typeParameter);
        }

        /// <summary>
        /// Constructs a host function prototype with the given name,
        /// first and second paramater <see cref="Type"/>s and return
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="typeResult">Return value <see cref="Type"/>.</param>
        /// <param name="strName">Host function name.</param>
        /// <param name="typeParameter0">First parameter <see cref="Type"/>.</param>
        /// <param name="typeParameter1">Second parameter <see cref="Type"/>.</param>
        public HostFunctionPrototype(
            Type typeResult, String strName,
            Type typeParameter0, Type typeParameter1)
            : this(typeResult, strName, new List<Type>())
        {
            m_listParameterTypes.Add(typeParameter0);
            m_listParameterTypes.Add(typeParameter1);
        }

        /// <summary>
        /// Constructs a host function prototype with the given name,
        /// first, second and thirdparamater <see cref="Type"/>s and return
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="typeResult">Return value <see cref="Type"/>.</param>
        /// <param name="strName">Host function name.</param>
        /// <param name="typeParameter0">First parameter <see cref="Type"/>.</param>
        /// <param name="typeParameter1">Second parameter <see cref="Type"/>.</param>
        /// <param name="typeParameter2">Third parameter <see cref="Type"/>.</param>
        public HostFunctionPrototype(Type typeResult, String strName, Type typeParameter0,
            Type typeParameter1, Type typeParameter2)
            : this(typeResult, strName, new List<Type>())
        {
            m_listParameterTypes.Add(typeParameter0);
            m_listParameterTypes.Add(typeParameter1);
            m_listParameterTypes.Add(typeParameter2);
        }

        /// <summary>
        /// Verifies the given parameter values against the count and
        /// types of parameters defined in the function prototype. An
        /// exception is thrown if the parameters fail verification.
        /// </summary>
        /// <param name="listParameters">List of parameter values.</param>
        public void VerifyParameters(List<object> listParameters)
        {
            if (listParameters.Count != m_listParameterTypes.Count)
                throw new ExecutionException("Host function parameter count mismatch.");

            for (int iIndex = 0; iIndex < listParameters.Count; iIndex++)
            {
                // ignore untyped parameter
                if (m_listParameterTypes[iIndex] == null) continue;

                // ignore null parameter value
                if (listParameters[iIndex] == null) continue;

                Type typeExpected = m_listParameterTypes[iIndex];
                Type typeSpecified = listParameters[iIndex].GetType();
                if (typeExpected != typeSpecified)
                    throw new ExecutionException(
                        "Parameter of type '" + typeSpecified.Name 
                        + "' specified instead of type '"
                        + typeExpected.Name + "' in host function '"
                        + m_strName+"'.");
            }
        }

        /// <summary>
        /// Verifies the given result object against the return type
        /// defined by the prototype. An exception is thrown if the
        /// return object fails verification.
        /// </summary>
        /// <param name="objectResult"></param>
        public void VerifyResult(object objectResult)
        {
            if (m_typeResult == null) return;

            if (objectResult.GetType() != m_typeResult)
                throw new ExecutionException(
                    "Result of type '" + objectResult.GetType().Name 
                    + "' returned instead of type '"
                    + m_typeResult.Name + "' from host function '"
                    + m_strName+"'.");
        }

        /// <summary>
        /// Returns a string representation of the host function
        /// prototype.
        /// </summary>
        /// <returns>A string representation of the host function
        /// prototype.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ToString(m_typeResult));
            stringBuilder.Append(" host.");
            stringBuilder.Append(m_strName);
            stringBuilder.Append("(");
            for (int iIndex = 0; iIndex < m_listParameterTypes.Count; iIndex++)
            {
                if (iIndex > 0) stringBuilder.Append(", ");
                stringBuilder.Append(ToString(m_listParameterTypes[iIndex]));
            }
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Host function name.
        /// </summary>
        public String Name
        {
            get { return m_strName; }
        }

        /// <summary>
        /// Parameter type list.
        /// </summary>
        public List<Type> ParameterTypes
        {
            get { return m_listParameterTypes; }
        }

        /// <summary>
        /// Function result type.
        /// </summary>
        public Type Result
        {
            get { return m_typeResult; }
        }

        /// <summary>
        /// Host function handler. This property is set only when handlers
        /// are bound at <see cref="ScriptManager"/> level.
        /// </summary>
        public HostFunctionHandler Handler
        {
            get { return m_hostFunctionHandler; }
            internal set { m_hostFunctionHandler = value; }
        }

        #endregion
    }
}
