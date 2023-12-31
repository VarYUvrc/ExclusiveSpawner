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
            private VRCPlayerApi _localPlayer;
            private bool isAssigned = false;
            private int delayFrame => Random.Range(0, 30);

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

            /// <summary>
            /// 部屋の割り当てを行う
            /// </summary>
            public void AssignRoom()
            {
                  RequestSerialization();
                  Networking.SetOwner(_localPlayer, gameObject);

                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        // 空き部屋があったら自分のIdを登録する
                        if (RoomUserArray[i] == -1)
                        {
                              RoomUserArray[i] = localPlayerId;
                              // 部屋の割り当てが成功したかチェックする
                              SendCustomEventDelayedFrames(nameof(CheckAssign), delayFrame);
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

            /// <summary>
            /// 自分が適切に割り当てられているかチェック
            /// </summary>
            public void CheckAssign()
            {
                  if (isAssigned) return;

                  RequestSerialization();
                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        if (RoomUserArray[i] == localPlayerId)
                        {
                              // ちゃんと割り当てられていたらテレポート
                              TeleportSequence(i);
                              isAssigned = true;
                              return;
                        }
                  }

                  // 割り当てが無かったら部屋の割り当てをランダム遅延実行
                  SendCustomEventDelayedFrames(nameof(AssignRoom), delayFrame);
            }

            public override void OnPlayerJoined(VRCPlayerApi player)
            {
                  // 必要なかったら終わり
                  if (!Utilities.IsValid(player)) return;
                  if (!player.isLocal) return;

                  CheckAssign();
            }
            public override void OnPlayerLeft(VRCPlayerApi player)
            {
                  if (!Networking.IsOwner(_localPlayer, gameObject)) return;

                  for (int i = 0; i < RoomUserArray.Length; i++)
                  {
                        // Leaveしたプレイヤーが使っていた要素を-1にする
                        if (RoomUserArray[i] == player.playerId)
                        {
                              RoomUserArray[i] = -1;
                              break;
                        }
                  }
            }
      }
}