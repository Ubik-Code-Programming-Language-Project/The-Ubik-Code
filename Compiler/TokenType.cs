using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Compiler
{
    /// <summary>
    /// The token type identifier related to the lexemes extracted by the script
    /// lexer.
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// Script inclusion keyword.
        /// </summary>
        Include,

        /// <summary>
        /// Global variable declaration.
        /// </summary>
        Global,

        /// <summary>
        /// Local variable declaration.
        /// </summary>
        Var,

        /// <summary>
        /// Opening brace.
        /// </summary>
        LeftBrace,

        /// <summary>
        /// Closing brace.
        /// </summary>
        RightBrace,

        /// <summary>
        /// Opening parenthesis.
        /// </summary>
        LeftPar,

        /// <summary>
        /// Closing parenthesis.
        /// </summary>
        RightPar,

        /// <summary>
        /// Opening bracket.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// Closing bracket.
        /// </summary>
        RightBracket,

        /// <summary>
        /// Membership operator.
        /// </summary>
        Period,

        /// <summary>
        /// Argument delimeter.
        /// </summary>
        Comma,

        /// <summary>
        /// Statement delimeter.
        /// </summary>
        SemiColon,

        /// <summary>
        /// Pre or post increment operator (++).
        /// </summary>
        Increment,

        /// <summary>
        /// Pre or post decrement operator (--).
        /// </summary>
        Decrement,

        /// <summary>
        /// Numeric addition / string concatentation / array operator.
        /// </summary>
        Plus,

        /// <summary>
        /// Numeric substraction / array operator.
        /// </summary>
        Minus,

        /// <summary>
        /// Multiplication operator.
        /// </summary>
        Multiply,

        /// <summary>
        /// Division operator.
        /// </summary>
        Divide,

        /// <summary>
        /// Exponent operator.
        /// </summary>
        Power,

        /// <summary>
        /// Modulo operator.
        /// </summary>
        Modulo,

        /// <summary>
        /// Assignment operator.
        /// </summary>
        Assign,

        /// <summary>
        /// Additive assignment operator.
        /// </summary>
        AssignPlus,

        /// <summary>
        /// Subtractive assignment operator.
        /// </summary>
        AssignMinus,

        /// <summary>
        /// Multiplicative assignment operator.
        /// </summary>
        AssignMultiply,

        /// <summary>
        /// Divisive assignment operator.
        /// </summary>
        AssignDivide,

        /// <summary>
        /// Exponential assignment operator.
        /// </summary>
        AssignPower,

        /// <summary>
        /// Modulo assignment operator.
        /// </summary>
        AssignModulo,

        /// <summary>
        /// Boolean conjunction operator.
        /// </summary>
        And,

        /// <summary>
        /// Boolean disjunction operator.
        /// </summary>
        Or,

        /// <summary>
        /// Boolean negation operator.
        /// </summary>
        Not,

        /// <summary>
        /// Equality operator.
        /// </summary>
        Equal,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        NotEqual,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        Greater,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        GreaterOrEqual,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        Less,

        /// <summary>
        /// Inequality operator.
        /// </summary>
        LessOrEqual,

        /// <summary>
        /// Execution control keyword.
        /// </summary>
        Yield,

        /// <summary>
        /// Wait semaphore keyword.
        /// </summary>
        Wait,

        /// <summary>
        /// Notify semaphore keyword.
        /// </summary>
        Notify,

        /// <summary>
        /// Critical section specifier.
        /// </summary>
        Lock,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        If,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        Else,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        Switch,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        Case,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        Default,

        /// <summary>
        /// Conditional expression keyword
        /// </summary>
        Colon,

        /// <summary>
        /// Looping expression keyword.
        /// </summary>
        While,

        /// <summary>
        /// Looping expression keyword.
        /// </summary>
        For,

        /// <summary>
        /// Looping expression keyword.
        /// </summary>
        Foreach,

        /// <summary>
        /// Looping expression keyword.
        /// </summary>
        In,

        /// <summary>
        /// Looping control keyword.
        /// </summary>
        Break,

        /// <summary>
        /// Looping control keyword.
        /// </summary>
        Continue,

        /// <summary>
        /// Function declaration keyword.
        /// </summary>
        Function,

        /// <summary>
        /// Function control keyword.
        /// </summary>
        Return,

        /// <summary>
        /// Concurrency control keyword.
        /// </summary>
        Thread,

        /// <summary>
        /// Variable or function identifier.
        /// </summary>
        Identifier,

        /// <summary>
        /// Null reference keyword.
        /// </summary>
        Null,

        /// <summary>
        /// Integer literal.
        /// </summary>
        Integer,

        /// <summary>
        /// Floating point literal.
        /// </summary>
        Float,

        /// <summary>
        /// Boolean literal.
        /// </summary>
        Boolean,

        /// <summary>
        /// String literal.
        /// </summary>
        String
    }
}
