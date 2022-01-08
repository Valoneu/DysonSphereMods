using System.Collections.Concurrent;
using static FactoryMultiplier.Util.Log;

namespace FactoryMultiplier.Util
{
    public static class ItemUtil
    {
        private static ConcurrentDictionary<int, ERecipeType> recipeByProtoId = new();

        public static ERecipeType GetRecipeByProtoId(int protoId)
        {
            if (recipeByProtoId.ContainsKey(protoId))
                return recipeByProtoId[protoId];
            logger.LogDebug($"looking up recipe by protoid {protoId}");
            var itemProto = LDB.items.Select(protoId);
            recipeByProtoId[itemProto.ID] = itemProto.prefabDesc.assemblerRecipeType;
            return recipeByProtoId[itemProto.ID];
        }
    }
}