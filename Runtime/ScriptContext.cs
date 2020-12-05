using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Ubik.Compiler;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents one of potentially many executable instances of a
    /// particular script.
    /// </summary>
    public class ScriptContext
    {
        #region Private Classes

        private class FunctionFrame
        {
            public ScriptFunction m_scriptFunction;
            public VariableDictionary m_variableDictionary;
            public int m_iNextInstruction;
        }

        #endregion

        #region Private Variables

        private ScriptFunction m_scriptFunction;
        private Script m_script;
        private ScriptExecutable m_scriptExecutable;
        private Stack<FunctionFrame> m_stackFunctionFrames;
        private Stack<object> m_stackParameters;
        private Dictionary<object, ScriptInstruction> m_dictLocks;
        private List<ScriptContext> m_listThreads;
        private ScriptInstruction m_scriptInstruction;
        private VariableDictionary m_variableDictionaryLocal;
        private bool m_bInterruptOnHostfunctionCall;
        private bool m_bInterruped;
        private bool m_bTerminated;

        #endregion

        #region Private Methods

        private HostFunctionHandler m_scriptHandler;

        private object ResolveOperand(Operand operand)
        {
            object objectSource = null;
            switch (operand.Type)
            {
                case OperandType.Literal:
                    return operand.Value;
                case OperandType.Variable:
                    return m_variableDictionaryLocal[(string) operand.Value];
                case OperandType.LiteralIndexedVariable:
                    objectSource
                        = m_variableDictionaryLocal[(String)operand.Value];

                    if (objectSource.GetType() == typeof(AssociativeArray))
                    {
                        AssociativeArray associativeArray
                            = (AssociativeArray)objectSource;
                        object objectValue = associativeArray[operand.IndexLiteral];
                        return objectValue;
                        //return associativeArray[operand.IndexLiteral];
                    }
                    else if (objectSource.GetType() == typeof(String))
                    {
                        String strSource = (String)objectSource;
                        object objectIndex = operand.IndexLiteral;

                        // handle string length
                        if (objectIndex.GetType() == typeof(String)
                            && ((String)objectIndex) == "length")
                            return strSource.Length;

                        // otherwise, must be a char index
                        if (objectIndex.GetType() != typeof(int))
                            throw new ExecutionException(
                                "Only integers are allowed for string indexing.");
                        return strSource[(int)objectIndex] + "";
                    }
                    else
                        throw new ExecutionException(
                            "Only associative arrays and strings can be indexed.");
                case OperandType.VariableIndexedVariable:
                    objectSource
                        = m_variableDictionaryLocal[(String)operand.Value];

                    if (objectSource.GetType() == typeof(AssociativeArray))
                    {
                        AssociativeArray associativeArray
                            = (AssociativeArray)objectSource;
                        object objectIndex
                            = m_variableDictionaryLocal[operand.IndexIdentifier];
                        return associativeArray[objectIndex];
                    }
                    else if (objectSource.GetType() == typeof(String))
                    {
                        String strSource = (String)objectSource;
                        object objectIndex
                            = m_variableDictionaryLocal[operand.IndexIdentifier];
                        if (objectIndex.GetType() != typeof(int))
                            throw new ExecutionException(
                                "Only integers are allowed for string indexing.");
                        return strSource[(int)objectIndex] + "";
                    }
                    else
                        throw new ExecutionException(
                            "Only associative arrays and strings can be indexed.");
                default:
                    throw new ExecutionException(
                        "Cannot resolve operand type '"+ operand.Type + "'.");
            }
        }

        private void AssignVariable(Operand operandDest, object objectValue)
        {
            // dest id
            String strIdentifierDest = (String)operandDest.Value;

            switch (operandDest.Type)
            {
                case OperandType.Variable:
                    // copy value (or assoc array ref)
                    m_variableDictionaryLocal[strIdentifierDest]
                        = objectValue;
                    break;
                case OperandType.LiteralIndexedVariable:
                case OperandType.VariableIndexedVariable:
                    // copy into array element
                    AssociativeArray associativeArray = null;

                    // dest value
                    object objectValueDest = null;
                    if (m_variableDictionaryLocal.IsDeclared(strIdentifierDest))
                        objectValueDest
                            = m_variableDictionaryLocal[strIdentifierDest];
                    else
                        objectValueDest = NullReference.Instance;

                    if (objectValueDest.GetType() != typeof(AssociativeArray))
                       // if old type not array and non-null assigned, throw error
                        throw new ExecutionException("Indexed destination "
                            + operandDest + " is not an associative array.");
                    else
                        associativeArray = (AssociativeArray)objectValueDest;

                    // assign into array
                    if (operandDest.Type == OperandType.LiteralIndexedVariable)
                    {
                        // literal-indexed array
                        object objectIndex = operandDest.IndexLiteral;
                        associativeArray[objectIndex] = objectValue;
                    }
                    else
                    {
                        // variable-indexed arrau
                        String strIdentifierIndex = operandDest.IndexIdentifier;
                        object objectIndex = m_variableDictionaryLocal[strIdentifierIndex];
                        associativeArray[objectIndex] = objectValue;
                    }
                    break;
                case OperandType.Literal:
                    throw new ExecutionException("MOV destination operand cannot be a literal.");
            }
        }

        private void ProcessArithmeticInstruction()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);
            // source value
            object objectValueSource
                = ResolveOperand(m_scriptInstruction.Operand1);

            Type typeDest = objectValueDest.GetType();
            Type typeSource = objectValueSource.GetType();

            // handle arrays and string concatenation
            if (m_scriptInstruction.Opcode == Opcode.ADD)
            {
                if (typeDest == typeof(String))
                {
                    AssignVariable(m_scriptInstruction.Operand0,
                        objectValueDest.ToString() + objectValueSource.ToString());
                    return;
                }

                if (typeDest == typeof(AssociativeArray))
                {
                    ((AssociativeArray)objectValueDest).Add(objectValueSource);
                    return;
                }
            }

            // handle array and string subtraction
            if (m_scriptInstruction.Opcode == Opcode.SUB)
            {
                if (typeDest == typeof(String))
                {
                    AssignVariable(m_scriptInstruction.Operand0,
                        objectValueDest.ToString().Replace(objectValueSource.ToString(), ""));
                    return;
                }
                if (typeDest == typeof(AssociativeArray))
                {
                    ((AssociativeArray)objectValueDest).Subtract(objectValueSource);
                    return;
                }
            }

            float fValueDest = 0.0f;
            float fValueSource = 0.0f;
            float fResult = 0.0f;

            if (typeDest == typeof(int))
                fValueDest = (float)(int)objectValueDest;
            else if (typeDest == typeof(float))
                fValueDest = (float)objectValueDest;
            else
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in arithmetic instructions.");

            if (typeSource == typeof(int))
                fValueSource = (float)(int)objectValueSource;
            else if (typeSource == typeof(float))
                fValueSource = (float)objectValueSource;
            else
                throw new ExecutionException(
                    "Values of type '" + typeSource.Name
                    + "' cannot be used in arithmetic instructions.");

            switch (m_scriptInstruction.Opcode)
            {
                case Opcode.ADD: fResult = fValueDest + fValueSource; break;
                case Opcode.SUB: fResult = fValueDest - fValueSource; break;
                case Opcode.MUL: fResult = fValueDest * fValueSource; break;
                case Opcode.DIV: fResult = fValueDest / fValueSource; break;
                case Opcode.POW: fResult = (float)Math.Pow(fValueDest, fValueSource); break;
                case Opcode.MOD: fResult = fValueDest % fValueSource; break;
                default:
                    throw new ExecutionException(
                        "Invalid arithmetic instruction '"
                        + m_scriptInstruction.Opcode + "'.");
            }

            if (typeDest == typeof(int) && typeSource == typeof(int))
                AssignVariable(m_scriptInstruction.Operand0, (int)fResult);
            else
                AssignVariable(m_scriptInstruction.Operand0, fResult);
        }

        private void ProcessRelationalInstruction()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);
            // source value
            object objectValueSource
                = ResolveOperand(m_scriptInstruction.Operand1);

            Type typeDest = objectValueDest.GetType();
            Type typeSource = objectValueSource.GetType();

            bool bResult = false;

            // handle null comparisons
            if (typeDest == typeof(NullReference) || typeSource == typeof(NullReference))
            {
                switch (m_scriptInstruction.Opcode)
                {
                    case Opcode.CEQ: bResult = objectValueDest == objectValueSource; break;
                    case Opcode.CNE: bResult = objectValueDest != objectValueSource; break;
                    default:
                        throw new ExecutionException(
                            "Only CEQ, CNE and CNL instructions may reference NULL values.");
                }
                AssignVariable(m_scriptInstruction.Operand0, bResult);
                return;
            }

            // handle string comparisons
            if (typeDest == typeof(String) && typeSource == typeof(String))
            {
                String strDest = (String) objectValueDest;
                String strSource = (String) objectValueSource;
                switch (m_scriptInstruction.Opcode)
                {
                    case Opcode.CEQ: bResult = strDest == strSource; break;
                    case Opcode.CNE: bResult = strDest != strSource; break;
                    case Opcode.CG: bResult = strDest.CompareTo(strSource) > 0; break;
                    case Opcode.CGE: bResult = strDest.CompareTo(strSource) >= 0; break;
                    case Opcode.CL: bResult = strDest.CompareTo(strSource) < 0; break;
                    case Opcode.CLE: bResult = strDest.CompareTo(strSource) <= 0; break;
                }
                AssignVariable(m_scriptInstruction.Operand0, bResult);
                return;
            }

            float fValueDest = 0.0f;
            float fValueSource = 0.0f;

            if (typeDest == typeof(int))
                fValueDest = (float)(int)objectValueDest;
            else if (typeDest == typeof(float))
                fValueDest = (float)objectValueDest;
            else
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in relational instructions.");

            if (typeSource == typeof(int))
                fValueSource = (float)(int)objectValueSource;
            else if (typeSource == typeof(float))
                fValueSource = (float)objectValueSource;
            else
                throw new ExecutionException(
                    "Values of type '" + typeSource.Name
                    + "' cannot be used in relational instructions.");

            switch (m_scriptInstruction.Opcode)
            {
                case Opcode.CEQ: bResult = fValueDest == fValueSource; break;
                case Opcode.CNE: bResult = fValueDest != fValueSource; break;
                case Opcode.CG: bResult = fValueDest > fValueSource; break;
                case Opcode.CGE: bResult = fValueDest >= fValueSource; break;
                case Opcode.CL: bResult = fValueDest < fValueSource; break;
                case Opcode.CLE: bResult = fValueDest <= fValueSource; break;
                default:
                    throw new ExecutionException(
                        "Invalid relational instruction '"
                        + m_scriptInstruction.Opcode + "'.");
            }

            AssignVariable(m_scriptInstruction.Operand0, bResult);
        }

        private void ProcessLogicalInstruction()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);
            // source value
            object objectValueSource
                = ResolveOperand(m_scriptInstruction.Operand1);

            Type typeDest = objectValueDest.GetType();
            Type typeSource = objectValueSource.GetType();

            if (typeDest != typeof(bool))
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in logical expressions.");

            if (typeSource != typeof(bool))
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in logical expressions.");

            bool bResult = false;
            bool bValueDest = (bool)objectValueDest;
            bool bValueSource = (bool)objectValueSource;

            switch (m_scriptInstruction.Opcode)
            {
                case Opcode.AND: bResult = bValueDest && bValueSource; break;
                case Opcode.OR: bResult = bValueDest || bValueSource; break;
                default:
                    throw new ExecutionException(
                        "Invalid relational instruction '"
                        + m_scriptInstruction.Opcode + "'.");
            }

            AssignVariable(m_scriptInstruction.Operand0, bResult);
        }

        private void ProcessIterator(AssociativeArray associativeArray)
        {
            if (associativeArray.Count == 0) return;

            object objectIterator = ResolveOperand(m_scriptInstruction.Operand0);

            bool bFoundKey = false;
            object objectNextKey = null;
            foreach (object objectKey in associativeArray.Keys)
            {
                if (bFoundKey)
                {
                    objectNextKey = objectKey;
                    break;
                }

                if (objectKey == objectIterator)
                    bFoundKey = true;
            }

            if (!bFoundKey)
            {
                // if no matching iterator found, set it to first
                Dictionary<object, object>.KeyCollection.Enumerator enumKeys
                    = associativeArray.Keys.GetEnumerator();
                enumKeys.MoveNext();
                objectNextKey = enumKeys.Current;
            }

            if (objectNextKey == null)
                objectNextKey = NullReference.Instance;

            m_variableDictionaryLocal[m_scriptInstruction.Operand0.Value.ToString()]
                = objectNextKey;
        }

        private void ProcessIterator(String strValue)
        {
            if (strValue.Length == 0) return;

            object objectIterator = ResolveOperand(m_scriptInstruction.Operand0);

            if (objectIterator.GetType() != typeof(int))
            {
                // if type not int, treat as mismatch and set to first
                m_variableDictionaryLocal[m_scriptInstruction.Operand0.Value.ToString()] = 0;
                return;
            }

            int iIterator = (int)objectIterator;

            if (iIterator < strValue.Length - 1)
                m_variableDictionaryLocal[m_scriptInstruction.Operand0.Value.ToString()]
                    = iIterator + 1;
            else
                m_variableDictionaryLocal[m_scriptInstruction.Operand0.Value.ToString()]
                    = NullReference.Instance;
        }

        private void ProcessDBG()
        {
        }

        private void ProcessNOP()
        {
        }

        private void ProcessDCG()
        {
            // should not run in functions
            throw new ExecutionException(
                "DCG opcodes cannot be executed within a function frame.");
        }

        private void ProcessDCL()
        {
            String strIdentifier
                = (String) m_scriptInstruction.Operand0.Value;

            m_variableDictionaryLocal[strIdentifier]
                = NullReference.Instance;
        }

        private void ProcessINT()
        {
            m_bInterruped = true;
        }

        private void ProcessLOCK()
        {
            object objectValue
                = ResolveOperand(m_scriptInstruction.Operand0);

            if (objectValue.GetType() == typeof(NullReference))
                throw new ExecutionException("Lock key must be a literal value.");

            if (m_script.Manager.Locks.ContainsKey(objectValue))
            {
                ScriptContext scriptContextLocks
                    = m_script.Manager.Locks[objectValue];
                if (scriptContextLocks == this && m_dictLocks[objectValue] != m_scriptInstruction)
                    throw new ExecutionException(
                        "Nested locks cannot share the same locking key.");

                // repeat instruction
                FunctionFrame functionFrame = m_stackFunctionFrames.Peek();
                --functionFrame.m_iNextInstruction;
                

                // interrupt
                m_bInterruped = true;
            }
            else
            {
                m_script.Manager.Locks[objectValue] = this;
                m_dictLocks[objectValue] = m_scriptInstruction;
            }
        }

        private void ProcessULCK()
        {
            object objectValue
                = ResolveOperand(m_scriptInstruction.Operand0);

            if (objectValue.GetType() == typeof(NullReference))
                throw new ExecutionException("Lock key must be a literal value.");


            if (!m_script.Manager.Locks.ContainsKey(objectValue))
                throw new ExecutionException("Lock '" + objectValue + "' is already unlocked.");

            m_dictLocks.Remove(objectValue);
            m_script.Manager.Locks.Remove(objectValue);
        }

        private void ProcessMOV()
        {
            object objectValue
                = ResolveOperand(m_scriptInstruction.Operand1);

            AssignVariable(m_scriptInstruction.Operand0,
                objectValue);
        }

        private void ProcessINC()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);

            Type typeDest = objectValueDest.GetType();

            if (typeDest == typeof(int))
            {
                int iValue = (int)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = ++iValue;
            }
            else if (typeDest == typeof(float))
            {
                float fValue = (float)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = ++fValue;
            }
            else
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in arithmetic increment instruction.");
        }

        private void ProcessDEC()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);

            Type typeDest = objectValueDest.GetType();

            if (typeDest == typeof(int))
            {
                int iValue = (int)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = --iValue;
            }
            else if (typeDest == typeof(float))
            {
                float fValue = (float)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = --fValue;
            }
            else
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in arithmetic decrement instruction.");
        }

        private void ProcessNEG()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);

            Type typeDest = objectValueDest.GetType();

            if (typeDest == typeof(int))
            {
                int iValue = (int)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = -iValue;
            }
            else if (typeDest == typeof(float))
            {
                float fValue = (float)objectValueDest;
                m_variableDictionaryLocal[strIdentifierDest] = -fValue;
            }
            else
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in arithmetic negation instruction.");
        }

        private void ProcessADD()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessSUB()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessMUL()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessDIV()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessPOW()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessMOD()
        {
            ProcessArithmeticInstruction();
        }

        private void ProcessCNL()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);

            m_variableDictionaryLocal[strIdentifierDest]
                = objectValueDest == NullReference.Instance;
        }

        private void ProcessCEQ()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessCNE()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessCG()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessCGE()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessCL()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessCLE()
        {
            ProcessRelationalInstruction();
        }

        private void ProcessOR()
        {
            ProcessLogicalInstruction();
        }

        private void ProcessAND()
        {
            ProcessLogicalInstruction();
        }

        private void ProcessNOT()
        {
            // dest id
            String strIdentifierDest
                = (String)m_scriptInstruction.Operand0.Value;
            // dest value
            object objectValueDest
                = ResolveOperand(m_scriptInstruction.Operand0);
 
            Type typeDest = objectValueDest.GetType();

            if (typeDest != typeof(bool))
                throw new ExecutionException(
                    "Values of type '" + typeDest.Name
                    + "' cannot be used in logical negation instruction.");

            m_variableDictionaryLocal[strIdentifierDest] = !((bool)objectValueDest);
        }

        private void ProcessJMP()
        {
            ScriptInstruction scriptInstructionTarget
                = m_scriptInstruction.Operand0.InstructionRef;
            FunctionFrame functionFrame = m_stackFunctionFrames.Peek();
            functionFrame.m_iNextInstruction
                = (int) scriptInstructionTarget.Address;
        }
        
        private void ProcessJT()
        {
            object objectCondition
                = ResolveOperand(m_scriptInstruction.Operand0);
            bool bCondition = (bool)objectCondition;
            if (!bCondition) return;

            ScriptInstruction scriptInstructionTarget
                = m_scriptInstruction.Operand1.InstructionRef;
            FunctionFrame functionFrame = m_stackFunctionFrames.Peek();
            functionFrame.m_iNextInstruction
                = (int)scriptInstructionTarget.Address;
        }

        private void ProcessJF()
        {
            object objectCondition
                = ResolveOperand(m_scriptInstruction.Operand0);
            bool bCondition = (bool)objectCondition;
            if (bCondition) return;

            ScriptInstruction scriptInstructionTarget
                = m_scriptInstruction.Operand1.InstructionRef;
            FunctionFrame functionFrame = m_stackFunctionFrames.Peek();
            functionFrame.m_iNextInstruction
                = (int)scriptInstructionTarget.Address;
        }

        private void ProcessCLRA()
        {
            if (m_scriptInstruction.Operand0.Type != OperandType.Variable)
                throw new ExecutionException(
                    "Operand in CLA instruction must be a simple variable.");
            AssociativeArray associativeArray
                = new AssociativeArray();
            String strIdentifierArray
                = m_scriptInstruction.Operand0.Value.ToString();
            m_variableDictionaryLocal[strIdentifierArray] = associativeArray;
        }

        private void ProcessNEXT()
        {
            if (m_scriptInstruction.Operand0.Type != OperandType.Variable)
                throw new ExecutionException(
                    "Iterator operand in NEXT instruction must be a simple variable.");

            if (m_scriptInstruction.Operand1.Type != OperandType.Variable)
                throw new ExecutionException(
                    "Array operand reference in NEXT instruction must be a simple variable.");

            object objectEnumerable = m_variableDictionaryLocal[
                m_scriptInstruction.Operand1.Value.ToString()];

            if (objectEnumerable.GetType() == typeof(AssociativeArray))
                ProcessIterator((AssociativeArray)objectEnumerable);
            else if (objectEnumerable.GetType() == typeof(String))
                ProcessIterator((String)objectEnumerable);
            else
                throw new ExecutionException(
                    "Enumerable operand in NEXT instruction must be an associative array or a string.");
        }

        private void ProcessPUSH()
        {
            object objectValue
                = ResolveOperand(m_scriptInstruction.Operand0);
            m_stackParameters.Push(objectValue);
        }

        private void ProcessPOP()
        {
            String strIdentifier = null;
            object objectValue = m_stackParameters.Pop();
            Operand operand = m_scriptInstruction.Operand0;
            switch (operand.Type)
            {
                case OperandType.Variable:
                    strIdentifier
                        = operand.Value.ToString();
                    m_variableDictionaryLocal[strIdentifier] = objectValue;
                    break;
                case OperandType.LiteralIndexedVariable:
                case OperandType.VariableIndexedVariable:
                    strIdentifier = operand.Value.ToString();
                    object objectArray = m_variableDictionaryLocal[strIdentifier];
                    if (objectArray.GetType() != typeof(AssociativeArray))
                        throw new ExecutionException(
                            "Associative array expected for POP instruction with indexed operand.");
                    AssociativeArray associativeArray = (AssociativeArray)objectArray;

                    if (operand.Type == OperandType.LiteralIndexedVariable)
                        // pop into literal-indexed array
                        associativeArray[operand.IndexLiteral] = objectValue;
                    else
                    {
                        // pop into variable-indexed array
                        object objectIndexValue = m_variableDictionaryLocal[operand.IndexIdentifier];
                        associativeArray[objectIndexValue] = objectValue;
                    }
                    break;
                default:
                    throw new ExecutionException(
                        "Operand type '" + operand.Type + "' not supported by POP instruction.");

            }
        }

        private void ProcessCALL()
        {
            // get function
            ScriptFunction scriptFunction
                = m_scriptInstruction.Operand0.ScriptFunctionRef;
            ScriptInstruction scriptInstructionTarget
                = scriptFunction.EntryPoint;

            FunctionFrame functionFrame = new FunctionFrame();
            functionFrame.m_scriptFunction = scriptFunction;
            functionFrame.m_variableDictionary
                = VariableDictionary.CreateLocalDictionary(
                    m_scriptExecutable.ScriptDictionary);
            m_variableDictionaryLocal = functionFrame.m_variableDictionary;
            functionFrame.m_iNextInstruction
                = (int) scriptInstructionTarget.Address;

            m_stackFunctionFrames.Push(functionFrame);
        }

        private void ProcessRET()
        {
            m_stackFunctionFrames.Pop();

            if (m_stackFunctionFrames.Count == 0)
            {
                m_bTerminated = true;
                return;
            }

            m_variableDictionaryLocal
                = m_stackFunctionFrames.Peek().m_variableDictionary;
        }

        private void ProcessHOST()
        {
            // get function proto
            HostFunctionPrototype hostFunctionPrototype
                = m_scriptInstruction.Operand0.HostFunctionRef;

            // determine which handler to use
            HostFunctionHandler hostFunctionHandler = null;
            if (hostFunctionPrototype.Handler == null)
            {
                // if prototype handler not set, but
                // context handler set, use it
                if (m_scriptHandler != null)
                    hostFunctionHandler = m_scriptHandler;
            }
            else
                // prefer prototype handler over context handler
                hostFunctionHandler = hostFunctionPrototype.Handler;

            // pop values from stack into list
            List<object> listParameters = new List<object>();
            for (int iIndex = 0; iIndex < hostFunctionPrototype.ParameterTypes.Count; iIndex++)
                listParameters.Insert(0, m_stackParameters.Pop());

            // verify param list against proto
            hostFunctionPrototype.VerifyParameters(listParameters);

            // delegate call to handler (if set)
            object objectResult = null;
            if (hostFunctionHandler != null)
            {
                objectResult = hostFunctionHandler.OnHostFunctionCall(
                    hostFunctionPrototype.Name, listParameters);

                // verify result against proto
                hostFunctionPrototype.VerifyResult(objectResult);
            }

            // push result into stack
            if (objectResult == null)
                objectResult = NullReference.Instance;
            m_stackParameters.Push(objectResult);

            if (m_bInterruptOnHostfunctionCall)
                m_bInterruped = true;
        }

        private void ProcessTHRD()
        {
            // get function
            ScriptFunction scriptFunction
                = m_scriptInstruction.Operand0.ScriptFunctionRef;

            // pop values from stack into list
            List<object> listParameters = new List<object>();
            for (int iIndex = 0; iIndex < scriptFunction.ParameterCount; iIndex++)
                listParameters.Insert(0, m_stackParameters.Pop());

            // create new sub-context to act as a thread
            ScriptContext scriptContextThread
                = new ScriptContext(scriptFunction, listParameters);

            // assign host function handler from parent
            scriptContextThread.Handler = m_scriptHandler;

            // add to thread list
            m_listThreads.Add(scriptContextThread);
        }

        private uint ExecuteThreads()
        {
            uint uiExecuted = 0;
            // execute all active threads 'concurrently'
            foreach (ScriptContext scriptContextThread in m_listThreads)
                if (!scriptContextThread.Terminated)
                    uiExecuted += scriptContextThread.Execute(1);

            // elimiate terminated threads
            for (int iIndex = m_listThreads.Count - 1; iIndex >= 0; iIndex--)
                if (m_listThreads[iIndex].Terminated)
                    m_listThreads.RemoveAt(iIndex);

            return uiExecuted;
        }

        private void ExecuteInstruction()
        {
            // get top frame
            FunctionFrame functionFrame = m_stackFunctionFrames.Peek();

            // fetch next instruction
            m_scriptInstruction
                = m_scriptExecutable.InstructionsInternal[
                    functionFrame.m_iNextInstruction++];

            switch (m_scriptInstruction.Opcode)
            {
                case Opcode.DBG:  ProcessDBG();  break;
                case Opcode.NOP:  ProcessNOP();  break;
                case Opcode.DCG:  ProcessDCG();  break;
                case Opcode.DCL:  ProcessDCL();  break;
                case Opcode.INT:  ProcessINT();  break;
                case Opcode.LOCK: ProcessLOCK(); break;
                case Opcode.ULCK: ProcessULCK(); break;
                case Opcode.MOV:  ProcessMOV();  break;
                case Opcode.INC:  ProcessINC(); break;
                case Opcode.DEC:  ProcessDEC(); break;
                case Opcode.NEG:  ProcessNEG(); break;
                case Opcode.ADD:  ProcessADD();  break;
                case Opcode.SUB:  ProcessSUB();  break;
                case Opcode.MUL:  ProcessMUL();  break;
                case Opcode.DIV:  ProcessDIV();  break;
                case Opcode.POW:  ProcessPOW();  break;
                case Opcode.MOD:  ProcessMOD(); break;
                case Opcode.CNL: ProcessCNL(); break;
                case Opcode.CEQ:  ProcessCEQ();  break;
                case Opcode.CNE:  ProcessCNE();  break;
                case Opcode.CG:   ProcessCG();   break;
                case Opcode.CGE:  ProcessCGE();  break;
                case Opcode.CL:   ProcessCL();   break;
                case Opcode.CLE:  ProcessCLE();  break;
                case Opcode.OR:   ProcessOR();   break;
                case Opcode.AND:  ProcessAND();  break;
                case Opcode.NOT:  ProcessNOT();  break;
                case Opcode.JMP:  ProcessJMP();  break;
                case Opcode.JT:   ProcessJT();   break;
                case Opcode.JF:   ProcessJF();   break;
                case Opcode.CLRA: ProcessCLRA(); break;
                case Opcode.NEXT: ProcessNEXT(); break;
                case Opcode.PUSH: ProcessPUSH(); break;
                case Opcode.POP:  ProcessPOP();  break;
                case Opcode.CALL: ProcessCALL(); break;
                case Opcode.RET:  ProcessRET();  break;
                case Opcode.HOST: ProcessHOST(); break;
                case Opcode.THRD: ProcessTHRD(); break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs a script context with the given <see cref="ScriptFunction"/>
        /// entry point and function parameters.
        /// </summary>
        /// <param name="scriptFunction">Script function to execute.</param>
        /// <param name="listParameters">Script function parameters.</param>
        public ScriptContext(ScriptFunction scriptFunction, List<object> listParameters)
        {
            if (scriptFunction.ParameterCount > listParameters.Count)
                throw new ExecutionException("Missing function parameters.");
            if (scriptFunction.ParameterCount < listParameters.Count)
                throw new ExecutionException("Too many function parameters.");

            m_scriptFunction = scriptFunction;
            m_script = scriptFunction.Executable.Script;
            m_scriptExecutable = m_script.Executable;
            m_stackFunctionFrames = new Stack<FunctionFrame>();
            m_stackParameters = new Stack<object>();
            m_dictLocks = new Dictionary<object, ScriptInstruction>();
            m_listThreads = new List<ScriptContext>();
            m_scriptHandler = null;

            m_bInterruptOnHostfunctionCall = false;

            Reset();

            // push any passed parameters
            foreach (object objectParameter in listParameters)
            {
                if (objectParameter == null)
                    m_stackParameters.Push(NullReference.Instance);
                else
                {
                    Type typeParameter = objectParameter.GetType();
                    if (typeParameter == typeof(NullReference))
                        m_stackParameters.Push(NullReference.Instance);
                    else if (typeParameter == typeof(int)
                        || typeParameter == typeof(float)
                        || typeParameter == typeof(bool)
                        || typeParameter == typeof(String)
                        || typeParameter == typeof(AssociativeArray))
                        m_stackParameters.Push(objectParameter);
                    else
                        throw new ExecutionException("Parameters of type '"
                            + typeParameter.Name + "' not allowed.");
                }
            }
        }

        /// <summary>
        /// Constructs a script context with the given <see cref="ScriptFunction"/>
        /// entry point. No parameters are assumed.
        /// </summary>
        /// <param name="scriptFunction">Script function to execute.</param>
        public ScriptContext(ScriptFunction scriptFunction)
            : this(scriptFunction, new List<object>())
        {
        }

        /// <summary>
        /// Constructs a script context for the given <see cref="Script"/> assuming
        /// a main() function with the given parameter values.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <param name="listParameters">Script function parameters.</param>
        public ScriptContext(Script script, List<object> listParameters)
            : this(script.Executable.MainFunction, listParameters)
        {
        }

        /// <summary>
        /// Constructs a script context for the given <see cref="Script"/> assuming
        /// a main() function with no parameters.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        public ScriptContext(Script script)
            : this(script.Executable.MainFunction,
                new List<object>())
        {
        }

        /// <summary>
        /// Resets execution of the script context.
        /// </summary>
        public void Reset()
        {
            m_stackFunctionFrames.Clear();
            FunctionFrame functionFrame = new FunctionFrame();
            functionFrame.m_scriptFunction = m_scriptFunction;
            functionFrame.m_variableDictionary
                = VariableDictionary.CreateLocalDictionary(
                    m_scriptExecutable.ScriptDictionary);
            functionFrame.m_iNextInstruction = (int) m_scriptFunction.EntryPoint.Address;
            m_stackFunctionFrames.Push(functionFrame);

            m_stackParameters.Clear();

            m_scriptInstruction = null;
            m_variableDictionaryLocal = functionFrame.m_variableDictionary;

            // release all locks held by this context
            foreach (object objectLock in m_dictLocks.Keys)
                m_script.Manager.Locks.Remove(objectLock);
            m_dictLocks.Clear();

            m_bTerminated = false;
            m_bInterruped = false;
        }

        /// <summary>
        /// Executes up to the given number of instructions and returns
        /// the actual number of instructions executed.
        /// </summary>
        /// <param name="uiMaxInstructions">Maximum instructions to
        /// execute.</param>
        /// <returns>Actual number of instructions executed.</returns>
        public uint Execute(uint uiMaxInstructions)
        {
            m_variableDictionaryLocal.ExposeTemporaryVariables();

            m_bInterruped = false;
            uint uiExecuted = 0;
            while (!Terminated && !m_bInterruped && uiExecuted < uiMaxInstructions)
            {
                ExecuteInstruction();
                ++uiExecuted;
                uiExecuted += ExecuteThreads();
            }

            m_variableDictionaryLocal.HideTemporaryVariables();
            return uiExecuted;
        }

        /// <summary>
        /// Executes the script context for up to the given time
        /// interval and returns the number of instructions executed.
        /// </summary>
        /// <param name="tsInterval">Time interval allowed for
        /// execution.</param>
        /// <returns>Actual number of instructions executed.</returns>
        public uint Execute(TimeSpan tsInterval)
        {
            DateTime dtIntervalEnd = DateTime.Now + tsInterval;

            m_variableDictionaryLocal.ExposeTemporaryVariables();

            m_bInterruped = false;
            uint uiExecuted = 0;
            while (!Terminated && !m_bInterruped)
            {
                ExecuteInstruction();
                ++uiExecuted;
                uiExecuted += ExecuteThreads();

                if (DateTime.Now >= dtIntervalEnd) break;
            }

            m_variableDictionaryLocal.HideTemporaryVariables();
            return uiExecuted;
        }

        /// <summary>
        /// Executes the script context until the end of the initial
        /// function specified or an interrupt is generated.
        /// </summary>
        /// <returns>Actual number of instructions executed.</returns>
        public uint Execute()
        {
            m_variableDictionaryLocal.ExposeTemporaryVariables();

            m_bInterruped = false;
            uint uiExecuted = 0;
            while (!Terminated && !m_bInterruped)
            {
                ExecuteInstruction();
                ++uiExecuted;
                uiExecuted += ExecuteThreads();
            }

            m_variableDictionaryLocal.HideTemporaryVariables();
            return uiExecuted;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// <see cref="Script"/> associated with this context.
        /// </summary>
        public Script Script
        {
            get { return m_script; }
        }

        /// <summary>
        /// Boolean flag to enable or disable interrupts whenever
        /// a host function is invoked.
        /// </summary>
        public bool InterruptOnHostfunctionCall
        {
            get { return m_bInterruptOnHostfunctionCall; }
            set { m_bInterruptOnHostfunctionCall = value; }
        }

        /// <summary>
        /// Child thread contexts generated by the script.
        /// </summary>
        public ReadOnlyCollection<ScriptContext> ChildThreads
        {
            get { return m_listThreads.AsReadOnly(); }
        }

        /// <summary>
        /// Boolean flag indicating if an interrupt was generated
        /// after the last execution run. The flag is cleared in
        /// subsequent executions.
        /// </summary>
        public bool Interrupted
        {
            get { return m_bInterruped; }
        }

        /// <summary>
        /// Boolean flag indicating if the initially specified
        /// function has completed execution or otherwise.
        /// </summary>
        public bool Terminated
        {
            get { return m_bTerminated; }
        }

        /// <summary>
        /// Index to the next instruction to be executed.
        /// </summary>
        public int NextInstruction
        {
            get
            {
                if (m_stackFunctionFrames.Count == 0) return -1;
                return m_stackFunctionFrames.Peek().m_iNextInstruction;
            }
        }

        /// <summary>
        /// Current execution function stack.
        /// </summary>
        public ReadOnlyCollection<ScriptFunction> FunctionStack
        {
            get
            {
                List<ScriptFunction> listFunctions = new List<ScriptFunction>();
                foreach (FunctionFrame functionFrame in m_stackFunctionFrames)
                    listFunctions.Add(functionFrame.m_scriptFunction);
                return new List<ScriptFunction>(listFunctions).AsReadOnly();
            }
        }

        /// <summary>
        /// Current parameter value stack.
        /// </summary>
        public ReadOnlyCollection<object> ParameterStack
        {
            get { return new List<object>(m_stackParameters).AsReadOnly(); }
        }

        /// <summary>
        /// Variable dictionary defined at local scope.
        /// </summary>
        public VariableDictionary LocalDictionary
        {
            get { return m_variableDictionaryLocal; }
        }

        /// <summary>
        /// Context-level host function handler. This handler
        /// is ignored by host functions with handlers defined
        /// at <see cref="ScriptManager"/> level.
        /// </summary>
        public HostFunctionHandler Handler
        {
            get { return m_scriptHandler; }
            set { m_scriptHandler = value; }
        }

        #endregion
    }
}
