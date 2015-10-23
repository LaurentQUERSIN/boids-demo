using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ShipStateManager
{
    private readonly List<ShipEvent> _history = new List<ShipEvent>();

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private long _lastTimeStamp;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private long _targetTimeStamp;
    private ushort _team;

    private bool _shouldRender = false;
    private bool _shouldRemove = false;
    private bool _shouldComputeTarget = false;

    private readonly List<UsedSkillMsg> _skillsLaunched = new List<UsedSkillMsg>();

    public GameObject Obj { get; set; }

    private void InsertInHistory(ShipEvent shipEvent)
    {
        lock (this._history)
        {
            if (!this._history.Any() || shipEvent.TimeStamp >= this._history.Last().TimeStamp)
            {
                this._history.Add(shipEvent);
            }
            else
            {
                this._history.Insert(this._history.FindIndex(ev => ev.TimeStamp > shipEvent.TimeStamp), shipEvent);
            }
        }
    }

    public void RegisterCreation(ShipCreatedDto shipDto)
    {
        var shipCreatedEvent = new CreatedEvent
        {
            X = shipDto.x,
            Y = shipDto.y,
            Rotation = shipDto.rot,
            Team = shipDto.team,
            TimeStamp = shipDto.timestamp
        };

        this.InsertInHistory(shipCreatedEvent);
    }
    public void RegisterPosition(float x, float y, float rot, long timeStamp)
    {
        var shipPositionEvent = new UpdatePositionEvent
        {
            X = x,
            Y = y,
            Rotation = rot,
            TimeStamp = timeStamp
        };
        this.InsertInHistory(shipPositionEvent);
    }

    public void RegisterRemoved(long timeStamp)
    {
        this.InsertInHistory(new RemovedEvent { TimeStamp = timeStamp });
    }

    public void RegisterStatusChanged(ShipStatus shipStatus, long timeStamp)
    {
        this.InsertInHistory(new StatusEvent { NewStatus = shipStatus, TimeStamp = timeStamp });
    }

    public void RegisterSkill(UsedSkillMsg skill, long timeStamp)
    {
        this.InsertInHistory(new SkillUsedEvent { Skill = skill, TimeStamp = timeStamp });
    }

    public ShipRenderingInfos GetRenderingInfos(long timeStamp)
    {
        this._skillsLaunched.Clear();
        lock (this._history)
        {
            while (this._history.Any() && this._history[0].TimeStamp <= timeStamp)
            {
                this._history[0].ApplyEvent(this);
                this._history.RemoveAt(0);
            }

            if (this._shouldComputeTarget)
            {
                var nextPosition = this._history.OfType<UpdatePositionEvent>().FirstOrDefault();

                if (nextPosition != null)
                {
                    this._targetPosition = new Vector3(nextPosition.X, nextPosition.Y);
                    this._targetRotation = Quaternion.Euler(0, 0, nextPosition.Rotation * (180 / (float)Math.PI));
                    this._targetTimeStamp = nextPosition.TimeStamp;
                }
                else
                {
                    this._targetPosition = this._lastPosition;
                    this._targetRotation = this._lastRotation;
                    this._targetTimeStamp = long.MaxValue;
                }
            }

        }

        ShipRenderingInfos result;
        if (this._shouldRender)
        {
            result = new ShipRenderingInfos();
            result.Position = this.ComputePosition(timeStamp);
            result.Rotation = this.ComputeRotation(timeStamp);
            result.Team = this._team;
            result.Kind = this.Obj == null ? ShipRenderingInfos.RenderingKind.AddShip : ShipRenderingInfos.RenderingKind.DrawShip;
        }
        else
        {
            result = new ShipRenderingInfos
            {
                Kind = this._shouldRemove ? ShipRenderingInfos.RenderingKind.RemoveShip : ShipRenderingInfos.RenderingKind.HideShipe
            };
        }

        result.Skills = this._skillsLaunched.ToList();

        if (result.Skills.Count > 0)
            Debug.Log("y a des tirs !");

        return result;
    }

    private float ComputeT(long clock)
    {
        return (float)(clock - this._lastTimeStamp) / (this._targetTimeStamp - this._lastTimeStamp);
    }

    private Vector3 ComputePosition(long clock)
    {
        return Vector3.Lerp(this._lastPosition, this._targetPosition, this.ComputeT(clock));
    }

    private Quaternion ComputeRotation(long clock)
    {
        return Quaternion.Slerp(this._lastRotation, this._targetRotation, this.ComputeT(clock));
    }
    private abstract class ShipEvent
    {
        public long TimeStamp { get; set; }

        public abstract void ApplyEvent(ShipStateManager state);
    }

    private class UpdatePositionEvent : ShipEvent
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }

        public override void ApplyEvent(ShipStateManager state)
        {
            state._lastPosition = new Vector3(this.X, this.Y);
            state._lastRotation = Quaternion.Euler(0, 0, this.Rotation * (180 / (float)Math.PI));
            state._lastTimeStamp = this.TimeStamp;
            state._shouldComputeTarget = true;
        }
    }

    private class CreatedEvent : UpdatePositionEvent
    {
        public ushort Team { get; set; }

        public override void ApplyEvent(ShipStateManager state)
        {
            base.ApplyEvent(state);
            state._team = this.Team;
            state._shouldRender = true;
        }
    }

    private class StatusEvent : ShipEvent
    {
        public ShipStatus NewStatus { get; set; }

        public override void ApplyEvent(ShipStateManager state)
        {
            switch (NewStatus)
            {
                case ShipStatus.InGame:
                    state._shouldRender = true;
                    break;
                default:
                    state._shouldRender = false;
                    break;
            }
        }
    }

    private class RemovedEvent : ShipEvent
    {
        public override void ApplyEvent(ShipStateManager state)
        {
            state._shouldRender = false;
            state._shouldRemove = true;
        }
    }

    private class SkillUsedEvent : ShipEvent
    {
        public override void ApplyEvent(ShipStateManager state)
        {
            state._skillsLaunched.Add(this.Skill);
        }

        public UsedSkillMsg Skill { get; set; }
    }
}
