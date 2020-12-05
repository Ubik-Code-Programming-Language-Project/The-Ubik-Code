using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Ubik.Compiler;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents the compiled execuable form of a script.
    /// </summary>
    public class ScriptExecutable
    {
        #region Private Variables

        private Script m_script;
        private List<ScriptInstruction> m_listScriptInstructions;
        private Dictionary<String, ScriptFunction> m_dictScriptFunctions;
        private VariableDictionary m_variableDictionaryScript;

        #endregion

        #region Private Methods

        private void ShiftAddressRef(Operand operand)
        {
            // ignore null operand
            if (operand == null) return;
            // ignore if any operand other than instruction ref
            if (operand.Type != OperandType.InstructionRef) return;
            ScriptInstruction scriptInstructionRef = operand.InstructionRef;

            // ignore if refererenced instruction is not NOP or DBG
            if (scriptInstructionRef.Opcode != Opcode.NOP
                && scriptInstructionRef.Opcode != Opcode.DBG) return;
            
            // shift references to next non-NOP/DBG instruction
            ScriptInstruction scriptInstructionNext = scriptInstructionRef;
            while (scriptInstructionNext.Opcode == Opcode.NOP
                || scriptInstructionNext.Opcode == Opcode.DBG)
                scriptInstructionNext = m_listScriptInstructions[
                    (int)scriptInstructionNext.Address + 1];
            operand.InstructionRef = scriptInstructionNext;
        }

        #endregion

        #region Internal Methods

        internal void EliminateNullOpcodes()
        {
            RenumberInstructions();

            // process script instructions
            foreach (ScriptInstruction scriptInstruction in m_listScriptInstructions)
            {
                ShiftAddressRef(scriptInstruction.Operand0);
                ShiftAddressRef(scriptInstruction.Operand1);
            }

            // process entry points
            foreach (ScriptFunction scriptFunction in m_dictScriptFunctions.Values)
            {
                ScriptInstruction scriptInstruction = scriptFunction.EntryPoint;

                ScriptInstruction scriptInstructionNext = scriptInstruction;
                while (scriptInstructionNext.Opcode == Opcode.NOP
                    || scriptInstructionNext.Opcode == Opcode.DBG)
                    scriptInstructionNext = m_listScriptInstructions[
                        (int)scriptInstructionNext.Address + 1];
                scriptFunction.EntryPoint = scriptInstructionNext;
            }

            // remove NOPs
            for (int iIndex = m_listScriptInstructions.Count - 1; iIndex >= 0; iIndex--)
                if (m_listScriptInstructions[iIndex].Opcode == Opcode.NOP)
                    m_listScriptInstructions.RemoveAt(iIndex);

            RenumberInstructions();
        }

        internal void RenumberInstructions()
        {
            for (int iIndex = 0; iIndex < m_listScriptInstructions.Count; iIndex++)
                m_listScriptInstructions[iIndex].Address = (uint)iIndex;
        }

        internal List<ScriptInstruction> InstructionsInternal
        {
            get { return m_listScriptInstructions; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs an executable form for the given <see cref="Script"/>.
        /// </summary>
        /// <param name="script"><see cref="Script"/> associated with the
        /// executable.</param>
        public ScriptExecutable(Script script)
        {
            m_script = script;
            m_listScriptInstructions = new List<ScriptInstruction>();
            m_dictScriptFunctions = new Dictionary<string, ScriptFunction>();
            m_variableDictionaryScript
                = VariableDictionary.CreateScriptDictionary(
                    script.Manager.GlobalDictionary);
        }

        /// <summary>
        /// Checks if the executable has a 'main' function defined.
        /// </summary>
        /// <returns>True if 'main' function defined, false otherwise.
        /// </returns>
        public bool HasMainFunction()
        {
            return m_dictScriptFunctions.ContainsKey("main");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// <see cref="Script"/> associated with the executable.
        /// </summary>
        public Script Script
        {
            get { return m_script; }
        }

        /// <summary>
        /// Instruction stream comprising the executable form.
        /// </summary>
        public ReadOnlyCollection<ScriptInstruction> Instructions
        {
            get { return m_listScriptInstructions.AsReadOnly(); }
        }

        /// <summary>
        /// <see cref="ScriptFunction"/> map indexed by function name.
        /// </summary>
        public Dictionary<String, ScriptFunction> Functions
        {
            get { return m_dictScriptFunctions; }
        }

        /// <summary>
        /// Returns the 'main' <see cref="ScriptFunction"/> if defined
        /// or throws an exception otherwise.
        /// </summary>
        public ScriptFunction MainFunction
        {
            get
            {
                if (!m_dictScriptFunctions.ContainsKey("main"))
                    throw new ParserException(
                        "The script does not contain a main(...) function.");
                return m_dictScriptFunctions["main"];
            }
        }

        /// <summary>
        /// The variable dictionary with a script scope.
        /// </summary>
        public VariableDictionary ScriptDictionary
        {
            get { return m_variableDictionaryScript; }
        }

        #endregion
    }
}
