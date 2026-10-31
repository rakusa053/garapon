using System;
using System.Collections;
using System.Collections.Generic;
using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class GaraponMachine : Interactable
    {
        const float SpinSpeed = 180f;
        const float SpinAcceleration = 360f;
        const float ShutterOpenAngle = 22f;
        const float StrayDistance = 8f;
        const float DrawTimeout = 10f;
        const float BallLifetimeInTray = 2.5f;
        const float BallRadius = 0.15f;
        const int BallCount = 30;

        static readonly Color IdleBallColor = new Color(0.55f, 0.55f, 0.58f);

        [SerializeField] Rigidbody drum;
        [SerializeField] Collider shutter;
        [SerializeField] Transform exitSensor;
        [SerializeField] Material ballMaterial;

        readonly List<GaraponBall> balls = new List<GaraponBall>();
        readonly MaterialPropertyBlockCache colors = new MaterialPropertyBlockCache();

        float angle;
        float speed;
        float drawElapsed;
        bool drawing;

        public bool IsBusy => drawing || speed > 0f;
        public int BallsAwarded { get; private set; }
        public int BallsDiscarded { get; private set; }
        public int BallsEscaped { get; private set; }
        public int BallsInMachine => balls.Count;
        public float TotalSpinDegrees { get; private set; }

        public event Action<PrizeTierDef> PrizeAwarded;

        public override string Prompt => IsBusy
            ? "ガラポン回転中"
            : $"ガラポンを回す(抽選券 {GameSession.Instance.State.Tickets:N0}枚)";

        void Start()
        {
            for (var i = 0; i < BallCount; i++)
            {
                balls.Add(CreateBall(i));
            }
        }

        void FixedUpdate()
        {
            speed = Mathf.MoveTowards(speed, drawing ? SpinSpeed : 0f, SpinAcceleration * Time.fixedDeltaTime);
            angle = Mathf.Repeat(angle + speed * Time.fixedDeltaTime, 360f);
            TotalSpinDegrees += speed * Time.fixedDeltaTime;
            drum.MoveRotation(Quaternion.Euler(0f, 0f, angle));

            var holeFacesDown = Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) < ShutterOpenAngle;
            shutter.enabled = !(drawing && holeFacesDown);

            if (drawing)
            {
                drawElapsed += Time.fixedDeltaTime;
                if (drawElapsed >= DrawTimeout)
                {
                    ForceBallOut();
                }
            }

            RecoverStrayBalls();
        }

        void RecoverStrayBalls()
        {
            foreach (var ball in balls)
            {
                if ((ball.Body.position - drum.position).sqrMagnitude > StrayDistance * StrayDistance)
                {
                    BallsEscaped++;
                    ResetBall(ball);
                }
            }
        }

        public override void Interact()
        {
            var session = GameSession.Instance;
            if (IsBusy)
            {
                return;
            }

            if (!session.Lottery.CanDraw)
            {
                session.Post("抽選券がありません。スーパーで買い物をしましょう");
                return;
            }

            drawing = true;
            drawElapsed = 0f;
        }

        public void OnBallExited(GaraponBall ball)
        {
            var session = GameSession.Instance;
            var tier = drawing ? session.Lottery.Draw() : null;
            if (tier == null)
            {
                BallsDiscarded++;
                ResetBall(ball);
                return;
            }

            drawing = false;
            BallsAwarded++;
            colors.Apply(ball.BallRenderer, PrizeColors.ToColor(tier.Color));
            StartCoroutine(RecycleAfterDelay(ball));
            session.HandlePrize(tier);
            PrizeAwarded?.Invoke(tier);
        }

        // A safety net so a draw cannot hang if no ball happens to roll through the opening.
        void ForceBallOut()
        {
            foreach (var ball in balls)
            {
                if (IsInsideDrum(ball))
                {
                    ball.Body.position = exitSensor.position + Vector3.up * BallRadius;
                    ball.Body.linearVelocity = Vector3.down;
                    drawElapsed = 0f;
                    return;
                }
            }
        }

        bool IsInsideDrum(GaraponBall ball)
        {
            return (ball.Body.position - drum.position).sqrMagnitude < 1f;
        }

        IEnumerator RecycleAfterDelay(GaraponBall ball)
        {
            yield return new WaitForSeconds(BallLifetimeInTray);
            ResetBall(ball);
        }

        GaraponBall CreateBall(int index)
        {
            var ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = $"Ball {index}";
            ballObject.transform.localScale = Vector3.one * BallRadius * 2f;
            ballObject.GetComponent<Renderer>().sharedMaterial = ballMaterial;

            var body = ballObject.AddComponent<Rigidbody>();
            body.mass = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var ball = ballObject.AddComponent<GaraponBall>();
            ResetBall(ball, index);
            return ball;
        }

        void ResetBall(GaraponBall ball)
        {
            ResetBall(ball, UnityEngine.Random.Range(0, BallCount));
        }

        void ResetBall(GaraponBall ball, int slot)
        {
            var offset = new Vector3(
                (slot % 4 - 1.5f) * 0.32f,
                (slot / 4 % 4 - 1.5f) * 0.32f,
                (slot / 16 - 0.5f) * 0.32f);
            ball.Body.linearVelocity = Vector3.zero;
            ball.Body.angularVelocity = Vector3.zero;
            ball.Body.position = drum.position + offset;
            ball.transform.position = ball.Body.position;
            colors.Apply(ball.BallRenderer, IdleBallColor);
        }
    }
}
