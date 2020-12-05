using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Compiler
{
    /// <summary>
    /// Exception for script parsing errors.
    /// </summary>
    public class ParserException
        : UbikException
    {
        #region Private Variables

        private Token m_token;

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructs an exception.
        /// </summary>
        public ParserException()
            : base()
        {
            m_token = null;
        }

        /// <summary>
        /// Constructs an exception with the given message.
        /// </summary>
        /// <param name="strMessage">Exception message.</param>
        public ParserException(String strMessage)
            : base(strMessage)
        {
            m_token = null;
        }

        /// <summary>
        /// Constructs an exception with the given message
        /// and inner exception reference.
        /// </summary>
        /// <param name="strMessage">Exception message.</param>
        /// <param name="exceptionInner">Inner exception reference.</param>
        public ParserException(String strMessage, Exception exceptionInner)
            : base(strMessage, exceptionInner)
        {
            m_token = null;
        }

        /// <summary>
        /// Constructs an exception with the given message and
        /// parsing token.
        /// </summary>
        /// <param name="strMessage">Exception message.</param>
        /// <param name="token">Parsing token related to the
        /// exception.</param>
        public ParserException(String strMessage, Token token)
            : base(strMessage + " Line " + token.SourceLine
                + ", character "+token.SourceCharacter+": "
                + token.SourceText)
        {
            m_token = token;
        }

        #endregion
    }
}
