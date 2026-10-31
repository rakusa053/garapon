using System.Collections.Generic;
using UnityEngine;

namespace Garapon.Game
{
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> Active = new List<Interactable>();

        public abstract string Prompt { get; }
        public abstract void Interact();

        protected virtual void OnEnable()
        {
            Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        public static Interactable FindNearest(Vector3 position, float reach)
        {
            Interactable nearest = null;
            var nearestDistance = reach * reach;
            foreach (var candidate in Active)
            {
                var offset = candidate.transform.position - position;
                offset.y = 0;
                if (offset.sqrMagnitude < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = offset.sqrMagnitude;
                }
            }

            return nearest;
        }
    }
}
