using System;
using System.Collections.Generic;
using System.Text;

namespace Ubik.Runtime
{
    /// <summary>
    /// Underlying implementation of the script's array type.
    /// </summary>
    public class AssociativeArray
        : Dictionary<object, object>
    {
        #region Private Methods

        private void OutputValue(StringBuilder stringBuilder, object objectValue)
        {
            if (objectValue.GetType() == typeof(String))
            {
                stringBuilder.Append("\"");
                stringBuilder.Append(objectValue);
                stringBuilder.Append("\"");
            }
            else
                stringBuilder.Append(objectValue);
        }

        private bool EqualValues(object objectValue1, object objectValue2)
        {
            Type type1 = objectValue1.GetType();
            Type type2 = objectValue2.GetType();

            if (type1 == typeof(int) && type2 == typeof(int))
                return (int)objectValue1 == (int)objectValue2;
            else if (type1 == typeof(int) && type2 == typeof(float))
                return (int)objectValue1 == (float)objectValue2;
            else if (type1 == typeof(float) && type2 == typeof(int))
                return (float)objectValue1 == (int)objectValue2;
            else if (type1 == typeof(float) && type2 == typeof(float))
                return (float)objectValue1 == (float)objectValue2;
            else if (type1 == typeof(String) || type2 == typeof(String))
                return objectValue1.ToString() == objectValue2.ToString();
            else
                return objectValue1 == objectValue2;
        }

        private void AddValue(object objectValue)
        {
            int iIndex = 0;
            while (ContainsKey(iIndex))
                ++iIndex;
            this[iIndex] = objectValue;
        }

        private void SubtractValue(object objectValue)
        {
            List<object> listValues = new List<object>();
            foreach (object objectOldValue in Values)
                if (!EqualValues(objectOldValue, objectValue))
                    listValues.Add(objectOldValue);

            Clear();
            int iIndex = 0;
            foreach (Object objectOldValue in listValues)
                this[iIndex++] = objectOldValue;
        }

        private void AddArray(AssociativeArray assocativeArray)
        {
            int iIndex = 0;
            while (ContainsKey(iIndex)) ++iIndex;

            foreach (object objectValue in assocativeArray.Values)
                this[iIndex++] = objectValue;
        }

        private void SubtractArray(AssociativeArray associativeArray)
        {
            foreach (object objectValue in associativeArray.Values)
                SubtractValue(objectValue);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a simple value using an automatically generated numeric
        /// index, or appends the context of another array.
        /// </summary>
        /// <param name="objectValue">Simple or array value to add.</param>
        public void Add(object objectValue)
        {
            if (objectValue.GetType() == typeof(AssociativeArray))
                AddArray((AssociativeArray)objectValue);
            else
                AddValue(objectValue);
        }

        /// <summary>
        /// Removes the given element value or performs a set
        /// subtraction if the given parameter is an array.
        /// </summary>
        /// <param name="objectValue">Simple or array value to remove.
        /// </param>
        public void Subtract(object objectValue)
        {
            if (objectValue.GetType() == typeof(AssociativeArray))
                SubtractArray((AssociativeArray)objectValue);
            else
                SubtractValue(objectValue);
        }

        /// <summary>
        /// Returns a string representation of the array.
        /// </summary>
        /// <returns>String representation of the array.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder
                = new StringBuilder();

            stringBuilder.Append("{");

            bool bFirst = true;
            foreach (object objectKey in Keys)
            {
                if (!bFirst)
                    stringBuilder.Append(", ");
                bFirst = false;

                OutputValue(stringBuilder, objectKey);

                stringBuilder.Append(":");

                OutputValue(stringBuilder, this[objectKey]);

            }

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns an array element using the given index object.
        /// </summary>
        /// <param name="objectKey">Index object.</param>
        /// <returns>Array element returned by the indexer.</returns>
        public new object this[object objectKey]
        {
            get
            {
                // handle size property
                if (objectKey.GetType() == typeof(String)
                    && ((String)objectKey) == "size")
                    return this.Count;

                if (!ContainsKey(objectKey))
                    return NullReference.Instance;

                return base[objectKey];
            }
            set
            {
                // handle null key
                if (objectKey == null)
                    objectKey = NullReference.Instance;

                // handle size property
                if (objectKey.GetType() == typeof(String)
                    && ((String)objectKey) == "size")
                    throw new ExecutionException("Cannot modify read-only array property 'size'.");

                // handle null value
                if (value == null)
                    base[objectKey] = NullReference.Instance;
                else
                    base[objectKey] = value;
            }
        }

        #endregion
    }
}
