using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem.XR;
using System.Linq;

public class NetworkVRPlayer : NetworkBehaviour
{
    //[SerializeField]
    //private Vector2 placementArea = new Vector2(-10.0f, 10.0f);

    public override void OnNetworkSpawn() => DisableClientInput();
    

    public void EnableClientInput()
    {

    }

    public void DisableClientInput()
    {
        if (IsClient && !IsOwner) {
            var clientActionControllers = GetComponentsInChildren<ActionBasedController>();
            var clientDeviceControllers = GetComponentsInChildren<XRBaseController>();
            //var directInteractors = GetComponentsInChildren<XRDirectInteractor>();

            var clientHead = GetComponentInChildren<TrackedPoseDriver>();
            var clientCamera = GetComponentInChildren<Camera>();

            clientCamera.enabled = false;
            clientHead.enabled = false;

            foreach (var input in clientActionControllers)
            {
                input.enableInputActions = false;
                input.enableInputTracking = false;
                input.enabled = false;
            }

            foreach (var DBcontroller in clientDeviceControllers) {
                DBcontroller.enableInputActions = false;
                DBcontroller.enableInputTracking = false;
                //DBcontroller.enabled = false;
            }

            //foreach (var directIteractor in directInteractors) {
            //    directIteractor.enabled = false;
            //}
        }
    }

    private void Start()
    {
        //if (IsClient && IsOwner)
        //{
        //    transform.position = new Vector3(Random.Range(placementArea.x, placementArea.y),
        //        transform.position.y, Random.Range(placementArea.x, placementArea.y));
        //}
    }


    public void OnSelectGrabbable(SelectEnterEventArgs eventArgs)
    {
        if (IsClient && IsOwner)
        {
            var networkObjectSelected = eventArgs.interactableObject.transform.GetComponent<NetworkObject>();
            var weaponRB = eventArgs.interactableObject.transform.GetComponent<Rigidbody>();
            weaponRB.isKinematic = false;
            if (networkObjectSelected != null)
                RequestGrabbableOwnershipServerRpc(
                    OwnerClientId,
                    networkObjectSelected,
                    weaponRB.velocity,
                    weaponRB.angularVelocity,
                    weaponRB.position);
        }
    }

    public void OnReleaseGrabbable(SelectExitEventArgs eventArgs) {
        if (IsClient && IsOwner)
        {
            var networkObjectSelected = eventArgs.interactableObject.transform.GetComponent<NetworkObject>();
            var weaponRB = eventArgs.interactableObject.transform.GetComponent<Rigidbody>();
            weaponRB.isKinematic = false;
            if (networkObjectSelected != null)
                RequestGrabbableOwnershipServerRpc(
                    NetworkManager.ServerClientId,
                    networkObjectSelected,
                    weaponRB.velocity,
                    weaponRB.angularVelocity,
                    weaponRB.position);
        }
    } 

    [ServerRpc(RequireOwnership = false)]
    public void RequestGrabbableOwnershipServerRpc(
        ulong newOwnerClientId, NetworkObjectReference networkObjectReference,
        Vector3 v, Vector3 av, Vector3 p)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            networkObject.ChangeOwnership(newOwnerClientId);
            var acc = NetworkManager.ConnectedClients[newOwnerClientId].PlayerObject.GetComponent<BaseAccessor>();
            acc.Player.RequestGrabbableOwnershipClientRpc(networkObjectReference, v, av, p);
        }
    }

    [ClientRpc]
    public void RequestGrabbableOwnershipClientRpc(
        NetworkObjectReference networkObjectReference,
        Vector3 v, Vector3 av, Vector3 p)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            var r = networkObject.GetComponent<Rigidbody>();
            r.position = p;
            r.velocity = v;
            r.angularVelocity = av;
        }
    }
}
