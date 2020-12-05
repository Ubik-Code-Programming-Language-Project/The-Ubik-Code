using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Ubik.Collections;
using Ubik.Runtime;

namespace Ubik.Compiler
{
    /// <summary>
    /// Represents the source and compiled form of a script.
    /// </summary>
    public class Script
    {
        #region Private Variables

        private ScriptManager m_scriptManager;
        private String m_strName;
        private List<String> m_listSourceLines;
        private ScriptExecutable m_scriptExecutable;

        #endregion

        #region Private Methods

        private void LoadScript(String strScriptName)
        {
            ScriptLoader scriptLoader = m_scriptManager.Loader;
            m_listSourceLines = scriptLoader.LoadScript(strScriptName);
            m_listSourceLines.Add(" "); // add extra space to aid lexer

            Dictionary<String, bool> dictIncluedScripts
                = new Dictionary<string, bool>();

            for (int iIndex = 0; iIndex < m_listSourceLines.Count; iIndex++)
            {
                String strSourceLine = m_listSourceLines[iIndex];
                List<String> listSourceLinesSingle = new List<string>();
                listSourceLinesSingle.Add(strSourceLine);

                ScriptLexer scriptLexer = new ScriptLexer(listSourceLinesSingle);
                List<Token> listTokens = null;
                try
                {
                    listTokens = scriptLexer.GetTokens();
                }
                catch (Exception)
                {
                    // if unexpected end of stream, ignore line
                    continue;
                }

                // ignore if empty line
                if (listTokens.Count == 0) continue;

                // ignore if first token is not include
                if (listTokens[0].Type != TokenType.Include) continue;

                // expect more tokens after include
                if (listTokens.Count < 2)
                    throw new ParserException(
                        "Include path expected in include statement.");

                // expect string literal
                if (listTokens[1].Type != TokenType.String)
                    throw new ParserException(
                        "String literal expected after 'include' keyword.");

                // expect semicolon
                if (listTokens.Count < 3)
                    throw new ParserException(
                        "Semicolon ';' expected at the end of the include statement.");
                if (listTokens[2].Type != TokenType.SemiColon)
                    throw new ParserException(
                        "Semicolon ';' expected at the end of the include statement.");
                if (listTokens.Count > 3)
                    throw new ParserException(
                        "Nothing expected after semicolon ';' at the end of the include statement.");

                // get include name
                String strScriptInclude = (String)listTokens[1].Lexeme;

                // remove include statement
                m_listSourceLines.RemoveAt(iIndex);

                // do not include script more than once
                if (dictIncluedScripts.ContainsKey(strScriptInclude))
                    continue;

                // load include script source
                List<String> listIncludeLines
                    = scriptLoader.LoadScript(strScriptInclude);

                // insert include source
                m_listSourceLines.InsertRange(iIndex, listIncludeLines);

                // keep track of included scripts
                dictIncluedScripts[strScriptInclude] = true;

                // reposition line index at newly inserted include
                --iIndex;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Conscructs a script associated with the given ScriptManager
        /// and using the given resource name.
        /// </summary>
        /// <param name="scriptManager">ScriptManager associated with
        /// the script.</param>
        /// <param name="strScriptName">Resource name for loading the
        /// script.</param>
        public Script(ScriptManager scriptManager, String strScriptName)
        {
            m_scriptManager = scriptManager;
            m_strName = strScriptName;

            try
            {
                LoadScript(strScriptName);

                ScriptLexer scriptLexer = new ScriptLexer(m_listSourceLines);
                List<Token> listTokens = scriptLexer.GetTokens();

                // parse/compile script
                ScriptParser scriptParser = new ScriptParser(this, listTokens);
                scriptParser.DebugMode = m_scriptManager.DebugMode;
                m_scriptExecutable = scriptParser.Parse();

                // optimise
                if (m_scriptManager.OptimiseCode)
                {
                    ExecutionOptimiser executionOptimiser
                        = new ExecutionOptimiser(m_scriptExecutable);
                    executionOptimiser.OptimiserInfo = false;
                    executionOptimiser.Optimise();
                }
            }
            catch (Exception exception)
            {
                throw new UbikException(
                    "Error while loading or compiling script '" + strScriptName
                    + "'.", exception);
            }
        }

        /// <summary>
        /// Checks if the script has a 'main' function defined.
        /// </summary>
        /// <returns>True if 'main' function defined, false otherwise.
        /// </returns>
        public bool HasMainFunction()
        {
            return m_scriptExecutable.HasMainFunction();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Script Manager associated with the script.
        /// </summary>
        public ScriptManager Manager
        {
            get { return m_scriptManager; }
        }

        /// <summary>
        /// Script resource name.
        /// </summary>
        public String Name
        {
            get { return m_strName; }
        }

        /// <summary>
        /// Script source code in line-list form.
        /// </summary>
        public ReadOnlyCollection<String> SourceLines
        {
            get { return m_listSourceLines.AsReadOnly(); } 
        }

        /// <summary>
        /// Script source in continuous string form.
        /// </summary>
        public String Source
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (String strLine in m_listSourceLines)
                {
                    stringBuilder.Append(strLine);
                    stringBuilder.Append("\r\n");
                }
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Compiled executable associated with the script.
        /// </summary>
        public ScriptExecutable Executable
        {
            get { return m_scriptExecutable; }
        }

        /// <summary>
        /// Script-level variable dictionary. This dictionary is
        /// accessible to all <see cref="ScriptContext"/>s created
        /// for this script.
        /// </summary>
        public VariableDictionary ScriptDictionary
        {
            get { return m_scriptExecutable.ScriptDictionary; }
        }

        /// <summary>
        /// Function entry points within the script.
        /// </summary>
        public ReadOnlyDictionary<String, ScriptFunction> Functions
        {
            get
            {
                return new ReadOnlyDictionary<String,ScriptFunction>(
                    m_scriptExecutable.Functions);
            }
        }

        /// <summary>
        /// Returns the 'main' <see cref="ScriptFunction"/> if defined
        /// or throws an exception otherwise.
        /// </summary>
        public ScriptFunction MainFunction
        {
            get { return m_scriptExecutable.MainFunction; }
        }

        #endregion
    }
}
