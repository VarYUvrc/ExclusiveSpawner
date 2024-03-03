using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Varyu.ExclusiveSpawner
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExclusiveSpawnerV2 : UdonSharpBehaviour
    {
        private const int MAX_CHECK_COUNT = 4;

        [SerializeField] private Transform VRCWorldSpawn;
        [SerializeField] private Transform[] SpawnPoints;
        [SerializeField, Header("スポーン位置確定後に表示するObject")]
        private GameObject EnableObject;

        [UdonSynced] private int[] RoomUserArray;
        private int localPlayerId = -1;
        private VRCPlayerApi _localPlayer;
        private bool isFirstTime = true;
        private bool isNeedTeleport = false;
        private int checkedCount = 0;
        private int currentRoom = -1;
        /// <summary>一連のシークエンス内で用いる遅延実行間隔</summary>
        private int delayFrame => Random.Range(0, 30);
        /// <summary>１回目の割り当て以降のチェック間隔</summary>
        private int checkInterval => Random.Range(300, 600);

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

            SetIsNeedTeleport(true);
        }

        /// <summary>
        /// 部屋の割り当てを行う
        /// </summary>
        public void AssignRoom()
        {
            RequestSerialization();
            Networking.SetOwner(_localPlayer, gameObject);

            bool isAssined = false;

            for (int i = 0; i < RoomUserArray.Length; i++)
            {
                // 空き部屋があったら自分のIdを登録する
                if (RoomUserArray[i] == -1)
                {
                    RoomUserArray[i] = localPlayerId;
                    isAssined = true;
                    break;
                }
            }
            // 空き部屋がなければランダムに割り当て
            if (!isAssined) RoomUserArray[Random.Range(0, RoomUserArray.Length)] = localPlayerId;

            // 部屋の割り当てが成功したかチェックする
            checkedCount = 0;
            SendCustomEventDelayedFrames(nameof(CheckAssign), delayFrame);
        }

        /// <summary>
        /// 自分が適切に割り当てられているかチェック
        /// </summary>
        public void CheckAssign()
        {
            RequestSerialization();
            for (int i = 0; i < RoomUserArray.Length; i++)
            {
                // 自分のIdがあったらチェックカウントを増やす
                if (RoomUserArray[i] == localPlayerId)
                {
                    checkedCount++;

                    // 初回は指定回数確認する　2回目以降は１回だけ
                    if (checkedCount >= MAX_CHECK_COUNT || !isFirstTime)
                    {
                        isFirstTime = false;
                        // ちゃんと割り当てられていたらそこをスポーンにする
                        // 現在と異なる部屋の場合のみ処理を行う
                        if (currentRoom != i) SetSpawnPosition(i);
                        currentRoom = i;

                        // チェックのループに入れる
                        SendCustomEventDelayedFrames(nameof(CheckAssign), checkInterval);

                        return;
                    }
                    // チェック回数足りなかったらランダム時間後に再度チェック
                    SendCustomEventDelayedFrames(nameof(CheckAssign), delayFrame);
                    return;
                }

            }
            // 割り当てが無かったら部屋の割り当てをランダムフレーム後に遅延実行
            SendCustomEventDelayedFrames(nameof(AssignRoom), delayFrame);
        }

        /// <summary>
        /// スポーン地点の指定と、周囲の可視化オブジェクトの移動
        /// 必要であればテレポートも実行
        /// </summary>
        /// <param name="roomNumber"></param>
        private void SetSpawnPosition(int roomNumber)
        {
            // ワールドスポーンを移動
            VRCWorldSpawn.position = SpawnPoints[roomNumber].position;
            VRCWorldSpawn.rotation = SpawnPoints[roomNumber].rotation;

            // EnableObjectを移動して可視化
            EnableObject.transform.position = SpawnPoints[roomNumber].position;
            EnableObject.SetActive(true);

            // テレポートが必要なら実行
            if (isNeedTeleport) _localPlayer.TeleportTo(VRCWorldSpawn.position, VRCWorldSpawn.rotation);
        }

        /// <summary>
        /// スポーン地点変更時にテレポートが必要かどうかを設定する
        /// </summary>
        /// <param name="value"></param>
        public void SetIsNeedTeleport(bool value)
        {
            isNeedTeleport = value;
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (!player.isLocal) return;

            //リスポーンしたらテレポート対象となる
            SetIsNeedTeleport(true);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            // 必要なかったら終わり
            if (!Utilities.IsValid(player)) return;
            if (player.isLocal)
            {
                // 自分なら即時に割り当て実行
                AssignRoom();
            }
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
