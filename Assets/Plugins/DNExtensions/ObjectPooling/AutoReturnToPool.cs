using System;
using UnityEngine;

namespace DNExtensions.ObjectPooling
{
        public class AutoReturnToPool : MonoBehaviour, IPooledObject
        {


                private float _lifeTime;
                private bool _isInitialized;


                private void Update()
                {
                        if (!_isInitialized) return;

                        _lifeTime -= Time.deltaTime;
                        if (_lifeTime <= 0f)
                        {
                                _isInitialized = false;
                                ObjectPooler.ReturnObjectToPool(gameObject);
                        }
                }


                public void Initialize(float lifeTime)
                {
                        _lifeTime = lifeTime;
                        _isInitialized = true;
                }

                public void OnPoolGet()
                {

                }

                public void OnPoolReturn()
                {

                }

                public void OnPoolRecycle()
                {
                        _isInitialized = false;
                }
        }
}