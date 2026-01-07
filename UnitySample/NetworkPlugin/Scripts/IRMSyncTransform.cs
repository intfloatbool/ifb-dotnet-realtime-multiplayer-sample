using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace NetworkPlugin.Scripts
{
    public class IRMSyncTransform : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private MeshRenderer _meshRenderer;
        private bool _isReady = false;
        private Func<bool> _getIsMine;
        private Action<Vector3> _onLocalPositionUpdated;
        private CompositeDisposable _compositeDisposable = new CompositeDisposable();
        private Vector3? _lastRemotePos;
        private Vector3 _remoteVel;
        
        [SerializeField] private float _syncDelaySeconds = 0.2f;
        
        private float _lastSyncTime;

        public bool IsMine => _getIsMine.Invoke();

        public void Setup(Func<bool> getIsMine, int id, Action<Vector3> onLocalPositionUpdated)
        {

            gameObject.name = $"[{GetType().Name}]_id_{id}";
            
            _onLocalPositionUpdated = onLocalPositionUpdated;
            _getIsMine = getIsMine;
            _isReady = true;

            Color color = Color.magenta;

            switch (id)
            {
                case 0:
                {
                    color = Color.red;
                    break;
                }
                case 1:
                {
                    color = Color.blue;
                    break;
                }
                case 2:
                {
                    color = Color.yellow;
                    break;
                }
            }
            
            _meshRenderer.material.SetColor("_BaseColor", color);
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            bool isMine = _getIsMine();

            if (isMine)
            {
                LocalInputLoop();

                if ((Time.time - _lastSyncTime) > _syncDelaySeconds)
                {
                    _onLocalPositionUpdated?.Invoke(transform.position);
                    _lastSyncTime = Time.time;
                    
                }
            }
            else
            {
                RemoteInputLoop();
            }
        }

        private void LocalInputLoop()
        {
            var inputX = Input.GetAxis("Horizontal");
            var inputZ = Input.GetAxis("Vertical");

            if (Mathf.Approximately(inputX, 0f) && Mathf.Approximately(inputZ, 0f))
            {
                return;
            }
            
            var move = new Vector3(inputX, 0, inputZ);
            transform.Translate(move * _moveSpeed * Time.deltaTime);
        }

        private void RemoteInputLoop()
        {
            if (!_lastRemotePos.HasValue)
            {
                return;
            }
            
            transform.position = Vector3.SmoothDamp(transform.position, _lastRemotePos.Value, ref _remoteVel,  0.16f, float.MaxValue);
                
        }

        public void SetRemotePos(Vector3 remotePos)
        {
            _lastRemotePos= remotePos;
        }

        private void OnDestroy()
        {
            _compositeDisposable.Clear();
        }
    }
}