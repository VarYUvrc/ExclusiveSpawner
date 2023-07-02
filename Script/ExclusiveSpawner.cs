using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Varyu.ExclusiveSpawner
{
      [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
      public class ExclusiveSpawner : UdonSharpBehaviour
      {
            [SerializeField] private Transform VRCWorldSpawn;
            [SerializeField] private Transform[] SpawnPoints;
            [SerializeField] private GameObject[] EnableObjects;

            [UdonSynced] public int[] RoomUserArray;
            private int localPlayerId = -1;
            private int delayFrame = -1;
            private VRCPlayerApi _localPlayer;

            void Start()
            {
                  _localPlayer = Networking.LocalPlayer;
                  if (_localPlayer == null) return;

                  RoomUserArray = new int[SpawnPoints.Length];
                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        RoomUserArray[i] = -1;
                  }

                  localPlayerId = _localPlayer.playerId;
            }

            public void AssignRoom()
            {
                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        Networking.SetOwner(_localPlayer, gameObject);
                        RequestSerialization();
                        if (RoomUserArray[i] == -1)
                        {
                              RoomUserArray[i] = localPlayerId;
                              TeleportSequence(i);
                              break;
                        }
                  }
            }

            private void TeleportSequence(int roomNumber)
            {
                  // ワールドスポーンを移動
                  VRCWorldSpawn.position = SpawnPoints[roomNumber].position;
                  VRCWorldSpawn.rotation = SpawnPoints[roomNumber].rotation;

                  // そこにワープ
                  _localPlayer.TeleportTo(VRCWorldSpawn.position, VRCWorldSpawn.rotation);

                  // EnableObjectsの処理
                  for (int i = 0; i < EnableObjects.Length; i++)
                  {
                        EnableObjects[i].SetActive(i == roomNumber);
                  }
            }

            public override void OnPlayerJoined(VRCPlayerApi player)
            {
                  // 必要なかったら終わり
                  if (!Utilities.IsValid(player)) return;
                  if (!player.isLocal) return;

                  // 部屋の割り当てを遅延実行
                  // 同時にJoinした人がいた場合の安全策
                  delayFrame = localPlayerId % RoomUserArray.Length;
                  SendCustomEventDelayedFrames(nameof(AssignRoom), delayFrame * 3);
            }
            public override void OnPlayerLeft(VRCPlayerApi player)
            {
                  if (!Networking.IsOwner(_localPlayer, gameObject)) return;

                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        if (RoomUserArray[i] == player.playerId)
                        {
                              RoomUserArray[i] = -1;
                              break;
                        }
                  }
            }
      }
}