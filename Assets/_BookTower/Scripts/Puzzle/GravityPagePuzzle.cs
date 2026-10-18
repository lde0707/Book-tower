using System.Collections.Generic;
using UnityEngine;
using BookTower.Core;
using BookTower.Rooms;
using BookTower.UI;

namespace BookTower.Puzzles
{
    // Gravity acts on the marked archive cargo, not the player. Rails constrain depth.
    public sealed partial class GravityPagePuzzle : PuzzleBase
    {
        public Rigidbody[] cargo;
        public Transform[] destinations;
        public Transform arrow;
        public Transform exitLeaf;
        public Transform completedPage;
        public Vector3 completedPageHinge;
        public string lesson;
        public string[] hints;
        public float acceleration = 12f, tolerance = .22f;
        public float railSpacing=1f;
        public Vector3[] railOrigins;
        public int[] verificationRoute;
        public int Direction { get; private set; }
        public int Moves { get; private set; }
        public bool Moving { get; private set; }
        public bool Ready => initialized && !Moving;
        public int Matched { get; private set; }
        Vector3[] initial, pausedVelocity, priorFixedPositions;
        readonly Stack<(Vector3[] positions,int direction)> history = new();
        Room room;
        bool initialized, suspended;
        float stillTime, moveTime;
        static readonly Vector3[] axes = { Vector3.down, Vector3.left, Vector3.up, Vector3.right };

