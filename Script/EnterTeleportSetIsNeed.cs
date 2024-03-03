
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Varyu.ExclusiveSpawner
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EnterTeleportSetIsNeed : UdonSharpBehaviour
    {
        [SerializeField] GameObject[] HideObject;
        [SerializeField] GameObject ShowObject;
        [SerializeField] Transform Destination;
        [SerializeField] private ExclusiveSpawnerV2 ExclusiveSpawner;
        int _localPlayerID;

        void Start()
        {
            _localPlayerID = Networking.LocalPlayer.playerId;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if(!player.isLocal) return;
            ShowObject.SetActive(true);
            foreach (GameObject obj in HideObject)
            {
                obj.SetActive(false);
            }
            if(player.playerId == _localPlayerID)player.TeleportTo(Destination.position,Destination.rotation);
            ExclusiveSpawner.SetIsNeedTeleport(false);
        }
    }
}
