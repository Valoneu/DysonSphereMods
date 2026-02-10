using System.Collections.Concurrent;
using System.Linq;
using DysonSphereMods.Shared;

namespace FactoryMultiplier.Util
{
    public static class ItemUtil
    {
        private static readonly ConcurrentDictionary<int, ERecipeType> _recipeByProtoId = new();

        public static ERecipeType GetRecipeByProtoId(int protoId)
        {
            if (_recipeByProtoId.TryGetValue(protoId, out var type))
                return type;
            
            var itemProto = LDB.items.Select(protoId);
            if (itemProto?.prefabDesc != null)
            {
                type = itemProto.prefabDesc.assemblerRecipeType;
                _recipeByProtoId[protoId] = type;
                return type;
            }
            return ERecipeType.None;
        }

        private static ConcurrentDictionary<int, byte> _rayPhotonReceiverProtos;
        public static bool IsPhotonRayReceiver(int protoId)
        {
            if (_rayPhotonReceiverProtos == null)
            {
                _rayPhotonReceiverProtos = new ConcurrentDictionary<int, byte>();
                foreach (var item in LDB.items.dataArray)
                {
                    if (item.prefabDesc.gammaRayReceiver)
                        _rayPhotonReceiverProtos[item.ID] = 0;
                }
            }
            return _rayPhotonReceiverProtos.ContainsKey(protoId);
        }

        private static ItemProto _ejectorProto;
        public static ItemProto EjectorProto
        {
            get
            {
                if (_ejectorProto == null)
                {
                    _ejectorProto = LDB.items.dataArray.FirstOrDefault(i => i.prefabDesc.isEjector);
                }
                return _ejectorProto;
            }
        }

        private static ItemProto _siloProto;
        public static ItemProto SiloProto
        {
            get
            {
                if (_siloProto == null)
                {
                    _siloProto = LDB.items.dataArray.FirstOrDefault(i => i.prefabDesc.isSilo);
                }
                return _siloProto;
            }
        }
    }
}