        void Start()
        {
            if (!ValidateConfiguration()) { enabled = false; return; }
            GravityPageRailPhysics.Configure(this);
            room = GetComponentInParent<Room>();
            initial = Snapshot(); pausedVelocity = new Vector3[cargo.Length];
            priorFixedPositions = new Vector3[cargo.Length];
            foreach (var body in cargo) { body.useGravity = false; body.isKinematic = true; body.interpolation=RigidbodyInterpolation.None; }
            InitializePresentation();
            initialized = true; UpdateArrow();
        }
        Vector3[] Snapshot()
        {
            var positions = new Vector3[cargo.Length];
            for (int i = 0; i < cargo.Length; i++) positions[i] = cargo[i].position;
            return positions;
        }
        bool CanOperate => isActiveAndEnabled && initialized && (!room || room.IsVisible) &&
            (!GameFlow.Instance || GameFlow.Instance.AcceptsPlayerInput) && State != PuzzleState.Solved;
        public bool SetDirection(int direction)
        {
            if (!CanOperate || direction < 0 || direction > 3) return false;
            if(Moving){PaperUI.Notify("기록이 멈추면 다음 방향을 고를 수 있어. 되돌리기는 지금도 가능해.");return false;}
            Activate(); history.Push((Snapshot(),Direction)); Direction = direction; Moves++;
            Moving = true; stillTime = moveTime = 0; suspended = false;
            for (int i = 0; i < cargo.Length; i++) priorFixedPositions[i] = cargo[i].position;
            foreach (var body in cargo)
            {
                body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation|
                    (direction%2==0?RigidbodyConstraints.FreezePositionX:RigidbodyConstraints.FreezePositionY);
                body.isKinematic = false; body.linearVelocity = Vector3.zero;body.interpolation=RigidbodyInterpolation.Interpolate;body.WakeUp();
            }
            UpdateArrow();
            BookTower.Audio.SfxDirector.PlayMechanism("paper_fold", transform.position, .32f);
            return true;
        }
        public bool Undo()
        {
            if (!CanOperate || history.Count == 0) return false;
            var previous=history.Pop();Restore(previous.positions);Direction=previous.direction;UpdateArrow();
            Moves = Mathf.Max(0, Moves - 1); return true;
        }
        public bool ResetCargo()
        {
            if (!CanOperate) return false;
            history.Clear(); Restore(initial); Moves = 0; return true;
        }
        void Restore(Vector3[] positions)
        {
            for (int i = 0; i < cargo.Length; i++)
            {
                Teleport(cargo[i],positions[i]);
            }
            Moving = suspended = false; stillTime = moveTime = 0;
            Direction = 0; UpdateArrow(); Physics.SyncTransforms();
        }
        static void Teleport(Rigidbody body,Vector3 position)
        {
            // Discard interpolation history as well as the physics pose. Otherwise a
            // same-frame Undo + new direction resurrects the previous flight pose.
            body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;
            body.transform.position=position;body.position=position;
        }
        void FixedUpdate()
        {
            if (!initialized || !Moving) return;
            bool pause = (room && !room.IsVisible) || (GameFlow.Instance && !GameFlow.Instance.AcceptsPlayerInput);
            if (pause)
            {
                if (!suspended) for (int i=0;i<cargo.Length;i++)
                { pausedVelocity[i]=cargo[i].linearVelocity;cargo[i].isKinematic=true; }
                suspended=true;return;
            }
            if(suspended) for(int i=0;i<cargo.Length;i++)
            {cargo[i].isKinematic=false;cargo[i].linearVelocity=pausedVelocity[i];}
            suspended=false;moveTime+=Time.fixedDeltaTime;
            bool still=true;
            for(int i=0;i<cargo.Length;i++)
            {
                var body=cargo[i];
                // Contact solvers can retain small velocities under a shared load.
                // Settling is measured from actual displacement, not residual velocity.
                still &= (body.position-priorFixedPositions[i]).sqrMagnitude < railSpacing*railSpacing*.000016f;
                priorFixedPositions[i]=body.position;
                body.AddForce(transform.TransformDirection(axes[Direction])*acceleration,ForceMode.Acceleration);
            }
            stillTime = still && moveTime > .4f ? stillTime + Time.fixedDeltaTime : 0;
            if(stillTime>.3f)
            {
                Moving=false;
                for(int i=0;i<cargo.Length;i++)
                {
                    var body=cargo[i];var settledPosition=body.position;Teleport(body,settledPosition);
                    // Centring detents compensate tiny contact gaps at a rail junction.
                    // Only millimetre-scale corrections are allowed, never a skipped cell.
                    if(railOrigins!=null&&i<railOrigins.Length)
                    {
                        var p=body.transform.localPosition;var offset=(p-railOrigins[i])/railSpacing;
                        var centred=railOrigins[i]+new Vector3(Mathf.Round(offset.x),Mathf.Round(offset.y),0)*railSpacing;
                        if(Vector3.Distance(p,centred)<railSpacing*.035f)Teleport(body,body.transform.parent.TransformPoint(centred));
                    }
                }
            }
            // Recovery is always available; no timeout grants a false solve.
        }
        protected override bool CheckSolved()
        {
            if(!initialized||Moving||Moves==0)return false;
            Matched=0;
            for(int i=0;i<cargo.Length;i++)
                if((cargo[i].position-destinations[i].position).sqrMagnitude<tolerance*tolerance)Matched++;
            return Matched==cargo.Length;
        }
        protected override void Update()
        {
            if(!initialized)return;
            if (!room || room.IsVisible) base.Update();
            if (State == PuzzleState.Solved) UpdateSolvedPresentation();
        }
        bool ValidateConfiguration()
        {
            bool valid = cargo != null && cargo.Length > 0 && destinations != null &&
                destinations.Length == cargo.Length && railSpacing > 0 && tolerance > 0 && acceleration > 0;
            if (valid) for (int i = 0; i < cargo.Length; i++) valid &= cargo[i] && destinations[i];
            if (!valid) Debug.LogError($"[GravityPage] Invalid cargo/destination or motion configuration: {name}", this);
            return valid;
        }
        void UpdateArrow(){if(arrow)arrow.localRotation=Quaternion.Euler(0,0,-Direction*90f);}
        protected override void OnActivated(){PaperUI.Notify(lesson);}
        protected override void OnHintEscalated(int level)
        {if(hints!=null&&hints.Length>0)PaperUI.Notify(hints[Mathf.Min(level-1,hints.Length-1)]);}
        protected override void OnSolved()
        {
            PaperUI.Instance?.Objective("Goal.Gravity."+(room?room.RoomId:PuzzleId),"돌아올 자리를 남겼다 · 모아와 다음 장으로 가기");
            PaperUI.Notify("기록이 제자리로 돌아왔다. 다음 장으로 가자.");
            BookTower.Audio.SfxDirector.PlayMechanism("binding_door_open",transform.position,.6f);
        }
    }
}
