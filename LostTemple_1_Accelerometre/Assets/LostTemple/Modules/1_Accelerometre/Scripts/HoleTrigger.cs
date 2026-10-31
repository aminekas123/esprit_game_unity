using UnityEngine;

namespace LostTemple
{
    /// <summary>Trou du plateau (salle 1) : si la boule passe dessus, elle tombe et revient au départ.</summary>
    [RequireComponent(typeof(Collider))]
    public class HoleTrigger : MonoBehaviour
    {
        public TiltBallPuzzle puzzle;

        void OnTriggerEnter(Collider other)
        {
            if (puzzle != null && other.attachedRigidbody == puzzle.ball) puzzle.OnBallFellInHole();
        }
    }
}
