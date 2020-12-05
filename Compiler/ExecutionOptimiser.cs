using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Runtime;

namespace Ubik.Compiler
{
    internal class ExecutionOptimiser
    {
        #region Private Variables

        private ScriptExecutable m_scriptExecutable;
        private bool m_bOptimiserInfo;
        private bool m_bOptimisationComplete;

        #endregion

        #region Private Methods

        private bool ThreeInstructionsAvailable(int iIndex)
        {
            return iIndex < m_scriptExecutable.InstructionsInternal.Count - 2;
        }

        private bool TwoInstructionsAvailable(int iIndex)
        {
            return iIndex < m_scriptExecutable.InstructionsInternal.Count - 1;
        }

        private bool IsTemporaryVariable(String strIdentifier)
        {
            return strIdentifier.StartsWith("__tmp");
        }

        private bool IsTemporaryVariable(Operand operand)
        {
            if (operand.Type != OperandType.Variable)
                return false;

            return IsTemporaryVariable(operand.Value.ToString());
        }

        private bool IsTemporaryVariableIndex(Operand operand)
        {
            if (operand.Type != OperandType.VariableIndexedVariable)
                return false;

            return operand.IndexIdentifier.StartsWith("__tmp");
        }

        private bool IsUnaryOperator(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.INC:
                case Opcode.DEC:
                case Opcode.NEG:
                case Opcode.NOT:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsBinaryOperator(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.ADD:
                case Opcode.SUB:
                case Opcode.MUL:
                case Opcode.DIV:
                case Opcode.POW:
                case Opcode.CEQ:
                case Opcode.CNE:
                case Opcode.CG: 
                case Opcode.CGE:
                case Opcode.CL: 
                case Opcode.CLE:
                case Opcode.OR: 
                case Opcode.AND:
                    return true;
                default:
                    return false;
            }
        }

        private void InsertOptimiserInfo(int iIndex, String strInfo)
        {
            if (!m_bOptimiserInfo) return;

            ScriptInstruction scriptInstruction
                = new ScriptInstruction(Opcode.DBG,
                    Operand.CreateLiteral(0),
                    Operand.CreateLiteral("OPTIMIZER: " + strInfo));
            m_scriptExecutable.InstructionsInternal.Insert(
                iIndex, scriptInstruction);
        }

        private void OptimiseBinaryExpressionEvaluation(int iIndex)
        {
            // MOV  tmp0, (src0)        MOV  tmp0, (src0)
            // MOV  tmp1, (src1)   =>   (OP) tmp0, (src1)
            // (OP) tmp0, tmp1          NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];
            ScriptInstruction scriptInstruction2
                = listInstructions[iIndex + 2];

            // opcode matches
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (scriptInstruction1.Opcode != Opcode.MOV) return;
            if (!IsBinaryOperator(scriptInstruction2.Opcode))
                return;

            // specific variables are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction2.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction2.Operand1)) return;

            // different tmp ids
            if (scriptInstruction0.Operand0.Value.ToString()
                == scriptInstruction1.Operand0.Value.ToString())
                return;

            // tmp0 positions
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction2.Operand0.Value.ToString())
                return;

            // tmp1 positions
            if (scriptInstruction1.Operand0.Value.ToString()
                != scriptInstruction2.Operand1.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Binary Expression Evaluation");

            scriptInstruction1.Opcode = scriptInstruction2.Opcode;
            scriptInstruction1.Operand0 = scriptInstruction0.Operand0;
            scriptInstruction2.Opcode = Opcode.NOP;
            scriptInstruction2.Operand0 = null;
            scriptInstruction2.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseUnaryExpressionAssignment(int iIndex)
        {
            // MOV  tmp, src           MOV  dest, src
            // (OP) tmp         =>     (OP) dest
            // MOV  dest, tmp          NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];
            ScriptInstruction scriptInstruction2
                = listInstructions[iIndex + 2];

            // opcode matches
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (!IsUnaryOperator(scriptInstruction1.Opcode)) return;
            if (scriptInstruction2.Opcode != Opcode.MOV) return;

            // specific variables are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction2.Operand1)) return;

            // same tmp ids
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand0.Value.ToString())
                return;
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction2.Operand1.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Unary Expression Assignment");

            scriptInstruction0.Operand0 = scriptInstruction2.Operand0;
            scriptInstruction1.Operand0 = scriptInstruction2.Operand0;
            scriptInstruction2.Opcode = Opcode.NOP;
            scriptInstruction2.Operand0 = null;
            scriptInstruction2.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseBinaryExpressionAssignment(int iIndex)
        {
            // MOV  tmp, (src0)        MOV  dest, (src0)
            // (OP) tmp, (src1)   =>   (OP) dest, (src1)
            // MOV  dest, tmp          NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];
            ScriptInstruction scriptInstruction2
                = listInstructions[iIndex + 2];

            // opcode matches
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (!IsBinaryOperator(scriptInstruction1.Opcode)) return;
            if (scriptInstruction2.Opcode != Opcode.MOV) return;

            // specific variables are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction2.Operand1)) return;

            // same tmp ids
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand0.Value.ToString())
                return;
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction2.Operand1.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Binary Expression Assignment");

            scriptInstruction0.Operand0 = scriptInstruction2.Operand0;
            scriptInstruction1.Operand0 = scriptInstruction2.Operand0;
            scriptInstruction2.Opcode = Opcode.NOP;
            scriptInstruction2.Operand0 = null;
            scriptInstruction2.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseInstructionTriples(int iIndex)
        {
            OptimiseUnaryExpressionAssignment(iIndex);

            OptimiseBinaryExpressionEvaluation(iIndex);
            OptimiseBinaryExpressionAssignment(iIndex);
        }

        private void OptimisePushOperation(int iIndex)
        {
            // MOV  tmp0, (src)   =>   PUSH (src)
            // PUSH tmp0               NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (scriptInstruction1.Opcode != Opcode.PUSH) return;

            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand0)) return;

