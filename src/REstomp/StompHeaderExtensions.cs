using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace REstomp
{
    public static class StompHeaderExtensions
    {

        public static IDictionary<string, string> UniqueKeys(this ImmutableArray<KeyValuePair<string, string>> headerPairs)
        {
            Dictionary<string, string> keys = new Dictionary<string, string>();

            foreach (var keyValuePair in headerPairs.Where(keyValuePair => !keys.ContainsKey(keyValuePair.Key)))
            {
                keys.Add(keyValuePair.Key, keyValuePair.Value);
            }

            return keys;
        }

        public static string GetValueOrNull(this ImmutableArray<KeyValuePair<string, string>> headerPairs, string key)
        {
            foreach (var keyValuePair in headerPairs)
            {
                if (key == keyValuePair.Key)
                {
                    return keyValuePair.Value;
                }
            }

            return null;
        }

        public static bool TryGetValue(this ImmutableArray<KeyValuePair<string, string>> headerPairs, string key, out string value)
        {
            foreach (var keyValuePair in headerPairs)
            {
                if (key == keyValuePair.Key)
                {
                    value = keyValuePair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}