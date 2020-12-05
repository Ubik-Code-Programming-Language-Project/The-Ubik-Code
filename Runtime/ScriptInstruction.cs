using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents a single script instruction consisting of an operator code
    /// and optionally one or more operands.
    /// </summary>
    public class ScriptInstruction
    {
        #region Private Variables

        private uint m_uiAddress;
        private Opcode m_opcode;
        private Operand m_operand0;
        private Operand m_operand1;

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs a double-operand instruction with the given opcode and
        /// two operands.
        /// </summary>
        /// <param name="opcode">Instruction opcode.</param>
        /// <param name="operand0">First instruction operand.</param>
        /// <param name="operand1">Second instruction operand.</param>
        public ScriptInstruction(Opcode opcode, Operand operand0, Operand operand1)
        {
            m_uiAddress = 0;
            m_opcode = opcode;
            m_operand0 = operand0;
            m_operand1 = operand1;
        }

        /// <summary>
        /// Constructs a single-operand instruction with the given opcode and
        /// operand.
        /// </summary>
        /// <param name="opcode">Instruction opcode.</param>
        /// <param name="operand0">Instruction operand.</param>
        public ScriptInstruction(Opcode opcode, Operand operand0)
            : this(opcode, operand0, null)
        {
        }

        /// <summary>
        /// Constructs a zero-operand instruction with the given opcode.
        /// </summary>
        /// <param name="opcode">Instruction opcode.</param>
        public ScriptInstruction(Opcode opcode)
            : this(opcode, null, null)
        {
        }

        /// <summary>
        /// Returns a string representation of the instruction.
        /// </summary>
        /// <returns>A string representation of the instruction.</returns>
        public override string ToString()
        {
            if (m_opcode == Opcode.DBG)
            {
                int iLine = (int)m_operand0.Value;
                return "Ln: " + iLine.ToString("000000") + " " + m_operand1.Value;
            }

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(m_uiAddress.ToString("[00000000]"));
            stringBuilder.Append("    ");

            stringBuilder.Append(m_opcode.ToString());
            int iOpcodeLength = m_opcode.ToString().Length;
            if (iOpcodeLength == 2)
                stringBuilder.Append("  ");
            if (iOpcodeLength == 3)
                stringBuilder.Append(" ");

            if (m_operand0 != null)
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(m_operand0.ToString());
            }

            if (m_operand1 != null)
            {
                stringBuilder.Append(", ");
                stringBuilder.Append(m_operand1.ToString());
            }

            return stringBuilder.ToString();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Numeric address of the script instruction.
        /// </summary>
        public uint Address
        {
            get { return m_uiAddress; }
            set { m_uiAddress = value; }
        }

        /// <summary>
        /// Instruction opcode.
        /// </summary>
        public Opcode Opcode
        {
            get { return m_opcode; }
            set { m_opcode = value; }
        }

        /// <summary>
        /// Optional first operand.
        /// </summary>
        public Operand Operand0
        {
            get { return m_operand0; }
            set { m_operand0 = value; }
        }

        /// <summary>
        /// Optional second operand.
        /// </summary>
        public Operand Operand1
        {
            get { return m_operand1; }
            set { m_operand1 = value; }
        }

        #endregion
    }
}
