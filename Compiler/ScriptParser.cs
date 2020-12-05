using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Runtime;

namespace Ubik.Compiler
{
    internal class ScriptParser
    {
        #region Private Structs

        private struct Variable
        {
            public string m_strName;
            public VariableScope m_variablescope;
            public Type m_typeInferred;

            public Variable(String strName, VariableScope variableScope, Type typeInferred)
            {
                m_strName = strName;
                m_variablescope = variableScope;
                m_typeInferred = typeInferred;
            }
        }

        private struct FunctionDescriptor
        {
            public String m_strFunctionName;
            public uint m_uiParameterCount;
            public ScriptInstruction m_scriptInstruction;
        }

        private struct LoopControl
        {
            public ScriptInstruction m_scriptInstructionBreak;
            public ScriptInstruction m_scriptInstructionContinue;
        }

        #endregion

        #region Private Variables

        private Script m_script;
        private bool m_bDebugMode;
        private List<Token> m_listTokens;
        private int m_iNextToken;
        private Dictionary<String, bool> m_dictScriptVariables;
        private Dictionary<String, bool> m_dictFunctionVariables;
        private int m_iFunctionVariableLevel;
        private Dictionary<ScriptInstruction, FunctionDescriptor> m_dictUnresolvedFunctionCalls;
        private Stack<LoopControl> m_stackLoopControl;
        private TypeInferer m_typeInferer;
        private ScriptExecutable m_scriptExecutable;

        #endregion

        #region Private Methods

        private bool HasMoreTokens()
        {
            return m_iNextToken < m_listTokens.Count;
        }

        private Token ReadToken()
        {
            if (!HasMoreTokens())
                throw new ParserException("Unexpected end of token stream.");
            return m_listTokens[m_iNextToken++];
        }

        private Token PeekToken()
        {
            if (!HasMoreTokens())
                throw new ParserException("Unexpected end of token stream.");
            return m_listTokens[m_iNextToken];
        }

        private void UndoToken()
        {
            if (m_iNextToken <= 0)
                throw new ParserException("No more tokens to undo.");
            --m_iNextToken;
        }

        private bool IsAssignmentOperator(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Assign:
                case TokenType.AssignPlus:
                case TokenType.AssignMinus:
                case TokenType.AssignMultiply:
                case TokenType.AssignDivide:
                case TokenType.AssignPower:
                case TokenType.AssignModulo:
                    return true;
                default:
                    return false;
            }
        }

