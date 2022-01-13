using System.Collections.Concurrent;
using System.Linq;
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

        private static ConcurrentDictionary<int, byte> _siloProtos;
        public static bool IsSilo(int protoId)
        {
            if (_siloProtos == null)
            {
                _siloProtos = new ConcurrentDictionary<int, byte>();
                LDB.items.dataArray.ToList().FindAll(i => i.prefabDesc.isSilo).ForEach(i => _siloProtos[i.ID] = 0);
            }
            return _siloProtos.ContainsKey(protoId);
        }

        private static ConcurrentDictionary<int, byte> _rayPhotonReceiverProtos;
        public static bool IsPhotonRayReceiver(int protoId)
        {
            if (_rayPhotonReceiverProtos == null)
            {
                _rayPhotonReceiverProtos = new ConcurrentDictionary<int, byte>();
                LDB.items.dataArray.ToList().FindAll(i => i.prefabDesc.gammaRayReceiver).ForEach(i => _rayPhotonReceiverProtos[i.ID] = 0);
            }
            return _rayPhotonReceiverProtos.ContainsKey(protoId);
        }

    }
}