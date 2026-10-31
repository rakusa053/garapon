using System.Collections.Generic;
using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    public sealed class TrashField : MonoBehaviour
    {
        const float BasePickupRadius = 1.5f;

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

        void Start()
        {
            for (var i = 0; i < pieceCount; i++)
            {
                pieces.Add(CreatePiece());
            }
        }

        void Update()
        {
            var session = GameSession.Instance;
            var radius = BasePickupRadius + (float)session.Upgrades.GetValue(UpgradeStat.PickupRadius);
            foreach (var piece in pieces)
            {
                var offset = piece.Transform.position - player.position;
                offset.y = 0;
                if (offset.sqrMagnitude <= radius * radius)
                {
                    session.Economy.CollectTrash(piece.Definition);
                    Scatter(piece);
                }
            }
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
            piece.Transform.localPosition = new Vector3(
                Random.Range(-size.x / 2, size.x / 2),
                0.15f,
                Random.Range(-size.y / 2, size.y / 2));
            piece.Transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var hue = Mathf.Abs(piece.Definition.Id.GetHashCode()) % 100 / 100f;
            colors.Apply(piece.Transform.GetComponent<Renderer>(), Color.HSVToRGB(hue, 0.55f, 0.9f));
        }
    }
}
