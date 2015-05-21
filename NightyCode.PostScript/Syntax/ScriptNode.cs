namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;

    #endregion


    public class ScriptNode : ProcedureNode
    {
        #region Constants and Fields

        private readonly Dictionary<string, List<OperatorNode>> _defines = new Dictionary<string, List<OperatorNode>>();
        private readonly Dictionary<string, List<string>> _operatorAliases = new Dictionary<string, List<string>>();

        #endregion


        #region Properties

        internal Dictionary<string, List<OperatorNode>> Defines
        {
            get
            {
                return _defines;
            }
        }

        internal Dictionary<string, List<string>> OperatorAliases
        {
            get
            {
                return _operatorAliases;
            }
        }

        #endregion


        #region Public Methods

        public void AddOperatorAlias(OperatorNode defOperatorNode, string alias)
        {
            string key = GetDefinitionKey(defOperatorNode);

            List<string> aliases;
            if (!_operatorAliases.TryGetValue(key, out aliases))
            {
                aliases = new List<string>();
                _operatorAliases.Add(key, aliases);
            }

            aliases.Add(alias);
        }

        #endregion


        #region Methods

        internal void AddDefinition(OperatorNode defOperatorNode)
        {
            string key = GetDefinitionKey(defOperatorNode);

            List<OperatorNode> definitions;
            if (!_defines.TryGetValue(key, out definitions))
            {
                definitions = new List<OperatorNode>();
                _defines.Add(key, definitions);
            }

            if (!definitions.Contains(defOperatorNode))
            {
                definitions.Add(defOperatorNode);
            }
        }


        private static string GetDefinitionKey(OperatorNode defOperatorNode)
        {
            if (defOperatorNode.OperatorName != "def")
            {
                throw new InvalidOperationException("The collection can only operate with def operators.");
            }

            string key = defOperatorNode.Nodes[0].Text.Substring(1);
            return key;
        }

        #endregion
    }
}