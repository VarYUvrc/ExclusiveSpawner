
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Varyu.ExclusiveSpawner
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExclusiveSpawner : UdonSharpBehaviour
    {
        [SerializeField] private Transform VRCWorldSpawn;
        [SerializeField] private Transform[] SpawnPoints;
        [SerializeField] private GameObject[] EnableObjects;

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            // 必要なかったら終わり
            if (!Utilities.IsValid(player)) return;
            if (!player.isLocal) return;

            // プレイヤーIDを取得
            int playerID = Networking.LocalPlayer.playerId;

            // プレイヤーIDが0または17以上の場合、IDを8から16の範囲にランダムに設定
            if (playerID < 1 || playerID > 17)
            {
                playerID = Random.Range(8, 16);
            }

            // スポーンポイントIndexをプレイヤーIDから計算（0-16の範囲にするために-1する）
            int spawnPointIndex = playerID - 1;

            // スポーンポイント変更
            VRCWorldSpawn.position = SpawnPoints[spawnPointIndex].position;
            VRCWorldSpawn.rotation = SpawnPoints[spawnPointIndex].rotation;

            // そこにワープ
            player.TeleportTo(VRCWorldSpawn.position, VRCWorldSpawn.rotation);

            // EnableObjectsの処理
            for (int i = 0; i < EnableObjects.Length; i++)
            {
                EnableObjects[i].SetActive(i == spawnPointIndex);
            }
        }
    }
}