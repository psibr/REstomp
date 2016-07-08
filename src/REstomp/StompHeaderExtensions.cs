using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace REstomp
{
    public static class StompHeaderExtensions
    {

        public static ImmutableArray<string> UniqueKeys(this ImmutableArray<KeyValuePair<string, string>> headerPairs)
        {
            List<string> keys = new List<string>();

            foreach (var keyValuePair in headerPairs)
            {
                if(!keys.Contains(keyValuePair.Key))
                    keys.Add(keyValuePair.Key);
            }

            return keys.ToImmutableArray();
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