            // must be same tmps
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand0.Value.ToString())
                return;
            
            InsertOptimiserInfo(iIndex, "Push Operation");

            scriptInstruction0.Opcode = Opcode.PUSH;
            scriptInstruction0.Operand0 = scriptInstruction0.Operand1;
            scriptInstruction0.Operand1 = null;
            scriptInstruction1.Opcode = Opcode.NOP;
            scriptInstruction1.Operand0 = null;
            scriptInstruction1.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimisePopOperation(int iIndex)
        {
            // POP tmp0         =>   POP dest
            // MOV dest, tmp0        NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            if (scriptInstruction0.Opcode != Opcode.POP) return;
            if (scriptInstruction1.Opcode != Opcode.MOV) return;

            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand1)) return;

            // must be same tmps
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand1.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Pop Operation");

            scriptInstruction0.Opcode = Opcode.POP;
            scriptInstruction0.Operand0 = scriptInstruction1.Operand0;
            scriptInstruction1.Opcode = Opcode.NOP;
            scriptInstruction1.Operand0 = null;
            scriptInstruction1.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseLiteralAssignment(int iIndex)
        {
            // MOV tmp0, (src)   =>   MOV dest, (src)
            // MOV dest, tmp0         NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            // opcode check
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (scriptInstruction1.Opcode != Opcode.MOV) return;

            // specific vars are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand1)) return;

            // must be same tmps
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand1.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Literal Assignment");

            scriptInstruction0.Operand0 = scriptInstruction1.Operand0;
            scriptInstruction1.Opcode = Opcode.NOP;
            scriptInstruction1.Operand0 = null;
            scriptInstruction1.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseConditionalJumps(int iIndex)
        {
            // MOV  tmp, cmd    =>   JT/F cmd, addr
            // JT/F tmp, addr        NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            // opcode check
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (scriptInstruction1.Opcode != Opcode.JT
                && scriptInstruction1.Opcode != Opcode.JF) return;

            // specific vars are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariable(scriptInstruction1.Operand0)) return;

            // must be same tmps
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand0.Value.ToString())
                return;

            InsertOptimiserInfo(iIndex, "Conditional Jump Expressions");

            scriptInstruction0.Opcode = scriptInstruction1.Opcode;
            scriptInstruction0.Operand0 = scriptInstruction0.Operand1;
            scriptInstruction0.Operand1 = scriptInstruction1.Operand1;
            scriptInstruction1.Opcode = Opcode.NOP;
            scriptInstruction1.Operand0 = null;
            scriptInstruction1.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseArrayIndices(int iIndex)
        {
            // MOV tmp, (idx)         =>   MOV var(idx), (expr)
            // MOV var[tmp], (expr)        NOP

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            // opcode check
            if (scriptInstruction0.Opcode != Opcode.MOV) return;
            if (scriptInstruction1.Opcode != Opcode.MOV) return;

            // verify idx is simple variable
            if (scriptInstruction0.Operand1.Type != OperandType.Variable) return;

            // check position of dest array
            if (scriptInstruction1.Operand0.Type
                != OperandType.VariableIndexedVariable) return;

            // specific vars are tmps
            if (!IsTemporaryVariable(scriptInstruction0.Operand0)) return;
            if (!IsTemporaryVariableIndex(scriptInstruction1.Operand0)) return;

            // must be same tmps
            if (scriptInstruction0.Operand0.Value.ToString()
                != scriptInstruction1.Operand0.IndexIdentifier)
                return;

            InsertOptimiserInfo(iIndex, "Array Index");

            scriptInstruction0.Operand0 = scriptInstruction1.Operand0;
            scriptInstruction0.Operand0.IndexIdentifier
                = scriptInstruction0.Operand1.Value.ToString();
            scriptInstruction0.Operand1 = scriptInstruction1.Operand1;
            scriptInstruction1.Opcode = Opcode.NOP;
            scriptInstruction1.Operand0 = null;
            scriptInstruction1.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void EliminateSequentialJumps(int iIndex)
        {
            // 0000 JMP  [0001]    =>   0000 NOP
            // 0001 ...                 0001 ...

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction0
                = listInstructions[iIndex];
            ScriptInstruction scriptInstruction1
                = listInstructions[iIndex + 1];

            // opcode check
            if (scriptInstruction0.Opcode != Opcode.JMP) return;

            // address must refer to next instruction
            if (scriptInstruction0.Operand0.InstructionRef
                != scriptInstruction1) return;

            InsertOptimiserInfo(iIndex, "Sequentual Jump Elimination");

            scriptInstruction0.Opcode = Opcode.NOP;
            scriptInstruction0.Operand0 = null;
            scriptInstruction0.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseInstructionPairs(int iIndex)
        {
            OptimisePushOperation(iIndex);
            OptimisePopOperation(iIndex);
            OptimiseLiteralAssignment(iIndex);
            OptimiseConditionalJumps(iIndex);
            OptimiseArrayIndices(iIndex);
            EliminateSequentialJumps(iIndex);
        }

        private void EliminateSelfAssignments(int iIndex)
        {
            // MOV v, v             => NOP
            // or
            // MOV v1[v2], v1[v2]   => NOP
            // or
            // MOV v[lit], v[lit]   => NOP

            ScriptInstruction scriptInstruction
                = m_scriptExecutable.InstructionsInternal[iIndex];

            // must be MOV
            if (scriptInstruction.Opcode != Opcode.MOV) return; 

            // dest and source must be same type
            Operand operand0 = scriptInstruction.Operand0;
            Operand operand1 = scriptInstruction.Operand1;
            if (operand0.Type != operand1.Type) return;

            // dest and source must be simple or array ref
            if (operand0.Type != OperandType.Variable
                && operand0.Type != OperandType.LiteralIndexedVariable
                && operand1.Type != OperandType.VariableIndexedVariable)
                return;

            // simple or array ids must be same
            if (operand0.Value.ToString() != operand1.Value.ToString())
                return;

            // match indices if applicable
            switch (operand0.Type)
            {
                case OperandType.LiteralIndexedVariable:
                    if (operand0.IndexLiteral.ToString()
                        != operand1.IndexLiteral.ToString())
                        return;
                    break;
                case OperandType.VariableIndexedVariable:
                    if (operand0.IndexIdentifier != operand1.IndexIdentifier)
                        return;
                    break;
            }

            InsertOptimiserInfo(iIndex, "Self Assignment Removal");

            scriptInstruction.Opcode = Opcode.NOP;
            scriptInstruction.Operand0 = null;
            scriptInstruction.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseConstantConditionalJumps(int iIndex)
        {
            // JT true,  addr   =>   JMP addr

            // JT false, addr   =>   NOP

            // JF true,  addr   =>   NOP

            // JF false, addr   =>   JMP addr

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            ScriptInstruction scriptInstruction
                = listInstructions[iIndex];

            // opcode check
            if (scriptInstruction.Opcode != Opcode.JT
                && scriptInstruction.Opcode != Opcode.JF) return;

            // condition var is literal
            if (scriptInstruction.Operand0.Type != OperandType.Literal) return;

            // condition literal is boolean
            if (scriptInstruction.Operand0.Value.GetType() != typeof(bool)) return;
            bool bCondition = (bool) scriptInstruction.Operand0.Value;

            InsertOptimiserInfo(iIndex, "Constant Conditional Jump");

            Opcode opcodeJump = scriptInstruction.Opcode;
            if ((opcodeJump == Opcode.JT && bCondition)
                || (opcodeJump == Opcode.JF && !bCondition))
            {
                scriptInstruction.Opcode = Opcode.JMP;
                scriptInstruction.Operand0 = scriptInstruction.Operand1;
                scriptInstruction.Operand1 = null;
            }
            else
            {
                scriptInstruction.Opcode = Opcode.NOP;
                scriptInstruction.Operand0 = null;
                scriptInstruction.Operand1 = null;
            }

            m_bOptimisationComplete = false;
        }

        private void OptimiseIncrementsAndDecrements(int iIndex)
        {
            // ADD var, 1   =>   INC var

            // ADD var, -1  =>   DEC var

            // SUB var, 1   =>   DEC var

            // SUB var, -1  =>   INC var

            ScriptInstruction scriptInstruction
                = m_scriptExecutable.InstructionsInternal[iIndex];

            // must be ADD or SUB
            if (scriptInstruction.Opcode != Opcode.ADD
                && scriptInstruction.Opcode != Opcode.SUB) return;

            // destination must be simple or array variable
            if (scriptInstruction.Operand0.Type != OperandType.Variable
                && scriptInstruction.Operand0.Type != OperandType.LiteralIndexedVariable
                && scriptInstruction.Operand0.Type != OperandType.VariableIndexedVariable)
                return;

            // source must be literal
            if (scriptInstruction.Operand1.Type != OperandType.Literal) return;

            // source literal must be numeric
            object objectLiteral = scriptInstruction.Operand1.Value;
            Type typeLiteral = objectLiteral.GetType();
            if (typeLiteral != typeof(int) && typeLiteral != typeof(float)) return;

            // if int literal, value must be 1 or -1
            if (typeLiteral == typeof(int) && Math.Abs((int)objectLiteral) != 1) return;

            // if float literal, value must be 1.0 or -1.0
            if (typeLiteral == typeof(float) && Math.Abs((float)objectLiteral) != 1.0f) return;

            InsertOptimiserInfo(iIndex, "Increment/Decrement Optimisation");

            float fValue = 0.0f;

            if (typeLiteral == typeof(int))
                // int
                fValue = (float)(int)objectLiteral;
            else
                // floar
                fValue = (float)objectLiteral;


            Opcode opcodeOld = scriptInstruction.Opcode;
            bool bIncrement = opcodeOld == Opcode.ADD && fValue > 0.0f
                || opcodeOld == Opcode.SUB && fValue < 0.0f;

            scriptInstruction.Opcode = bIncrement ? Opcode.INC : Opcode.DEC;
            scriptInstruction.Operand1 = null;

            m_bOptimisationComplete = false;
        }

        private void OptimiseSingleInstructions(int iIndex)
        {
            EliminateSelfAssignments(iIndex);
            OptimiseConstantConditionalJumps(iIndex);
            OptimiseIncrementsAndDecrements(iIndex);
        }

        private void TraverseActiveInstructions(
            Dictionary<ScriptInstruction, bool> dictScriptInstructionsActive,
            int iNextInstuction)
        {
            while (true)
            {
                ScriptInstruction scriptInstruction
                    = m_scriptExecutable.InstructionsInternal[iNextInstuction];
                if (dictScriptInstructionsActive.ContainsKey(scriptInstruction))
                    return;

                dictScriptInstructionsActive[scriptInstruction] = true;

                switch (scriptInstruction.Opcode)
                {
                    case Opcode.JMP:
                        iNextInstuction
                            = (int) scriptInstruction.Operand0.InstructionRef.Address;
                        break;
                    case Opcode.JT:
                    case Opcode.JF:
                        TraverseActiveInstructions(dictScriptInstructionsActive,
                            (int)scriptInstruction.Operand1.InstructionRef.Address);
                        ++iNextInstuction;
                        break;
                    case Opcode.CALL:
                    case Opcode.THRD:
                        TraverseActiveInstructions(dictScriptInstructionsActive,
                            (int)scriptInstruction.Operand0.ScriptFunctionRef.EntryPoint.Address);
                        ++iNextInstuction;
                        break;
                    case Opcode.RET:
                        return;
                    default:
                        ++iNextInstuction;
                        break;
                }
            }
        }

        private void EliminateDeadCode()
        {
            m_scriptExecutable.RenumberInstructions();

            Dictionary<ScriptInstruction, bool> dictScriptInstructionsActive
                = new Dictionary<ScriptInstruction, bool>();

            foreach (ScriptFunction scriptFunction in m_scriptExecutable.Functions.Values)
            {
                TraverseActiveInstructions(dictScriptInstructionsActive,
                    (int) scriptFunction.EntryPoint.Address);
            }

            foreach (ScriptInstruction scriptInstruction in m_scriptExecutable.InstructionsInternal)
            {
                if (scriptInstruction.Opcode == Opcode.DBG) continue;
                if (scriptInstruction.Opcode == Opcode.DCG) continue;
                if (scriptInstruction.Opcode == Opcode.DCL) continue;

                if (!dictScriptInstructionsActive.ContainsKey(scriptInstruction))
                {
                    scriptInstruction.Opcode = Opcode.NOP;
                    scriptInstruction.Operand0 = null;
                    scriptInstruction.Operand1 = null;

                    m_bOptimisationComplete = false;
                }
            }
        }

        private void EliminateUnusedTempVariables()
        {
            Dictionary<String, ScriptInstruction> dictTempVariableAssignments
                = new Dictionary<string, ScriptInstruction>();

            foreach (ScriptInstruction scriptInstruction in m_scriptExecutable.InstructionsInternal)
            {
                Opcode opcode = scriptInstruction.Opcode;
                if ((opcode == Opcode.MOV || opcode == Opcode.CLRA)
                    && IsTemporaryVariable(scriptInstruction.Operand0))
                {
                    dictTempVariableAssignments[scriptInstruction.Operand0.Value.ToString()]
                        = scriptInstruction;
                }

                Operand operandDest = scriptInstruction.Operand0;
                if (operandDest == null) continue;

                // do not eliminate tmp if used in PUSH, JT, JF instruction
                if (opcode != Opcode.MOV && IsTemporaryVariable(operandDest))
                    dictTempVariableAssignments.Remove(
                        operandDest.Value.ToString());

                // do not eliminate tmp if used with an index in an assignment
                if ((operandDest.Type == OperandType.LiteralIndexedVariable
                        || operandDest.Type == OperandType.VariableIndexedVariable)
                    && IsTemporaryVariable(operandDest.Value.ToString()))
                    dictTempVariableAssignments.Remove(
                        operandDest.Value.ToString());

                // do not eliminate tmp if used as an index in an assignment
                if (operandDest.Type == OperandType.VariableIndexedVariable
                    && IsTemporaryVariable(operandDest.IndexIdentifier))
                    dictTempVariableAssignments.Remove(
                        operandDest.IndexIdentifier);

                // do not eliminate tmp if used as source id or index
                Operand operandSource = scriptInstruction.Operand1;
                if (operandSource == null) continue;
                switch (operandSource.Type)
                {
                    case OperandType.Literal: continue;
                    case OperandType.Variable:
                    case OperandType.LiteralIndexedVariable:
                        if (IsTemporaryVariable(operandSource.Value.ToString()))
                            dictTempVariableAssignments.Remove(
                                operandSource.Value.ToString());
                        break;
                    case OperandType.VariableIndexedVariable:
                        if (IsTemporaryVariable(operandSource.Value.ToString()))
                            dictTempVariableAssignments.Remove(
                                operandSource.Value.ToString());
                        if (IsTemporaryVariable(operandSource.IndexIdentifier))
                            dictTempVariableAssignments.Remove(
                                operandSource.IndexIdentifier);
                        break;
                }
            }

            // remove remaining unused assignments
            foreach (ScriptInstruction scriptInstruction
                in dictTempVariableAssignments.Values)
            {
                String strOldInstruction = scriptInstruction.ToString();

                scriptInstruction.Opcode = Opcode.NOP;
                scriptInstruction.Operand0 = null;
                scriptInstruction.Operand1 = null;
            }

            if (dictTempVariableAssignments.Count > 0)
                m_bOptimisationComplete = false;
            //}
        }

        #endregion

        #region Public Methods

        public ExecutionOptimiser(ScriptExecutable scriptExecutable)
        {
            m_bOptimiserInfo = false;
            m_scriptExecutable = scriptExecutable;
        }

        public void Optimise()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            // perform peep-hole optimisations
            m_bOptimisationComplete = false;
            while (!m_bOptimisationComplete)
            {
                m_bOptimisationComplete = true;

                for (int iIndex = 0; iIndex < listInstructions.Count; iIndex++)
                {
                    // triple instruction optimisations
                    if (ThreeInstructionsAvailable(iIndex))
                        OptimiseInstructionTriples(iIndex);

                    // double instruction optimisations
                    if (TwoInstructionsAvailable(iIndex))
                        OptimiseInstructionPairs(iIndex);

                    // single istruction optimisations
                    OptimiseSingleInstructions(iIndex);

                    // eliminate null opcodes at each iteration
                    m_scriptExecutable.EliminateNullOpcodes();
                }
            }


            // eliminate unused temp vars
            EliminateUnusedTempVariables();

            // eliminate dead code
            EliminateDeadCode();

            m_scriptExecutable.EliminateNullOpcodes();
        }

        public bool OptimiserInfo
        {
            get { return m_bOptimiserInfo; }
            set { m_bOptimiserInfo = value; }
        }

        #endregion
    }
}
