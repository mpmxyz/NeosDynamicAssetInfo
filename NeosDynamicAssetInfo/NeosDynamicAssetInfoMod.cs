using BaseX;
using FrooxEngine;
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
        public override string Version => "2.1.0";
        public override string Link => "https://github.com/mpmxyz/NeosDynamicAssetInfo/";

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_ENABLED = new ModConfigurationKey<bool>("enabled", "Enable injecting dynamic variables into imports", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_NON_PERSISTENT = new ModConfigurationKey<bool>("nonPersistent", "Prevent attached information from being a permanent part of the asset", () => false);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_GRABBABLE = new ModConfigurationKey<bool>("grabbable", "Attach reference to grabbable component", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_NON_GENERIC_REFERENCES = new ModConfigurationKey<bool>("nonGeneric", "Attach generic IAssetProvider<T> values", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_GENERIC_REFERENCES = new ModConfigurationKey<bool>("generic", "Attach generic IAssetProvider<T> values", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> KEY_URL = new ModConfigurationKey<bool>("url", "Attach asset url", () => true);

        private static NeosDynamicAssetInfoMod Instance;

        private static bool Enabled => Instance.GetConfiguration().GetValue(KEY_ENABLED);
        private static bool Persistent => !Instance.GetConfiguration().GetValue(KEY_NON_PERSISTENT);
        private static bool EnableGrabbable => Instance.GetConfiguration().GetValue(KEY_GRABBABLE);
        private static bool EnableNonGeneric => Instance.GetConfiguration().GetValue(KEY_NON_GENERIC_REFERENCES);
        private static bool EnableGeneric => Instance.GetConfiguration().GetValue(KEY_GENERIC_REFERENCES);
        private static bool EnableURL => Instance.GetConfiguration().GetValue(KEY_URL);

        public override void OnEngineInit()
        {
            Instance = this;
            AssetImportHooks.PostImport += AttachGeneralAssetInfo;
        }

        private const string SPACE_NAME = "asset";
        private const string DV_SLOT_NAME = "asset_dvs";

        private const string ASSET_COUNT_NAME = "asset_count";
        private const string GRABBABLE_NAME = "grabbable";
        private const string URL_PREFIX = "url";
        private const string ASSET_PREFIX = "asset";

        private static void AttachGeneralAssetInfo(Slot slot, Type mainAssetType, IList<IAssetProvider> allAssets)
        {
            if (Enabled)
            {
                GetDynamicLocations(slot, out var dv_slot, out var dv_space);

                var grabbable = slot.GetComponent<Grabbable>();
                if (grabbable != null && EnableGrabbable)
                {
                    AttachReference(dv_slot, dv_space, GRABBABLE_NAME, grabbable);
                }

                AttachValue(dv_slot, dv_space, ASSET_COUNT_NAME, allAssets.Count());

                int i = 0;
                foreach (IAssetProvider asset in allAssets)
                {
                    if (EnableNonGeneric)
                    {
                        AttachReference(dv_slot, dv_space, ASSET_PREFIX + i, asset);
                    }

                    if (EnableGeneric)
                    {
                        foreach (Type assetType in EnumerateAllGenericInterfaceVariants(asset.GetType(), typeof(IAssetProvider<>)))
                        {
                            typeof(NeosDynamicAssetInfoMod)
                                .GetMethod(nameof(AttachReference), BindingFlags.NonPublic | BindingFlags.Static)
                                .MakeGenericMethod(assetType)
                                .Invoke(null, new object[] { dv_slot, dv_space, ASSET_PREFIX + i, asset, false });
                        }
                    }

                    if (asset is IStaticAssetProvider staticAsset && EnableURL)
                    {
                        AttachValue(dv_slot, dv_space, URL_PREFIX + i, staticAsset.URL);
                    }

                    i++;
                }
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
            dv_space = GetDynamicVariableSpace(slot, out var spaceWasCreated);

            if (!dv_space.TryReadValue(DV_SLOT_NAME, out dv_slot) || dv_slot == null)
            {
                dv_slot = slot.AddSlot(DV_SLOT_NAME, Persistent);
                AttachReference(dv_slot, dv_space, DV_SLOT_NAME, dv_slot);
            }
            if (spaceWasCreated)
            {
                dv_slot.DestroyWhenDestroyed(dv_space);
            }
        }
        private static DynamicVariableSpace GetDynamicVariableSpace(Slot slot, out bool wasCreated)
        {
            var space = slot.FindSpace(SPACE_NAME);

            if (space == null)
            {
                space = slot.AttachComponent<DynamicVariableSpace>();
                space.Persistent = Persistent;
                space.SpaceName.Value = SPACE_NAME;
                wasCreated = true;
            }
            else
            {
                wasCreated = false;
            }

            return space;
        }

        private static void AttachValue<T>(Slot dv_slot, DynamicVariableSpace dv_space, string name, T value)
        {
            if (!dv_space.TryWriteValue(name, value))
            {
                dv_slot.CreateVariable(name, value);
            }
        }

        private static void AttachReference<T>(Slot dv_slot, DynamicVariableSpace dv_space, string name, T value) where T : class, IWorldElement
        {
            if (!dv_space.TryWriteValue(name, value))
            {
                var dv = dv_slot.CreateReferenceVariable(name, value);
                dv.UpdateLinking(); //missing in CreateReferenceVariable
            }
        }
    }
}