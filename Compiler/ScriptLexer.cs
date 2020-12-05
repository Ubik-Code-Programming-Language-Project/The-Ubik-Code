using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Compiler
{
    internal class ScriptLexer
    {
        #region Private Enumerated Types

        private enum LexState
        {
            Space,
            CommentOrDivideOrAssignDivide,
            LineComment,
            BlockCommentStart,
            BlockCommentEnd,
            AssignOrEqual,
            PlusOrIncrementOrAssignPlus,
            MinusOrDecrementOrAssignMinus,
            MultiplyOrAssignMultiply,
            PowerOrAssignPower,
            ModuloOrAssignModulo,
            And,
            Or,
            NotOrNotEqual,
            GreaterOrGreaterEqual,
            LessOrLessEqual,
            IdentifierOrKeyword,
            String,
            StringEscape,
            IntegerOrFloat,
            Float
        }

        #endregion

        #region Private Variables

        private List<String> m_listSourceLines;
        private int m_iSourceLine;
        private int m_iSourceChar;
        private LexState m_lexState;

        #endregion

        #region Private Methods

        private void ThrowInvalidCharacterException(char ch)
        {
            throw new LexerException("Unexpected character '" + ch + "'.",
                m_iSourceLine, m_iSourceChar, m_listSourceLines[m_iSourceLine]);
        }

        private bool EndOfSource
        {
            get { return m_iSourceLine >= m_listSourceLines.Count; }
        }

        private char ReadChar()
        {
            if (EndOfSource)
                throw new LexerException("End of source reached.");

            char ch = m_listSourceLines[m_iSourceLine][m_iSourceChar++];

            if (m_iSourceChar >= m_listSourceLines[m_iSourceLine].Length)
            {
                m_iSourceChar = 0;
                ++m_iSourceLine;
            }

            return ch;
        }

        private void UndoChar()
        {
            if (m_iSourceLine == 0 && m_iSourceChar == 0)
                throw new LexerException(
                    "Cannot undo char beyond start of source.");
            --m_iSourceChar;
            if (m_iSourceChar < 0)
            {
                --m_iSourceLine;
                m_iSourceChar = m_listSourceLines[m_iSourceLine].Length - 1;
            }
        }

        #endregion

        #region Public Methods

        public ScriptLexer(List<String> listSourceLines)
        {
            m_listSourceLines = new List<string>();
            foreach (String strSourceLine in listSourceLines)
                m_listSourceLines.Add(strSourceLine + "\r\n");
           
            m_iSourceLine = 0;
            m_iSourceChar = 0;
            m_lexState = LexState.Space;
        }

        public List<Token> GetTokens()
        {
            m_iSourceLine = 0;
            m_iSourceChar = 0;
            m_lexState = LexState.Space;
            String strToken = null;

            List<Token> listTokens = new List<Token>();

            while (!EndOfSource)
            {
                String strSourceLine = m_listSourceLines[m_iSourceLine];
                char ch = ReadChar();

                switch (m_lexState)
                {
                    case LexState.Space:
                        switch (ch)
                        {
                            case ' ':
                            case '\t':
                            case '\r':
                            case '\n':
                                break; // ignore whitespace
                            case '{':
                                listTokens.Add(new Token(TokenType.LeftBrace,
                                    "{", m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case '}':
                                listTokens.Add(new Token(TokenType.RightBrace, "}",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case '(':
                                listTokens.Add(new Token(TokenType.LeftPar, "(",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case ')':
                                listTokens.Add(new Token(TokenType.RightPar, ")",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case '[':
                                listTokens.Add(new Token(TokenType.LeftBracket, "[",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case ']':
                                listTokens.Add(new Token(TokenType.RightBracket, "]",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case '.':
                                listTokens.Add(new Token(TokenType.Period, ".",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case ',':
                                listTokens.Add(new Token(TokenType.Comma, ",",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case ';':
                                listTokens.Add(new Token(TokenType.SemiColon, ";",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            case '=':
                                m_lexState = LexState.AssignOrEqual;
                                break;
                            case '+':
                                m_lexState = LexState.PlusOrIncrementOrAssignPlus;
                                break;
                            case '-':
                                m_lexState = LexState.MinusOrDecrementOrAssignMinus;
                                break;
                            case '*':
                                m_lexState = LexState.MultiplyOrAssignMultiply;
                                break;
                            case '/':
                                m_lexState = LexState.CommentOrDivideOrAssignDivide;
                                break;
                            case '^':
                                m_lexState = LexState.PowerOrAssignPower;
                                break;
                            case '%':
                                m_lexState = LexState.ModuloOrAssignModulo;
                                break;
                            case '&':
                                m_lexState = LexState.And;
                                break;
                            case '|':
                                m_lexState = LexState.Or;
                                break;
                            case '!':
                                m_lexState = LexState.NotOrNotEqual;
                                break;
                            case '>':
                                m_lexState = LexState.GreaterOrGreaterEqual;
                                break;
                            case '<':
                                m_lexState = LexState.LessOrLessEqual;
                                break;
                            case '\"':
                                strToken = "";
                                m_lexState = LexState.String;
                                break;
                            case ':':
                                listTokens.Add(new Token(TokenType.Colon, ":",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                break;
                            default:
                                if (ch == '_' || char.IsLetter(ch))
                                {
                                    m_lexState = LexState.IdentifierOrKeyword;
                                    strToken = "" + ch;
                                }
                                else if (char.IsDigit(ch))
                                {
                                    strToken = "" + ch;
                                    m_lexState = LexState.IntegerOrFloat;
                                }
                                else
                                    ThrowInvalidCharacterException(ch);
                                break;
                        } 
                        break;
                    case LexState.CommentOrDivideOrAssignDivide:
                        switch (ch)
                        {
                            case '/':
                                m_lexState = LexState.LineComment;
                                break;
                            case '*':
                                m_lexState = LexState.BlockCommentStart;
                                break;
                            case '=':
                                listTokens.Add(new Token(TokenType.AssignDivide, "/=",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                m_lexState = LexState.Space;
                                break;
                            default:
                                listTokens.Add(new Token(TokenType.Divide, "/",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                                UndoChar();
                                m_lexState = LexState.Space;
                                break;
                        }
                        break;
                    case LexState.LineComment:
                        if (ch == '\n')
                            m_lexState = LexState.Space;
                        break;
                    case LexState.BlockCommentStart:
                        if (ch == '*')
                            m_lexState = LexState.BlockCommentEnd;
                        break;
                    case LexState.BlockCommentEnd:
                        if (ch == '/')
                            m_lexState = LexState.Space;
                        else
                            m_lexState = LexState.BlockCommentStart;
                        break;
                    case LexState.AssignOrEqual:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.Equal, "==",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Assign, "=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.PlusOrIncrementOrAssignPlus:
                        if (ch == '+')
                        {
                            listTokens.Add(new Token(TokenType.Increment, "++",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.AssignPlus, "+=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Plus, "+",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.MinusOrDecrementOrAssignMinus:
                        if (ch == '-')
                        {
                            listTokens.Add(new Token(TokenType.Decrement, "--",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.AssignMinus, "-=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Minus, "-",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.MultiplyOrAssignMultiply:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.AssignMultiply, "*=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Multiply, "*",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.PowerOrAssignPower:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.AssignPower, "^=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Power, "^",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.ModuloOrAssignModulo:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.AssignModulo, "%=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Modulo, "%",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.And:
                        if (ch == '&')
                        {
                            listTokens.Add(new Token(TokenType.And, "&&",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                            ThrowInvalidCharacterException(ch);
                            break;
                    case LexState.Or:
                        if (ch == '|')
                        {
                            listTokens.Add(new Token(TokenType.Or, "||",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                            ThrowInvalidCharacterException(ch);
                        break;
                    case LexState.NotOrNotEqual:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.NotEqual, "!=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Not, "!",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.GreaterOrGreaterEqual:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.GreaterOrEqual, ">=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Greater, ">",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.LessOrLessEqual:
                        if (ch == '=')
                        {
                            listTokens.Add(new Token(TokenType.LessOrEqual, "<=",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else
                        {
                            listTokens.Add(new Token(TokenType.Less, "<",
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.IdentifierOrKeyword:
                        if (ch == '_' || char.IsLetterOrDigit(ch))
                            strToken += ch;
                        else
                        {
                            TokenType tokenType;
                            if (strToken == "include")
                                tokenType = TokenType.Include;
                            else if (strToken == "global")
                                tokenType = TokenType.Global;
                            else if (strToken == "var")
                                tokenType = TokenType.Var;
                            else if (strToken == "yield")
                                tokenType = TokenType.Yield;
                            else if (strToken == "wait")
                                tokenType = TokenType.Wait;
                            else if (strToken == "notify")
                                tokenType = TokenType.Notify;
                            else if (strToken == "lock")
                                tokenType = TokenType.Lock;
                            else if (strToken == "if")
                                tokenType = TokenType.If;
                            else if (strToken == "else")
                                tokenType = TokenType.Else;
                            else if (strToken == "while")
                                tokenType = TokenType.While;
                            else if (strToken == "for")
                                tokenType = TokenType.For;
                            else if (strToken == "foreach")
                                tokenType = TokenType.Foreach;
                            else if (strToken == "in")
                                tokenType = TokenType.In;
                            else if (strToken == "switch")
                                tokenType = TokenType.Switch;
                            else if (strToken == "case")
                                tokenType = TokenType.Case;
                            else if (strToken == "default")
                                tokenType = TokenType.Default;
                            else if (strToken == "break")
                                tokenType = TokenType.Break;
                            else if (strToken == "continue")
                                tokenType = TokenType.Continue;
                            else if (strToken == "function")
                                tokenType = TokenType.Function;
                            else if (strToken == "return")
                                tokenType = TokenType.Return;
                            else if (strToken == "thread")
                                tokenType = TokenType.Thread;
                            else if (strToken == "null")
                                tokenType = TokenType.Null;
                            else if (strToken == "true" || strToken == "false")
                                tokenType = TokenType.Boolean;
                            else
                                tokenType = TokenType.Identifier;
                            
                            if (tokenType == TokenType.Boolean)
                                listTokens.Add(new Token(tokenType, strToken == "true",
                                    m_iSourceLine, m_iSourceChar, strSourceLine));
                            else
                                listTokens.Add(new Token(tokenType, strToken,
                                    m_iSourceLine, m_iSourceChar, strSourceLine));

                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.String:
                        if (ch == '\"')
                        {
                            listTokens.Add(new Token(TokenType.String, strToken,
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            m_lexState = LexState.Space;
                        }
                        else if (ch == '\\')
                            m_lexState = LexState.StringEscape;
                        else if (ch == '\r' || ch == '\n')
                            throw new LexerException("String literal cannot span multiple lines.",
                                m_iSourceLine, m_iSourceChar, m_listSourceLines[m_iSourceLine]);
                        else
                            strToken += ch;
                        break;
                    case LexState.StringEscape:
                        if (ch == '\\' || ch == '\"')
                        {
                            strToken += ch;
                            m_lexState = LexState.String;
                        }
                        else if (ch == 't')
                        {
                            strToken += '\t';
                            m_lexState = LexState.String;
                        }
                        else if (ch == 'r')
                        {
                            strToken += '\r';
                            m_lexState = LexState.String;
                        }
                        else if (ch == 'n')
                        {
                            strToken += '\n';
                            m_lexState = LexState.String;
                        }
                        else
                            throw new LexerException(
                                "Invalid string escape sequence '\\" + ch + "'.",
                                m_iSourceLine, m_iSourceChar, m_listSourceLines[m_iSourceLine]);
                        break;
                    case LexState.IntegerOrFloat:
                        if (char.IsDigit(ch))
                            strToken += ch;
                        else if (ch == '.')
                        {
                            strToken += ch;
                            m_lexState = LexState.Float;
                        }
                        else
                        {
                            int iValue = int.Parse(strToken);
                            listTokens.Add(new Token(TokenType.Integer, iValue,
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    case LexState.Float:
                        if (char.IsDigit(ch))
                            strToken += ch;
                        else
                        {
                            float fValue = float.Parse(strToken);
                            listTokens.Add(new Token(TokenType.Float, fValue,
                                m_iSourceLine, m_iSourceChar, strSourceLine));
                            UndoChar();
                            m_lexState = LexState.Space;
                        }
                        break;
                    default:
                        throw new LexerException("Unhandled lexer state.");
                }
            }

            if (m_lexState != LexState.Space)
            {
                throw new LexerException(
                    "Unexpected end of source reached.");
            }

            return listTokens;
        }

        #endregion
    }
}
