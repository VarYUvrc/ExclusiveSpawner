# TL;DR
- プレイヤーを排他的に別のスポーン地点に割り振る処理
- ローカルでプレイヤーのいる部屋だけがActiveになる

# How to Install

## 任意のUnity Projectに直接導入する場合
```shell
$ cd <your Unity project path>/Asset/<any folder>
$ git clone https://github.com/VarYUvrc/ExclusiveSpawner.git
```
- Sample Sceneも利用可能
- git環境が必要
## スクリプトだけ手動で導入する場合
Unity ProjectのSceneを開き、Projectタブで右クリック>Create>U# Script>ExclusiveSpawnerという名前で一度作成し、リポジトリのScript/ExclusiveSpawner.csの中身をコピペ>適当なObjectにAdd Componet>Udon Behavior>ExclusiveSpawnerを選択
- git環境が不要