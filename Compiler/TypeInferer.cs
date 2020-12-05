using System;
using System.Collections.Generic;
using System.Text;

using Ubik.Runtime;

namespace Ubik.Compiler
{
    internal class TypeInferer
    {
        #region Private Static Variables

        private static Dictionary<TokenType, Dictionary<String, Type>> s_dictInferenceMap;

        #endregion

        #region Private Methods

        private String MakeKey(Type typeDest, Type typeSource)
        {
            return typeDest + "-" + typeSource;
        }

        private String GetTypeDescription(Type type)
        {
            if (type == null) return "Unknown";

            if (type == typeof(NullReference))
                return "Null";
            else if (type == typeof(int))
                return "Integer";
            else if (type == typeof(float))
                return "Float";
            else if (type == typeof(bool))
                return "Boolean";
            else if (type == typeof(String))
                return "String";
            else if (type == typeof(AssociativeArray))
                return "Array";
            else
                throw new ParserException("Type '" + type.Name
                    + "' not supported by type inference system.");
        }

        #endregion;

        #region Public Methods

        public TypeInferer()
        {
            if (s_dictInferenceMap != null) return;

            s_dictInferenceMap = new Dictionary<TokenType, Dictionary<String, Type>>();

            Type typeNull = typeof(NullReference);
            Type typeInt = typeof(int);
            Type typeFloat = typeof(float);
            Type typeBool = typeof(bool);
            Type typeString = typeof(String);
            Type typeArray = typeof(AssociativeArray);

            // And, Or
            Dictionary<String, Type> dictInferenceAndOr = new Dictionary<string, Type>();

            dictInferenceAndOr[MakeKey(null, null)] = typeBool;
            dictInferenceAndOr[MakeKey(null, typeBool)] = typeBool;
            dictInferenceAndOr[MakeKey(typeBool, null)] = typeBool;
            dictInferenceAndOr[MakeKey(typeBool, typeBool)] = typeBool;

            s_dictInferenceMap[TokenType.And] = dictInferenceAndOr;
            s_dictInferenceMap[TokenType.Or] = dictInferenceAndOr;

            // equal, not equal
            Dictionary<String, Type> dictInferenceEquality = new Dictionary<string, Type>();

            dictInferenceEquality[MakeKey(null, null)] = typeBool;
            dictInferenceEquality[MakeKey(null, typeNull)] = typeBool;
            dictInferenceEquality[MakeKey(null, typeInt)] = typeBool;
            dictInferenceEquality[MakeKey(null, typeFloat)] = typeBool;
            dictInferenceEquality[MakeKey(null, typeBool)] = typeBool;
            dictInferenceEquality[MakeKey(null, typeString)] = typeBool;

            dictInferenceEquality[MakeKey(typeNull, null)] = typeBool;
            dictInferenceEquality[MakeKey(typeNull, typeNull)] = typeBool;

            dictInferenceEquality[MakeKey(typeInt, null)] = typeBool;
            dictInferenceEquality[MakeKey(typeInt, typeInt)] = typeBool;
            dictInferenceEquality[MakeKey(typeInt, typeFloat)] = typeBool;

            dictInferenceEquality[MakeKey(typeFloat, null)] = typeBool;
            dictInferenceEquality[MakeKey(typeFloat, typeInt)] = typeBool;
            dictInferenceEquality[MakeKey(typeFloat, typeFloat)] = typeBool;

            dictInferenceEquality[MakeKey(typeBool, null)] = typeBool;
            dictInferenceEquality[MakeKey(typeBool, typeBool)] = typeBool;

            dictInferenceEquality[MakeKey(typeString, null)] = typeBool;
            dictInferenceEquality[MakeKey(typeString, typeString)] = typeBool;

            s_dictInferenceMap[TokenType.Equal] = dictInferenceEquality;
            s_dictInferenceMap[TokenType.NotEqual] = dictInferenceEquality;

            // >, >=, <, <=
            Dictionary<String, Type> dictInferenceRelative = new Dictionary<string, Type>();

            dictInferenceRelative[MakeKey(null, null)] = typeBool;
            dictInferenceRelative[MakeKey(null, typeInt)] = typeBool;
            dictInferenceRelative[MakeKey(null, typeFloat)] = typeBool;
            dictInferenceRelative[MakeKey(null, typeString)] = typeBool;

            dictInferenceRelative[MakeKey(typeInt, null)] = typeBool;
            dictInferenceRelative[MakeKey(typeInt, typeInt)] = typeBool;
            dictInferenceRelative[MakeKey(typeInt, typeFloat)] = typeBool;

            dictInferenceRelative[MakeKey(typeFloat, null)] = typeBool;
            dictInferenceRelative[MakeKey(typeFloat, typeInt)] = typeBool;
            dictInferenceRelative[MakeKey(typeFloat, typeFloat)] = typeBool;

            dictInferenceRelative[MakeKey(typeString, null)] = typeBool;
            dictInferenceRelative[MakeKey(typeString, typeString)] = typeBool;

            s_dictInferenceMap[TokenType.Greater] = dictInferenceRelative;
            s_dictInferenceMap[TokenType.GreaterOrEqual] = dictInferenceRelative;
            s_dictInferenceMap[TokenType.Less] = dictInferenceRelative;
            s_dictInferenceMap[TokenType.LessOrEqual] = dictInferenceRelative;

            // plus
            Dictionary<String, Type> dictInferencePlus = new Dictionary<string, Type>();
            dictInferencePlus[MakeKey(null, null)] = null;
            dictInferencePlus[MakeKey(null, typeInt)] = null;
            dictInferencePlus[MakeKey(null, typeFloat)] = null;
            dictInferencePlus[MakeKey(null, typeBool)] = null;
            dictInferencePlus[MakeKey(null, typeString)] = null;
            dictInferencePlus[MakeKey(null, typeArray)] = typeArray;

            dictInferencePlus[MakeKey(typeInt, null)] = null;
            dictInferencePlus[MakeKey(typeInt, typeInt)] = typeInt;
            dictInferencePlus[MakeKey(typeInt, typeFloat)] = typeFloat;

            dictInferencePlus[MakeKey(typeFloat, null)] = null;
            dictInferencePlus[MakeKey(typeFloat, typeInt)] = typeFloat;
            dictInferencePlus[MakeKey(typeFloat, typeFloat)] = typeFloat;

            dictInferencePlus[MakeKey(typeString, null)] = typeString;
            dictInferencePlus[MakeKey(typeString, typeInt)] = typeString;
            dictInferencePlus[MakeKey(typeString, typeFloat)] = typeString;
            dictInferencePlus[MakeKey(typeString, typeBool)] = typeString;
            dictInferencePlus[MakeKey(typeString, typeString)] = typeString;
            dictInferencePlus[MakeKey(typeString, typeArray)] = typeString;

            dictInferencePlus[MakeKey(typeArray, null)] = typeArray;
            dictInferencePlus[MakeKey(typeArray, typeInt)] = typeArray;
            dictInferencePlus[MakeKey(typeArray, typeFloat)] = typeArray;
            dictInferencePlus[MakeKey(typeArray, typeBool)] = typeArray;
            dictInferencePlus[MakeKey(typeArray, typeString)] = typeArray;
            dictInferencePlus[MakeKey(typeArray, typeArray)] = typeArray;

            s_dictInferenceMap[TokenType.Plus] = dictInferencePlus;

            // minus
            Dictionary<String, Type> dictInferenceMinus = new Dictionary<string, Type>();
            dictInferenceMinus[MakeKey(null, null)] = null;
            dictInferenceMinus[MakeKey(null, typeInt)] = null;
            dictInferenceMinus[MakeKey(null, typeFloat)] = null;
            dictInferenceMinus[MakeKey(null, typeBool)] = typeArray;
            dictInferenceMinus[MakeKey(null, typeString)] = null;
            dictInferenceMinus[MakeKey(null, typeArray)] = typeArray;

            dictInferenceMinus[MakeKey(typeInt, null)] = null;
            dictInferenceMinus[MakeKey(typeInt, typeInt)] = typeInt;
            dictInferenceMinus[MakeKey(typeInt, typeFloat)] = typeFloat;

            dictInferenceMinus[MakeKey(typeFloat, null)] = null;
            dictInferenceMinus[MakeKey(typeFloat, typeInt)] = typeFloat;
            dictInferenceMinus[MakeKey(typeFloat, typeFloat)] = typeFloat;

            dictInferenceMinus[MakeKey(typeString, null)] = typeString;
            dictInferenceMinus[MakeKey(typeString, typeString)] = typeString;

            dictInferenceMinus[MakeKey(typeArray, null)] = typeArray;
            dictInferenceMinus[MakeKey(typeArray, typeInt)] = typeArray;
            dictInferenceMinus[MakeKey(typeArray, typeFloat)] = typeArray;
            dictInferenceMinus[MakeKey(typeArray, typeBool)] = typeArray;
            dictInferenceMinus[MakeKey(typeArray, typeString)] = typeArray;
            dictInferenceMinus[MakeKey(typeArray, typeArray)] = typeArray;

            s_dictInferenceMap[TokenType.Minus] = dictInferenceMinus;

            // multiply, divide, power, modulo
            Dictionary<String, Type> dictInferenceFactor = new Dictionary<string, Type>();

            dictInferenceFactor[MakeKey(null, null)] = null;
            dictInferenceFactor[MakeKey(null, typeInt)] = null;
            dictInferenceFactor[MakeKey(null, typeFloat)] = typeFloat;

            dictInferenceFactor[MakeKey(typeInt, null)] = null;
            dictInferenceFactor[MakeKey(typeInt, typeInt)] = typeInt;
            dictInferenceFactor[MakeKey(typeInt, typeFloat)] = typeFloat;

            dictInferenceFactor[MakeKey(typeFloat, null)] = typeFloat;
            dictInferenceFactor[MakeKey(typeFloat, typeInt)] = typeFloat;
            dictInferenceFactor[MakeKey(typeFloat, typeFloat)] = typeFloat;

            s_dictInferenceMap[TokenType.Multiply] = dictInferenceFactor;
            s_dictInferenceMap[TokenType.Divide] = dictInferenceFactor;
            s_dictInferenceMap[TokenType.Power] = dictInferenceFactor;
            s_dictInferenceMap[TokenType.Modulo] = dictInferenceFactor;
        }

        public Type GetInferredType(Token token, Type typeDest, Type typeSource)
        {
            if (!s_dictInferenceMap.ContainsKey(token.Type))
                throw new ParserException(
                    "Cannot infer type for token '" + token
                    + "' as it is not a binary operator.");

            Dictionary<String, Type> dictInferenceSubMap
                = s_dictInferenceMap[token.Type];

            String strKey = MakeKey(typeDest, typeSource);
            if (!dictInferenceSubMap.ContainsKey(strKey))
            {
                String strTypeDest = GetTypeDescription(typeDest);
                String strTypeSource = GetTypeDescription(typeSource);
                throw new ParserException(
                    "Binary operation '" + token.Lexeme
                    + "' cannot be applied on types " + strTypeDest
                    + " and " + strTypeSource + ".");
            }

            return dictInferenceSubMap[strKey];
        }

        #endregion
    }
}
