using BaseX;
using FrooxEngine;
using HarmonyLib;
using NeosAssetImportHook;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NeosDynamicAssetInfo
{

    public class NeosDynamicAssetInfoMod : NeosMod
    {
        public override string Name => "NeosDynamicAssetInfo";
        public override string Author => "mpmxyz";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/mpmxyz/NeosDynamicAssetInfo/";
        public override void OnEngineInit()
        {
            //TODO: mod options: non-persistent, feature flags, enabled/disabled
            AssetImportHooks.PostImport += AttachGeneralAssetInfo;
        }

        private const string SPACE_NAME = "asset";
        private const string DV_SLOT_NAME = "asset_dvs";

        private const string ASSET_COUNT_NAME = "asset_count";
        private const string GRABBABLE_NAME = "grabbable";
        private const string URL_PREFIX = "url";
        private const string ASSET_PREFIX = "asset";

        private static void AttachGeneralAssetInfo(Slot slot, Type mainAssetType, IEnumerable<IAssetProvider> allAssets)
        {
            GetDynamicLocations(slot, out var dv_slot, out var dv_space);

            var grabbable = slot.GetComponent<Grabbable>();
            if (grabbable != null)
            {
                AttachReference(dv_slot, dv_space, GRABBABLE_NAME, grabbable);
            }

            AttachValue(dv_slot, dv_space, ASSET_COUNT_NAME, allAssets.Count());

            int i = 0;
            foreach (IAssetProvider asset in allAssets)
            {
                AttachReference(dv_slot, dv_space, ASSET_PREFIX + i, asset);
                
                foreach (Type assetType in EnumerateAllGenericInterfaceVariants(asset.GetType(), typeof(IAssetProvider<>)))
                {
                    typeof(NeosDynamicAssetInfoMod)
                        .GetMethod(nameof(AttachReference), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(assetType)
                        .Invoke(null, new object[] { dv_slot, dv_space, ASSET_PREFIX + i, asset, false });
                }

                if (asset is IStaticAssetProvider staticAsset)
                {
                    AttachValue(dv_slot, dv_space, URL_PREFIX + i, staticAsset.URL);
                }

                i++;
            }
        }

        private static IEnumerable<Type> EnumerateAllGenericInterfaceVariants(Type type, Type genericInterfaceType)
        {
            foreach (Type interfaceType in type.EnumerateInterfacesRecursively())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterfaceType)
                {
                    yield return interfaceType;
                }
            }
        }

        private static void GetDynamicLocations(Slot slot, out Slot dv_slot, out DynamicVariableSpace dv_space)
        {
            dv_space = GetDynamicVariableSpace(slot);

            if (!dv_space.TryReadValue(DV_SLOT_NAME, out dv_slot) || dv_slot == null)
            {
                dv_slot = slot.AddSlot(DV_SLOT_NAME);
                if (!dv_space.TryWriteValue(DV_SLOT_NAME, dv_slot))
                {
                    var dv = dv_slot.CreateReferenceVariable(DV_SLOT_NAME, dv_slot);
                    dv.UpdateLinking(); //is missing in CreateReferenceVariable
                }
            }
        }
        private static DynamicVariableSpace GetDynamicVariableSpace(Slot slot)
        {
            var space = slot.FindSpace(SPACE_NAME);

            if (space == null)
            {
                space = slot.AttachComponent<DynamicVariableSpace>();
                space.SpaceName.Value = SPACE_NAME;
            }

            return space;
        }

        private static void AttachValue<T>(Slot dv_slot, DynamicVariableSpace dv_space, string name, T value, bool indexed = false)
        {
            if (indexed)
            {
                name = GetIndexedName<T>(dv_space, name);
            }

            if (!dv_space.TryWriteValue(name, value))
            {
                dv_slot.CreateVariable(name, value);
            }
        }

        private static void AttachReference<T>(Slot dv_slot, DynamicVariableSpace dv_space, string name, T value, bool indexed = false) where T : class, IWorldElement
        {
            if (indexed)
            {
                name = GetIndexedName<T>(dv_space, name);
            }

            if (!dv_space.TryWriteValue(name, value))
            {
                var dv = dv_slot.CreateReferenceVariable(name, value);
                dv.UpdateLinking(); //missing in CreateReferenceVariable
            }
        }

        private static string GetIndexedName<T>(DynamicVariableSpace dv_space, string name)
        {
            var index = 0;
            string indexedName;
            do
            {
                indexedName = $"name{index}";
                index++;
            } while (dv_space.TryReadValue<T>(indexedName, out var ignored));
            return indexedName;
        }
    }
}