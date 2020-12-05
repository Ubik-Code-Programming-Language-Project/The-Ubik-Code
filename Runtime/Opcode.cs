using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Represents operator codes used in script instructions.
    /// </summary>
    public enum Opcode
    {
        /// <summary>
        /// Debug information.
        /// </summary>
        DBG,

        /// <summary>
        /// Null (placeholder) operator.
        /// </summary>
        NOP,

        /// <summary>
        /// Declare global variable.
        /// </summary>
        DCG,

        /// <summary>
        /// Declare local variable.
        /// </summary>
        DCL,

        /// <summary>
        /// Interrupt execution.
        /// </summary>
        INT,

        /// <summary>
        /// Critical section lock.
        /// </summary>
        LOCK,

        /// <summary>
        /// Critical section unlock.
        /// </summary>
        ULCK,

        /// <summary>
        /// Move data.
        /// </summary>
        MOV,

        /// <summary>
        /// Increment variable.
        /// </summary>
        INC,

        /// <summary>
        /// Decrement variable.
        /// </summary>
        DEC,

        /// <summary>
        /// Negate variable.
        /// </summary>
        NEG,

        /// <summary>
        /// Addition
        /// </summary>
        ADD,

        /// <summary>
        /// Subtraction.
        /// </summary>
        SUB,

        /// <summary>
        /// Multiplication.
        /// </summary>
        MUL,

        /// <summary>
        /// Division.
        /// </summary>
        DIV,

        /// <summary>
        /// Exponent.
        /// </summary>
        POW,

        /// <summary>
        /// Modulo.
        /// </summary>
        MOD,

        /// <summary>
        /// Compare to NULL.
        /// </summary>
        CNL,

        /// <summary>
        /// Equal
        /// </summary>
        CEQ,

        /// <summary>
        /// Not equal.
        /// </summary>
        CNE,

        /// <summary>
        /// Greater than.
        /// </summary>
        CG,

        /// <summary>
        /// Greater than or equal.
        /// </summary>
        CGE,

        /// <summary>
        /// Less than.
        /// </summary>
        CL,

        /// <summary>
        /// Less than or equal.
        /// </summary>
        CLE,
        
        /// <summary>
        /// Bolean OR.
        /// </summary>
        OR,

        /// <summary>
        /// Boolean AND
        /// </summary>
        AND,

        /// <summary>
        /// Boolean NOT
        /// </summary>
        NOT,

        /// <summary>
        /// Unconditional jump.
        /// </summary>
        JMP,

        /// <summary>
        /// Jump if true.
        /// </summary>
        JT,

        /// <summary>
        /// Jump if false.
        /// </summary>
        JF,

        /// <summary>
        /// Clear array.
        /// </summary>
        CLRA,

        /// <summary>
        /// Array iterator.
        /// </summary>
        NEXT,

        /// <summary>
        /// Push argument on stack.
        /// </summary>
        PUSH,

        /// <summary>
        /// Pop argument from stack.
        /// </summary>
        POP,

        /// <summary>
        /// Call routine.
        /// </summary>
        CALL,

        /// <summary>
        /// Return from routine.
        /// </summary>
        RET,

        /// <summary>
        /// Invoke host function.
        /// </summary>
        HOST,

        /// <summary>
        /// Spawn thread.
        /// </summary>
        THRD
    }
}
