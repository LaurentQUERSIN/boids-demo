using UnityEngine;
using System.Collections;
using Models;
using Stormancer;
using System.Threading.Tasks;
using Stormancer.Core;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using Stormancer.Diagnostics;
using Stormancer.Authentication;

public class GameEngine : MonoBehaviour
{
    public long Delay = 1000;
    public Color EvenTeamColor;
    public Color OddTeamColor;

    private Scene _scene;
    private Client _client;

    private readonly ConcurrentDictionary<ushort, ShipStateManager> _gameObjects = new ConcurrentDictionary<ushort, ShipStateManager>();

    public GameObject ShipPrefab;

    // Use this for initialization
    void Start()
    {
        UniRx.MainThreadDispatcher.Initialize();
        var loadingCanvas = GameObject.Find("LoadingCanvas");
        var config = Stormancer.ClientConfiguration.ForAccount("d9590543-56c3-c94a-f7bf-c394b26deb15", "boids-test");
        config.Logger = DebugLogger.Instance;
        this._client = new Stormancer.Client(config);


        Debug.Log("calling GetPublicScene");
        _client.Authenticator().LoginAsViewer().ContinueWith(
            task =>
            {
                if (!task.IsFaulted)
                {
                    Debug.Log("test1");
                    var MatchMakerScene = task.Result;
                    var matchmaker = new MatchmakerClient(MatchMakerScene);
                    matchmaker.Connect().Then(s =>
                    {
                        matchmaker.FindMatch().Then(result =>
                        {
                            _client.GetScene(result.Token).Then(scene =>
                            {
                                _scene = scene;
                                scene.AddRoute("position.update", OnPositionUpdate);
                                scene.AddRoute<ushort>("ship.remove", OnShipRemoved);
                                scene.AddRoute("ship.usedSkill", OnUsedSkill);
                                scene.AddRoute<StatusChangedMsg>("ship.statusChanged", OnStatusChanged);
                                scene.AddRoute<ShipCreatedDto[]>("ship.add", OnShipAdded);
                                scene.AddRoute("ship.pv", OnShipPv);

                                _scene.Connect().Then(() =>
                                {
                                    Debug.Log("Call dispatcher to hide UI.");
                                    MainThread.Post(() =>
                                    {
                                        Debug.Log("Hiding UI.");
                                        loadingCanvas.SetActive(false);//Hide loading ui.


                                    });

                                })
                                .ContinueWith(t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        Debug.LogException(t.Exception.InnerException);
                                    }
                                });
                            });
                        });
                    });
                }
                else
                // task.IsFaulted
                {
                    Debug.Log("task faulted");
                }
            });
    }

    private void OnStatusChanged(StatusChangedMsg statusChanged)
    {
        ShipStateManager ship;
        if (!_gameObjects.TryGetValue(statusChanged.shipId, out ship))
        {
            ship = new ShipStateManager();
            ship = _gameObjects.AddOrUpdate(statusChanged.shipId, ship, (_, val) => val);
        }

        ship.RegisterStatusChanged(statusChanged.status, this._client.Clock - this._client.LastPing / 2);
    }

    private void OnUsedSkill(Packet<IScenePeer> packet)
    {
        while (packet.Stream.Position < packet.Stream.Length)
        {
            var skill = packet.ReadObject<UsedSkillMsg>();

            ShipStateManager originShip;
            if (_gameObjects.TryGetValue(skill.origin, out originShip))
            {
                originShip.RegisterSkill(skill, this._client.Clock - this._client.LastPing / 2);
            }
        }
    }

    private void OnShipPv(Packet<IScenePeer> obj)
    {
        // Do nothing, we don't display ship's HP
    }



    // Update is called once per frame
    void Update()
    {
        var deleteArray = new List<ushort>();
        foreach (var kvp in _gameObjects)
        {
            var key = kvp.Key;

            var ship = kvp.Value;
            var renderingInfos = ship.GetRenderingInfos(this._client.Clock - Delay);

            switch (renderingInfos.Kind)
            {
                case ShipRenderingInfos.RenderingKind.RemoveShip:
                    deleteArray.Add(key);
                    break;
                case ShipRenderingInfos.RenderingKind.AddShip:
                    var obj = (GameObject)Instantiate(ShipPrefab, renderingInfos.Position, renderingInfos.Rotation);

                    var color = renderingInfos.Team % 2 == 0 ? this.EvenTeamColor : this.OddTeamColor;
                    obj.GetComponent<BoidBehavior>().Color = color;

                    ship.Obj = obj;
                    break;
                case ShipRenderingInfos.RenderingKind.DrawShip:
                    if (ship.Obj != null)
                    {
                        ship.Obj.GetComponent<Renderer>().enabled = true;
                        ship.Obj.transform.position = renderingInfos.Position;
                        ship.Obj.transform.rotation = renderingInfos.Rotation;
                    }
                    break;
                case ShipRenderingInfos.RenderingKind.HideShipe:
                    if (ship.Obj != null)
                    {
                        ship.Obj.GetComponent<Renderer>().enabled = false;
                        ship.Obj.GetComponent<BoidBehavior>().Explode(true);
                    }
                    break;
            }

            foreach (var skill in renderingInfos.Skills)
            {
                if (skill.weaponId == "canon")
                {
                    if (ship.Obj != null)
                    {
                        ShipStateManager targetShip;
                        if (this._gameObjects.TryGetValue(skill.shipId, out targetShip) && targetShip.Obj != null)
                        {
                            ship.Obj.GetComponentInChildren<Canon>().Shoot(targetShip.Obj.transform);

                            if (skill.success)
                            {
                                targetShip.Obj.GetComponent<BoidBehavior>().Explode(false);
                            }
                        }
                    }
                }
            }
        }

        foreach (var id in deleteArray)
        {
            ShipStateManager ship;
            _gameObjects.TryRemove(id, out ship);
            if (ship.Obj != null)
            {
                GameObject.Destroy(ship.Obj);
            }
        }
    }

    //private void OnShipAdded(Packet<IScenePeer> obj)
    //{

    //    var shipInfos = obj.ReadObject<ShipCreatedDto>();
    //    var rot = Quaternion.Euler(0, 0, shipInfos.rot * (180 / (float)Math.PI));
    //    var ship = new Ship { Id = shipInfos.id, LastRot = rot, TargetRot = rot, Target = new Vector3(shipInfos.x, shipInfos.y), Last = new Vector3(shipInfos.x, shipInfos.y), TargetDate = DateTime.UtcNow };
    //    UnityEngine.Debug.Log(string.Format("Ship {0} added ", shipInfos.id));

    //    _gameObjects.Add(ship.Id, ship);

    //}

    private void OnShipAdded(ShipCreatedDto[] shipDtos)
    {
        foreach (var shipDto in shipDtos)
        {
            ShipStateManager ship;
            if (!_gameObjects.TryGetValue(shipDto.id, out ship))
            {
                ship = new ShipStateManager();
                ship = _gameObjects.AddOrUpdate(shipDto.id, ship, (_, val) => val);
            }

            ship.RegisterCreation(shipDto);
        }
    }

    private void OnShipRemoved(ushort id)
    {
        UnityEngine.Debug.Log(string.Format("Ship {0} removed ", id));
        ShipStateManager ship;
        if (_gameObjects.TryGetValue(id, out ship))
        {
            ship.RegisterRemoved(this._client.Clock - this._client.LastPing / 2);
        }
    }

    private void OnPositionUpdate(Packet<IScenePeer> packet)
    {

        using (var reader = new BinaryReader(packet.Stream))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var id = reader.ReadUInt16();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var rot = reader.ReadSingle();
                var time = reader.ReadInt64();


                ShipStateManager ship;
                if (!_gameObjects.TryGetValue(id, out ship))
                {
                    ship = new ShipStateManager();
                    ship = _gameObjects.AddOrUpdate(id, ship, (_, val) => val);
                }

                ship.RegisterPosition(x, y, rot, time);
            }
        }
    }

    public void OnDestroy()
    {
        if (this._scene != null)
        {
            if (this._scene.Connected)
            {
                this._scene.Disconnect();
            }
            this._scene = null;
        }
        if (this._client != null)
        {
            this._client.Dispose();
            this._client = null;
        }
        UnityEngine.Debug.Log("GameEngine destroyed.");
    }

    private class Ship
    {
        public ushort Id { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Last { get; set; }

        public Vector3 Current { get; set; }

        public Quaternion LastRot { get; set; }
        public Quaternion CurrentRot { get; set; }

        public Quaternion TargetRot { get; set; }


        public DateTime TargetDate { get; set; }
        public DateTime LastDate { get; set; }

        public GameObject Obj { get; set; }
        public bool Deletable { get; internal set; }

        public ushort? Team { get; set; }

        public ShipStatus Status { get; set; }
    }
}

