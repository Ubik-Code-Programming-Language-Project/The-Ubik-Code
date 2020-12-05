using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Exception thrown when runtime errors occur.
    /// </summary>
    public class ExecutionException
        : UbikException
    {
        #region Public Methods

        /// <summary>
        /// Constructs a parameter-less exception.
        /// </summary>
        public ExecutionException()
            : base()
        {
        }

        /// <summary>
        /// Constructs an exception with the given message.
        /// </summary>
        /// <param name="strMessage"></param>
        public ExecutionException(String strMessage)
            : base(strMessage)
        {
        }

        /// <summary>
        /// Constructs an exception with the given message
        /// and inner exception reference.
        /// </summary>
        /// <param name="strMessage">Exception message.</param>
        /// <param name="exceptionInner">Inner exception reference.</param>
        public ExecutionException(String strMessage, Exception exceptionInner)
            : base(strMessage, exceptionInner)
        {
        }

        #endregion
    }
}
