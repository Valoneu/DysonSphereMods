using System.Collections.Concurrent;
using System.Linq;
using static FactoryMultiplier.Util.Log;

namespace FactoryMultiplier.Util
{
    public static class ItemUtil
    {
        private static readonly ConcurrentDictionary<int, ERecipeType> recipeByProtoId = new();

        public static ERecipeType GetRecipeByProtoId(int protoId)
        {
            if (recipeByProtoId.ContainsKey(protoId))
                return recipeByProtoId[protoId];
            logger.LogDebug($"looking up recipe by protoid {protoId}");
            var itemProto = LDB.items.Select(protoId);
            recipeByProtoId[itemProto.ID] = itemProto.prefabDesc.assemblerRecipeType;
            return recipeByProtoId[itemProto.ID];
        }

        private static ConcurrentDictionary<int, byte> rayPhotonReceiverProtos;
        public static bool IsPhotonRayReceiver(int protoId)
        {
            if (rayPhotonReceiverProtos == null)
            {
                rayPhotonReceiverProtos = new ConcurrentDictionary<int, byte>();
                LDB.items.dataArray.ToList().FindAll(i => i.prefabDesc.gammaRayReceiver).ForEach(i => rayPhotonReceiverProtos[i.ID] = 0);
            }
            return rayPhotonReceiverProtos.ContainsKey(protoId);
        }

        // --- FIX IS HERE ---
        private static ItemProto _ejectorProto;
        public static ItemProto ejectorProto
        {
            get
            {
                if (_ejectorProto == null)
                {
                    _ejectorProto = LDB.items.dataArray.ToList().Find(i => i.prefabDesc.isEjector);
                }
                return _ejectorProto;
            }
        }

        private static ItemProto _siloProto;
        public static ItemProto GetSiloProto()
        {
            if (_siloProto == null)
            {
                _siloProto = LDB.items.dataArray.ToList().Find(i => i.prefabDesc.isSilo);
            }
            return _siloProto;
        }
    }
}