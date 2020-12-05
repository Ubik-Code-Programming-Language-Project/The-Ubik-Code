using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    internal class NullReference
    {
        #region Private Static Variables

        private static NullReference s_nullReference;

        #endregion

        #region Private Methods

        private NullReference()
        {
        }

        #endregion

        #region Public Methods

        public static NullReference Instance
        {
            get
            {
                if (s_nullReference == null)
                    s_nullReference = new NullReference();
                return s_nullReference;
            }
        }

        public override string ToString()
        {
            return "NULL";
        }

        #endregion
    }
}
