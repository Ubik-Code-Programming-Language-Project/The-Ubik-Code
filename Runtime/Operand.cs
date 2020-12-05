using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Compiler;
using Ubik.Runtime;

namespace Ubik.Runtime
{
    /// <summary>
    /// Byte code operand representation.
    /// </summary>
    public class Operand
    {
        #region Private Variables

        private OperandType m_operandType;
        private object m_objectValue;
        private object m_objectIndex;

        #endregion

        #region Private Methods

        private Operand(OperandType operandType, object objectValue, object objectIndex)
        {
            m_operandType = operandType;
            m_objectValue = objectValue;
            m_objectIndex = objectIndex;
        }

        private String ToString(object objectValue)
        {
            if (objectValue.GetType() == typeof(String))
                return "\"" + objectValue + "\"";
            else
                return objectValue.ToString();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Creates a literal operand using the given literal value.
        /// </summary>
        /// <param name="objectValue">Literal value.</param>
        /// <returns>Literal operand.</returns>
        public static Operand CreateLiteral(object objectValue)
        {
            return new Operand(OperandType.Literal, objectValue, null);
        }

        /// <summary>
        /// Creates a variable operand using the given variable identifier.
        /// </summary>
        /// <param name="strIdentifier">Variable identifier.</param>
        /// <returns>Simple variable operand</returns>
        public static Operand CreateVariable(String strIdentifier)
        {
            return new Operand(OperandType.Variable, strIdentifier, null);
        }

        /// <summary>
        /// Creates a variable reference indexed by a literal value.
        /// </summary>
        /// <param name="strIdentifier">Identifier for the indexed variable.</param>
        /// <param name="objectIndex">Literal index.</param>
        /// <returns>Literal-indexed variable operand.</returns>
        public static Operand CreateLiteralIndexedVariable(
            String strIdentifier, object objectIndex)
        {
            return new Operand(OperandType.LiteralIndexedVariable, strIdentifier, objectIndex);
        }

        /// <summary>
        /// Creates a variable reference indexed by another variable.
        /// </summary>
        /// <param name="strIdentifier">Identifier for the indexed variable.
        /// </param>
        /// <param name="strIndexIdentifier">Identifier for the index.</param>
        /// <returns>Variable-indexed variable operand.</returns>
        public static Operand CreateVariableIndexedVariable(
            String strIdentifier, String strIndexIdentifier)
        {
            return new Operand(OperandType.VariableIndexedVariable, strIdentifier, strIndexIdentifier);
        }

        /// <summary>
        /// Creates a <see cref="ScriptInstruction"/> reference.
        /// </summary>
        /// <param name="scriptInstruction">Script instruction referred by
        /// the operand.</param>
        /// <returns>Script instruction reference operand.</returns>
        public static Operand CreateInstructionRef(
            ScriptInstruction scriptInstruction)
        {
            return new Operand(OperandType.InstructionRef, scriptInstruction, null);
        }

        /// <summary>
        /// Creates a <see cref="ScriptFunction"/> reference.
        /// </summary>
        /// <param name="scriptFunction">Script function referred by
        /// the operand.</param>
        /// <returns>Script function reference operand.</returns>
        public static Operand CreateScriptFunctionRef(
            ScriptFunction scriptFunction)
        {
            return new Operand(OperandType.ScriptFunctionRef, scriptFunction, null);
        }

        /// <summary>
        /// Creates a <see cref="HostFunctionPrototype"/> reference.
        /// </summary>
        /// <param name="hostFunctionPrototype">Host function referred by
        /// the operand.</param>
        /// <returns>Host function reference operand.</returns>
        public static Operand CreateHostFunctionRef(
            HostFunctionPrototype hostFunctionPrototype)
        {
            return new Operand(OperandType.HostFunctionRef, hostFunctionPrototype, null);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a string representation of the operand.
        /// </summary>
        /// <returns>String representation of the operand.</returns>
        public override string ToString()
        {
            switch (m_operandType)
            {
                case OperandType.Literal:
                    return ToString(m_objectValue);
                case OperandType.Variable:
                    return m_objectValue.ToString();
                case OperandType.LiteralIndexedVariable:
                    return m_objectValue + "[" + ToString(m_objectIndex) + "]";
                case OperandType.VariableIndexedVariable:
                    return m_objectValue + "[" + m_objectIndex + "]";
                case OperandType.InstructionRef:
                    return "[" + ((ScriptInstruction) m_objectValue).Address.ToString("00000000") + "]";
                case OperandType.ScriptFunctionRef:
                    {
                        ScriptFunction scriptFunction = (ScriptFunction)m_objectValue;
                        return "[" + scriptFunction.EntryPoint.Address.ToString("00000000") + "] : " + scriptFunction.Name + "(...)";
                    }
                case OperandType.HostFunctionRef:
                    return m_objectValue.ToString();
                default:
                    return m_operandType.ToString();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The operand's <see cref="OperandType"/>.
        /// </summary>
        public OperandType Type
        {
            get { return m_operandType; }
            set { m_operandType = value; }
        }

        /// <summary>
        /// Value interpretation of the operand.
        /// </summary>
        public object Value
        {
            get { return m_objectValue; }
        }

        /// <summary>
        /// Index literal interpretation of the operand.
        /// </summary>
        public object IndexLiteral
        {
            get
            {
                if (m_operandType != OperandType.LiteralIndexedVariable)
                    throw new ExecutionException(
                        "Index identifier can only be accessed for literal-indexed variables.");
                return m_objectIndex;
            }
            set
            {
                if (m_operandType != OperandType.LiteralIndexedVariable)
                    throw new ExecutionException(
                        "Index identifier can only be accessed for literal-indexed variables.");
                m_objectIndex = value;
            }
        }

        /// <summary>
        /// Index variable interpretation of the operand.
        /// </summary>
        public String IndexIdentifier
        {
            get
            {
                if (m_operandType != OperandType.VariableIndexedVariable)
                    throw new ExecutionException(
                        "Index identifier can only be accessed for variable-indexed variables.");
                return (String) m_objectIndex; 
            }
            set
            {
                if (m_operandType != OperandType.VariableIndexedVariable)
                    throw new ExecutionException(
                        "Index identifier can only be accessed for variable-indexed variables.");
                m_objectIndex = value;
            }
        }

        /// <summary>
        /// <see cref="ScriptInstruction"/> interpretation of the operand.
        /// </summary>
        public ScriptInstruction InstructionRef
        {
            get
            {
                if (m_operandType != OperandType.InstructionRef)
                    throw new ParserException(
                        "Operand is not an instruction reference.");

                return (ScriptInstruction) m_objectValue;
            }
            set
            {
                if (m_operandType != OperandType.InstructionRef)
                    throw new ParserException(
                        "Operand is not an instruction reference.");

                m_objectValue = value;
            }
        }

        /// <summary>
        /// <see cref="ScriptFunction"/> interpretation of the operand.
        /// </summary>
        public ScriptFunction ScriptFunctionRef
        {
            get
            {
                if (m_operandType != OperandType.ScriptFunctionRef)
                    throw new ParserException(
                        "Operand is not a script function reference.");

                return (ScriptFunction)m_objectValue;
            }
            set
            {
                if (m_operandType != OperandType.ScriptFunctionRef)
                    throw new ParserException(
                        "Operand is not a script function reference.");

                m_objectValue = value;
            }
        }

        /// <summary>
        /// <see cref="HostFunctionPrototype"/> interpretation of the
        /// operand.
        /// </summary>
        public HostFunctionPrototype HostFunctionRef
        {
            get
            {
                if (m_operandType != OperandType.HostFunctionRef)
                    throw new ParserException(
                        "Operand is not a host function reference.");

                return (HostFunctionPrototype)m_objectValue;
            }
            set
            {
                if (m_operandType != OperandType.HostFunctionRef)
                    throw new ParserException(
                        "Operand is not a host function reference.");

                m_objectValue = value;
            }
        }

        #endregion
    }
}
