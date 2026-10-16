// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalLogic.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Avatar;
using UltimateXR.CameraUtils;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UltimateXR.Devices.Keyboard;
using UltimateXR.Examples.FullScene.Doors;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Math;
using UltimateXR.Locomotion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateXR.Examples.FullScene
{
    public class GlobalLogic : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [Header("Spawn positions")][SerializeField] private Transform _spawnMain;
        // [SerializeField]                              private Transform _spawnLab; // Referencia Null en Inspector
        // [SerializeField]                              private Transform _spawnControllers; // Referencia Null en Inspector
        // [SerializeField]                              private Transform _spawnShootingRange; // Referencia Null en Inspector

        [Header("Location volumes")][SerializeField] private BoxCollider _boxSpawnRoomMirror;
        [SerializeField] private BoxCollider _boxSpawnRoomDoor;
        // [SerializeField]                              private BoxCollider _boxCentralRoom; // Referencia Null en Inspector
        // [SerializeField]                              private BoxCollider _boxLabRoom; // Referencia Null en Inspector
        // [SerializeField]                              private BoxCollider _boxControllerRoom; // Referencia Null en Inspector
        // [SerializeField]                              private BoxCollider _boxShootingRange; // Referencia Null en Inspector

        [Header("Relevant elements")][SerializeField] private GameObject _rootUnrestrictedArea;
        // [SerializeField]                               private GameObject   _rootRestrictedArea; // Referencia Null en Inspector
        // [SerializeField]                               private UxrComponent _mirrorComponent; // Referencia Null en Inspector
        // [SerializeField]                               private GameObject   _controllerRoomElements; // Referencia Null en Inspector
        // [SerializeField]                               private GameObject   _rootLabElements; // Referencia Null en Inspector
        [SerializeField] private ArmoredDoor _armoredDoor;

        #endregion

        #region Unity

        /// <summary>
        ///     Subscribes to avatar events.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            UxrManager.AvatarMoved += UxrManager_AvatarMoved;
            UxrManager.AvatarsUpdated += UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Unsubscribes from avatar events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            UxrManager.AvatarMoved -= UxrManager_AvatarMoved;
            UxrManager.AvatarsUpdated -= UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Initializes the visible elements.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            if (_spawnMain != null)
            {
                UxrManager.Instance.MoveAvatarTo(UxrAvatar.LocalAvatar, _spawnMain);
            }

            UpdateVisibility();
        }

        /// <summary>
        ///     Handles some keyboard shortcuts to reset, quit or quick spawn to different places.
        /// </summary>
        private void Update()
        {
            if (UxrKeyboardInput.GetPressDown(UxrKey.Enter))
            {
                SceneManager.LoadScene(0);
            }
            else if (UxrKeyboardInput.GetPressDown(UxrKey.Q))
            {
                Application.Quit();
            }

            if (UxrKeyboardInput.GetPressDown(UxrKey.Digit1) && _spawnMain != null)
            {
                UxrManager.Instance.TeleportLocalAvatar(_spawnMain.position, _spawnMain.rotation, UxrTranslationType.Fade);
            }
            // else if (UxrKeyboardInput.GetPressDown(UxrKey.Digit2))
            // {
            //     // Comentado para evitar NullReferenceException porque _spawnLab es null
            //     UxrManager.Instance.TeleportLocalAvatar(_spawnLab.position, _spawnLab.rotation, UxrTranslationType.Fade);
            // }
            // else if (UxrKeyboardInput.GetPressDown(UxrKey.Digit3))
            // {
            //     // Comentado para evitar NullReferenceException porque _spawnControllers es null
            //     UxrManager.Instance.TeleportLocalAvatar(_spawnControllers.position, _spawnControllers.rotation, UxrTranslationType.Fade);
            // }
            // else if (UxrKeyboardInput.GetPressDown(UxrKey.Digit4))
            // {
            //     // Comentado para evitar NullReferenceException porque _spawnShootingRange es null
            //     UxrManager.Instance.TeleportLocalAvatar(_spawnShootingRange.position, _spawnShootingRange.rotation, UxrTranslationType.Fade);
            // }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when the avatar moved/teleported. We use it to enable/disable objects based on potential visibility.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event parameters</param>
        private void UxrManager_AvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            UpdateVisibility();
        }

        /// <summary>
        ///     Called each frame after all avatars have been updated.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            if (UxrAvatar.LocalAvatar == null || UxrCameraWallFade.IsAvatarPeekingThroughGeometry(UxrAvatar.LocalAvatar))
            {
                return;
            }

            // CORRECCIÓN: Mantenemos el cuarto principal (_rootUnrestrictedArea) siempre encendido
            if (_rootUnrestrictedArea != null)
            {
                _rootUnrestrictedArea.CheckSetActive(true);
            }

            // Comentado porque _rootRestrictedArea y _boxShootingRange son null
            // _rootRestrictedArea.CheckSetActive(UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxShootingRange) || _armoredDoor.OpenValue > 0.0f);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the visible elements based on the current avatar position.
        /// </summary>
        private void UpdateVisibility()
        {
            if (UxrAvatar.LocalAvatar == null || UxrCameraWallFade.IsAvatarPeekingThroughGeometry(UxrAvatar.LocalAvatar))
            {
                return;
            }

            // CORRECCIÓN: Mantenemos el cuarto principal (_rootUnrestrictedArea) siempre encendido
            if (_rootUnrestrictedArea != null)
            {
                _rootUnrestrictedArea.CheckSetActive(true);
            }

            // Comentado porque _mirrorComponent es null
            // _mirrorComponent.CheckSetEnabled(UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxSpawnRoomMirror));

            // Comentado porque _rootRestrictedArea y _boxShootingRange son null
            // _rootRestrictedArea.CheckSetActive(UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxShootingRange) || _armoredDoor.OpenValue > 0.0f);

            // if (UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxSpawnRoomMirror))
            // {
            //     // _controllerRoomElements y _rootLabElements son null
            //     // _controllerRoomElements.CheckSetActive(false);
            //     // _rootLabElements.CheckSetActive(false);
            // }
            // else if (UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxSpawnRoomDoor))
            // {
            //     // _controllerRoomElements y _rootLabElements son null
            //     // _controllerRoomElements.CheckSetActive(false);
            //     // _rootLabElements.CheckSetActive(true);
            // }
            // else if (UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxCentralRoom) || UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxLabRoom) || UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxControllerRoom))
            // {
            //     // _boxCentralRoom, _boxLabRoom, _boxControllerRoom, _controllerRoomElements y _rootLabElements son null
            //     // _controllerRoomElements.CheckSetActive(true);
            //     // _rootLabElements.CheckSetActive(true);
            // }
            // else if (UxrAvatar.LocalAvatar.CameraPosition.IsInsideBox(_boxShootingRange))
            // {
            //     // _boxShootingRange, _controllerRoomElements y _rootLabElements son null
            //     // _controllerRoomElements.CheckSetActive(false);
            //     // _rootLabElements.CheckSetActive(false);
            // }
        }

        #endregion
    }
}