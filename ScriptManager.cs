using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Collections;
using Ubik.Compiler;
using Ubik.Runtime;

namespace Ubik
{
    /// <summary>
    /// Represents a global script domain where scripts can be
    /// loaded and executed.
    /// </summary>
    public class ScriptManager
    {
        #region Private Variables

        private ScriptLoader m_scriptLoader;
        private VariableDictionary m_variableDictionaryGlobal;
        private Dictionary<String, HostFunctionPrototype>
            m_dictHostFunctionPrototypes;
        private Dictionary<object, ScriptContext> m_dictLocks;
        private bool m_bDebugMode;
        private bool m_bOptimiseCode;

        #endregion

        #region Internal Properties

        internal Dictionary<object, ScriptContext> Locks
        {
            get { return m_dictLocks; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructsa script manager.
        /// </summary>
        public ScriptManager()
        {
            m_scriptLoader = new ScriptLoaderDefault();
            m_variableDictionaryGlobal = VariableDictionary.CreateGlobalDictionary();
            m_dictHostFunctionPrototypes
                = new Dictionary<string, HostFunctionPrototype>();
            m_dictLocks = new Dictionary<object, ScriptContext>();
            m_bDebugMode = true;
            m_bOptimiseCode = true;
        }

        /// <summary>
        /// Checks if a host function prototype is registered with the given
        /// name.
        /// </summary>
        /// <param name="strName">Name of the host function.</param>
        /// <returns>True if host function registered, or false otherwise.
        /// </returns>
        public bool IsHostFunctionRegistered(String strName)
        {
            return m_dictHostFunctionPrototypes.ContainsKey(strName);
        }

        /// <summary>
        /// Registers the given <see cref="HostModule"/> with the script
        /// manager.
        /// </summary>
        /// <param name="hostModule">Host module to register.</param>
        public void RegisterHostModule(HostModule hostModule)
        {
            foreach (HostFunctionPrototype hostFunctionPrototype
                in hostModule.HostFunctionPrototypes)
                RegisterHostFunction(hostFunctionPrototype, hostModule);
        }

        /// <summary>
        /// Registers the given <see cref="HostFunctionPrototype"/> with an
        /// accompanying <see cref="HostFunctionHandler"/>. Handlers
        /// defined at <see cref="ScriptContext"/> level for this function
        /// are ignored.
        /// </summary>
        /// <param name="hostFunctionPrototype">Host function prototype to
        /// register.</param>
        /// <param name="hostFunctionHandler">Handler associated with the
        /// host function.</param>
        public void RegisterHostFunction(
            HostFunctionPrototype hostFunctionPrototype,
            HostFunctionHandler hostFunctionHandler)
        {
            String strName = hostFunctionPrototype.Name;
            if (m_dictHostFunctionPrototypes.ContainsKey(strName))
                throw new UbikException(
                    "Host function '" + strName + "' already registered.");

            hostFunctionPrototype.Handler = hostFunctionHandler;
            m_dictHostFunctionPrototypes[strName] = hostFunctionPrototype;
        }

        /// <summary>
        /// Registers the given <see cref="HostFunctionPrototype"/> without
        /// a handler. If a <see cref="Script"/> uses the given host
        /// function, the handler must be bound at
        /// <see cref="ScriptContext"/> level.
        /// </summary>
        /// <param name="hostFunctionPrototype">Host function prototype to
        /// register.</param>
        public void RegisterHostFunction(
            HostFunctionPrototype hostFunctionPrototype)
        {
            RegisterHostFunction(hostFunctionPrototype, null);
        }

        /// <summary>
        /// Clears all the currently active locks.
        /// </summary>
        public void ClearActiveLocks()
        {
            m_dictLocks.Clear();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The <see cref="ScriptLoader"/> associated with the script manager.
        /// The loader defines the loading and 'include' mechanism used by
        /// the scripts. The default loader implementation is disk-based.
        /// </summary>
        public ScriptLoader Loader
        {
            get { return m_scriptLoader; }
            set { m_scriptLoader = value; }
        }

        /// <summary>
        /// The variable dictionary at global level.
        /// </summary>
        public VariableDictionary GlobalDictionary
        {
            get { return m_variableDictionaryGlobal; }
        }

        /// <summary>
        /// Registered <see cref="HostFunctionPrototype"/>s indexed by name.
        /// </summary>
        public ReadOnlyDictionary<String, HostFunctionPrototype>
            HostFunctions
        {
            get
            {
                return new ReadOnlyDictionary<string, HostFunctionPrototype>(
                    m_dictHostFunctionPrototypes);
            }
        }

        /// <summary>
        /// Controls generation of debug instructions for traceability
        /// purposes.
        /// </summary>
        public bool DebugMode
        {
            get { return m_bDebugMode; }
            set { m_bDebugMode = value; }
        }

        /// <summary>
        /// Enables or disables peephole optimisation of the generated
        /// byte code.
        /// </summary>
        public bool OptimiseCode
        {
            get { return m_bOptimiseCode; }
            set { m_bOptimiseCode = value; }
        }

        /// <summary>
        /// The currently active locks mapped to the
        /// owning <see cref="ScriptContext"/>s.
        /// </summary>
        public ReadOnlyDictionary<object, ScriptContext> ActiveLocks
        {
            get { return new ReadOnlyDictionary<object,ScriptContext>(m_dictLocks); }
        }

        #endregion
    }
}
