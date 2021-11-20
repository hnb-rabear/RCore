using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Service
{
    [RequireComponent(typeof(UnityPayment))]
    public class IAPHelper : MonoBehaviour
    {
        #region Members

        private static IAPHelper mInstance;
        public static IAPHelper Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = FindObjectOfType<IAPHelper>();
                return mInstance;
            }
        }

        private UnityPayment m_UnityPayment;

        #endregion

        //=============================================

        #region MonoBehaviour

        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }

        #endregion

        //=============================================

        #region Public

        public void Init(List<string> skus, Action<bool> pOnFinished)
        {
            m_UnityPayment = new UnityPayment();
            m_UnityPayment.Init(skus, pOnFinished);
        }

        public void Purchase(string sku, Action<bool, string> pAction)
        {
            m_UnityPayment.Purchase(sku, pAction);
        }

        public decimal GetLocalizedPrice(string pPackageId)
        {
            return m_UnityPayment.GetLocalizedPrice(pPackageId);
        }

        public string GetLocalizedPriceString(string pPackageId)
        {
            return m_UnityPayment.GetLocalizedPriceString(pPackageId);
        }

        #endregion
    }
}