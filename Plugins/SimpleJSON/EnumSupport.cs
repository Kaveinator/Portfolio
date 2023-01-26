using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJSON {
    public static class EnumSupport {
        public static bool TryGetEnum<TEnum>(this JSONNode node, out TEnum result) where TEnum : Enum {
            result = default;
            if (node == null || node.IsNull) return false;
            //if (node.IsNumber)
            //    result = (TEnum)(object)node;
            //else
            if ((node.IsNumber || node.IsString) && Enum.TryParse(typeof(TEnum), node.Value, out object res))
                result = (TEnum)res;
            else return false;
            return true;
        }
        public static TEnum GetEnumOrDefault<TEnum>(this JSONNode node, string aKey, TEnum aDefault) where TEnum : Enum {
            if (string.IsNullOrEmpty(aKey) || ((node = node?[aKey])?.IsNull ?? true)) return aDefault;
            if (node.TryGetEnum<TEnum>(out TEnum aEnum))
                return aEnum;
            return aDefault;
        }
        public static TEnum GetEnumOrDefault<TEnum>(this JSONNode node, int aIndex, TEnum aDefault) where TEnum : Enum {
            if (node?.IsNull ?? true) return aDefault;
            if (node[aIndex].TryGetEnum<TEnum>(out TEnum aEnum))
                return aEnum;
            return aDefault;
        }
    }
}