        private Opcode GetAssignmentOpcode(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Assign: return Opcode.MOV;
                case TokenType.AssignPlus: return Opcode.ADD;
                case TokenType.AssignMinus: return Opcode.SUB;
                case TokenType.AssignMultiply: return Opcode.MUL;
                case TokenType.AssignDivide: return Opcode.DIV;
                case TokenType.AssignPower: return Opcode.POW;
                case TokenType.AssignModulo: return Opcode.MOD;
                default:
                    throw new ParserException("Token '" + tokenType
                        + "' is not a valid assignment operator.");
            }
        }

        private Type GetLiteralType(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Integer: return typeof(int);
                case TokenType.Float: return typeof(float);
                case TokenType.Boolean: return typeof(bool);
                case TokenType.String: return typeof(String);
                default: throw new ParserException(
                    "Specified token type cannot be matched to a literal type.");
            }
        }

        private Type VerifyInferredType(
            Token tokenOperator, Type type1, Type type2)
        {
            return m_typeInferer.GetInferredType(tokenOperator, type1, type2);
        }

        private void DeclareLocalVar(String strIdentifier)
        {
            if (m_dictScriptVariables.ContainsKey(strIdentifier))
                throw new ParserException(
                    "Cannot declare local variable '" + strIdentifier
                    + "' as it is already defined at script or global level.");
            if (m_dictFunctionVariables.ContainsKey(strIdentifier))
                throw new ParserException(
                    "Local variable '" + strIdentifier
                    + "' already declared.");

            m_dictFunctionVariables[strIdentifier] = true;

            m_scriptExecutable.InstructionsInternal.Add
                (new ScriptInstruction(Opcode.DCL,
                    Operand.CreateVariable(strIdentifier)));
        }

        private void AllocateTemporaryVariableLevel()
        {
            ++m_iFunctionVariableLevel;
        }

        private String AllocateTemporaryVariable()
        {
            int iTempIndex = 0;
            while (true)
            {
                String strIdentifier = "__tmp_" + m_iFunctionVariableLevel + "_" + iTempIndex;
                if (!m_dictFunctionVariables.ContainsKey(strIdentifier)
                    && !m_dictScriptVariables.ContainsKey(strIdentifier))
                {
                    m_dictFunctionVariables[strIdentifier] = true;
                    return strIdentifier;
                }
                ++iTempIndex;
            }
        }

        private void FreeTemporaryVariableLevel()
        {
            if (m_iFunctionVariableLevel == 0) return;

            String strPattern = "__tmp_" + m_iFunctionVariableLevel + "_";

            List<String> listVariablesToRemove = new List<String>();
            foreach (String strIdentifier in m_dictFunctionVariables.Keys)
                if (strIdentifier.StartsWith(strPattern))
                    listVariablesToRemove.Add(strIdentifier);

            foreach (String strIdentifier in listVariablesToRemove)
                m_dictFunctionVariables.Remove(strIdentifier);

            --m_iFunctionVariableLevel;
        }

        private void ReadSemicolon()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.SemiColon)
                throw new ParserException(
                    "Closing semicolon expected.", token);
        }

        private void ReadComma()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Comma)
                throw new ParserException(
                    "Comma ',' expected.", token);
        }

        private String ReadIdentifier()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Identifier)
                throw new ParserException(
                    "Identifier expected.", token);
            return token.Lexeme.ToString();
        }

        private String ReadDeclaredIdentifier()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Identifier)
                throw new ParserException(
                    "Identifier expected.", token);

            String strIdentifier = token.Lexeme.ToString();

            if (!m_dictScriptVariables.ContainsKey(strIdentifier)
                && !m_dictFunctionVariables.ContainsKey(strIdentifier))
                throw new ParserException(
                    "Undeclared variable identifier '" + strIdentifier + "'.",
                    token);

            return strIdentifier;
        }

        private void ReadLeftParenthesis()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.LeftPar)
                throw new ParserException("Left parenthesis '(' expected.", token);
        }

        private void ReadRightParenthesis()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.RightPar)
                throw new ParserException("Right parenthesis ')' expected.", token);
        }

        private void ReadLeftBrace()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.LeftBrace)
                throw new ParserException("Left brace '{' expected.", token);
        }

        private void ReadRightBrace()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.RightBrace)
                throw new ParserException("Right brace '}' expected.", token);
        }

        private void ReadLeftBracket()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.LeftBracket)
                throw new ParserException(
                    "Left bracket '[' expected for array indexing expression.",
                    token);
        }

        private void ReadRightBracket()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.RightBracket)
                throw new ParserException(
                    "Right bracket ']' expected for array indexing expression.",
                    token);
        }

        private void ReadPeriod()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Period)
                throw new ParserException(
                    "Period '.' expected for member variable expression.",
                    token);
        }

        private void InsertDebugInfo(Token token)
        {
            if (!m_bDebugMode) return;

            String strSourceText = token.SourceText;
            strSourceText = strSourceText.Replace("\r", "").Replace("\n", "");

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.DBG,
                    Operand.CreateLiteral(token.SourceLine),
                    Operand.CreateLiteral(strSourceText)));
        }

        private void ParseYield()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Yield)
                throw new ParserException(
                    "Yield statement must start with the 'yield' keyword.",
                    token);

            ReadSemicolon();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.INT));
        }

        private void ParseWait()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Wait)
                throw new ParserException(
                    "Wait statement must start with the 'wait' keyword.",
                    token);

            String strIdentifier = ReadDeclaredIdentifier();
            ReadSemicolon();

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;
             
            // 0000: JT  id 0003
            // 0001: INT
            // 0002: JMP 0000
            // 0003: NOP

            ScriptInstruction scriptInstructionNOP
                = new ScriptInstruction(Opcode.NOP);

            ScriptInstruction scriptInstructionJEQ
                = new ScriptInstruction(
                    Opcode.JT,
                    Operand.CreateVariable(strIdentifier),
                    Operand.CreateInstructionRef(scriptInstructionNOP));
            listInstructions.Add(scriptInstructionJEQ);

            listInstructions.Add(
                new ScriptInstruction(Opcode.INT));

            listInstructions.Add(
                new ScriptInstruction(
                    Opcode.JMP,
                    Operand.CreateInstructionRef(scriptInstructionJEQ)));

            listInstructions.Add(scriptInstructionNOP);

        }

        private void ParseNotify()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Notify)
                throw new ParserException(
                    "Notify statement must start with the 'notify' keyword.",
                    token);

            String strIdentifier = ReadDeclaredIdentifier();
            ReadSemicolon();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(
                    Opcode.MOV,
                    Operand.CreateVariable(strIdentifier),
                    Operand.CreateLiteral(true)));
        }

        private void ParseScriptOrGlobalVariableDeclaration()
        {
            // get var
            Token token = ReadToken();
            if (token.Type != TokenType.Global && token.Type != TokenType.Var)
                throw new ParserException(
                    "Script variable declaration must start with the 'global' or 'var' keyword.",
                    token);

            InsertDebugInfo(token);

            Opcode opcodeDeclare = token.Type == TokenType.Global ? Opcode.DCG : Opcode.DCL;

            // get first variable identifier
            String strIdentifier = ReadIdentifier();

            while (true)
            {
                if (m_dictScriptVariables.ContainsKey(strIdentifier))
                    throw new ParserException(
                        "Cannot declare script variable '" + strIdentifier
                        + "' as it is already defined at script or global level.",
                        token);
                m_dictScriptVariables[strIdentifier] = true;

                // 0000 DCG identifier
                // or
                // 0000 DCL identifier
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(opcodeDeclare,
                        Operand.CreateVariable(strIdentifier)));

                // also handle declarations on the fly
                if (opcodeDeclare == Opcode.DCG)
                    m_script.Manager.GlobalDictionary[strIdentifier]
                        = NullReference.Instance;
                else
                    m_scriptExecutable.ScriptDictionary[strIdentifier]
                        = NullReference.Instance;

                // if next token is semicolon, end of statement
                token = ReadToken();
                if (token.Type == TokenType.SemiColon) return;

                // otherwise, expect comma
                if (token.Type != TokenType.Comma)
                    throw new ParserException(
                        "Comma required between identifiers in variable declaration.",
                        token);

                // read next variable id
                strIdentifier = ReadIdentifier();
            }
        }

        private void ParseLocalVariableDeclaration()
        {
            // get var
            Token token = ReadToken();
            if (token.Type != TokenType.Var)
                throw new ParserException(
                    "Variable declaration must start with the 'var' keyword.",
                    token);

            // get first variable identifier
            String strIdentifier = ReadIdentifier();

            while (true)
            {
                DeclareLocalVar(strIdentifier);

                // if next token is semicolon, end of statement
                token = ReadToken();
                if (token.Type == TokenType.SemiColon) return;

                // check for declarative assignment
                if (token.Type == TokenType.Assign)
                {
                    UndoToken();
                    UndoToken();
                    ParseAssignment();
                    token = ReadToken();
                    if (token.Type == TokenType.SemiColon) return;
                }

                // otherwise, expect comma
                if (token.Type != TokenType.Comma)
                    throw new ParserException(
                        "Comma ',' required between identifiers in variable declaration.",
                        token);

                // read next variable id
                strIdentifier = ReadIdentifier();
            }
        }

        private void ParseThreadCall()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Thread)
                throw new ParserException(
                    "Keyword 'thread' expected for thread call.", token);

            ParseScriptFunctionCall(true);

            ReadSemicolon();
        }

        private Variable ParseHostFunctionCall()
        {
            String strFunctionName = ReadIdentifier();

            ReadLeftParenthesis();

            List<object> listParameters = new List<object>();

            // parse parameters

            // 0000 PUSH tmp1
            // 0001 PUSH tmp2
            // :
            // 0010 PUSH tmpN

            int iParameterCount = 0;

            if (PeekToken().Type != TokenType.RightPar)
            {
                while (true)
                {
                    Variable variableParameter = ParseExpression();

                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(
                            Opcode.PUSH,
                            Operand.CreateVariable(variableParameter.m_strName)));

                    ++iParameterCount;

                    if (PeekToken().Type == TokenType.RightPar)
                        break;
                    else
                        ReadComma();
                }
            }

            ReadRightParenthesis();

            ScriptManager scriptManager
                = m_scriptExecutable.Script.Manager;
            if (!scriptManager.HostFunctions.ContainsKey(strFunctionName))
                throw new ParserException(
                    "Host function '" + strFunctionName
                    + "' not registered with the associated ScriptManager.");

            HostFunctionPrototype hostFunctionPrototype
                = scriptManager.HostFunctions[strFunctionName];

            // validate parameter count
            if (hostFunctionPrototype.ParameterTypes.Count > iParameterCount)
                throw new ParserException(
                    "Call to host function '" + strFunctionName
                    + "' has missing parameters.");

            if (hostFunctionPrototype.ParameterTypes.Count < iParameterCount)
                throw new ParserException(
                    "Call to host function '" + strFunctionName
                    + "' has too many parameters.");

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.HOST,
                    Operand.CreateHostFunctionRef(hostFunctionPrototype)));

            // 0000 POP tmp

            Variable variable = new Variable();
            variable.m_strName = AllocateTemporaryVariable();
            variable.m_variablescope = VariableScope.Local;
            variable.m_typeInferred = null;

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.POP,
                    Operand.CreateVariable(variable.m_strName)));

            return variable;
        }

        private Variable ParseScriptFunctionCall(bool bThread)
        {
            String strFunctionName = ReadIdentifier();

            // check if function already declared
            bool bFunctionDeclared = true;
            if (!m_scriptExecutable.Functions.ContainsKey(strFunctionName))
                bFunctionDeclared = false;

            ReadLeftParenthesis();

            // parse and push parameters

            uint uiParameterCount = 0;

            // 0000 PUSH tmp1
            // 0001 PUSH tmp2
            // :
            // 0010 PUSH tmpN

            if (PeekToken().Type != TokenType.RightPar)
            {
                while (true)
                {
                    Variable variableParameter = ParseExpression();

                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(
                            Opcode.PUSH,
                            Operand.CreateVariable(variableParameter.m_strName)));

                    ++uiParameterCount;

                    if (PeekToken().Type == TokenType.RightPar)
                        break;
                    else
                        ReadComma();
                }
            }

            ReadRightParenthesis();

            ScriptInstruction scriptInstructionCall = null;
            //ScriptInstruction scriptInstructionRef = null;
            ScriptFunction scriptFunction = null;

            if (bFunctionDeclared)
            {
                // if declared, get function entry point
                scriptFunction
                    = m_scriptExecutable.Functions[strFunctionName];

                if (scriptFunction.ParameterCount > uiParameterCount)
                    throw new ParserException(
                        "Call to function '" + strFunctionName
                        + "' has missing parameters.");

                if (scriptFunction.ParameterCount < uiParameterCount)
                    throw new ParserException(
                        "Call to function '" + strFunctionName
                        + "' has too many parameters.");

                //scriptInstructionRef = scriptFunction.EntryPoint;
            }

            Variable variable = new Variable();

            if (bThread)
            {
                // 0000 THRD (function)
                scriptInstructionCall
                    = new ScriptInstruction(Opcode.THRD,
                        Operand.CreateScriptFunctionRef(scriptFunction));
                m_scriptExecutable.InstructionsInternal.Add(
                    scriptInstructionCall);
            }
            else
            {
                // 0000 CALL (function)
                scriptInstructionCall
                    = new ScriptInstruction(Opcode.CALL,
                        Operand.CreateScriptFunctionRef(scriptFunction));
                m_scriptExecutable.InstructionsInternal.Add(
                    scriptInstructionCall);

                // 0001 POP tmp
                variable.m_strName = AllocateTemporaryVariable();
                variable.m_variablescope = VariableScope.Local;
                variable.m_typeInferred = null;

                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.POP,
                        Operand.CreateVariable(variable.m_strName)));
            }

            if (!bFunctionDeclared)
            {
                // keep forward reference for eventual resolution

                FunctionDescriptor functionDescriptor = new FunctionDescriptor();
                functionDescriptor.m_strFunctionName = strFunctionName;
                functionDescriptor.m_uiParameterCount = uiParameterCount;
                functionDescriptor.m_scriptInstruction = null; // not known yet
                m_dictUnresolvedFunctionCalls[scriptInstructionCall] = functionDescriptor;
                //scriptInstructionRef = null;
            }

            return variable;
        }

        private Variable ParseFunctionCall()
        {
            String strFunctionName = ReadIdentifier();
            UndoToken();

            // check if function name matches host function
            if (m_scriptExecutable.Script.Manager.IsHostFunctionRegistered(
                strFunctionName))
                return ParseHostFunctionCall();
            else
                return ParseScriptFunctionCall(false);
        }

        private Variable ParseIndexedVariable()
        {
            String strIdentifier = ReadIdentifier();

            String strIdentifierArray = strIdentifier;
            String strIdentifierTemp = null;

            // 0000 MOV tmp1, var[idxexpr1]
            // 0001 MOV tmp2, tmp1[idxexpr2]
            // :
            // 000N MOV tmpN, tmpN-1[idxexprN]

            while (PeekToken().Type == TokenType.LeftBracket)
            {
                ReadLeftBracket();

                // (do indexing expression)
                Variable variableIndex = ParseExpression();

                ReadRightBracket();

                strIdentifierTemp = AllocateTemporaryVariable();

                //0000 MOV tmp, var[index expression]
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(strIdentifierTemp),
                        Operand.CreateVariableIndexedVariable(strIdentifierArray, variableIndex.m_strName)));

                strIdentifierArray = strIdentifierTemp;
            }

            Variable variable = new Variable(strIdentifierTemp, VariableScope.Local, null);
            return variable;
        }

        private Variable ParseMemberVariable()
        {
            String strIdentifier = ReadIdentifier();

            String strIdentifierArray = strIdentifier;
            String strIdentifierTemp = null;

            // 0000 MOV tmp1, var[member1]
            // 0001 MOV tmp2, tmp1[member2]
            // :
            // 000N MOV tmpN, tmpN-1[memberN]

            while (PeekToken().Type == TokenType.Period)
            {
                ReadPeriod();

                // (do indexing expression)
                String strMemberValue = ReadIdentifier();

                strIdentifierTemp = AllocateTemporaryVariable();

                //0000 MOV tmp2, var["member"]

                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(strIdentifierTemp),
                        Operand.CreateLiteralIndexedVariable(strIdentifierArray, strMemberValue)));

                strIdentifierArray = strIdentifierTemp;
            }

            Variable variable = new Variable(strIdentifierTemp, VariableScope.Local, null);
            return variable;
        }

        private Variable ParsePreIncrement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Increment)
                throw new ParserException(
                    "Pre-increment must start with the '++' increment operator.",
                    token);

            String strIdentifier = ReadDeclaredIdentifier();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.INC, Operand.CreateVariable(strIdentifier)));

            return new Variable(strIdentifier, VariableScope.Local, null);
        }

        private Variable ParsePreDecrement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Decrement)
                throw new ParserException(
                    "Pre-increment must start with the '--' decrement operator.",
                    token);

            String strIdentifier = ReadDeclaredIdentifier();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.DEC, Operand.CreateVariable(strIdentifier)));

            return new Variable(strIdentifier, VariableScope.Local, null);
        }

        private Variable ParsePostIncrement()
        {
            String strIdentifier = ReadDeclaredIdentifier();

            Token token = ReadToken();
            if (token.Type != TokenType.Increment)
                throw new ParserException(
                    "Post-increment must follow the indentifier with the '++' increment operator.",
                    token);

            String strIdentifierTemp = AllocateTemporaryVariable();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTemp),
                    Operand.CreateVariable(strIdentifier)));

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.INC, Operand.CreateVariable(strIdentifier)));

            return new Variable(strIdentifierTemp, VariableScope.Local, null);
        }

        private Variable ParsePostDecrement()
        {
            String strIdentifier = ReadDeclaredIdentifier();

            Token token = ReadToken();
            if (token.Type != TokenType.Decrement)
                throw new ParserException(
                    "Post-decrement must follow the indentifier with the '--' decrement operator.",
                    token);

            String strIdentifierTemp = AllocateTemporaryVariable();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTemp),
                    Operand.CreateVariable(strIdentifier)));

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.DEC, Operand.CreateVariable(strIdentifier)));

            return new Variable(strIdentifierTemp, VariableScope.Local, null);
        }

        private Variable ParsePrimary()
        {
            Token token = ReadToken();

            Variable variable = new Variable();

            switch (token.Type)
            {
                case TokenType.Minus:
                    variable = ParsePrimary();

                    //0000 MOV tmp, (expr)
                    //0001 NEG tmp

                    String strIdentifierTemp = AllocateTemporaryVariable();
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(strIdentifierTemp),
                        Operand.CreateVariable(variable.m_strName)));
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.NEG,
                        Operand.CreateVariable(strIdentifierTemp)));

                    variable.m_strName = strIdentifierTemp;

                    return variable;
                case TokenType.Increment:
                    // pre-increment
                    UndoToken();
                    variable = ParsePreIncrement();
                    return variable;
                case TokenType.Decrement:
                    // pre-decrement
                    UndoToken();
                    variable = ParsePreDecrement();
                    return variable;
                case TokenType.Null:
                    // null reference
                    variable.m_strName = AllocateTemporaryVariable();
                    variable.m_variablescope = VariableScope.Local;
                    variable.m_typeInferred = typeof(NullReference);

                    //0000 MOV tmp, NULL
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(variable.m_strName),
                        Operand.CreateLiteral(NullReference.Instance)));

                    return variable;
                case TokenType.Integer:
                case TokenType.Float:
                case TokenType.Boolean:
                case TokenType.String:
                    // literal value
                    variable.m_strName = AllocateTemporaryVariable();
                    variable.m_variablescope = VariableScope.Local;
                    variable.m_typeInferred = GetLiteralType(token.Type);

                    //0000 MOV tmp, (literal)
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(variable.m_strName),
                        Operand.CreateLiteral(token.Lexeme)));

                    return variable;
                case TokenType.Identifier:
                    // simple variable, indexed variable or function call
                    String strIdentifier = token.Lexeme.ToString();

                    switch (PeekToken().Type)
                    {
                        case TokenType.Increment:
                            // post-increment
                            UndoToken(); // undo id
                            return ParsePostIncrement();
                        case TokenType.Decrement:
                            // post-decrement
                            UndoToken(); // undo id
                            return ParsePostDecrement();
                        case TokenType.LeftBracket:
                            // indexed variable
                            UndoToken(); // undo id
                            return ParseIndexedVariable();
                        case TokenType.Period:
                            // indexed variable
                            UndoToken(); // undo id
                            return ParseMemberVariable();
                        case TokenType.LeftPar:
                            // function call
                            UndoToken(); // undo id
                            return ParseFunctionCall();
                        default:
                            // simple variable
                            UndoToken();
                            strIdentifier = ReadDeclaredIdentifier();

                            variable.m_strName = AllocateTemporaryVariable();
                            variable.m_variablescope = VariableScope.Local;
                            variable.m_typeInferred = null;

                            //0000 MOV tmp, var
                            m_scriptExecutable.InstructionsInternal.Add(
                                new ScriptInstruction(Opcode.MOV,
                                    Operand.CreateVariable(variable.m_strName),
                                    Operand.CreateVariable(strIdentifier)));

                            return variable;
                    }
                case TokenType.LeftPar:
                    variable = ParseExpression();
                    ReadRightParenthesis();
                    return variable;
                default:
                    throw new ParserException(
                        "Invalid token '" + token + "' in expression.",
                        token);
            }
        }

        private Variable ParseArrayExpression()
        {
            ReadLeftBrace();

            int iArrayIndex = 0;

            String strIdentifierArray = AllocateTemporaryVariable();
            String strIdentifierIndex = AllocateTemporaryVariable();

            // initialise array variable
            // CLA tmparray

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.CLRA,
                    Operand.CreateVariable(strIdentifierArray)));

            if (PeekToken().Type != TokenType.RightBrace)
            {
                while (true)
                {
                    Variable variableValue = ParseExpression();

                    Token token = PeekToken();
                    if (token.Type == TokenType.Comma
                        || token.Type == TokenType.RightBrace)
                    {
                        // MOV tmparray[idx++], tmpvalue
                        m_scriptExecutable.InstructionsInternal.Add(
                            new ScriptInstruction(Opcode.MOV,
                                Operand.CreateLiteralIndexedVariable(
                                    strIdentifierArray, iArrayIndex++),
                                Operand.CreateVariable(variableValue.m_strName)));

                        if (token.Type == TokenType.RightBrace)
                            break;

                        ReadComma();
                    }
                    else if (token.Type == TokenType.Colon)
                    {
                        ReadToken();

                        Variable variableKey = variableValue;
                        variableValue = ParseExpression();

                        // MOV tmparray[tmpkey], tmpvalue
                        m_scriptExecutable.InstructionsInternal.Add(
                            new ScriptInstruction(Opcode.MOV,
                                Operand.CreateVariableIndexedVariable(
                                    strIdentifierArray, variableKey.m_strName),
                                Operand.CreateVariable(variableValue.m_strName)));

                        if (PeekToken().Type == TokenType.RightBrace)
                            break;

                        ReadComma();
                    }
                    else
                        throw new ParserException(
                            "Comma ',' or colon ':' expected in array expression.");
                }
            }

            ReadRightBrace();

            Variable variableArray = new Variable(strIdentifierArray,
                VariableScope.Local, typeof(AssociativeArray));
            return variableArray;
        }

        private Variable ParseFactor()
        {
            // array is a 'factor' that can be added to
            // or substracted from
            if (PeekToken().Type == TokenType.LeftBrace)
                return ParseArrayExpression();

            Variable variableBase = ParsePrimary();

            Token token = PeekToken();

            if (token.Type == TokenType.Power)
            {
                ReadToken();

                Variable variablePower = new Variable();
                if (PeekToken().Type == TokenType.LeftPar)
                {
                    // exponent is expression in parentheses
                    ReadToken();
                    variablePower = ParseExpression();
                    ReadRightParenthesis();

                    variableBase.m_typeInferred
                        = VerifyInferredType(token,
                            variableBase.m_typeInferred,
                            variablePower.m_typeInferred);
                }
                else
                    variablePower = ParsePrimary();

                //0000 POW tmpBase, tmpPower
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.POW,
                        Operand.CreateVariable(variableBase.m_strName),
                        Operand.CreateVariable(variablePower.m_strName)));
            }

            // if no power, leave as is
            return variableBase;
        }

        private Variable ParseTerm()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableTemp1 = ParseFactor();
            Variable variableTemp2 = new Variable();

            while (true)
            {
                Token token = ReadToken();

                switch (token.Type)
                {
                    case TokenType.Multiply:
                        variableTemp2 = ParseFactor();

                        //0000 MUL tmp1, tmp2
                        listInstructions.Add(
                            new ScriptInstruction(Opcode.MUL,
                                Operand.CreateVariable(variableTemp1.m_strName),
                                Operand.CreateVariable(variableTemp2.m_strName)));

                        variableTemp1.m_typeInferred = VerifyInferredType(token,
                            variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);
                        break;
                    case TokenType.Divide:
                        variableTemp2 = ParseFactor();

                        //0000 DIV tmp1, tmp2
                        listInstructions.Add(
                            new ScriptInstruction(Opcode.DIV,
                                Operand.CreateVariable(variableTemp1.m_strName),
                                Operand.CreateVariable(variableTemp2.m_strName)));

                        variableTemp1.m_typeInferred = VerifyInferredType(token,
                            variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);
                        break;
                    case TokenType.Modulo:
                        variableTemp2 = ParseFactor();

                        //0000 MOD tmp1, tmp2
                        listInstructions.Add(
                            new ScriptInstruction(Opcode.MOD,
                                Operand.CreateVariable(variableTemp1.m_strName),
                                Operand.CreateVariable(variableTemp2.m_strName)));

                        variableTemp1.m_typeInferred = VerifyInferredType(token,
                            variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);
                        break;
                    default:
                        UndoToken();
                        return variableTemp1;
                }
            }
        }

        private Variable ParseArithmeticExpression()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableTemp1 = ParseTerm();
            Variable variableTemp2 = new Variable();

            while(true)
            {
                Token token = ReadToken();

                switch (token.Type)
                {
                    case TokenType.Plus:
                        variableTemp2 = ParseTerm();

                        //0000 ADD tmp1, tmp2
                        listInstructions.Add(
                            new ScriptInstruction(Opcode.ADD,
                                Operand.CreateVariable(variableTemp1.m_strName),
                                Operand.CreateVariable(variableTemp2.m_strName)));

                        variableTemp1.m_typeInferred = VerifyInferredType(token,
                            variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);
                        break;
                    case TokenType.Minus:
                        variableTemp2 = ParseTerm();

                        //0000 SUB tmp1, tmp2
                        listInstructions.Add(
                            new ScriptInstruction(Opcode.SUB,
                                Operand.CreateVariable(variableTemp1.m_strName),
                                Operand.CreateVariable(variableTemp2.m_strName)));
                        break;
                    default:
                        UndoToken();
                        return variableTemp1;
                }
            }
        }

        private Variable ParseRelation()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableTemp1 = ParseArithmeticExpression();
            Variable variableTemp2 = new Variable();

            Token token = ReadToken();

            Opcode opcodeRelational = Opcode.CEQ;
            bool bRelationalOperator = true;
            switch (token.Type)
            {
                case TokenType.Equal: opcodeRelational = Opcode.CEQ; break;
                case TokenType.NotEqual: opcodeRelational = Opcode.CNE; break;
                case TokenType.Greater: opcodeRelational = Opcode.CG; break;
                case TokenType.GreaterOrEqual: opcodeRelational = Opcode.CGE; break;
                case TokenType.Less: opcodeRelational = Opcode.CL; break;
                case TokenType.LessOrEqual: opcodeRelational = Opcode.CLE; break;
                default: bRelationalOperator = false; break;
            }

            if (bRelationalOperator)
            {
                variableTemp2 = ParseArithmeticExpression();

                //0000 (cmp) tmp1, tmp2
                listInstructions.Add(
                    new ScriptInstruction(opcodeRelational,
                        Operand.CreateVariable(variableTemp1.m_strName),
                        Operand.CreateVariable(variableTemp2.m_strName)));

                variableTemp1.m_typeInferred = VerifyInferredType(token,
                    variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);

                return variableTemp1;
            }
            else
            {
                UndoToken();
                return variableTemp1;
            }
        }

        private Variable ParseProposition()
        {
            Variable variable = new Variable();

            if (PeekToken().Type == TokenType.Not)
            {
                ReadToken();

                if (PeekToken().Type == TokenType.LeftPar)
                    variable = ParseExpression();
                else
                    variable = ParseRelation();

                //0000 NOT tmp1
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.NOT,
                        Operand.CreateVariable(variable.m_strName)));

                return variable;
            }
            else
                return ParseRelation();
        }

        private Variable ParseConjunction()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableTemp1 = ParseProposition();
            
            while (true)
            {
                Token token = ReadToken();

                if (token.Type == TokenType.And)
                {
                    Variable variableTemp2 = ParseProposition();

                    //0000 AND tmp1, tmp2
                    listInstructions.Add(
                        new ScriptInstruction(Opcode.AND,
                            Operand.CreateVariable(variableTemp1.m_strName),
                            Operand.CreateVariable(variableTemp2.m_strName)));

                    variableTemp1.m_typeInferred = VerifyInferredType(token,
                        variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);
                    break;
                }
                else
                {
                    UndoToken();
                    return variableTemp1;
                }
            }

            return variableTemp1;
        }

        private Variable ParseDisjunction()
        {
            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableTemp1 = ParseConjunction();

            while (true)
            {
                Token token = ReadToken();

                if (token.Type == TokenType.Or)
                {
                    Variable variableTemp2 = ParseConjunction();

                    //0000 OR tmp1, tmp2
                    listInstructions.Add(
                        new ScriptInstruction(Opcode.OR,
                            Operand.CreateVariable(variableTemp1.m_strName),
                            Operand.CreateVariable(variableTemp2.m_strName)));

                    variableTemp1.m_typeInferred = VerifyInferredType(token,
                        variableTemp1.m_typeInferred, variableTemp2.m_typeInferred);

                    break;
                }
                else
                {
                    UndoToken();
                    return variableTemp1;
                }
            }

            return variableTemp1;
        }

        private Variable ParseSimpleVariableAssignment()
        {
            String strIdentifier = ReadDeclaredIdentifier();

            Token tokenAssign = ReadToken();

            if (!IsAssignmentOperator(tokenAssign.Type))
                throw new ParserException(
                    "Assignment operator '=' or one of its variants expected in assignment statement.",
                    tokenAssign);

            // do expression
            Variable variableExpression = ParseExpression();

            // (op) dest, tmpexpr
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(GetAssignmentOpcode(tokenAssign.Type),
                    Operand.CreateVariable(strIdentifier),
                    Operand.CreateVariable(variableExpression.m_strName)));

            // alloc tmp var for assign expr tmp
            String strIdentifierTmp = AllocateTemporaryVariable();
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTmp),
                    Operand.CreateVariable(strIdentifier)));
            Variable variable = new Variable(strIdentifierTmp, VariableScope.Local,
                variableExpression.m_typeInferred);
            return variable;
        }

        private Variable ParseIndexedVariableAssignment()
        {
            /*
             * x[e1][e2][e3] (OP)= (expr)
             * 
             * MOV  tmp1e,       (e1)
             * MOV  tmp1,        x[tmp1e]
             * MOV  tmp2e,       (e2)
             * MOV  tmp2,        tmp1[tmp2e]
             * MOV  tmp3e,       (e3)
             * 
             * MOV  tmpexpr,     (expr)
             * (op) tmp2[tmp3e], tmpexpr;
             */

            String strIdentifier = ReadDeclaredIdentifier();

            List<ScriptInstruction> listInstructions
                = m_scriptExecutable.InstructionsInternal;

            Variable variableElementTmp = new Variable();
            String strIdentifierSource = strIdentifier;
            String strIdentifierDest = null;
            while (!IsAssignmentOperator(PeekToken().Type))
            {
                ReadLeftBracket();
                variableElementTmp = ParseExpression();
                ReadRightBracket();

                if (!IsAssignmentOperator(PeekToken().Type))
                {
                    strIdentifierDest = AllocateTemporaryVariable();
                    // if not last element, dereference one level
                    listInstructions.Add(
                        new ScriptInstruction(Opcode.MOV,
                            Operand.CreateVariable(strIdentifierDest),
                            Operand.CreateVariableIndexedVariable(
                                strIdentifierSource, variableElementTmp.m_strName)));
                    strIdentifierSource = strIdentifierDest;
                }
            }

            Token tokenAssign = ReadToken();

            Variable variableExpression = ParseExpression();

            // handle one-level index
            if (strIdentifierDest == null)
                strIdentifierDest = strIdentifier;

            // innermost level assignment
            listInstructions.Add(
                new ScriptInstruction(GetAssignmentOpcode(tokenAssign.Type),
                    Operand.CreateVariableIndexedVariable(strIdentifierDest, variableElementTmp.m_strName),
                    Operand.CreateVariable(variableExpression.m_strName)));

            // alloc tmp var for assign expr tmp
            String strIdentifierTmp = AllocateTemporaryVariable();
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTmp),
                   Operand.CreateVariableIndexedVariable(strIdentifierDest, variableElementTmp.m_strName)));
            Variable variable = new Variable(strIdentifierTmp, VariableScope.Local,
                variableExpression.m_typeInferred);
            return variable;
        }

        private Variable ParseMemberVariableAssignment()
        {
            /* x.a.b.c.d (op)= val;
             * 
             * 0000 MOV tmp1, x["a"]
             * 0001 MOV tmp2, tmp1["b"]
             * 0002 MOV tmp3, tmp2["c"]
             * 0004 (op) tmp3["d"], val
             */

            // single or multi-indexed assignment

            String strIdentifier = ReadDeclaredIdentifier();

            List<String> listMemberValues = new List<String>();

            while (PeekToken().Type == TokenType.Period)
            {
                ReadPeriod();
                listMemberValues.Add(ReadIdentifier());
            }

            Token tokenAssign = ReadToken();
            if (!IsAssignmentOperator(tokenAssign.Type))
                throw new ParserException(
                    "Assign '=' operator or assignment variant expected in member variable assignment.",
                    tokenAssign);

            // do expression
            Variable variableExpression = ParseExpression();

            String strIdentifierDest = null;
            String strIdentifierSource = strIdentifier;
            for (int iIndex = 0; iIndex < listMemberValues.Count - 1; iIndex++)
            {
                strIdentifierDest = AllocateTemporaryVariable();
                String strIdentifierSourceIndex = listMemberValues[iIndex];

                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(strIdentifierDest),
                        Operand.CreateLiteralIndexedVariable(strIdentifierSource,
                            strIdentifierSourceIndex)));

                strIdentifierSource = strIdentifierDest;
            }

            // handle simple 1-level case
            if (strIdentifierDest == null)
                strIdentifierDest = strIdentifier;

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(GetAssignmentOpcode(tokenAssign.Type),
                    Operand.CreateLiteralIndexedVariable(strIdentifierDest,
                        listMemberValues[listMemberValues.Count - 1]),
                    Operand.CreateVariable(variableExpression.m_strName)));



            // alloc tmp var for assign expr tmp
            String strIdentifierTmp = AllocateTemporaryVariable();
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTmp),
                    Operand.CreateLiteralIndexedVariable(strIdentifierDest,
                        listMemberValues[listMemberValues.Count - 1])));
            Variable variable = new Variable(strIdentifierTmp, VariableScope.Local,
                variableExpression.m_typeInferred);
            return variable;
        }

        private Variable ParseAssignment()
        {
            String strIdentifier = ReadDeclaredIdentifier();

            Token token = PeekToken();
            switch (token.Type)
            {
                case TokenType.Assign:
                case TokenType.AssignPlus:
                case TokenType.AssignMinus:
                case TokenType.AssignMultiply:
                case TokenType.AssignDivide:
                case TokenType.AssignPower:
                case TokenType.AssignModulo:
                    // simple variable assignment
                    UndoToken(); // undo id
                    return ParseSimpleVariableAssignment();
                case TokenType.LeftBracket:
                    // single or multi-indexed assignment
                    UndoToken(); // undo id
                    return ParseIndexedVariableAssignment();
                default:
                    // single or multi-member assignment
                    UndoToken(); // undo id
                    return ParseMemberVariableAssignment();
            }
        }

        private bool IsArrayLValue()
        {
            Token tokenStart = PeekToken();
            if (tokenStart.Type != TokenType.Identifier)
                return false;

            // checkpoint
            int iInstructionCheckpoint = m_scriptExecutable.InstructionsInternal.Count;

            String strIdentifier = ReadIdentifier();

            while (PeekToken().Type == TokenType.LeftBracket)
            {
                ReadLeftBracket();
                ParseExpression();
                ReadRightBracket();
            }

            Token tokenNext = ReadToken();

            while (PeekToken() != tokenStart)
                UndoToken();

            // rollback instructions
            m_scriptExecutable.InstructionsInternal.RemoveRange(
                iInstructionCheckpoint,
                m_scriptExecutable.InstructionsInternal.Count - iInstructionCheckpoint);

            return IsAssignmentOperator(tokenNext.Type);
        }

        private bool IsMemberLValue()
        {
            Token tokenStart = PeekToken();
            if (tokenStart.Type != TokenType.Identifier)
                return false;

            String strIdentifier = ReadIdentifier();

            while (PeekToken().Type == TokenType.Period)
            {
                ReadPeriod();
                Token token = ReadToken();
                if (token.Type != TokenType.Identifier)
                {
                    while (PeekToken() != tokenStart)
                        UndoToken();
                    return false;
                }
            }

            Token tokenNext = ReadToken();

            while (PeekToken() != tokenStart)
                UndoToken();

            return IsAssignmentOperator(tokenNext.Type);
        }

        private Variable ParseExpression()
        {
            if (IsArrayLValue() || IsMemberLValue())
                return ParseAssignment();
            else
                return ParseDisjunction();
        }

        private void ParseConditionalStatement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.If)
                throw new ParserException(
                    "Keyword 'if' expected for conditional statement.",
                    token);

            ReadLeftParenthesis();

            Variable variableCondition = ParseExpression();

            ReadRightParenthesis();

            // 0000 (condition expr)
            // :
            // 0010 JF (expr) 0021
            // 0011 (true block)
            // :
            // 0020 JMP 0030
            // 0021 NOP (false addr)
            // 0022 (false block - or empty)
            // :
            // 0030 NOP (end addr)

            ScriptInstruction scriptInstructionNOPFalse
                = new ScriptInstruction(Opcode.NOP);

            ScriptInstruction scriptInstructionNOPEnd
                = new ScriptInstruction(Opcode.NOP);

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JF,
                    Operand.CreateVariable(variableCondition.m_strName),
                    Operand.CreateInstructionRef(scriptInstructionNOPFalse)));

            ParseStatementBlock();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                    Operand.CreateInstructionRef(scriptInstructionNOPEnd)));

            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPFalse);

            if (PeekToken().Type == TokenType.Else)
            {
                ReadToken();
                ParseStatementBlock();
            }

            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPEnd);
        }

        private void ParseWhileStatement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.While)
                throw new ParserException(
                    "Keyword 'while' expected for while() loop.",
                    token);

            // 0000 NOP (start)
            // 0001 (condition expr)
            // :
            // 0010 JF  (cond) 0021
            // 0011 (while block)
            // :
            // 0020 JMP [0000]
            // 0021 NOP (end addr)

            ScriptInstruction scriptInstructionNOPStart
                = new ScriptInstruction(Opcode.NOP);
            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPStart);

            ReadLeftParenthesis();

            Variable variableCondition = ParseExpression();

            if (variableCondition.m_typeInferred != null
                && variableCondition.m_typeInferred != typeof(bool))
                throw new ParserException(
                    "For statement condition must be a logical expression.",
                    token);

            ReadRightParenthesis();

            ScriptInstruction scriptInstructionNOPEnd
                = new ScriptInstruction(Opcode.NOP);

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JF,
                    Operand.CreateVariable(variableCondition.m_strName),
                    Operand.CreateInstructionRef(scriptInstructionNOPEnd)));

            // loop control for break/continue statements
            LoopControl loopControl = new LoopControl();
            loopControl.m_scriptInstructionBreak = scriptInstructionNOPEnd;
            loopControl.m_scriptInstructionContinue = scriptInstructionNOPStart;
            m_stackLoopControl.Push(loopControl);

            ParseStatementBlock();

            // pop loop control from stack
            m_stackLoopControl.Pop();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                    Operand.CreateInstructionRef(scriptInstructionNOPStart)));

            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPEnd);
        }

        private void ParseForStatement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.For)
                throw new ParserException(
                    "Keyword 'for' expected for for() loop.",
                    token);

            // 0000 (initial assignment)
            // :
            // 0010 NOP (for start)
            // 0011 (termination expr)
            // :
            // 0020 JF (expr) 0041
            // 0021 (inner block)
            // :
            // 0030 NOP (loop expr - continue)
            // 0031 (loop expression)
            // :
            // 0040 JMP [0010]
            // 0041 NOP (end addr)

            ReadLeftParenthesis();

            // initialiser is optional
            if (PeekToken().Type == TokenType.SemiColon)
                ReadSemicolon();
            else if (PeekToken().Type == TokenType.Var)
                ParseLocalVariableDeclaration();
            else
            {
                ParseAssignment();
                ReadSemicolon();
            }

            ScriptInstruction scriptInstructionNOPStart
                = new ScriptInstruction(Opcode.NOP);
            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPStart);

            ScriptInstruction scriptInstructionNOPContinue
                = new ScriptInstruction(Opcode.NOP);

            // condition expression is optional
            Variable variableCondition = new Variable();
            if (PeekToken().Type == TokenType.SemiColon)
            {
                // MOV tmp, TRUE
                variableCondition = new Variable(AllocateTemporaryVariable(),
                    VariableScope.Local, typeof(bool));
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(variableCondition.m_strName),
                        Operand.CreateLiteral(true)));
                ReadSemicolon();
            }
            else
            {
                // MOV tmp, (cond expr)
                variableCondition = ParseExpression();

                if (variableCondition.m_typeInferred != null
                    && variableCondition.m_typeInferred != typeof(bool))
                    throw new ParserException(
                        "For statement condition must be a logical expression.",
                        token);

                ReadSemicolon();
            }

            // looping assignment is optional
            List<ScriptInstruction> listInstructionsLoopExpression
                = null;
            if (PeekToken().Type != TokenType.RightPar)
            {
                // track looping assignment block for eventual displacement
                int iLoopExpressionStart = m_scriptExecutable.InstructionsInternal.Count;

                ParseExpression();

                // looping expression instruction count
                int iLoopExpressionCount
                    = m_scriptExecutable.InstructionsInternal.Count - iLoopExpressionStart; ;

                // extract looping assignment
                listInstructionsLoopExpression
                    = m_scriptExecutable.InstructionsInternal.GetRange(
                        iLoopExpressionStart, iLoopExpressionCount);
                m_scriptExecutable.InstructionsInternal.RemoveRange(
                    iLoopExpressionStart, iLoopExpressionCount);
            }
            else
                listInstructionsLoopExpression = new List<ScriptInstruction>();

            ReadRightParenthesis();

            ScriptInstruction scriptInstructionNOPEnd
                = new ScriptInstruction(Opcode.NOP);

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JF,
                    Operand.CreateVariable(variableCondition.m_strName),
                    Operand.CreateInstructionRef(scriptInstructionNOPEnd)));

            // loop control for break/continue statements
            LoopControl loopControl = new LoopControl();
            loopControl.m_scriptInstructionBreak = scriptInstructionNOPEnd;
            loopControl.m_scriptInstructionContinue = scriptInstructionNOPContinue;
            m_stackLoopControl.Push(loopControl);

            // parse for block
            ParseStatementBlock();

            // pop loop control from stack
            m_stackLoopControl.Pop();

            // NOP (loop expression)
            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPContinue);

            // re-insert looping expression before jump
            m_scriptExecutable.InstructionsInternal.AddRange(
                listInstructionsLoopExpression);

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                    Operand.CreateInstructionRef(scriptInstructionNOPStart)));

            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPEnd);
        }

        private void ParseForeachStatement()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Foreach)
                throw new ParserException(
                    "Keyword 'foreach' expected for foreach() loop.", token);

            ReadLeftParenthesis();

            // iterator
            String strIdentifierKey = null;
            String strIdentifierValue = ReadDeclaredIdentifier();

            token = ReadToken();

            // optional key
            if (token.Type == TokenType.Comma)
            {
                strIdentifierKey = strIdentifierValue;
                strIdentifierValue = ReadDeclaredIdentifier();
                token = ReadToken();
            }

            if (token.Type != TokenType.In)
                throw new ParserException(
                    "Keyword 'in' expected after iterator variable.",
                    token);

            // tmpkey := tmkpkey or key
            if (strIdentifierKey == null)
                strIdentifierKey = AllocateTemporaryVariable();

            // 0000 MOV tmpkey, NULL
            // 0001 NOP (expr start)
            // 0002 (array expr)
            // :
            // 0011 NXT tmpkey, (array expr)
            // 0012 MOV tmp, tmpkey
            // 0012 CNL tmp
            // 0012 JT  tmp, [0021]
            // 0013 MOV value, (array expr)[tmpkey]
            // 0013 (inner block)
            // :
            // 0020 JMP [0001]
            // 0021 NOP (end addr)

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierKey),
                    Operand.CreateLiteral(NullReference.Instance)));

            ScriptInstruction scriptInstrucitonNOPArrayExpression
                = new ScriptInstruction(Opcode.NOP);
            m_scriptExecutable.InstructionsInternal.Add(
                scriptInstrucitonNOPArrayExpression);

            // associative array
            Variable variableArray = ParseExpression();
            if (variableArray.m_typeInferred != null
                && variableArray.m_typeInferred != typeof(AssociativeArray))
                throw new ParserException(
                    "Invalid inferred array type detected in 'foreach' statement.",
                    token);

            ReadRightParenthesis();

             m_scriptExecutable.InstructionsInternal.Add(
                 new ScriptInstruction(Opcode.NEXT,
                    Operand.CreateVariable(strIdentifierKey),
                    Operand.CreateVariable(variableArray.m_strName)));

            String strIdentifierTemp = AllocateTemporaryVariable();

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTemp),
                    Operand.CreateVariable(strIdentifierKey)));

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.CNL,
                    Operand.CreateVariable(strIdentifierTemp)));

            ScriptInstruction scriptInstructionNOPEnd
                = new ScriptInstruction(Opcode.NOP);
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JT,
                    Operand.CreateVariable(strIdentifierTemp),
                    Operand.CreateInstructionRef(scriptInstructionNOPEnd)));

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierValue),
                    Operand.CreateVariableIndexedVariable(variableArray.m_strName, strIdentifierKey)));

            // loop control for break/continue statements
            LoopControl loopControl = new LoopControl();
            loopControl.m_scriptInstructionBreak = scriptInstructionNOPEnd;
            loopControl.m_scriptInstructionContinue = scriptInstrucitonNOPArrayExpression;
            m_stackLoopControl.Push(loopControl);

            // inner block
            ParseStatementBlock();

            // pop loop control from stack
            m_stackLoopControl.Pop();

            // loop back
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                    Operand.CreateInstructionRef(scriptInstrucitonNOPArrayExpression)));

            m_scriptExecutable.InstructionsInternal.Add(
                scriptInstructionNOPEnd);
        }

        private void ParseBreak()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Break)
                throw new ParserException(
                    "Keyword 'break' expected for break instruction.",
                    token);
            ReadSemicolon();

            if (m_stackLoopControl.Count == 0)
                throw new ParserException(
                    "Keyword 'break' can only be used in a looping construct.",
                    token);

            ScriptInstruction scriptInstructionBreak
                = m_stackLoopControl.Peek().m_scriptInstructionBreak;

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                Operand.CreateInstructionRef(scriptInstructionBreak)));
        }

        private void ParseContinue()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Continue)
                throw new ParserException(
                    "Keyword 'continue' expected for continue instruction.",
                    token);
            ReadSemicolon();

            if (m_stackLoopControl.Count == 0)
                throw new ParserException(
                    "Keyword 'continue' can only be used in a looping construct.",
                    token);

            ScriptInstruction scriptInstructionContinue
                = m_stackLoopControl.Peek().m_scriptInstructionContinue;

            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.JMP,
                Operand.CreateInstructionRef(scriptInstructionContinue)));
        }

        private void ParseSwitchStatement()
        {
            // switch (var)
            // {
            //   case v1:
            //   case v2:
            //   case v3:
            //       (stmt1)
            //   case v4:
            //       (stmt2)
            //   default:
            //       (stmt3)
            // }
            //
            //     MOV tmpcasemulti, FALSE
            //     (v1)
            //     MOV tmpcase, var
            //     CEQ tmpcase, (v1)
            //     OR tmpcasemulti, tmpcase
            //     (v2)
            //     MOV tmpcase, var
            //     CEQ tmpcase, (v2)
            //     OR tmpcasemulti, tmpcase
            //     (v3)
            //     MOV tmpcase, var
            //     CEQ tmpcase, (v3)
            //     OR tmpcasemulti, tmpcase
            //     JF tmpcasemulti, No_Stmt1
            //     (stmt1)
            //     JMP End_Switch
            // No_Stmt1:
            //     MOV tmpcasemulti, FALSE
            //     (v4)
            //     MOV tmpcase, var
            //     CEQ tmpcase, (v4)
            //     OR tmpcasemulti, tmpcase
            //     JF tmpcasemulti, No_Stmt2 (default)
            //     (stmt2)
            //     JMP End_Switch
            // No_Stmt2:
            //     (stmt3)
            // End_Switch:
            //
            Token token = ReadToken();
            if (token.Type != TokenType.Switch)
                throw new ParserException(
                    "Keyword 'switch' expected for switch statement.",
                    token);

            ReadLeftParenthesis();
            String strIdentifierSwitch = ReadDeclaredIdentifier();
            ReadRightParenthesis();

            ReadLeftBrace();

            token = PeekToken();
            if (token.Type != TokenType.Case && token.Type != TokenType.Default)
                throw new ParserException(
                    "Keyword 'case' or 'default' expected in switch statement.",
                    token);

            String strIdentifierTmpCaseMulti = AllocateTemporaryVariable();
            String strIdentifierTmpCase = AllocateTemporaryVariable();

            ScriptInstruction scriptInstructionNOPEndSwith
                = new ScriptInstruction(Opcode.NOP);

            // MOV tmpcasemulti, FALSE
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.MOV,
                    Operand.CreateVariable(strIdentifierTmpCaseMulti),
                    Operand.CreateLiteral(false)));

            while (PeekToken().Type != TokenType.Default
                && PeekToken().Type != TokenType.RightBrace)
            {
                token = ReadToken();
                if (token.Type != TokenType.Case)
                    throw new ParserException(
                        "Keyword 'case' expected in switch statement.",
                        token);

                InsertDebugInfo(token);

                // (vi)
                Variable variableExpression = ParseExpression();

                token = ReadToken();
                if (token.Type != TokenType.Colon)
                    throw new ParserException(
                        "Colon ':' expected after case expression.",
                        token);

                // MOV tmpcase, var
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.MOV,
                        Operand.CreateVariable(strIdentifierTmpCase),
                        Operand.CreateVariable(strIdentifierSwitch)));

                // CEQ tmpcase, (vi)
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.CEQ,
                        Operand.CreateVariable(strIdentifierTmpCase),
                        Operand.CreateVariable(variableExpression.m_strName)));

                // OR tmpcasemulti, tmpcase
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.OR,
                        Operand.CreateVariable(strIdentifierTmpCaseMulti),
                        Operand.CreateVariable(strIdentifierTmpCase)));

                if (PeekToken().Type != TokenType.Case)
                {
                    // assume start of case statement block
                    ScriptInstruction scriptInstructionNOPAfterBlock
                        = new ScriptInstruction(Opcode.NOP);

                    //JF tmpcasemulti, No_Stmti
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.JF,
                            Operand.CreateVariable(strIdentifierTmpCaseMulti),
                            Operand.CreateInstructionRef(scriptInstructionNOPAfterBlock)));

                    // (stmti)
                    ParseStatement();

                    // JMP End_Switch
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.JMP,
                            Operand.CreateInstructionRef(scriptInstructionNOPEndSwith)));

                    // No_stmti:
                    m_scriptExecutable.InstructionsInternal.Add(scriptInstructionNOPAfterBlock);

                    // MOV tmpcasemulti, FALSE  (reset multi-case)
                    m_scriptExecutable.InstructionsInternal.Add(
                        new ScriptInstruction(Opcode.MOV,
                            Operand.CreateVariable(strIdentifierTmpCaseMulti),
                            Operand.CreateLiteral(false)));
                }
            }

            token = ReadToken();

            // end if no default case
            if (token.Type == TokenType.RightBrace)
            {
                // EndSwitch:
                m_scriptExecutable.InstructionsInternal.Add(
                    scriptInstructionNOPEndSwith);
                return;
            }

            if (token.Type != TokenType.Default)
                throw new ParserException(
                    "Default case or closing brace '}' expected at end of switch statement.",
                    token);

            token = ReadToken();
            if (token.Type != TokenType.Colon)
                throw new ParserException(
                    "Colon ':' expected in default case expression.", token);

            // default stmt block
            ParseStatement();

            // closing brace (when default case included)
            ReadRightBrace();

            // EndSwitch:
            m_scriptExecutable.InstructionsInternal.Add(
                scriptInstructionNOPEndSwith);
        }

        private void ParseReturn()
        {
            Token token = ReadToken();
            if (token.Type != TokenType.Return)
                throw new ParserException(
                    "Return statement must start with the 'notify' keyword.",
                    token);

            if (PeekToken().Type == TokenType.SemiColon)
            {
                // return;
                ReadToken();
                // 0000 PUSH NULL
                // 0001 RET
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.PUSH,
                        Operand.CreateLiteral(NullReference.Instance)));
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.RET));
                return;
            }
            else
            {
                // return (expr);
                Variable variableReturn = ParseExpression();

                ReadSemicolon();

                // 0000 (expression)
                // :
                // 0010 PUSH tmp
                // 0002 RET
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.PUSH,
                        Operand.CreateVariable(variableReturn.m_strName)));
                m_scriptExecutable.InstructionsInternal.Add(
                    new ScriptInstruction(Opcode.RET));
                return;
            }
        }

        private void ParseStatement()
        {
            AllocateTemporaryVariableLevel();

            Token token = PeekToken();

            InsertDebugInfo(token);

            switch (token.Type)
            {
                case TokenType.SemiColon: ReadToken(); break; // empty statement
                case TokenType.Var: ParseLocalVariableDeclaration(); break;
                case TokenType.Yield: ParseYield(); break;
                case TokenType.Wait: ParseWait(); break;
                case TokenType.Notify: ParseNotify(); break;
                case TokenType.Lock: ParseLockedStatementBlock(); break;
                case TokenType.LeftBrace: ParseStatementBlock(); break;
                case TokenType.Increment:
                case TokenType.Decrement:
                case TokenType.LeftPar:
                case TokenType.Identifier:
                case TokenType.Null:
                case TokenType.Integer:
                case TokenType.Float:
                case TokenType.Boolean:
                case TokenType.String:
                    ParseExpression(); ReadSemicolon(); break; // throw away result
                case TokenType.If: ParseConditionalStatement(); break;
                case TokenType.While: ParseWhileStatement(); break;
                case TokenType.For: ParseForStatement(); break;
                case TokenType.Foreach: ParseForeachStatement(); break;
                case TokenType.Break: ParseBreak(); break;
                case TokenType.Continue: ParseContinue(); break;
                case TokenType.Switch: ParseSwitchStatement(); break;
                case TokenType.Return: ParseReturn(); break;
                case TokenType.Thread: ParseThreadCall(); break;
                default:
                    throw new ParserException(
                        "Unexpected token '" + token.Lexeme + "'.", token);
            }

            FreeTemporaryVariableLevel();
        }

        private void ParseStatementBlock()
        {
            Token token = PeekToken();

            // check if single statement
            if (token.Type != TokenType.LeftBrace)
            {
                ParseStatement();
                return;
            }

            // otherwise, statement block
            ReadToken();

            while (PeekToken().Type != TokenType.RightBrace)
                ParseStatement();

            ReadRightBrace();
        }

        private void ParseLockedStatementBlock()
        {
            Token token = ReadToken();

            // check if single statement
            if (token.Type != TokenType.Lock)
                throw new ExecutionException(
                    "Keyword 'lock' expected for lock statement block.");

            Variable variable = ParseExpression();

            // LOCK (tmpexpr)
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.LOCK,
                    Operand.CreateVariable(variable.m_strName)));

            ParseStatement();

            // ULCK (tmpexpr)
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.ULCK,
                    Operand.CreateVariable(variable.m_strName)));
        }

        private void ParseFunctionDeclaration()
        {
            // get function
            Token token = ReadToken();
            if (token.Type != TokenType.Function)
                throw new ParserException(
                    "Function declaration must start with the 'function' keyword.",
                    token);

            // function name
            String strFunctionName = ReadIdentifier();

            if (m_scriptExecutable.Functions.ContainsKey(strFunctionName))
                throw new ParserException(
                    "Function '" + strFunctionName + "' already declared.",
                    token);

            ReadLeftParenthesis();

            InsertDebugInfo(token);

            // function parameters

            List<String> listParameters = new List<string>();

            if (PeekToken().Type != TokenType.RightPar)
            {
                while (true)
                {
                    token = ReadToken();
                    if (token.Type != TokenType.Identifier)
                        throw new ParserException(
                            "Unexpected token '" + token.Lexeme
                            + "' in function declaration.", token);

                    String strParameterName = token.Lexeme.ToString();

                    DeclareLocalVar(strParameterName);
                    listParameters.Add(strParameterName);

                    token = PeekToken();
                    if (token.Type == TokenType.Comma)
                        ReadComma();
                    else if (token.Type == TokenType.RightPar)
                        break;
                    else
                        throw new ParserException(
                           "Comma ',' or right parenthesis ')' expected in function declararion.");
                }
            }

            ReadRightParenthesis();

            // 0000 NOP
            ScriptInstruction scriptInstructionFunctionEntry
                = new ScriptInstruction(Opcode.NOP);
            m_scriptExecutable.InstructionsInternal.Add(scriptInstructionFunctionEntry);

            // add function descriptor
            ScriptFunction scriptFunction
                = new ScriptFunction(m_scriptExecutable, strFunctionName,
                    listParameters, scriptInstructionFunctionEntry);
            m_scriptExecutable.Functions[strFunctionName] = scriptFunction;

            // 0000 POP IdN
            // 0001 POP Id(N-1)
            // :
            // 000N PoP Id0
            listParameters.Reverse();
            foreach (String strParameter in listParameters)
            {
                ScriptInstruction scriptInstructionPop
                    = new ScriptInstruction(
                        Opcode.POP,
                        Operand.CreateVariable(strParameter));
                m_scriptExecutable.InstructionsInternal.Add(scriptInstructionPop);
            }

            // function block
            ParseStatementBlock();

            // 0000 PUSH NULL
            // 0001 RET
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.PUSH,
                    Operand.CreateLiteral(NullReference.Instance)));
            m_scriptExecutable.InstructionsInternal.Add(
                new ScriptInstruction(Opcode.RET));

            // clear fuunction-scope variables
            m_dictFunctionVariables.Clear();
        }

        private void ParseScript()
        {
            while (HasMoreTokens())
            {
                Token token = PeekToken();

                if (token.Type == TokenType.Global || token.Type == TokenType.Var)
                    ParseScriptOrGlobalVariableDeclaration();
                else
                    break;
            }

            // do nothing if only var decls
            if (!HasMoreTokens())
                return;

            while (HasMoreTokens())
            {
                Token token = PeekToken();

                if (token.Type != TokenType.Function)
                    throw new ParserException(
                        "Only variable and function declarations expected in the outer scope.",
                        token);

                ParseFunctionDeclaration();
            }
        }

        private void ResolveForwardFunctionDeclarations()
        {
            foreach (ScriptInstruction scriptInstructionCall in m_dictUnresolvedFunctionCalls.Keys)
            {
                // get unresolved call
                FunctionDescriptor functionDescriptorUnresolved
                    = m_dictUnresolvedFunctionCalls[scriptInstructionCall];
                // get function name
                String strFunctionName = functionDescriptorUnresolved.m_strFunctionName;

                // attempt lookup
                if (!m_scriptExecutable.Functions.ContainsKey(strFunctionName))
                    throw new ParserException(
                        "Cannot call undeclared function '" + strFunctionName + "'.");

                ScriptFunction scriptFunction
                    = m_scriptExecutable.Functions[strFunctionName];

                // resolve function reference for CALL / THRD opcode
                scriptInstructionCall.Operand0.ScriptFunctionRef
                    = scriptFunction;
            }
        }

        #endregion

        #region Internal Properties

        internal bool DebugMode
        {
            get { return m_bDebugMode; }
            set { m_bDebugMode = value; }
        }

        #endregion

        #region Public Methods

        public ScriptParser(Script script, List<Token> listTokens)
        {
            m_script = script;
            m_bDebugMode = false;
            m_iNextToken = 0;
            m_dictScriptVariables = new Dictionary<String, bool>();
            m_dictFunctionVariables = new Dictionary<String, bool>();
            m_iFunctionVariableLevel = 0;
            m_dictUnresolvedFunctionCalls = new Dictionary<ScriptInstruction, FunctionDescriptor>();
            m_stackLoopControl = new Stack<LoopControl>();
            m_listTokens = new List<Token>(listTokens);
            m_typeInferer = new TypeInferer();
            m_scriptExecutable = null;
        }

        public ScriptExecutable Parse()
        {
            m_iNextToken = 0;
            m_dictScriptVariables.Clear();
            m_dictFunctionVariables.Clear();
            m_iFunctionVariableLevel = -1;
            m_dictUnresolvedFunctionCalls.Clear();
            m_stackLoopControl.Clear();

            m_scriptExecutable = new ScriptExecutable(m_script);

            ParseScript();

            // resolve forward function declarations
            ResolveForwardFunctionDeclarations();

            // eliminate null opcodes
            m_scriptExecutable.EliminateNullOpcodes();

            return m_scriptExecutable;
        }

        #endregion
    }
}
