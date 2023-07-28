using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Varyu.ExclusiveSpawner
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExclusiveSpawnerV2 : UdonSharpBehaviour
    {
        /// <summary> 割り当てを確認する回数 </summary>
        private const int CHECK_COUNT = 4;

        [SerializeField] private Transform VRCWorldSpawn;
        [SerializeField] private Transform[] SpawnPoints;
        [SerializeField] private GameObject EnableObject;

        [UdonSynced] public int[] RoomUserArray;
        private int localPlayerId = -1;
        private int checkedCount = 0;
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
            checkedCount = 0;
            isAssigned = false;
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

        /// <summary> スポーン地点の設定 </summary>
        private void SetSpawn(int roomNumber)
        {
            // ワールドスポーンを移動
            VRCWorldSpawn.position = SpawnPoints[roomNumber].position;
            VRCWorldSpawn.rotation = SpawnPoints[roomNumber].rotation;

            // EnableObjectを移動して可視化
            EnableObject.transform.position = SpawnPoints[roomNumber].position;
            EnableObject.SetActive(true);
        }

        /// <summary>
        /// 自分が適切に割り当てられているかチェック
        /// </summary>
        public void CheckAssign()
        {
            RequestSerialization();
            for (int i = 0; i < RoomUserArray.Length; i++)
            {
                if (RoomUserArray[i] == localPlayerId)
                {
                    checkedCount++;
                    if (checkedCount < CHECK_COUNT)
                    {
                        // チェック回数が足りなかったらもう一度チェック
                        SendCustomEventDelayedFrames(nameof(CheckAssign), delayFrame);
                        return;
                    }

                    // チェック回数が足りたらスポーン地点変更
                    SetSpawn(i);

                    // 初回ならばテレポート
                    if (!isAssigned)
                    {
                        _localPlayer.TeleportTo(VRCWorldSpawn.position, VRCWorldSpawn.rotation);
                    }
                    isAssigned = true;

                    return;
                }
            }

            // 割り当てが無かったら部屋の割り当てをランダムフレーム後に遅延実行
            SendCustomEventDelayedFrames(nameof(AssignRoom), delayFrame);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            // 必要なかったら終わり
            if (!Utilities.IsValid(player)) return;

            CheckAssign();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;

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
