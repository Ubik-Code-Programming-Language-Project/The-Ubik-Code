using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Ubik.Compiler
{
    internal class ScriptLoaderDefault
        : ScriptLoader
    {
        #region Public Methods

        public List<String> LoadScript(String strResourceName)
        {
            try
            {
                List<String> listSourceLines = new List<string>();

                StreamReader streamReader = new StreamReader(strResourceName);
                while (!streamReader.EndOfStream)
                    listSourceLines.Add(streamReader.ReadLine());
                streamReader.Close();

                return listSourceLines; 

            }
            catch (Exception exception)
            {
                throw new UbikException(
                    "Error while loading script '" + strResourceName
                    + "'.", exception);
            }
        }

        #endregion
    }
}
