using System.Collections.Generic;
using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class TrashField : MonoBehaviour
    {
        const float BasePickupRadius = 1.5f;
        const float LuckySpawnMin = 60f;
        const float LuckySpawnMax = 180f;
        const float LuckyLifetime = 15f;
        const float LuckyBlinkTime = 4f;

        static readonly Color LuckyColor = new Color(1f, 0.82f, 0.15f);

        sealed class Piece
        {
            public Transform Transform;
            public TrashDef Definition;
        }

        [SerializeField] Transform player;
        [SerializeField] Material material;
        [SerializeField] Vector2 size = new Vector2(30f, 24f);
        [SerializeField] int pieceCount = 40;

        readonly List<Piece> pieces = new List<Piece>();
        readonly MaterialPropertyBlockCache colors = new MaterialPropertyBlockCache();
        readonly MaterialPropertyBlockCache luckyColors = new MaterialPropertyBlockCache();

        Transform lucky;
        Renderer luckyRenderer;
        float luckyTimer;
        float luckyAge;

        void Start()
        {
            for (var i = 0; i < pieceCount; i++)
            {
                pieces.Add(CreatePiece());
            }

            CreateLucky();
            luckyTimer = NextLuckyDelay();
        }

        void Update()
        {
            var session = GameSession.Instance;
            var radius = BasePickupRadius + (float)session.Upgrades.GetValue(UpgradeStat.PickupRadius);
            foreach (var piece in pieces)
            {
                if (IsWithin(piece.Transform, radius))
                {
                    session.Economy.CollectTrash(piece.Definition, session.Combo.Register(Time.time));
                    Scatter(piece);
                }
            }

            UpdateLucky(session, radius);
        }

        bool IsWithin(Transform target, float radius)
        {
            var offset = target.position - player.position;
            offset.y = 0;
            return offset.sqrMagnitude <= radius * radius;
        }

        // A golden can appears every minute or few. Grab it before it disappears for a random bonus.
        void UpdateLucky(GameSession session, float radius)
        {
            if (!lucky.gameObject.activeSelf)
            {
                luckyTimer -= Time.deltaTime;
                if (luckyTimer <= 0f)
                {
                    ShowLucky();
                }

                return;
            }

            luckyAge += Time.deltaTime;
            lucky.localRotation = Quaternion.Euler(0f, luckyAge * 180f, 0f);
            var bob = 0.5f + Mathf.Sin(luckyAge * 4f) * 0.15f;
            var position = lucky.localPosition;
            lucky.localPosition = new Vector3(position.x, bob, position.z);

            // Blink during the last seconds as a warning.
            var remaining = LuckyLifetime - luckyAge;
            luckyRenderer.enabled = remaining > LuckyBlinkTime || Mathf.Repeat(luckyAge * 6f, 1f) < 0.6f;

            if (IsWithin(lucky, radius))
            {
                session.CollectLucky();
                HideLucky();
            }
            else if (remaining <= 0f)
            {
                HideLucky();
            }
        }

        // The travel-charm perk makes lucky trash turn up more often.
        float NextLuckyDelay()
        {
            var session = GameSession.Instance;
            return Random.Range(LuckySpawnMin, LuckySpawnMax) / (float)Prestige.LuckyRate(session.Data, session.State);
        }

        void CreateLucky()
        {
            var luckyObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            luckyObject.name = "LuckyTrash";
            Destroy(luckyObject.GetComponent<Collider>());
            lucky = luckyObject.transform;
            lucky.SetParent(transform, false);
            lucky.localScale = new Vector3(0.45f, 0.35f, 0.45f);
            luckyRenderer = luckyObject.GetComponent<Renderer>();
            luckyRenderer.sharedMaterial = material;
            luckyColors.Apply(luckyRenderer, LuckyColor);
            luckyObject.SetActive(false);
        }

        void ShowLucky()
        {
            luckyAge = 0f;
            lucky.localPosition = RandomPoint(0.5f);
            luckyRenderer.enabled = true;
            lucky.gameObject.SetActive(true);
        }

        void HideLucky()
        {
            lucky.gameObject.SetActive(false);
            luckyTimer = NextLuckyDelay();
        }

        Vector3 RandomPoint(float height)
        {
            return new Vector3(Random.Range(-size.x / 2, size.x / 2), height, Random.Range(-size.y / 2, size.y / 2));
        }

        Piece CreatePiece()
        {
            var piece = new Piece { Transform = GameObject.CreatePrimitive(PrimitiveType.Cube).transform };
            Destroy(piece.Transform.GetComponent<Collider>());
            piece.Transform.SetParent(transform, false);
            piece.Transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            piece.Transform.GetComponent<Renderer>().sharedMaterial = material;
            Scatter(piece);
            return piece;
        }

        void Scatter(Piece piece)
        {
            var trash = GameSession.Instance.Data.Trash;
            piece.Definition = trash[Random.Range(0, trash.Count)];
            piece.Transform.localPosition = RandomPoint(0.15f);
            piece.Transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var hue = Mathf.Abs(piece.Definition.Id.GetHashCode()) % 100 / 100f;
            colors.Apply(piece.Transform.GetComponent<Renderer>(), Color.HSVToRGB(hue, 0.55f, 0.9f));
        }
    }
}
