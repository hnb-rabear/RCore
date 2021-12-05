using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace RCore.Common
{

    [Serializable]
    public class AddressableCustomPool<TComponent> where TComponent : Component
    {
#if ADDRESSABLES
        [Serializable]
        public class Address
        {
            public string address;
            public AssetReference reference;
            public ComponentRef<TComponent> componentReference;
            public Address(string pAddress)
            {
                address = pAddress;
            }
            public Address(AssetReference pReference)
            {
                reference = pReference;
            }
            public Address(ComponentRef<TComponent> pComponentReference)
            {
                componentReference = pComponentReference;
            }
            public void LoadAsset(Action<TComponent> pCallback)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    AddressableUtil.LoadAssetAsync(address, null, pCallback);
                }
                else if (reference != null && reference.RuntimeKey.ToString() != "")
                {
                    if (reference.Asset == null)
                        AddressableUtil.LoadPrefabAsync(reference, null, pCallback);
                    else
                    {
                        (reference.Asset as GameObject).TryGetComponent(out TComponent prefab);
                        pCallback?.Invoke(prefab);
                    }
                }
                else if (componentReference != null && componentReference.RuntimeKey.ToString() != "")
                {
                    if (componentReference.Asset == null)
                        AddressableUtil.LoadPrefabAsync(componentReference, null, pCallback);
                    else
                    {
                        (componentReference.Asset as GameObject).TryGetComponent(out TComponent prefab);
                        pCallback?.Invoke(prefab);
                    }
                }
            }
            public void ReleaseAsset()
            {
                if (reference != null)
                    reference.ReleaseAsset();
                if (componentReference != null)
                    componentReference.ReleaseAsset();
            }
        }

        [SerializeField] protected Address m_Addresss;
        [SerializeField] protected Transform m_Parent;
        [SerializeField] protected string m_Name;
        [SerializeField] protected bool m_PushToLastSibling;
        [SerializeField] protected bool m_AutoRelocate;
        [SerializeField] protected int m_LimitNumber;

        protected int m_InitialCount;
        protected CustomPool<TComponent> m_CustomPool;

#region Init

        public AddressableCustomPool(ComponentRef<TComponent> pComponentReference, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Addresss = new Address(pComponentReference);
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public AddressableCustomPool(AssetReference pReference, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Addresss = new Address(pReference);
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public AddressableCustomPool(string pAddress, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Addresss = new Address(pAddress);
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public AddressableCustomPool(Address pAddress, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Addresss = pAddress;
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            InitAssetReference();
        }

        public void ReleaseAssetReference()
        {
            m_Addresss.ReleaseAsset();
        }

        public void InitAssetReference()
        {
            if (m_CustomPool != null)
                return;
            m_Addresss.LoadAsset(result =>
            {
                if (result != null)
                    m_CustomPool = new CustomPool<TComponent>(result, m_InitialCount, m_Parent, m_Name, m_AutoRelocate);
            });
        }

#endregion

#region Pool Methods

        public void Prepare(int pInitialCount)
        {
            m_CustomPool?.Prepare(pInitialCount);
        }

        public TComponent Spawn()
        {
            return m_CustomPool?.Spawn();
        }

        public TComponent Spawn(Transform pPoint)
        {
            return m_CustomPool?.Spawn(pPoint);
        }

        public TComponent Spawn(Vector3 position, bool pIsWorldPosition)
        {
            return m_CustomPool?.Spawn(position, pIsWorldPosition);
        }

        public TComponent Spawn(Vector3 position, bool pIsWorldPosition, ref bool pReused)
        {
            return m_CustomPool?.Spawn(position, pIsWorldPosition, ref pReused);
        }

        public void AddOutsiders(List<TComponent> pInSceneObjs)
        {
            m_CustomPool?.AddOutsiders(pInSceneObjs);
        }

        public void AddOutsider(TComponent pInSceneObj)
        {
            m_CustomPool?.AddOutsider(pInSceneObj);
        }

        public void Release(TComponent pObj)
        {
            m_CustomPool?.Release(pObj);
        }

        public void Release(TComponent pObj, float pDelay)
        {
            m_CustomPool?.Release(pObj, pDelay);
        }

        public void Release(TComponent pObj, ConditionalDelegate pCondition)
        {
            m_CustomPool?.Release(pObj, pCondition);
        }

        public void Release(GameObject pObj)
        {
            m_CustomPool?.Release(pObj);
        }

        public void Release(GameObject pObj, float pDelay)
        {
            m_CustomPool?.Release(pObj, pDelay);
        }

        public void Release(GameObject pObj, ConditionalDelegate pCondition)
        {
            m_CustomPool?.Release(pObj, pCondition);
        }

        public void ReleaseAll()
        {
            m_CustomPool?.ReleaseAll();
        }

        public void DestroyAll()
        {
            m_CustomPool?.DestroyAll();
        }

        public void Destroy(TComponent pItem)
        {
            m_CustomPool?.Destroy(pItem);
        }

        public TComponent FindFromActive(TComponent t)
        {
            return m_CustomPool?.FindFromActive(t);
        }

        public TComponent FindComponent(GameObject pObj)
        {
            return m_CustomPool?.FindComponent(pObj);
        }

        public TComponent GetFromActive(int pIndex)
        {
            return m_CustomPool?.GetFromActive(pIndex);
        }

        public TComponent FindFromInactive(TComponent t)
        {
            return FindFromInactive(t);
        }

        public void RelocateInactive()
        {
            m_CustomPool?.RelocateInactive();
        }

        public void SetParent(Transform pParent)
        {
            m_Parent = pParent;
            m_CustomPool?.SetParent(pParent);
        }

        public void SetName(string pName)
        {
            m_Name = pName;
            m_CustomPool?.SetName(pName);
        }

#endregion

#endif
    }
}