/**
 * Author NBear - nbhung71711@gmail.com - 2017
 **/

using RCore.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using static UnityEngine.AddressableAssets.Addressables;
#endif

namespace RCore.Common
{
    [Obsolete("Use AddressableCustomPool instead")]
    [Serializable]
    public class AddressablePool<TComponent> : CustomPool<TComponent> where TComponent : Component
    {
#if ADDRESSABLES
        [Header("Addressable Asset")]
        [SerializeField] protected AssetReference m_AssetReference;
        [SerializeField] protected ComponentRef<TComponent> m_ComponentReference;

        public AddressablePool(ComponentRef<TComponent> pAssetReference, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_ComponentReference = pAssetReference;
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public AddressablePool(AssetReference pAssetReference, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_AssetReference = pAssetReference;
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public void InitAssetReference()
        {
            if (m_Prefab != null)
            {
                Init();
                return;
            }
            if (m_ComponentReference != null && m_ComponentReference.RuntimeKey.ToString() != "")
            {
                if (m_ComponentReference.Asset == null)
                {
                    AddressableUtil.LoadPrefabAsync(m_ComponentReference, null, result =>
                    {
                        m_Prefab = result;
                        if (m_Prefab == null)
                            Debug.LogError($"Not found component {typeof(TComponent).FullName} in gameobject {result.name}");
                        Init();
                    });
                }
                else
                    (m_ComponentReference.Asset as GameObject).TryGetComponent(out m_Prefab);
            }

            if (m_AssetReference != null && m_AssetReference.RuntimeKey.ToString() != "")
            {
                if (m_AssetReference.Asset == null)
                {
                    AddressableUtil.LoadPrefabAsync<TComponent>(m_AssetReference, null, result =>
                    {
                        m_Prefab = result;
                        if (m_Prefab == null)
                            Debug.LogError($"Not found component {typeof(TComponent).FullName} in gameobject {result.name}");
                        Init();
                    });
                }
                else
                    (m_AssetReference.Asset as GameObject).TryGetComponent(out m_Prefab);
            }
        }

        public void ReleaseAssetReference()
        {
            m_ComponentReference.ReleaseAsset();
        }
#endif
    }
}