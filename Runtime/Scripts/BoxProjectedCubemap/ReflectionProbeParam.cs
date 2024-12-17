using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ST.Effect
{
    /// <summary>
    /// 
    /// </summary>
    [ExecuteInEditMode]
    public class ReflectionProbeParam : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        ReflectionProbe m_ReflProbe;

        /// <summary>
        /// 
        /// </summary>
        [Header("Reflection Probe 中心点")]
        public Vector3 reflProbeCenter;

        /// <summary>
        /// 
        /// </summary>
        [Header("Reflection Probe 碰撞Box的最小点")]
        public Vector3 reflProbeBoxMin;

        /// <summary>
        /// 
        /// </summary>
        [Header("Reflection Probe 碰撞Box的最大点")]
        public Vector3 reflProbeBoxMax;

        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        void Start()
        {
            m_ReflProbe = GetComponent<ReflectionProbe>();
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        void Update()
        {
            if (m_ReflProbe != null)
            {
                reflProbeCenter = m_ReflProbe.bounds.center;
                reflProbeBoxMin = m_ReflProbe.bounds.min;
                reflProbeBoxMax = m_ReflProbe.bounds.max;
            }
        }
    }
}

